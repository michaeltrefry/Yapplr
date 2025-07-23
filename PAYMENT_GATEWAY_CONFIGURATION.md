# Payment Gateway Configuration Guide

This guide covers the complete setup and configuration of PayPal and Stripe payment providers for the Yapplr payment system.

## Table of Contents
- [Overview](#overview)
- [PayPal Configuration](#paypal-configuration)
- [Stripe Configuration](#stripe-configuration)
- [Webhook Configuration](#webhook-configuration)
- [Environment Setup](#environment-setup)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)

## Overview

The Yapplr payment system supports multiple payment providers with a flexible architecture. Currently implemented providers:
- **PayPal**: Subscription billing and one-time payments
- **Stripe**: Advanced payment processing with extensive features

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

### General Debugging

1. **Enable Debug Logging**
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Yapplr.Api.Services.Payment": "Debug"
       }
     }
   }
   ```

2. **Check Provider Status**
   ```bash
   curl -X POST https://yourapi.com/api/admin/payments/providers/PayPal/test \
     -H "Authorization: Bearer YOUR_ADMIN_TOKEN"
   ```

3. **Webhook Testing Tools**
   - Use ngrok for local webhook testing
   - PayPal Webhook Simulator
   - Stripe CLI for webhook forwarding

## Production Deployment Checklist

- [ ] Update to live API credentials
- [ ] Configure production webhook URLs
- [ ] Set up SSL certificates
- [ ] Configure environment variables
- [ ] Run database migrations
- [ ] Test webhook endpoints
- [ ] Set up monitoring and alerting
- [ ] Configure backup and disaster recovery
- [ ] Perform security audit
- [ ] Document incident response procedures

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