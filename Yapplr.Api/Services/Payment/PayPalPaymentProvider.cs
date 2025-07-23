using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using Yapplr.Api.Configuration;
using Yapplr.Api.DTOs.Payment;
using Yapplr.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Yapplr.Api.Services.Payment;

public class PayPalPaymentProvider : IPaymentProvider
{
    private readonly PayPalConfiguration _config;
    private readonly ILogger<PayPalPaymentProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly YapplrDbContext _context;
    private readonly bool _isEnabled;
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);

    public string ProviderName => "PayPal";

    public PayPalPaymentProvider(
        IOptionsMonitor<PaymentProvidersConfiguration> config,
        ILogger<PayPalPaymentProvider> logger,
        HttpClient httpClient,
        YapplrDbContext context)
    {
        _config = config.CurrentValue.PayPal;
        _logger = logger;
        _httpClient = httpClient;
        _context = context;
        _isEnabled = _config.Enabled && !string.IsNullOrEmpty(_config.ClientId) && !string.IsNullOrEmpty(_config.ClientSecret);

        // Configure HttpClient
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        _httpClient.BaseAddress = new Uri(_config.Environment == "live" 
            ? "https://api-m.paypal.com" 
            : "https://api-m.sandbox.paypal.com");
    }

    public Task<bool> IsAvailableAsync()
    {
        return Task.FromResult(_isEnabled);
    }

    public async Task<CreateSubscriptionResult> CreateSubscriptionAsync(CreateSubscriptionRequest request)
    {
        try
        {
            if (!_isEnabled)
            {
                return new CreateSubscriptionResult 
                { 
                    Success = false, 
                    ErrorMessage = "PayPal provider is not enabled or configured" 
                };
            }

            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                return new CreateSubscriptionResult 
                { 
                    Success = false, 
                    ErrorMessage = "Failed to obtain PayPal access token" 
                };
            }

            // Get subscription tier details
            var subscriptionTier = await _context.SubscriptionTiers
                .FirstOrDefaultAsync(t => t.Name == request.PaymentProvider); // This needs to be fixed - should get tier by ID

            if (subscriptionTier == null)
            {
                return new CreateSubscriptionResult 
                { 
                    Success = false, 
                    ErrorMessage = "Subscription tier not found" 
                };
            }

            // Create PayPal subscription payload
            var subscriptionPayload = new
            {
                plan_id = await GetOrCreatePlanAsync(subscriptionTier, accessToken),
                start_time = DateTime.UtcNow.AddMinutes(1).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                quantity = "1",
                shipping_amount = new { currency_code = subscriptionTier.Currency, value = "0.00" },
                subscriber = new
                {
                    payment_source = request.PaymentMethodId != null ? new
                    {
                        paypal = new
                        {
                            experience_context = new
                            {
                                brand_name = "Yapplr",
                                locale = "en-US",
                                shipping_preference = "NO_SHIPPING",
                                user_action = "SUBSCRIBE_NOW",
                                payment_method_preference = "IMMEDIATE_PAYMENT_REQUIRED",
                                return_url = request.ReturnUrl ?? "https://yapplr.com/payment/success",
                                cancel_url = request.CancelUrl ?? "https://yapplr.com/payment/cancel"
                            }
                        }
                    } : null
                },
                application_context = new
                {
                    brand_name = "Yapplr",
                    locale = "en-US",
                    shipping_preference = "NO_SHIPPING",
                    user_action = "SUBSCRIBE_NOW",
                    payment_method_preference = "IMMEDIATE_PAYMENT_REQUIRED",
                    return_url = request.ReturnUrl ?? "https://yapplr.com/payment/success",
                    cancel_url = request.CancelUrl ?? "https://yapplr.com/payment/cancel"
                }
            };

            var json = JsonSerializer.Serialize(subscriptionPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

            var response = await _httpClient.PostAsync("/v1/billing/subscriptions", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var subscriptionResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                var subscriptionId = subscriptionResponse.GetProperty("id").GetString();
                var status = subscriptionResponse.GetProperty("status").GetString();
                
                // Check if approval is required
                var requiresAction = status == "APPROVAL_PENDING";
                string? authorizationUrl = null;

                if (requiresAction && subscriptionResponse.TryGetProperty("links", out var links))
                {
                    foreach (var link in links.EnumerateArray())
                    {
                        if (link.GetProperty("rel").GetString() == "approve")
                        {
                            authorizationUrl = link.GetProperty("href").GetString();
                            break;
                        }
                    }
                }

                return new CreateSubscriptionResult
                {
                    Success = true,
                    ExternalSubscriptionId = subscriptionId,
                    Status = status,
                    RequiresAction = requiresAction,
                    AuthorizationUrl = authorizationUrl,
                    NextBillingDate = DateTime.UtcNow.AddMonths(subscriptionTier.BillingCycleMonths)
                };
            }
            else
            {
                _logger.LogError("PayPal subscription creation failed: {StatusCode} - {Response}", 
                    response.StatusCode, responseContent);
                
                return new CreateSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = $"PayPal API error: {response.StatusCode}",
                    ErrorCode = response.StatusCode.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayPal subscription");
            return new CreateSubscriptionResult
            {
                Success = false,
                ErrorMessage = "Internal error creating subscription",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        await _tokenSemaphore.WaitAsync();
        try
        {
            // Check if we have a valid token
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
            {
                return _accessToken;
            }

            // Request new token
            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
            var content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authString}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var response = await _httpClient.PostAsync("/v1/oauth2/token", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                _accessToken = tokenResponse.GetProperty("access_token").GetString();
                var expiresIn = tokenResponse.GetProperty("expires_in").GetInt32();
                _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // Subtract 60 seconds for safety

                return _accessToken;
            }
            else
            {
                _logger.LogError("Failed to obtain PayPal access token: {StatusCode} - {Response}", 
                    response.StatusCode, responseContent);
                return null;
            }
        }
        finally
        {
            _tokenSemaphore.Release();
        }
    }

    private async Task<string?> GetOrCreatePlanAsync(Models.SubscriptionTier tier, string accessToken)
    {
        // For now, we'll create a plan on-demand. In production, you might want to pre-create plans
        // and store their IDs in the database
        
        var planPayload = new
        {
            product_id = await GetOrCreateProductAsync(tier, accessToken),
            name = $"Yapplr {tier.Name} Plan",
            description = tier.Description,
            status = "ACTIVE",
            billing_cycles = new[]
            {
                new
                {
                    frequency = new
                    {
                        interval_unit = "MONTH",
                        interval_count = tier.BillingCycleMonths
                    },
                    tenure_type = "REGULAR",
                    sequence = 1,
                    total_cycles = 0, // 0 means infinite
                    pricing_scheme = new
                    {
                        fixed_price = new
                        {
                            value = tier.Price.ToString("F2"),
                            currency_code = tier.Currency
                        }
                    }
                }
            },
            payment_preferences = new
            {
                auto_bill_outstanding = true,
                setup_fee_failure_action = "CONTINUE",
                payment_failure_threshold = 3
            }
        };

        var json = JsonSerializer.Serialize(planPayload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

        var response = await _httpClient.PostAsync("/v1/billing/plans", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var planResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            return planResponse.GetProperty("id").GetString();
        }
        else
        {
            _logger.LogError("Failed to create PayPal plan: {StatusCode} - {Response}", 
                response.StatusCode, responseContent);
            throw new Exception($"Failed to create PayPal plan: {response.StatusCode}");
        }
    }

    private async Task<string?> GetOrCreateProductAsync(Models.SubscriptionTier tier, string accessToken)
    {
        var productPayload = new
        {
            name = $"Yapplr {tier.Name}",
            description = tier.Description,
            type = "SERVICE",
            category = "SOFTWARE"
        };

        var json = JsonSerializer.Serialize(productPayload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

        var response = await _httpClient.PostAsync("/v1/catalogs/products", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            var productResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
            return productResponse.GetProperty("id").GetString();
        }
        else
        {
            _logger.LogError("Failed to create PayPal product: {StatusCode} - {Response}", 
                response.StatusCode, responseContent);
            throw new Exception($"Failed to create PayPal product: {response.StatusCode}");
        }
    }

    public async Task<CancelSubscriptionResult> CancelSubscriptionAsync(CancelSubscriptionRequest request)
    {
        try
        {
            if (!_isEnabled)
            {
                return new CancelSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = "PayPal provider is not enabled or configured"
                };
            }

            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                return new CancelSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to obtain PayPal access token"
                };
            }

            // PayPal cancel subscription payload
            var cancelPayload = new
            {
                reason = request.Reason
            };

            var json = JsonSerializer.Serialize(cancelPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            // Note: This needs the actual subscription ID - would need to be passed in the request
            var response = await _httpClient.PostAsync($"/v1/billing/subscriptions/{request.Reason}/cancel", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return new CancelSubscriptionResult
                {
                    Success = true,
                    CancelledAt = DateTime.UtcNow,
                    EffectiveEndDate = request.CancelImmediately ? DateTime.UtcNow : null,
                    Status = "CANCELLED"
                };
            }
            else
            {
                _logger.LogError("PayPal subscription cancellation failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                return new CancelSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = $"PayPal API error: {response.StatusCode}",
                    ErrorCode = response.StatusCode.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling PayPal subscription");
            return new CancelSubscriptionResult
            {
                Success = false,
                ErrorMessage = "Internal error cancelling subscription",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    public async Task<GetSubscriptionResult> GetSubscriptionAsync(string externalSubscriptionId)
    {
        try
        {
            if (!_isEnabled)
            {
                return new GetSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = "PayPal provider is not enabled or configured"
                };
            }

            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                return new GetSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to obtain PayPal access token"
                };
            }

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var response = await _httpClient.GetAsync($"/v1/billing/subscriptions/{externalSubscriptionId}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var subscriptionResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                var status = subscriptionResponse.GetProperty("status").GetString();
                DateTime? nextBillingDate = null;
                DateTime? cancelledAt = null;

                if (subscriptionResponse.TryGetProperty("billing_info", out var billingInfo) &&
                    billingInfo.TryGetProperty("next_billing_time", out var nextBilling))
                {
                    if (DateTime.TryParse(nextBilling.GetString(), out var nextBillingParsed))
                    {
                        nextBillingDate = nextBillingParsed;
                    }
                }

                return new GetSubscriptionResult
                {
                    Success = true,
                    ExternalSubscriptionId = externalSubscriptionId,
                    Status = status,
                    NextBillingDate = nextBillingDate,
                    CancelledAt = cancelledAt
                };
            }
            else
            {
                _logger.LogError("PayPal get subscription failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                return new GetSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = $"PayPal API error: {response.StatusCode}",
                    ErrorCode = response.StatusCode.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PayPal subscription");
            return new GetSubscriptionResult
            {
                Success = false,
                ErrorMessage = "Internal error getting subscription",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    public async Task<UpdateSubscriptionResult> UpdateSubscriptionAsync(UpdateSubscriptionRequest request)
    {
        try
        {
            if (!_isEnabled)
            {
                return new UpdateSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = "PayPal provider is not enabled or configured"
                };
            }

            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                return new UpdateSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to obtain PayPal access token"
                };
            }

            // For PayPal, subscription updates are limited - mainly plan changes
            // Payment method updates require canceling and recreating subscription
            if (request.NewSubscriptionTierId.HasValue)
            {
                var newTier = await _context.SubscriptionTiers
                    .FirstOrDefaultAsync(t => t.Id == request.NewSubscriptionTierId.Value);

                if (newTier == null)
                {
                    return new UpdateSubscriptionResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid subscription tier"
                    };
                }

                // PayPal doesn't support direct plan changes - would need to cancel and recreate
                // For now, return not supported
                return new UpdateSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = "PayPal subscription plan changes require canceling and recreating the subscription",
                    ErrorCode = "NOT_SUPPORTED"
                };
            }

            return new UpdateSubscriptionResult
            {
                Success = false,
                ErrorMessage = "No supported update operations specified",
                ErrorCode = "INVALID_REQUEST"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating PayPal subscription");
            return new UpdateSubscriptionResult
            {
                Success = false,
                ErrorMessage = "Internal error updating subscription",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    public async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request)
    {
        try
        {
            if (!_isEnabled)
            {
                return new ProcessPaymentResult
                {
                    Success = false,
                    ErrorMessage = "PayPal provider is not enabled or configured"
                };
            }

            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                return new ProcessPaymentResult
                {
                    Success = false,
                    ErrorMessage = "Failed to obtain PayPal access token"
                };
            }

            // Create PayPal payment payload for one-time payment
            var paymentPayload = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        amount = new
                        {
                            currency_code = request.Currency,
                            value = request.Amount.ToString("F2")
                        },
                        description = request.Description
                    }
                },
                payment_source = new
                {
                    paypal = new
                    {
                        experience_context = new
                        {
                            brand_name = "Yapplr",
                            locale = "en-US",
                            shipping_preference = "NO_SHIPPING",
                            user_action = "PAY_NOW",
                            return_url = "https://yapplr.com/payment/success",
                            cancel_url = "https://yapplr.com/payment/cancel"
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(paymentPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            _httpClient.DefaultRequestHeaders.Add("PayPal-Request-Id", Guid.NewGuid().ToString());

            var response = await _httpClient.PostAsync("/v2/checkout/orders", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var paymentResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                var orderId = paymentResponse.GetProperty("id").GetString();
                var status = paymentResponse.GetProperty("status").GetString();

                // Check if approval is required
                var requiresAction = status == "CREATED";
                string? authorizationUrl = null;

                if (requiresAction && paymentResponse.TryGetProperty("links", out var links))
                {
                    foreach (var link in links.EnumerateArray())
                    {
                        if (link.GetProperty("rel").GetString() == "approve")
                        {
                            authorizationUrl = link.GetProperty("href").GetString();
                            break;
                        }
                    }
                }

                return new ProcessPaymentResult
                {
                    Success = true,
                    ExternalTransactionId = orderId,
                    Status = status,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    ProcessedAt = DateTime.UtcNow,
                    RequiresAction = requiresAction,
                    AuthorizationUrl = authorizationUrl
                };
            }
            else
            {
                _logger.LogError("PayPal payment processing failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                return new ProcessPaymentResult
                {
                    Success = false,
                    ErrorMessage = $"PayPal API error: {response.StatusCode}",
                    ErrorCode = response.StatusCode.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayPal payment");
            return new ProcessPaymentResult
            {
                Success = false,
                ErrorMessage = "Internal error processing payment",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    public async Task<RefundPaymentResult> RefundPaymentAsync(RefundPaymentRequest request)
    {
        try
        {
            if (!_isEnabled)
            {
                return new RefundPaymentResult
                {
                    Success = false,
                    ErrorMessage = "PayPal provider is not enabled or configured"
                };
            }

            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                return new RefundPaymentResult
                {
                    Success = false,
                    ErrorMessage = "Failed to obtain PayPal access token"
                };
            }

            // Note: This is a simplified implementation
            // In practice, you'd need the capture ID from the original transaction
            // For now, return not implemented for PayPal refunds
            return new RefundPaymentResult
            {
                Success = false,
                ErrorMessage = "PayPal refunds require capture ID from original transaction",
                ErrorCode = "NOT_IMPLEMENTED"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayPal refund");
            return new RefundPaymentResult
            {
                Success = false,
                ErrorMessage = "Internal error processing refund",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    public async Task<CreatePaymentMethodResult> CreatePaymentMethodAsync(CreatePaymentMethodRequest request)
    {
        try
        {
            if (!_isEnabled)
            {
                return new CreatePaymentMethodResult
                {
                    Success = false,
                    ErrorMessage = "PayPal provider is not enabled or configured"
                };
            }

            // PayPal doesn't store payment methods in the same way as Stripe
            // PayPal users authenticate and approve payments through their PayPal account
            // For subscription billing, PayPal handles the payment method through the subscription agreement
            return new CreatePaymentMethodResult
            {
                Success = false,
                ErrorMessage = "PayPal payment methods are managed through PayPal's approval flow",
                ErrorCode = "NOT_SUPPORTED"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayPal payment method");
            return new CreatePaymentMethodResult
            {
                Success = false,
                ErrorMessage = "Internal error creating payment method",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    public async Task<DeletePaymentMethodResult> DeletePaymentMethodAsync(string externalPaymentMethodId)
    {
        try
        {
            if (!_isEnabled)
            {
                return new DeletePaymentMethodResult
                {
                    Success = false,
                    ErrorMessage = "PayPal provider is not enabled or configured"
                };
            }

            // PayPal doesn't support deleting payment methods through API
            // Users manage their payment methods through PayPal's interface
            return new DeletePaymentMethodResult
            {
                Success = false,
                ErrorMessage = "PayPal payment methods are managed through PayPal's interface",
                ErrorCode = "NOT_SUPPORTED"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting PayPal payment method");
            return new DeletePaymentMethodResult
            {
                Success = false,
                ErrorMessage = "Internal error deleting payment method",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    public async Task<GetPaymentMethodResult> GetPaymentMethodAsync(string externalPaymentMethodId)
    {
        try
        {
            if (!_isEnabled)
            {
                return new GetPaymentMethodResult
                {
                    Success = false,
                    ErrorMessage = "PayPal provider is not enabled or configured"
                };
            }

            // PayPal doesn't expose payment method details through API for security
            return new GetPaymentMethodResult
            {
                Success = false,
                ErrorMessage = "PayPal payment method details are not accessible through API",
                ErrorCode = "NOT_SUPPORTED"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PayPal payment method");
            return new GetPaymentMethodResult
            {
                Success = false,
                ErrorMessage = "Internal error getting payment method",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    public async Task<WebhookHandleResult> HandleWebhookAsync(WebhookRequest request)
    {
        try
        {
            if (!_isEnabled)
            {
                return new WebhookHandleResult
                {
                    Success = false,
                    ErrorMessage = "PayPal provider is not enabled or configured"
                };
            }

            var actionsPerformed = new List<string>();

            // Handle different PayPal webhook events
            switch (request.EventType)
            {
                case "BILLING.SUBSCRIPTION.CREATED":
                    actionsPerformed.Add("Subscription created event logged");
                    break;

                case "BILLING.SUBSCRIPTION.ACTIVATED":
                    actionsPerformed.Add("Subscription activated");
                    // Update subscription status in database
                    await UpdateSubscriptionStatusFromWebhook(request, "ACTIVE");
                    break;

                case "BILLING.SUBSCRIPTION.CANCELLED":
                    actionsPerformed.Add("Subscription cancelled");
                    await UpdateSubscriptionStatusFromWebhook(request, "CANCELLED");
                    break;

                case "BILLING.SUBSCRIPTION.SUSPENDED":
                    actionsPerformed.Add("Subscription suspended");
                    await UpdateSubscriptionStatusFromWebhook(request, "SUSPENDED");
                    break;

                case "BILLING.SUBSCRIPTION.EXPIRED":
                    actionsPerformed.Add("Subscription expired");
                    await UpdateSubscriptionStatusFromWebhook(request, "EXPIRED");
                    break;

                case "PAYMENT.SALE.COMPLETED":
                    actionsPerformed.Add("Payment completed");
                    await RecordPaymentFromWebhook(request, "COMPLETED");
                    break;

                case "PAYMENT.SALE.DENIED":
                    actionsPerformed.Add("Payment denied");
                    await RecordPaymentFromWebhook(request, "FAILED");
                    break;

                case "PAYMENT.SALE.REFUNDED":
                    actionsPerformed.Add("Payment refunded");
                    await RecordRefundFromWebhook(request);
                    break;

                default:
                    _logger.LogWarning("Unhandled PayPal webhook event type: {EventType}", request.EventType);
                    actionsPerformed.Add($"Unhandled event type: {request.EventType}");
                    break;
            }

            return new WebhookHandleResult
            {
                Success = true,
                EventType = request.EventType,
                EventId = request.EventId,
                Processed = true,
                ActionsPerformed = actionsPerformed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PayPal webhook");
            return new WebhookHandleResult
            {
                Success = false,
                ErrorMessage = "Internal error handling webhook",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    public async Task<bool> VerifyWebhookAsync(WebhookRequest request)
    {
        try
        {
            if (!_isEnabled || !_config.Webhooks.VerifySignature)
            {
                return true; // Skip verification if disabled
            }

            if (string.IsNullOrEmpty(_config.WebhookSecret))
            {
                _logger.LogWarning("PayPal webhook secret not configured, skipping verification");
                return true;
            }

            // PayPal webhook verification would require implementing their signature verification
            // This is a simplified implementation
            // In production, you'd verify the webhook signature using PayPal's verification API

            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogError("Failed to obtain access token for webhook verification");
                return false;
            }

            // For now, return true if we have the required configuration
            // In a full implementation, you'd call PayPal's webhook verification endpoint
            return !string.IsNullOrEmpty(request.Signature) && !string.IsNullOrEmpty(request.RawBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying PayPal webhook");
            return false;
        }
    }

    public async Task<string?> GetPaymentAuthorizationUrlAsync(PaymentAuthorizationRequest request)
    {
        try
        {
            if (!_isEnabled)
            {
                return null;
            }

            // For PayPal, the authorization URL is generated during subscription creation
            // This method would be used for standalone payment authorization
            var subscriptionRequest = new CreateSubscriptionRequest
            {
                PaymentProvider = request.SubscriptionTierId.ToString(),
                ReturnUrl = request.ReturnUrl,
                CancelUrl = request.CancelUrl,
                Metadata = request.Metadata
            };

            var result = await CreateSubscriptionAsync(subscriptionRequest);
            return result.Success ? result.AuthorizationUrl : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PayPal authorization URL");
            return null;
        }
    }

    public async Task<PaymentAuthorizationResult> HandlePaymentAuthorizationAsync(PaymentAuthorizationCallbackRequest request)
    {
        try
        {
            if (!_isEnabled)
            {
                return new PaymentAuthorizationResult
                {
                    Success = false,
                    ErrorMessage = "PayPal provider is not enabled or configured"
                };
            }

            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
            {
                return new PaymentAuthorizationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to obtain PayPal access token"
                };
            }

            // Extract subscription ID from callback parameters
            if (!request.QueryParameters.TryGetValue("subscription_id", out var subscriptionId))
            {
                return new PaymentAuthorizationResult
                {
                    Success = false,
                    ErrorMessage = "Missing subscription_id in callback parameters"
                };
            }

            // Get subscription details to verify authorization
            var subscriptionResult = await GetSubscriptionAsync(subscriptionId);
            if (!subscriptionResult.Success)
            {
                return new PaymentAuthorizationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to verify subscription authorization"
                };
            }

            return new PaymentAuthorizationResult
            {
                Success = true,
                ExternalSubscriptionId = subscriptionId,
                Status = subscriptionResult.Status,
                AuthorizedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PayPal authorization callback");
            return new PaymentAuthorizationResult
            {
                Success = false,
                ErrorMessage = "Internal error handling authorization",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    private async Task UpdateSubscriptionStatusFromWebhook(WebhookRequest request, string status)
    {
        try
        {
            if (request.Data.TryGetValue("resource", out var resourceObj) &&
                resourceObj is JsonElement resource &&
                resource.TryGetProperty("id", out var subscriptionIdElement))
            {
                var subscriptionId = subscriptionIdElement.GetString();
                if (!string.IsNullOrEmpty(subscriptionId))
                {
                    var subscription = await _context.UserSubscriptions
                        .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == subscriptionId);

                    if (subscription != null)
                    {
                        // Convert PayPal status to our enum
                        subscription.Status = status switch
                        {
                            "ACTIVE" => Models.SubscriptionStatus.Active,
                            "CANCELLED" => Models.SubscriptionStatus.Cancelled,
                            "SUSPENDED" => Models.SubscriptionStatus.Suspended,
                            "EXPIRED" => Models.SubscriptionStatus.Expired,
                            _ => subscription.Status // Keep current status if unknown
                        };
                        subscription.UpdatedAt = DateTime.UtcNow;

                        if (status == "CANCELLED" || status == "EXPIRED")
                        {
                            subscription.EndDate = DateTime.UtcNow;
                        }

                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Updated subscription {SubscriptionId} status to {Status}",
                            subscriptionId, status);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription status from webhook");
        }
    }

    private async Task RecordPaymentFromWebhook(WebhookRequest request, string status)
    {
        try
        {
            if (request.Data.TryGetValue("resource", out var resourceObj) &&
                resourceObj is JsonElement resource)
            {
                var transactionId = resource.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                var amountStr = resource.TryGetProperty("amount", out var amountElement) &&
                               amountElement.TryGetProperty("total", out var totalElement) ?
                               totalElement.GetString() : null;
                var currency = resource.TryGetProperty("amount", out var amountElement2) &&
                              amountElement2.TryGetProperty("currency", out var currencyElement) ?
                              currencyElement.GetString() : "USD";

                if (!string.IsNullOrEmpty(transactionId) &&
                    !string.IsNullOrEmpty(amountStr) &&
                    decimal.TryParse(amountStr, out var amount))
                {
                    // Find the subscription associated with this payment
                    var subscription = await _context.UserSubscriptions
                        .FirstOrDefaultAsync(s => s.PaymentProvider == "PayPal" && s.Status == Models.SubscriptionStatus.Active);

                    if (subscription != null)
                    {
                        var transaction = new Models.PaymentTransaction
                        {
                            UserId = subscription.UserId,
                            SubscriptionTierId = subscription.SubscriptionTierId,
                            ExternalTransactionId = transactionId,
                            PaymentProvider = "PayPal",
                            Amount = amount,
                            Currency = currency,
                            Status = status == "COMPLETED" ? Models.PaymentStatus.Completed : Models.PaymentStatus.Failed,
                            Type = Models.PaymentType.Subscription,
                            ProcessedAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.PaymentTransactions.Add(transaction);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Recorded payment transaction {TransactionId} for user {UserId}",
                            transactionId, subscription.UserId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording payment from webhook");
        }
    }

    private async Task RecordRefundFromWebhook(WebhookRequest request)
    {
        try
        {
            if (request.Data.TryGetValue("resource", out var resourceObj) &&
                resourceObj is JsonElement resource)
            {
                var refundId = resource.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                var parentPayment = resource.TryGetProperty("parent_payment", out var parentElement) ?
                                   parentElement.GetString() : null;
                var amountStr = resource.TryGetProperty("amount", out var amountElement) &&
                               amountElement.TryGetProperty("total", out var totalElement) ?
                               totalElement.GetString() : null;
                var currency = resource.TryGetProperty("amount", out var amountElement2) &&
                              amountElement2.TryGetProperty("currency", out var currencyElement) ?
                              currencyElement.GetString() : "USD";

                if (!string.IsNullOrEmpty(refundId) &&
                    !string.IsNullOrEmpty(amountStr) &&
                    decimal.TryParse(amountStr, out var amount))
                {
                    // Find the original transaction
                    var originalTransaction = await _context.PaymentTransactions
                        .FirstOrDefaultAsync(t => t.ExternalTransactionId == parentPayment);

                    if (originalTransaction != null)
                    {
                        var refundTransaction = new Models.PaymentTransaction
                        {
                            UserId = originalTransaction.UserId,
                            SubscriptionTierId = originalTransaction.SubscriptionTierId,
                            ExternalTransactionId = refundId,
                            PaymentProvider = "PayPal",
                            Amount = -amount, // Negative amount for refund
                            Currency = currency,
                            Status = Models.PaymentStatus.Completed,
                            Type = Models.PaymentType.Refund,
                            ProcessedAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow,
                            Description = $"Refund for transaction {originalTransaction.ExternalTransactionId}"
                        };

                        _context.PaymentTransactions.Add(refundTransaction);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Recorded refund transaction {RefundId} for original transaction {OriginalId}",
                            refundId, parentPayment);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording refund from webhook");
        }
    }
}
