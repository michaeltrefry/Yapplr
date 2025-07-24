# Payment Gateway Configuration Guide

This guide covers the complete setup and configuration of PayPal and Stripe payment providers for the Yapplr payment system, including the tiered subscription model and dynamic payment gateway configuration.

## Table of Contents
- [Overview](#overview)
- [Tiered Subscription Model](#tiered-subscription-model)
- [Dynamic Payment Gateway Configuration](#dynamic-payment-gateway-configuration)
- [PayPal Configuration](#paypal-configuration)
- [Stripe Configuration](#stripe-configuration)
- [Webhook Configuration](#webhook-configuration)
- [Environment Setup](#environment-setup)
- [Admin Interface Setup](#admin-interface-setup)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)

## Overview

The Yapplr payment system supports multiple payment providers with a flexible architecture and dynamic configuration. The system features:
- **Tiered Subscription Model**: Flexible subscription tiers with custom pricing and features
- **Dynamic Payment Gateway Configuration**: Admin-configurable payment providers without code changes
- **Multi-Provider Support**: PayPal, Stripe, and extensible architecture for additional providers
- **Real-time Configuration Updates**: Change payment settings instantly without application restarts

Currently implemented providers:
- **PayPal**: Subscription billing and one-time payments
- **Stripe**: Advanced payment processing with extensive features

## Tiered Subscription Model

Yapplr features a comprehensive subscription system that allows you to create multiple subscription tiers with different pricing and features.

### Subscription Tier Features
- **Flexible Pricing**: Set custom prices for each tier with support for multiple currencies
- **Billing Cycles**: Configure monthly, yearly, or custom billing periods
- **Feature Flags**: Control access to premium features like:
  - Verified badges
  - Ad-free experience
  - Premium content access
  - Enhanced upload limits
- **Trial Periods**: Configurable trial periods for new subscribers
- **Tier Management**: Create, edit, and manage subscription tiers through the admin interface

### Subscription Management
- **User Subscriptions**: Users can subscribe, upgrade, downgrade, and cancel subscriptions
- **Grace Periods**: Configurable grace periods for failed payments before suspension
- **Automatic Renewals**: Seamless subscription renewals with payment provider integration
- **Proration Support**: Automatic proration for subscription changes
- **Analytics**: Comprehensive subscription analytics and revenue tracking

## Dynamic Payment Gateway Configuration

The payment system features dynamic configuration that allows administrators to manage payment providers without code changes or application restarts.

### Key Features
- **Database-Driven Configuration**: All payment settings stored securely in the database
- **Admin Interface**: Complete web-based configuration interface
- **Real-time Updates**: Configuration changes take effect immediately
- **Secure Credential Storage**: Sensitive data encrypted in the database
- **Provider Priority**: Configure fallback order for payment provider selection
- **Health Monitoring**: Real-time provider status monitoring with automatic failover

### Configuration Management
- **Provider Settings**: Configure API keys, secrets, and environment settings
- **Environment Support**: Separate sandbox/test and production configurations
- **Validation**: Built-in validation and connectivity testing
- **Audit Trail**: Complete audit logging for all configuration changes
- **Backup/Restore**: Export and import configuration settings

### Admin Interface Access
1. Log in as an admin user
2. Navigate to `/admin/payment-configuration`
3. Configure payment providers and global settings
4. Test connectivity and save changes
5. Monitor provider health and performance

## PayPal Configuration

### 1. PayPal Developer Account Setup

1. **Create PayPal Developer Account**
   - Go to [PayPal Developer Portal](https://developer.paypal.com/)
   - Sign in with your PayPal account or create a new one
   - Navigate to "My Apps & Credentials"

2. **Create Application**
   - Click "Create App"
   - Choose "Default Application" or create a custom name
   - Select "Merchant" account type
   - Choose sandbox for testing, live for production

3. **Get API Credentials**
   - **Client ID**: Found in app details
   - **Client Secret**: Found in app details (keep secure)
   - **Webhook ID**: Generated when setting up webhooks

### 2. PayPal Products and Plans Setup

Before accepting subscriptions, you need to create products and billing plans:

```bash
# Example: Create a product via PayPal API
curl -X POST https://api-m.sandbox.paypal.com/v1/catalogs/products \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{
    "id": "yapplr-premium",
    "name": "Yapplr Premium",
    "description": "Premium subscription for Yapplr",
    "type": "SERVICE",
    "category": "SOFTWARE"
  }'

# Create billing plan
curl -X POST https://api-m.sandbox.paypal.com/v1/billing/plans \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{
    "product_id": "yapplr-premium",
    "name": "Yapplr Premium Monthly",
    "description": "Monthly premium subscription",
    "billing_cycles": [{
      "frequency": {
        "interval_unit": "MONTH",
        "interval_count": 1
      },
      "tenure_type": "REGULAR",
      "sequence": 1,
      "total_cycles": 0,
      "pricing_scheme": {
        "fixed_price": {
          "value": "29.99",
          "currency_code": "USD"
        }
      }
    }],
    "payment_preferences": {
      "auto_bill_outstanding": true,
      "setup_fee": {
        "value": "0",
        "currency_code": "USD"
      },
      "setup_fee_failure_action": "CONTINUE",
      "payment_failure_threshold": 3
    }
  }'
```

### 3. PayPal Configuration in appsettings.json

```json
{
  "PaymentProviders": {
    "Global": {
      "DefaultCurrency": "USD",
      "MaxPaymentRetries": 3,
      "RetryIntervalDays": 3,
      "GracePeriodDays": 7,
      "EnableTrialPeriods": true,
      "DefaultTrialDays": 14
    },
    "PayPal": {
      "Enabled": true,
      "Environment": "sandbox", // or "live" for production
      "ClientId": "YOUR_PAYPAL_CLIENT_ID",
      "ClientSecret": "YOUR_PAYPAL_CLIENT_SECRET",
      "WebhookSecret": "YOUR_PAYPAL_WEBHOOK_SECRET",
      "TimeoutSeconds": 30,
      "SupportedCurrencies": ["USD", "EUR", "GBP"],
      "SupportedPaymentMethods": ["paypal"],
      "Webhooks": {
        "VerifySignature": true,
        "RetryFailedWebhooks": true,
        "MaxRetryAttempts": 3
      }
    }
  }
}
```

## Stripe Configuration

### 1. Stripe Account Setup

1. **Create Stripe Account**
   - Go to [Stripe Dashboard](https://dashboard.stripe.com/)
   - Sign up for a new account
   - Complete account verification

2. **Get API Keys**
   - Navigate to "Developers" → "API keys"
   - **Publishable Key**: Starts with `pk_test_` (test) or `pk_live_` (live)
   - **Secret Key**: Starts with `sk_test_` (test) or `sk_live_` (live)
   - **Webhook Secret**: Generated when creating webhook endpoints

3. **Configure Products and Prices**
   - Go to "Products" in Stripe Dashboard
   - Create products for your subscription tiers
   - Set up recurring prices for each product

### 2. Stripe Configuration in appsettings.json

```json
{
  "PaymentProviders": {
    "Stripe": {
      "Enabled": true,
      "Environment": "test", // or "live" for production
      "PublishableKey": "pk_test_YOUR_PUBLISHABLE_KEY",
      "SecretKey": "sk_test_YOUR_SECRET_KEY",
      "WebhookSecret": "whsec_YOUR_WEBHOOK_SECRET",
      "TimeoutSeconds": 30,
      "SupportedCurrencies": ["USD", "EUR", "GBP", "CAD", "AUD"],
      "SupportedPaymentMethods": ["card", "sepa_debit", "ideal"],
      "Webhooks": {
        "VerifySignature": true,
        "RetryFailedWebhooks": true,
        "MaxRetryAttempts": 3
      }
    }
  }
}
```

## Webhook Configuration

### PayPal Webhooks

1. **Create Webhook in PayPal Developer Portal**
   - Go to your app in PayPal Developer Portal
   - Navigate to "Webhooks"
   - Click "Add Webhook"
   - **Webhook URL**: `https://yourdomain.com/api/payments/webhooks/paypal`

2. **Select Events to Subscribe**
   ```
   ✅ BILLING.SUBSCRIPTION.CREATED
   ✅ BILLING.SUBSCRIPTION.ACTIVATED
   ✅ BILLING.SUBSCRIPTION.CANCELLED
   ✅ BILLING.SUBSCRIPTION.SUSPENDED
   ✅ BILLING.SUBSCRIPTION.EXPIRED
   ✅ PAYMENT.SALE.COMPLETED
   ✅ PAYMENT.SALE.DENIED
   ✅ PAYMENT.SALE.REFUNDED
   ```

3. **Webhook Verification**
   - PayPal sends webhook events with signature headers
   - The system automatically verifies signatures using your webhook secret
   - Failed verification results in rejected webhooks

### Stripe Webhooks

1. **Create Webhook Endpoint in Stripe Dashboard**
   - Go to "Developers" → "Webhooks"
   - Click "Add endpoint"
   - **Endpoint URL**: `https://yourdomain.com/api/payments/webhooks/stripe`

2. **Select Events to Listen For**
   ```
   ✅ customer.subscription.created
   ✅ customer.subscription.updated
   ✅ customer.subscription.deleted
   ✅ invoice.payment_succeeded
   ✅ invoice.payment_failed
   ✅ charge.dispute.created
   ✅ payment_intent.succeeded
   ✅ payment_intent.payment_failed
   ```

3. **Webhook Security**
   - Stripe signs webhooks with your endpoint secret
   - Automatic signature verification prevents unauthorized requests
   - Timestamp validation prevents replay attacks

## Environment Setup

### Development Environment

```json
{
  "PaymentProviders": {
    "PayPal": {
      "Environment": "sandbox",
      "ClientId": "YOUR_SANDBOX_CLIENT_ID",
      "ClientSecret": "YOUR_SANDBOX_CLIENT_SECRET"
    },
    "Stripe": {
      "Environment": "test",
      "PublishableKey": "pk_test_...",
      "SecretKey": "sk_test_..."
    }
  }
}
```

### Production Environment

```json
{
  "PaymentProviders": {
    "PayPal": {
      "Environment": "live",
      "ClientId": "YOUR_LIVE_CLIENT_ID",
      "ClientSecret": "YOUR_LIVE_CLIENT_SECRET"
    },
    "Stripe": {
      "Environment": "live",
      "PublishableKey": "pk_live_...",
      "SecretKey": "sk_live_..."
    }
  }
}
```

### Environment Variables (Recommended for Production)

```bash
# PayPal
PAYPAL_CLIENT_ID=your_client_id
PAYPAL_CLIENT_SECRET=your_client_secret
PAYPAL_WEBHOOK_SECRET=your_webhook_secret

# Stripe
STRIPE_PUBLISHABLE_KEY=pk_live_your_key
STRIPE_SECRET_KEY=sk_live_your_key
STRIPE_WEBHOOK_SECRET=whsec_your_secret
```

Then reference in appsettings.json:
```json
{
  "PaymentProviders": {
    "PayPal": {
      "ClientId": "${PAYPAL_CLIENT_ID}",
      "ClientSecret": "${PAYPAL_CLIENT_SECRET}",
      "WebhookSecret": "${PAYPAL_WEBHOOK_SECRET}"
    },
    "Stripe": {
      "SecretKey": "${STRIPE_SECRET_KEY}",
      "WebhookSecret": "${STRIPE_WEBHOOK_SECRET}"
    }
  }
}
```

## Testing

### 1. PayPal Testing

**Test Credit Cards (Sandbox)**
```
Visa: 4012888888881881
Mastercard: 5555555555554444
American Express: 378282246310005
```

**Test PayPal Accounts**
- Create test accounts in PayPal Developer Portal
- Use sandbox.paypal.com for testing
- Test both successful and failed payments

**Testing Checklist**
- [ ] Subscription creation
- [ ] Subscription cancellation
- [ ] Payment success/failure
- [ ] Webhook delivery
- [ ] Refund processing

### 2. Stripe Testing

**Test Credit Cards**
```
Success: 4242424242424242
Decline: 4000000000000002
3D Secure: 4000000000003220
Insufficient Funds: 4000000000009995
```

**Testing Scenarios**
```bash
# Test subscription creation
curl -X POST https://localhost:5001/api/payments/subscriptions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "paymentProvider": "Premium",
    "startTrial": true,
    "returnUrl": "https://yourapp.com/success",
    "cancelUrl": "https://yourapp.com/cancel"
  }'

# Test webhook endpoint
curl -X POST https://localhost:5001/api/payments/webhooks/stripe \
  -H "Content-Type: application/json" \
  -H "Stripe-Signature: test_signature" \
  -d '{"type": "customer.subscription.created", "data": {...}}'
```

### 3. Admin Interface Testing

Access admin endpoints to verify functionality:
- `GET /api/admin/payments/subscriptions` - View all subscriptions
- `GET /api/admin/payments/analytics/overview` - Payment analytics
- `GET /api/admin/payments/providers` - Provider status
- `POST /api/admin/payments/providers/{name}/test` - Test provider connectivity

### 4. Subscription System Testing

#### Testing Subscription Tiers
```bash
# Get available subscription tiers
curl -X GET https://localhost:5001/api/subscriptions/tiers

# Get specific tier details
curl -X GET https://localhost:5001/api/subscriptions/tiers/1
```

#### Testing User Subscriptions
```bash
# Get current user's subscription
curl -X GET https://localhost:5001/api/subscriptions/my-subscription \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Assign subscription tier to user
curl -X POST https://localhost:5001/api/subscriptions/assign-tier \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{"subscriptionTierId": 1}'
```

#### Testing Payment Integration
```bash
# Create subscription with payment
curl -X POST https://localhost:5001/api/payments/subscriptions?subscriptionTierId=1 \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "paymentProvider": "PayPal",
    "startTrial": true,
    "returnUrl": "https://yourapp.com/success",
    "cancelUrl": "https://yourapp.com/cancel"
  }'

# Get current subscription details
curl -X GET https://localhost:5001/api/payments/subscriptions/current \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

#### Testing Admin Configuration
```bash
# Get payment configuration summary
curl -X GET https://localhost:5001/api/admin/payment-configuration/summary \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"

# Test payment provider connectivity
curl -X POST https://localhost:5001/api/admin/payment-configuration/providers/1/test \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"

# Get subscription analytics
curl -X GET https://localhost:5001/api/admin/subscriptions/analytics \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
```

### 5. Frontend Testing

#### Admin Interface Testing
1. **Payment Configuration Page**
   - Navigate to `/admin/payment-configuration`
   - Test provider configuration forms
   - Verify connectivity testing functionality
   - Check real-time status updates

2. **Subscription Management Page**
   - Navigate to `/admin/subscriptions`
   - Test tier creation and editing
   - Verify tier activation/deactivation
   - Check subscription analytics display

#### User Interface Testing
1. **Subscription Tier Display**
   - Check tier badges on user profiles
   - Verify feature access based on subscription
   - Test subscription upgrade/downgrade flows

2. **Payment Flow Testing**
   - Test subscription signup process
   - Verify payment provider integration
   - Check trial period functionality
   - Test subscription cancellation

## Admin Interface Setup

The payment system includes a comprehensive admin interface for managing payment providers and subscription tiers.

### Accessing the Admin Interface

1. **Admin User Setup**
   - Ensure you have an admin user account
   - Log in to the Yapplr frontend
   - Navigate to `/admin` to access the admin dashboard

2. **Payment Configuration**
   - Go to `/admin/payment-configuration`
   - Configure payment providers (PayPal, Stripe)
   - Set up global payment settings
   - Test provider connectivity

3. **Subscription Management**
   - Go to `/admin/subscriptions`
   - Create and manage subscription tiers
   - Set pricing and features for each tier
   - Monitor subscription analytics

### Payment Provider Configuration

#### Adding a New Payment Provider
1. **Navigate to Payment Configuration**
   - Go to `/admin/payment-configuration`
   - Click "Add Provider" or select an existing provider

2. **Configure Provider Settings**
   - **Provider Name**: Select PayPal or Stripe
   - **Environment**: Choose sandbox/test or live/production
   - **Priority**: Set provider priority order (lower number = higher priority)
   - **Timeout**: Configure API timeout in seconds
   - **Max Retries**: Set maximum retry attempts for failed requests

3. **Add Provider Credentials**
   - **PayPal**: Client ID, Client Secret, Webhook Secret
   - **Stripe**: Publishable Key, Secret Key, Webhook Secret
   - **Security**: Sensitive credentials are automatically encrypted

4. **Test Configuration**
   - Use the "Test Connectivity" button to verify settings
   - Check provider health status
   - Review any configuration errors

#### Global Payment Settings
- **Default Provider**: Set the primary payment provider
- **Default Currency**: Configure the default currency (USD, EUR, etc.)
- **Grace Period**: Set days before suspending failed payments
- **Trial Periods**: Enable/disable trial periods and set default duration
- **Retry Settings**: Configure payment retry attempts and intervals

### Subscription Tier Management

#### Creating Subscription Tiers
1. **Navigate to Subscription Management**
   - Go to `/admin/subscriptions`
   - Click "Create New Tier"

2. **Configure Tier Details**
   - **Name**: Tier name (e.g., "Premium", "Pro", "Enterprise")
   - **Description**: Detailed description of tier benefits
   - **Price**: Set price in the configured currency
   - **Billing Cycle**: Choose monthly, yearly, or custom period
   - **Sort Order**: Set display order for tier comparison

3. **Configure Features**
   - **Verified Badge**: Enable verified badge for this tier
   - **Show Advertisements**: Control ad display (true = show ads, false = ad-free)
   - **Custom Features**: Add additional features as JSON configuration

4. **Tier Management**
   - **Activate/Deactivate**: Control tier availability
   - **Set Default**: Mark a tier as the default option
   - **Edit/Delete**: Modify or remove existing tiers

#### Subscription Analytics
- **Revenue Tracking**: Monitor subscription revenue and trends
- **User Metrics**: Track active subscriptions and churn rates
- **Tier Performance**: Analyze popularity of different subscription tiers
- **Payment Analytics**: Review payment success rates and failures

## Database Migration

Run the payment system migrations:

```bash
# Add migration (if not already done)
dotnet ef migrations add AddPaymentEntities --project Yapplr.Api

# Update database
dotnet ef database update --project Yapplr.Api
```

## Security Considerations

### 1. API Keys Protection
- **Never commit API keys to version control**
- Use environment variables or secure key management
- Rotate keys regularly
- Use different keys for different environments

### 2. Webhook Security
- Always verify webhook signatures
- Use HTTPS for webhook endpoints
- Implement idempotency for webhook processing
- Log webhook events for audit trails

### 3. PCI Compliance
- Never store credit card information
- Use tokenization for payment methods
- Implement proper access controls
- Regular security audits

## Monitoring and Logging

### 1. Key Metrics to Monitor
- Payment success/failure rates
- Subscription churn rates
- Webhook delivery success
- API response times
- Error rates by provider

### 2. Logging Configuration

```json
{
  "Logging": {
    "LogLevel": {
      "Yapplr.Api.Services.Payment": "Information",
      "Yapplr.Api.Services.Background.PaymentBackgroundService": "Information"
    }
  }
}
```

### 3. Alerting Setup
- Failed payment thresholds
- Webhook delivery failures
- Provider downtime
- Unusual transaction patterns

## Troubleshooting

### Common PayPal Issues

**Issue**: "Invalid client credentials"
- **Solution**: Verify Client ID and Secret are correct for the environment
- Check if using sandbox credentials with live environment or vice versa

**Issue**: "Webhook signature verification failed"
- **Solution**: Ensure webhook secret matches the one from PayPal Developer Portal
- Verify webhook URL is accessible and returns 200 status

**Issue**: "Product not found"
- **Solution**: Create products and plans in PayPal before creating subscriptions
- Ensure product IDs match between PayPal and your application

### Common Stripe Issues

**Issue**: "No such customer"
- **Solution**: Ensure customer is created before subscription
- Check customer ID format and existence

**Issue**: "Your card was declined"
- **Solution**: Use test card numbers for testing
- Check if card requires 3D Secure authentication

**Issue**: "Invalid webhook signature"
- **Solution**: Verify webhook secret from Stripe Dashboard
- Ensure raw request body is used for signature verification

### Subscription System Issues

**Issue**: "Subscription tier not found"
- **Solution**: Ensure subscription tier exists and is active
- Check tier ID in admin interface
- Verify tier is not deleted or deactivated

**Issue**: "Payment provider not configured"
- **Solution**: Configure payment provider in admin interface
- Verify provider credentials are correct
- Check provider is enabled and has proper priority

**Issue**: "Subscription creation failed"
- **Solution**: Check payment provider connectivity
- Verify user doesn't already have an active subscription
- Check subscription tier pricing and configuration

**Issue**: "Trial period not working"
- **Solution**: Verify trial periods are enabled in global configuration
- Check subscription tier has trial period configured
- Ensure user hasn't already used trial period

### Admin Interface Issues

**Issue**: "Cannot access payment configuration"
- **Solution**: Ensure user has admin role
- Check authentication token is valid
- Verify admin authorization policy is configured

**Issue**: "Provider settings not saving"
- **Solution**: Check database connection
- Verify encryption key is configured for sensitive settings
- Check for validation errors in provider configuration

**Issue**: "Subscription analytics not loading"
- **Solution**: Ensure subscription data exists in database
- Check database indexes for performance
- Verify analytics service is running

### General Debugging

1. **Enable Debug Logging**
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Yapplr.Api.Services.Payment": "Debug",
         "Yapplr.Api.Services.Subscription": "Debug",
         "Yapplr.Api.Controllers.Admin.PaymentConfigurationController": "Debug"
       }
     }
   }
   ```

2. **Check Provider Status**
   ```bash
   curl -X POST https://yourapi.com/api/admin/payment-configuration/providers/1/test \
     -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
   ```

3. **Check Subscription System Status**
   ```bash
   curl -X GET https://yourapi.com/api/admin/subscription-system/status \
     -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
   ```

4. **Webhook Testing Tools**
   - Use ngrok for local webhook testing
   - PayPal Webhook Simulator
   - Stripe CLI for webhook forwarding

5. **Database Verification**
   ```sql
   -- Check payment provider configurations
   SELECT * FROM PaymentProviderConfigurations;

   -- Check subscription tiers
   SELECT * FROM SubscriptionTiers WHERE IsActive = true;

   -- Check user subscriptions
   SELECT * FROM UserSubscriptions WHERE Status = 0; -- Active subscriptions
   ```

## Production Deployment Checklist

### Payment System Setup
- [ ] Update to live API credentials for all payment providers
- [ ] Configure production webhook URLs for PayPal and Stripe
- [ ] Set up SSL certificates for webhook endpoints
- [ ] Configure environment variables for sensitive credentials
- [ ] Run database migrations for payment and subscription tables
- [ ] Test webhook endpoints with production providers
- [ ] Verify payment provider connectivity in production

### Subscription System Configuration
- [ ] Create production subscription tiers with proper pricing
- [ ] Configure trial periods and billing cycles
- [ ] Set up grace periods for failed payments
- [ ] Test subscription creation and cancellation flows
- [ ] Verify subscription analytics and reporting
- [ ] Configure subscription email notifications

### Security and Monitoring
- [ ] Set up monitoring and alerting for payment failures
- [ ] Configure backup and disaster recovery for payment data
- [ ] Perform security audit of payment configuration
- [ ] Document incident response procedures for payment issues
- [ ] Set up fraud detection and monitoring
- [ ] Configure PCI compliance measures

### Admin Interface
- [ ] Verify admin access to payment configuration
- [ ] Test payment provider management interface
- [ ] Confirm subscription tier management functionality
- [ ] Set up admin notifications for payment issues
- [ ] Document admin procedures for payment management

### Testing and Validation
- [ ] Test end-to-end subscription flows
- [ ] Verify webhook processing and retry logic
- [ ] Test payment failure scenarios and recovery
- [ ] Validate subscription analytics and reporting
- [ ] Confirm trial period and grace period functionality
- [ ] Test subscription upgrade and downgrade flows

## Support and Resources

### PayPal
- [PayPal Developer Documentation](https://developer.paypal.com/docs/)
- [PayPal Subscriptions API](https://developer.paypal.com/docs/subscriptions/)
- [PayPal Webhooks Guide](https://developer.paypal.com/docs/api-basics/notifications/webhooks/)

### Stripe
- [Stripe Documentation](https://stripe.com/docs)
- [Stripe Subscriptions](https://stripe.com/docs/billing/subscriptions)
- [Stripe Webhooks](https://stripe.com/docs/webhooks)

### Testing Tools
- [PayPal Sandbox](https://developer.paypal.com/developer/accounts/)
- [Stripe Test Mode](https://stripe.com/docs/testing)
- [ngrok](https://ngrok.com/) for local webhook testing