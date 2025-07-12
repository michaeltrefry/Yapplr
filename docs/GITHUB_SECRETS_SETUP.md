# GitHub Secrets Setup for RabbitMQ Deployment

This document outlines the GitHub secrets that need to be configured for deploying Yapplr with RabbitMQ support.

## Required GitHub Secrets

### Staging Environment

Add these secrets to your GitHub repository settings under **Settings > Secrets and variables > Actions**:

#### RabbitMQ Configuration
- `STAGE_RABBITMQ_USERNAME` - Username for RabbitMQ (e.g., `yapplr_stage`)
- `STAGE_RABBITMQ_PASSWORD` - Strong password for RabbitMQ (e.g., `StrongPassword123!`)

#### Existing Secrets (for reference)
- `STAGE_POSTGRES_PASSWORD` - PostgreSQL password
- `STAGE_JWT_SECRET_KEY` - JWT signing key
- `STAGE_SENDGRID_API_KEY` - SendGrid API key for emails
- `STAGE_SENDGRID_FROM_EMAIL` - From email address
- `STAGE_SENDGRID_FROM_NAME` - From name for emails
- `STAGE_EMAIL_PROVIDER` - Email provider (e.g., `sendgrid`)
- `STAGE_FIREBASE_PROJECT_ID` - Firebase project ID
- `STAGE_FIREBASE_SERVICE_ACCOUNT_KEY` - Firebase service account JSON
- `STAGE_API_DOMAIN_NAME` - API domain (e.g., `api-stage.yapplr.com`)
- `STAGE_CERTBOT_EMAIL` - Email for SSL certificates
- `STAGE_CERTBOT_DOMAIN` - Domain for SSL certificates

### Production Environment

#### RabbitMQ Configuration
- `PROD_RABBITMQ_USERNAME` - Username for RabbitMQ (e.g., `yapplr_prod`)
- `PROD_RABBITMQ_PASSWORD` - Strong password for RabbitMQ (different from staging)

#### Existing Secrets (for reference)
- `PROD_DATABASE_CONNECTION_STRING` - PostgreSQL connection string
- `PROD_JWT_SECRET_KEY` - JWT signing key (different from staging)
- `PROD_SENDGRID_API_KEY` - SendGrid API key for emails
- `PROD_SENDGRID_FROM_EMAIL` - From email address
- `PROD_SENDGRID_FROM_NAME` - From name for emails
- `PROD_EMAIL_PROVIDER` - Email provider (e.g., `sendgrid`)
- `PROD_FIREBASE_PROJECT_ID` - Firebase project ID
- `PROD_FIREBASE_SERVICE_ACCOUNT_KEY` - Firebase service account JSON
- `PROD_API_DOMAIN_NAME` - API domain (e.g., `api.yapplr.com`)
- `PROD_CERTBOT_EMAIL` - Email for SSL certificates
- `PROD_CERTBOT_DOMAIN` - Domain for SSL certificates

## Security Best Practices

### RabbitMQ Credentials
1. **Use Strong Passwords**: Generate random passwords with at least 16 characters
2. **Different Credentials**: Use different usernames and passwords for staging and production
3. **Regular Rotation**: Consider rotating RabbitMQ credentials periodically

### Example Strong Password Generation
```bash
# Generate a strong password
openssl rand -base64 32
```

### Recommended RabbitMQ Usernames
- Staging: `yapplr_stage_user`
- Production: `yapplr_prod_user`

## How to Add Secrets

1. Go to your GitHub repository
2. Click **Settings** tab
3. In the left sidebar, click **Secrets and variables** > **Actions**
4. Click **New repository secret**
5. Add the secret name and value
6. Click **Add secret**

## Verification

After adding the secrets, you can verify they're working by:

1. **Check Deployment Logs**: Look for RabbitMQ connection success in GitHub Actions logs
2. **Monitor RabbitMQ**: Access RabbitMQ management UI to see connections
3. **Test CQRS Endpoints**: Use the `/api/cqrs-test/*` endpoints to verify message processing

## Troubleshooting

### Common Issues

1. **Connection Refused**: Check that RabbitMQ credentials match between secrets and Docker Compose
2. **Authentication Failed**: Verify username and password are correct
3. **Missing Secrets**: Ensure all required secrets are added to GitHub

### Debug Steps

1. Check GitHub Actions logs for RabbitMQ connection errors
2. SSH into the server and check Docker logs: `docker logs rabbitmq`
3. Verify environment variables are set correctly in the container
4. Test RabbitMQ connection manually using the management UI

## Docker Compose Integration

The RabbitMQ credentials from GitHub secrets are automatically injected into the Docker Compose environment variables:

```yaml
# In docker-compose.stage.yml and docker-compose.prod.yml
rabbitmq:
  environment:
    - RABBITMQ_DEFAULT_USER=${STAGE_RABBITMQ_USERNAME}
    - RABBITMQ_DEFAULT_PASS=${STAGE_RABBITMQ_PASSWORD}
```

The API container receives these credentials via:

```yaml
# In docker-compose files
yapplr-api:
  environment:
    - RabbitMQ__Username=${STAGE_RABBITMQ_USERNAME}
    - RabbitMQ__Password=${STAGE_RABBITMQ_PASSWORD}
```

This ensures secure, environment-specific RabbitMQ authentication for your CQRS system.
