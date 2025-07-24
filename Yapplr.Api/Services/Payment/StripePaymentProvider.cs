using System.Text.Json;
using System.Text;
using Yapplr.Api.Configuration;
using Yapplr.Api.DTOs.Payment;
using Yapplr.Api.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Yapplr.Api.Services.Payment;

public class StripePaymentProvider : IPaymentProvider
{
    private readonly IDynamicPaymentConfigurationService _configService;
    private readonly ILogger<StripePaymentProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly YapplrDbContext _context;

    public string ProviderName => "Stripe";

    public StripePaymentProvider(
        IDynamicPaymentConfigurationService configService,
        ILogger<StripePaymentProvider> logger,
        HttpClient httpClient,
        YapplrDbContext context)
    {
        _configService = configService;
        _logger = logger;
        _httpClient = httpClient;
        _context = context;

        // Subscribe to configuration changes
        _configService.ConfigurationChanged += OnConfigurationChanged;
    }

    public async Task<bool> IsAvailableAsync()
    {
        var config = await _configService.GetStripeConfigurationAsync();
        return config != null && config.Enabled && !string.IsNullOrEmpty(config.SecretKey);
    }

    private async Task<StripeConfiguration?> GetConfigurationAsync()
    {
        return await _configService.GetStripeConfigurationAsync();
    }

    private async Task ConfigureHttpClientAsync()
    {
        var config = await GetConfigurationAsync();
        if (config != null)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
            _httpClient.BaseAddress = new Uri("https://api.stripe.com");
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.SecretKey}");
            _httpClient.DefaultRequestHeaders.Add("Stripe-Version", "2023-10-16");
        }
    }

    private async Task<bool> IsEnabledAsync()
    {
        return await IsAvailableAsync();
    }

    private void OnConfigurationChanged(object? sender, PaymentConfigurationChangedEventArgs e)
    {
        if (e.ProviderName == "Stripe")
        {
            _logger.LogInformation("Stripe configuration changed, will reconfigure on next request");
        }
    }

    public async Task<CreateSubscriptionResult> CreateSubscriptionAsync(CreateSubscriptionRequest request)
    {
        try
        {
            if (!await IsEnabledAsync())
            {
                return new CreateSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = "Stripe provider is not enabled or configured"
                };
            }

            // Configure HttpClient with current settings
            await ConfigureHttpClientAsync();

            // Get subscription tier details
            var subscriptionTier = await _context.SubscriptionTiers
                .FirstOrDefaultAsync(t => t.Name == request.PaymentProvider);

            if (subscriptionTier == null)
            {
                return new CreateSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = "Invalid subscription tier"
                };
            }

            // Create or get Stripe customer
            var customerId = await GetOrCreateCustomerAsync(request);
            if (string.IsNullOrEmpty(customerId))
            {
                return new CreateSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to create Stripe customer"
                };
            }

            // Create or get Stripe price
            var priceId = await GetOrCreatePriceAsync(subscriptionTier);
            if (string.IsNullOrEmpty(priceId))
            {
                return new CreateSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to create Stripe price"
                };
            }

            // Create subscription
            var subscriptionPayload = new
            {
                customer = customerId,
                items = new[]
                {
                    new { price = priceId }
                },
                payment_behavior = "default_incomplete",
                payment_settings = new
                {
                    save_default_payment_method = "on_subscription"
                },
                expand = new[] { "latest_invoice.payment_intent" },
                trial_period_days = request.StartTrial ? (await GetConfigurationAsync())?.Environment == "test" ? 7 : 14 : (int?)null
            };

            var json = JsonSerializer.Serialize(subscriptionPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v1/subscriptions", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var subscriptionResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                var subscriptionId = subscriptionResponse.GetProperty("id").GetString();
                var status = subscriptionResponse.GetProperty("status").GetString();
                
                // Check if payment intent requires action
                var requiresAction = false;
                string? authorizationUrl = null;

                if (subscriptionResponse.TryGetProperty("latest_invoice", out var invoice) &&
                    invoice.TryGetProperty("payment_intent", out var paymentIntent))
                {
                    var piStatus = paymentIntent.GetProperty("status").GetString();
                    requiresAction = piStatus == "requires_action" || piStatus == "requires_payment_method";
                    
                    if (requiresAction && paymentIntent.TryGetProperty("next_action", out var nextAction) &&
                        nextAction.TryGetProperty("redirect_to_url", out var redirectAction))
                    {
                        authorizationUrl = redirectAction.GetProperty("url").GetString();
                    }
                }

                DateTime? nextBillingDate = null;
                if (subscriptionResponse.TryGetProperty("current_period_end", out var periodEnd))
                {
                    nextBillingDate = DateTimeOffset.FromUnixTimeSeconds(periodEnd.GetInt64()).DateTime;
                }

                return new CreateSubscriptionResult
                {
                    Success = true,
                    ExternalSubscriptionId = subscriptionId,
                    Status = status,
                    RequiresAction = requiresAction,
                    AuthorizationUrl = authorizationUrl,
                    NextBillingDate = nextBillingDate
                };
            }
            else
            {
                _logger.LogError("Stripe subscription creation failed: {StatusCode} - {Response}", 
                    response.StatusCode, responseContent);
                
                return new CreateSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = $"Stripe API error: {response.StatusCode}",
                    ErrorCode = response.StatusCode.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe subscription");
            return new CreateSubscriptionResult
            {
                Success = false,
                ErrorMessage = "Internal error creating subscription",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    public async Task<CancelSubscriptionResult> CancelSubscriptionAsync(CancelSubscriptionRequest request)
    {
        try
        {
            if (!await IsEnabledAsync())
            {
                return new CancelSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = "Stripe provider is not enabled or configured"
                };
            }

            await ConfigureHttpClientAsync();

            // Stripe cancel subscription payload
            var cancelPayload = new
            {
                cancel_at_period_end = !request.CancelImmediately,
                cancellation_details = new
                {
                    comment = request.Reason
                }
            };

            var json = JsonSerializer.Serialize(cancelPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/v1/subscriptions/{request.Metadata?["subscription_id"]}", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var subscriptionResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var status = subscriptionResponse.GetProperty("status").GetString();
                
                DateTime? effectiveEndDate = null;
                if (request.CancelImmediately)
                {
                    effectiveEndDate = DateTime.UtcNow;
                }
                else if (subscriptionResponse.TryGetProperty("current_period_end", out var periodEnd))
                {
                    effectiveEndDate = DateTimeOffset.FromUnixTimeSeconds(periodEnd.GetInt64()).DateTime;
                }

                return new CancelSubscriptionResult
                {
                    Success = true,
                    CancelledAt = DateTime.UtcNow,
                    EffectiveEndDate = effectiveEndDate,
                    Status = status
                };
            }
            else
            {
                _logger.LogError("Stripe subscription cancellation failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                return new CancelSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = $"Stripe API error: {response.StatusCode}",
                    ErrorCode = response.StatusCode.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling Stripe subscription");
            return new CancelSubscriptionResult
            {
                Success = false,
                ErrorMessage = "Internal error cancelling subscription",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    private async Task<string?> GetOrCreateCustomerAsync(CreateSubscriptionRequest request)
    {
        try
        {
            // In a real implementation, you'd get user details from the request or context
            // For now, create a basic customer
            var customerPayload = new
            {
                email = "user@example.com", // This should come from the authenticated user
                metadata = request.Metadata ?? new Dictionary<string, string>()
            };

            var json = JsonSerializer.Serialize(customerPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v1/customers", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var customerResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                return customerResponse.GetProperty("id").GetString();
            }

            _logger.LogError("Failed to create Stripe customer: {StatusCode} - {Response}", 
                response.StatusCode, responseContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe customer");
            return null;
        }
    }

    private async Task<string?> GetOrCreatePriceAsync(Models.SubscriptionTier tier)
    {
        try
        {
            // Create or get product first
            var productId = await GetOrCreateProductAsync(tier);
            if (string.IsNullOrEmpty(productId))
            {
                return null;
            }

            // Create price
            var pricePayload = new
            {
                unit_amount = (int)(tier.Price * 100), // Stripe uses cents
                currency = tier.Currency.ToLower(),
                recurring = new
                {
                    interval = tier.BillingCycleMonths == 1 ? "month" : "year",
                    interval_count = tier.BillingCycleMonths == 12 ? 1 : tier.BillingCycleMonths
                },
                product = productId
            };

            var json = JsonSerializer.Serialize(pricePayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v1/prices", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var priceResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                return priceResponse.GetProperty("id").GetString();
            }

            _logger.LogError("Failed to create Stripe price: {StatusCode} - {Response}", 
                response.StatusCode, responseContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe price");
            return null;
        }
    }

    private async Task<string?> GetOrCreateProductAsync(Models.SubscriptionTier tier)
    {
        try
        {
            var productPayload = new
            {
                name = tier.Name,
                description = tier.Description,
                metadata = new Dictionary<string, string>
                {
                    ["tier_id"] = tier.Id.ToString()
                }
            };

            var json = JsonSerializer.Serialize(productPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v1/products", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var productResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                return productResponse.GetProperty("id").GetString();
            }

            _logger.LogError("Failed to create Stripe product: {StatusCode} - {Response}",
                response.StatusCode, responseContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe product");
            return null;
        }
    }

    public async Task<UpdateSubscriptionResult> UpdateSubscriptionAsync(UpdateSubscriptionRequest request)
    {
        try
        {
            if (!await IsEnabledAsync())
            {
                return new UpdateSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = "Stripe provider is not enabled or configured"
                };
            }

            await ConfigureHttpClientAsync();

            var subscriptionId = request.Metadata?["subscription_id"];
            if (string.IsNullOrEmpty(subscriptionId))
            {
                return new UpdateSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = "Missing subscription ID"
                };
            }

            var updatePayload = new Dictionary<string, object>();

            // Update subscription tier (price)
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

                var newPriceId = await GetOrCreatePriceAsync(newTier);
                if (string.IsNullOrEmpty(newPriceId))
                {
                    return new UpdateSubscriptionResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to create new price"
                    };
                }

                updatePayload["items"] = new[]
                {
                    new { price = newPriceId }
                };

                if (request.ProrateBilling)
                {
                    updatePayload["proration_behavior"] = "create_prorations";
                }
            }

            // Update payment method
            if (!string.IsNullOrEmpty(request.NewPaymentMethodId))
            {
                updatePayload["default_payment_method"] = request.NewPaymentMethodId;
            }

            if (!updatePayload.Any())
            {
                return new UpdateSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = "No update parameters provided"
                };
            }

            var json = JsonSerializer.Serialize(updatePayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"/v1/subscriptions/{subscriptionId}", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var subscriptionResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var status = subscriptionResponse.GetProperty("status").GetString();

                DateTime? nextBillingDate = null;
                if (subscriptionResponse.TryGetProperty("current_period_end", out var periodEnd))
                {
                    nextBillingDate = DateTimeOffset.FromUnixTimeSeconds(periodEnd.GetInt64()).DateTime;
                }

                return new UpdateSubscriptionResult
                {
                    Success = true,
                    ExternalSubscriptionId = subscriptionId,
                    Status = status,
                    NextBillingDate = nextBillingDate,
                    UpdatedAt = DateTime.UtcNow
                };
            }
            else
            {
                _logger.LogError("Stripe subscription update failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                return new UpdateSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = $"Stripe API error: {response.StatusCode}",
                    ErrorCode = response.StatusCode.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Stripe subscription");
            return new UpdateSubscriptionResult
            {
                Success = false,
                ErrorMessage = "Internal error updating subscription",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    public async Task<GetSubscriptionResult> GetSubscriptionAsync(string externalSubscriptionId)
    {
        try
        {
            if (!await IsEnabledAsync())
            {
                return new GetSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = "Stripe provider is not enabled or configured"
                };
            }

            await ConfigureHttpClientAsync();

            var response = await _httpClient.GetAsync($"/v1/subscriptions/{externalSubscriptionId}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var subscriptionResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                var status = subscriptionResponse.GetProperty("status").GetString();
                DateTime? nextBillingDate = null;
                DateTime? cancelledAt = null;

                if (subscriptionResponse.TryGetProperty("current_period_end", out var periodEnd))
                {
                    nextBillingDate = DateTimeOffset.FromUnixTimeSeconds(periodEnd.GetInt64()).DateTime;
                }

                if (subscriptionResponse.TryGetProperty("canceled_at", out var canceledAt) &&
                    !canceledAt.ValueKind.Equals(JsonValueKind.Null))
                {
                    cancelledAt = DateTimeOffset.FromUnixTimeSeconds(canceledAt.GetInt64()).DateTime;
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
                _logger.LogError("Stripe get subscription failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                return new GetSubscriptionResult
                {
                    Success = false,
                    ErrorMessage = $"Stripe API error: {response.StatusCode}",
                    ErrorCode = response.StatusCode.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Stripe subscription");
            return new GetSubscriptionResult
            {
                Success = false,
                ErrorMessage = "Internal error getting subscription",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    public async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request)
    {
        try
        {
            if (!await IsEnabledAsync())
            {
                return new ProcessPaymentResult
                {
                    Success = false,
                    ErrorMessage = "Stripe provider is not enabled or configured"
                };
            }

            await ConfigureHttpClientAsync();

            // Create payment intent for one-time payment
            var paymentIntentPayload = new
            {
                amount = (int)(request.Amount * 100), // Stripe uses cents
                currency = request.Currency.ToLower(),
                payment_method = request.PaymentMethodId,
                description = request.Description,
                confirm = true,
                return_url = "https://yapplr.com/payment/success"
            };

            var json = JsonSerializer.Serialize(paymentIntentPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v1/payment_intents", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var paymentResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                var paymentIntentId = paymentResponse.GetProperty("id").GetString();
                var status = paymentResponse.GetProperty("status").GetString();

                var requiresAction = status == "requires_action" || status == "requires_payment_method";
                string? authorizationUrl = null;

                if (requiresAction && paymentResponse.TryGetProperty("next_action", out var nextAction) &&
                    nextAction.TryGetProperty("redirect_to_url", out var redirectAction))
                {
                    authorizationUrl = redirectAction.GetProperty("url").GetString();
                }

                return new ProcessPaymentResult
                {
                    Success = true,
                    ExternalTransactionId = paymentIntentId,
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
                _logger.LogError("Stripe payment processing failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                return new ProcessPaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Stripe API error: {response.StatusCode}",
                    ErrorCode = response.StatusCode.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe payment");
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
            if (!await IsEnabledAsync())
            {
                return new RefundPaymentResult
                {
                    Success = false,
                    ErrorMessage = "Stripe provider is not enabled or configured"
                };
            }

            await ConfigureHttpClientAsync();

            var paymentIntentId = request.Metadata?["payment_intent_id"];
            if (string.IsNullOrEmpty(paymentIntentId))
            {
                return new RefundPaymentResult
                {
                    Success = false,
                    ErrorMessage = "Missing payment intent ID"
                };
            }

            var refundPayload = new Dictionary<string, object>
            {
                ["payment_intent"] = paymentIntentId
            };

            if (request.Amount.HasValue)
            {
                refundPayload["amount"] = (int)(request.Amount.Value * 100); // Stripe uses cents
            }

            if (!string.IsNullOrEmpty(request.Reason))
            {
                refundPayload["reason"] = request.Reason;
            }

            var json = JsonSerializer.Serialize(refundPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v1/refunds", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var refundResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                var refundId = refundResponse.GetProperty("id").GetString();
                var status = refundResponse.GetProperty("status").GetString();
                var amount = refundResponse.GetProperty("amount").GetInt32() / 100m; // Convert from cents
                var currency = refundResponse.GetProperty("currency").GetString()?.ToUpper();

                return new RefundPaymentResult
                {
                    Success = true,
                    ExternalRefundId = refundId,
                    ExternalTransactionId = paymentIntentId,
                    RefundedAmount = amount,
                    Currency = currency,
                    RefundedAt = DateTime.UtcNow,
                    Status = status
                };
            }
            else
            {
                _logger.LogError("Stripe refund failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                return new RefundPaymentResult
                {
                    Success = false,
                    ErrorMessage = $"Stripe API error: {response.StatusCode}",
                    ErrorCode = response.StatusCode.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe refund");
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
            if (!await IsEnabledAsync())
            {
                return new CreatePaymentMethodResult
                {
                    Success = false,
                    ErrorMessage = "Stripe provider is not enabled or configured"
                };
            }

            await ConfigureHttpClientAsync();

            var paymentMethodPayload = new Dictionary<string, object>
            {
                ["type"] = request.Type.ToLower()
            };

            // Handle card payment method
            if (request.Type.ToLower() == "card" && !string.IsNullOrEmpty(request.Token))
            {
                paymentMethodPayload["card"] = new { token = request.Token };
            }
            else if (request.Type.ToLower() == "card")
            {
                paymentMethodPayload["card"] = new
                {
                    number = request.CardNumber,
                    exp_month = request.ExpiryMonth,
                    exp_year = request.ExpiryYear,
                    cvc = request.Cvc
                };
            }

            var json = JsonSerializer.Serialize(paymentMethodPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v1/payment_methods", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var paymentMethodResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                var paymentMethodId = paymentMethodResponse.GetProperty("id").GetString();
                var type = paymentMethodResponse.GetProperty("type").GetString();

                string? brand = null;
                string? last4 = null;
                string? expiryMonth = null;
                string? expiryYear = null;

                if (paymentMethodResponse.TryGetProperty("card", out var card))
                {
                    brand = card.TryGetProperty("brand", out var brandElement) ? brandElement.GetString() : null;
                    last4 = card.TryGetProperty("last4", out var last4Element) ? last4Element.GetString() : null;
                    expiryMonth = card.TryGetProperty("exp_month", out var expMonthElement) ? expMonthElement.GetInt32().ToString() : null;
                    expiryYear = card.TryGetProperty("exp_year", out var expYearElement) ? expYearElement.GetInt32().ToString() : null;
                }

                return new CreatePaymentMethodResult
                {
                    Success = true,
                    ExternalPaymentMethodId = paymentMethodId,
                    Type = type,
                    Brand = brand,
                    Last4 = last4,
                    ExpiryMonth = expiryMonth,
                    ExpiryYear = expiryYear,
                    IsVerified = true // Stripe payment methods are verified upon creation
                };
            }
            else
            {
                _logger.LogError("Stripe payment method creation failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                return new CreatePaymentMethodResult
                {
                    Success = false,
                    ErrorMessage = $"Stripe API error: {response.StatusCode}",
                    ErrorCode = response.StatusCode.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe payment method");
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
            if (!await IsEnabledAsync())
            {
                return new DeletePaymentMethodResult
                {
                    Success = false,
                    ErrorMessage = "Stripe provider is not enabled or configured"
                };
            }

            await ConfigureHttpClientAsync();

            var response = await _httpClient.PostAsync($"/v1/payment_methods/{externalPaymentMethodId}/detach",
                new StringContent("{}", Encoding.UTF8, "application/json"));
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return new DeletePaymentMethodResult
                {
                    Success = true,
                    ExternalPaymentMethodId = externalPaymentMethodId,
                    DeletedAt = DateTime.UtcNow
                };
            }
            else
            {
                _logger.LogError("Stripe payment method deletion failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                return new DeletePaymentMethodResult
                {
                    Success = false,
                    ErrorMessage = $"Stripe API error: {response.StatusCode}",
                    ErrorCode = response.StatusCode.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Stripe payment method");
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
            if (!await IsEnabledAsync())
            {
                return new GetPaymentMethodResult
                {
                    Success = false,
                    ErrorMessage = "Stripe provider is not enabled or configured"
                };
            }

            await ConfigureHttpClientAsync();

            var response = await _httpClient.GetAsync($"/v1/payment_methods/{externalPaymentMethodId}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var paymentMethodResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                var type = paymentMethodResponse.GetProperty("type").GetString();

                string? brand = null;
                string? last4 = null;
                string? expiryMonth = null;
                string? expiryYear = null;

                if (paymentMethodResponse.TryGetProperty("card", out var card))
                {
                    brand = card.TryGetProperty("brand", out var brandElement) ? brandElement.GetString() : null;
                    last4 = card.TryGetProperty("last4", out var last4Element) ? last4Element.GetString() : null;
                    expiryMonth = card.TryGetProperty("exp_month", out var expMonthElement) ? expMonthElement.GetInt32().ToString() : null;
                    expiryYear = card.TryGetProperty("exp_year", out var expYearElement) ? expYearElement.GetInt32().ToString() : null;
                }

                return new GetPaymentMethodResult
                {
                    Success = true,
                    ExternalPaymentMethodId = externalPaymentMethodId,
                    Type = type,
                    Brand = brand,
                    Last4 = last4,
                    ExpiryMonth = expiryMonth,
                    ExpiryYear = expiryYear,
                    IsVerified = true
                };
            }
            else
            {
                _logger.LogError("Stripe get payment method failed: {StatusCode} - {Response}",
                    response.StatusCode, responseContent);

                return new GetPaymentMethodResult
                {
                    Success = false,
                    ErrorMessage = $"Stripe API error: {response.StatusCode}",
                    ErrorCode = response.StatusCode.ToString()
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Stripe payment method");
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
            if (!await IsEnabledAsync())
            {
                return new WebhookHandleResult
                {
                    Success = false,
                    ErrorMessage = "Stripe provider is not enabled or configured"
                };
            }

            var actionsPerformed = new List<string>();

            // Handle different Stripe webhook events
            switch (request.EventType)
            {
                case "customer.subscription.created":
                    actionsPerformed.Add("Subscription created event logged");
                    break;

                case "customer.subscription.updated":
                    actionsPerformed.Add("Subscription updated");
                    await UpdateSubscriptionStatusFromWebhook(request, "ACTIVE");
                    break;

                case "customer.subscription.deleted":
                    actionsPerformed.Add("Subscription cancelled");
                    await UpdateSubscriptionStatusFromWebhook(request, "CANCELLED");
                    break;

                case "invoice.payment_succeeded":
                    actionsPerformed.Add("Payment succeeded");
                    await RecordPaymentFromWebhook(request, "COMPLETED");
                    break;

                case "invoice.payment_failed":
                    actionsPerformed.Add("Payment failed");
                    await RecordPaymentFromWebhook(request, "FAILED");
                    break;

                case "charge.dispute.created":
                    actionsPerformed.Add("Chargeback created");
                    await RecordRefundFromWebhook(request);
                    break;

                default:
                    _logger.LogWarning("Unhandled Stripe webhook event type: {EventType}", request.EventType);
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
            _logger.LogError(ex, "Error handling Stripe webhook");
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
            var config = await GetConfigurationAsync();
            if (!await IsEnabledAsync() || config == null || string.IsNullOrEmpty(config.WebhookSecret))
            {
                return true; // Skip verification if disabled or not configured
            }

            // Stripe webhook signature verification
            var signature = request.Signature;
            var payload = request.RawBody;
            var secret = config.WebhookSecret;

            if (string.IsNullOrEmpty(signature) || string.IsNullOrEmpty(payload))
            {
                return false;
            }

            // Parse signature header
            var signatureParts = signature.Split(',');
            var timestamp = "";
            var v1Signature = "";

            foreach (var part in signatureParts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    switch (keyValue[0])
                    {
                        case "t":
                            timestamp = keyValue[1];
                            break;
                        case "v1":
                            v1Signature = keyValue[1];
                            break;
                    }
                }
            }

            if (string.IsNullOrEmpty(timestamp) || string.IsNullOrEmpty(v1Signature))
            {
                return false;
            }

            // Verify timestamp (within 5 minutes)
            if (long.TryParse(timestamp, out var webhookTimestamp))
            {
                var webhookTime = DateTimeOffset.FromUnixTimeSeconds(webhookTimestamp);
                if (Math.Abs((DateTimeOffset.UtcNow - webhookTime).TotalMinutes) > 5)
                {
                    return false;
                }
            }

            // Compute expected signature
            var signedPayload = $"{timestamp}.{payload}";
            var secretBytes = Encoding.UTF8.GetBytes(secret);
            var payloadBytes = Encoding.UTF8.GetBytes(signedPayload);

            using var hmac = new HMACSHA256(secretBytes);
            var computedHash = hmac.ComputeHash(payloadBytes);
            var computedSignature = Convert.ToHexString(computedHash).ToLower();

            return computedSignature == v1Signature;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Stripe webhook");
            return false;
        }
    }

    public async Task<string?> GetPaymentAuthorizationUrlAsync(PaymentAuthorizationRequest request)
    {
        try
        {
            if (!await IsEnabledAsync())
            {
                return null;
            }

            // For Stripe, authorization is typically handled through the frontend with Stripe.js
            // This method would be used for server-side redirect flows
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
            _logger.LogError(ex, "Error getting Stripe authorization URL");
            return null;
        }
    }

    public async Task<PaymentAuthorizationResult> HandlePaymentAuthorizationAsync(PaymentAuthorizationCallbackRequest request)
    {
        try
        {
            if (!await IsEnabledAsync())
            {
                return new PaymentAuthorizationResult
                {
                    Success = false,
                    ErrorMessage = "Stripe provider is not enabled or configured"
                };
            }

            // Extract payment intent or subscription ID from callback parameters
            if (request.QueryParameters.TryGetValue("payment_intent", out var paymentIntentId))
            {
                // Handle payment intent confirmation
                var response = await _httpClient.GetAsync($"/v1/payment_intents/{paymentIntentId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var paymentIntentResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    var status = paymentIntentResponse.GetProperty("status").GetString();

                    return new PaymentAuthorizationResult
                    {
                        Success = status == "succeeded",
                        Status = status,
                        AuthorizedAt = DateTime.UtcNow
                    };
                }
            }
            else if (request.QueryParameters.TryGetValue("subscription_id", out var subscriptionId))
            {
                // Handle subscription confirmation
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

            return new PaymentAuthorizationResult
            {
                Success = false,
                ErrorMessage = "Missing required callback parameters"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling Stripe authorization callback");
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
            if (request.Data.TryGetValue("object", out var objectData) &&
                objectData is JsonElement subscription &&
                subscription.TryGetProperty("id", out var subscriptionIdElement))
            {
                var subscriptionId = subscriptionIdElement.GetString();
                if (!string.IsNullOrEmpty(subscriptionId))
                {
                    var userSubscription = await _context.UserSubscriptions
                        .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == subscriptionId);

                    if (userSubscription != null)
                    {
                        // Convert Stripe status to our enum
                        userSubscription.Status = status switch
                        {
                            "active" => Models.SubscriptionStatus.Active,
                            "canceled" => Models.SubscriptionStatus.Cancelled,
                            "past_due" => Models.SubscriptionStatus.PastDue,
                            "unpaid" => Models.SubscriptionStatus.Suspended,
                            "incomplete" => Models.SubscriptionStatus.PastDue,
                            "incomplete_expired" => Models.SubscriptionStatus.Expired,
                            "trialing" => Models.SubscriptionStatus.Trial,
                            _ => userSubscription.Status // Keep current status if unknown
                        };
                        userSubscription.UpdatedAt = DateTime.UtcNow;

                        if (status == "canceled")
                        {
                            userSubscription.EndDate = DateTime.UtcNow;
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
            if (request.Data.TryGetValue("object", out var objectData) &&
                objectData is JsonElement invoice)
            {
                var chargeId = invoice.TryGetProperty("charge", out var chargeElement) ? chargeElement.GetString() : null;
                var subscriptionId = invoice.TryGetProperty("subscription", out var subElement) ? subElement.GetString() : null;
                var amountPaid = invoice.TryGetProperty("amount_paid", out var amountElement) ? amountElement.GetInt32() / 100m : 0m;
                var currency = invoice.TryGetProperty("currency", out var currencyElement) ? currencyElement.GetString()?.ToUpper() : "USD";

                if (!string.IsNullOrEmpty(chargeId) && !string.IsNullOrEmpty(subscriptionId))
                {
                    var subscription = await _context.UserSubscriptions
                        .FirstOrDefaultAsync(s => s.ExternalSubscriptionId == subscriptionId);

                    if (subscription != null)
                    {
                        var transaction = new Models.PaymentTransaction
                        {
                            UserId = subscription.UserId,
                            SubscriptionTierId = subscription.SubscriptionTierId,
                            ExternalTransactionId = chargeId,
                            PaymentProvider = "Stripe",
                            Amount = amountPaid,
                            Currency = currency,
                            Status = status == "COMPLETED" ? Models.PaymentStatus.Completed : Models.PaymentStatus.Failed,
                            Type = Models.PaymentType.Subscription,
                            ProcessedAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.PaymentTransactions.Add(transaction);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Recorded payment transaction {TransactionId} for user {UserId}",
                            chargeId, subscription.UserId);
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
            if (request.Data.TryGetValue("object", out var objectData) &&
                objectData is JsonElement dispute)
            {
                var chargeId = dispute.TryGetProperty("charge", out var chargeElement) ? chargeElement.GetString() : null;
                var amount = dispute.TryGetProperty("amount", out var amountElement) ? amountElement.GetInt32() / 100m : 0m;
                var currency = dispute.TryGetProperty("currency", out var currencyElement) ? currencyElement.GetString()?.ToUpper() : "USD";
                var disputeId = dispute.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;

                if (!string.IsNullOrEmpty(chargeId) && !string.IsNullOrEmpty(disputeId))
                {
                    var originalTransaction = await _context.PaymentTransactions
                        .FirstOrDefaultAsync(t => t.ExternalTransactionId == chargeId);

                    if (originalTransaction != null)
                    {
                        var refundTransaction = new Models.PaymentTransaction
                        {
                            UserId = originalTransaction.UserId,
                            SubscriptionTierId = originalTransaction.SubscriptionTierId,
                            ExternalTransactionId = disputeId,
                            PaymentProvider = "Stripe",
                            Amount = -amount, // Negative amount for dispute/chargeback
                            Currency = currency,
                            Status = Models.PaymentStatus.Completed,
                            Type = Models.PaymentType.Refund,
                            ProcessedAt = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow,
                            Description = $"Chargeback for transaction {originalTransaction.ExternalTransactionId}"
                        };

                        _context.PaymentTransactions.Add(refundTransaction);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Recorded chargeback transaction {DisputeId} for original transaction {OriginalId}",
                            disputeId, chargeId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording chargeback from webhook");
        }
    }
}
