# GitHub Secrets Required for Deployment

This document lists all the GitHub secrets that need to be configured in your repository settings for successful deployment to staging and production environments.

## Server Connection Secrets

### Staging
- `STAGE_SERVER_HOST` - IP address or hostname of staging server
- `STAGE_SERVER_USER` - SSH username for staging server
- `STAGE_SERVER_SSH_KEY` - Private SSH key for staging server access

### Production
- `PROD_SERVER_HOST` - IP address or hostname of production server
- `PROD_SERVER_USER` - SSH username for production server
- `PROD_SERVER_SSH_KEY` - Private SSH key for production server access

## Application Secrets

### Staging Environment
- `STAGE_POSTGRES_PASSWORD` - PostgreSQL database password
- `STAGE_JWT_SECRET_KEY` - JWT signing key (minimum 32 characters)
- `STAGE_API_DOMAIN_NAME` - API domain (e.g., stg-api.yapplr.com)
- `STAGE_API_BASE_URL` - Full API base URL for media URLs (e.g., https://stg-api.yapplr.com)

### Production Environment
- `PROD_DATABASE_CONNECTION_STRING` - Full PostgreSQL connection string
- `PROD_JWT_SECRET_KEY` - JWT signing key (minimum 32 characters)
- `PROD_API_DOMAIN_NAME` - API domain (e.g., api.yapplr.com)
- `PROD_API_BASE_URL` - Full API base URL for media URLs (e.g., https://api.yapplr.com)

## Email Service Secrets

### Staging
- `STAGE_SENDGRID_API_KEY` - SendGrid API key for staging
- `STAGE_SENDGRID_FROM_EMAIL` - From email address for staging
- `STAGE_SENDGRID_FROM_NAME` - From name for staging emails
- `STAGE_EMAIL_PROVIDER` - Email provider (SendGrid, Console, etc.)

### Production
- `PROD_SENDGRID_API_KEY` - SendGrid API key for production
- `PROD_SENDGRID_FROM_EMAIL` - From email address for production
- `PROD_SENDGRID_FROM_NAME` - From name for production emails
- `PROD_EMAIL_PROVIDER` - Email provider (SendGrid, Console, etc.)

## Firebase Secrets (for mobile push notifications)

### Staging
- `STAGE_FIREBASE_PROJECT_ID` - Firebase project ID for staging
- `STAGE_FIREBASE_SERVICE_ACCOUNT_KEY` - Firebase service account JSON key

### Production
- `PROD_FIREBASE_PROJECT_ID` - Firebase project ID for production
- `PROD_FIREBASE_SERVICE_ACCOUNT_KEY` - Firebase service account JSON key

## SSL Certificate Secrets

### Staging
- `STAGE_CERTBOT_EMAIL` - Email for Let's Encrypt certificates
- `STAGE_CERTBOT_DOMAIN` - Domain for SSL certificates

### Production
- `PROD_CERTBOT_EMAIL` - Email for Let's Encrypt certificates
- `PROD_CERTBOT_DOMAIN` - Domain for SSL certificates

## Message Queue Secrets

### Staging
- `STAGE_RABBITMQ_USERNAME` - RabbitMQ username
- `STAGE_RABBITMQ_PASSWORD` - RabbitMQ password

### Production
- `PROD_RABBITMQ_USERNAME` - RabbitMQ username
- `PROD_RABBITMQ_PASSWORD` - RabbitMQ password

## Cache Secrets

### Staging
- `STAGE_REDIS_CONNECTION_STRING` - Redis connection string

### Production
- `PROD_REDIS_CONNECTION_STRING` - Redis connection string

## Analytics Stack Secrets

### Staging InfluxDB
- `STAGE_INFLUXDB_TOKEN` - InfluxDB admin token
- `STAGE_INFLUXDB_USER` - InfluxDB admin username
- `STAGE_INFLUXDB_PASSWORD` - InfluxDB admin password
- `STAGE_INFLUXDB_ORG` - InfluxDB organization name (default: yapplr)
- `STAGE_INFLUXDB_BUCKET` - InfluxDB bucket name (default: analytics)

### Production InfluxDB
- `PROD_INFLUXDB_TOKEN` - InfluxDB admin token
- `PROD_INFLUXDB_USER` - InfluxDB admin username
- `PROD_INFLUXDB_PASSWORD` - InfluxDB admin password
- `PROD_INFLUXDB_ORG` - InfluxDB organization name (default: yapplr)
- `PROD_INFLUXDB_BUCKET` - InfluxDB bucket name (default: analytics)

### Staging Grafana
- `STAGE_GRAFANA_USER` - Grafana admin username
- `STAGE_GRAFANA_PASSWORD` - Grafana admin password
- `STAGE_GRAFANA_DOMAIN` - Grafana domain (e.g., stg-grafana.yapplr.com)

### Production Grafana
- `PROD_GRAFANA_USER` - Grafana admin username
- `PROD_GRAFANA_PASSWORD` - Grafana admin password
- `PROD_GRAFANA_DOMAIN` - Grafana domain (e.g., grafana.yapplr.com)

## Subdomain Configuration

The nginx configuration includes the following subdomains:

### Staging Subdomains
- `stg-api.yapplr.com` - Main API
- `stg.yapplr.com` - Frontend application
- `stg-logger.yapplr.com` - Seq logging interface
- `stg-grafana.yapplr.com` - Grafana analytics dashboard
- `stg-rabbitmq.yapplr.com` - RabbitMQ management UI

### Production Subdomains
- `api.yapplr.com` - Main API
- `yapplr.com` / `www.yapplr.com` / `app.yapplr.com` - Frontend application
- `logger.yapplr.com` - Seq logging interface
- `grafana.yapplr.com` - Grafana analytics dashboard
- `rabbitmq.yapplr.com` - RabbitMQ management UI

**Note**: Ensure your SSL certificates cover all these subdomains or use wildcard certificates.

## How to Add Secrets

1. Go to your GitHub repository
2. Click on **Settings** tab
3. In the left sidebar, click **Secrets and variables** → **Actions**
4. Click **New repository secret**
5. Add the secret name and value
6. Click **Add secret**

## Security Notes

- Use strong, unique passwords for all services
- Generate secure random tokens for JWT and InfluxDB
- Keep Firebase service account keys secure
- Use different credentials for staging and production
- Regularly rotate secrets, especially for production

## Example Values

```bash
# JWT Secret (generate with: openssl rand -base64 32)
PROD_JWT_SECRET_KEY=your-super-secret-jwt-key-that-should-be-at-least-32-characters-long

# InfluxDB Token (generate with: openssl rand -hex 32)
PROD_INFLUXDB_TOKEN=your-influxdb-admin-token-here

# Database Connection
PROD_DATABASE_CONNECTION_STRING=Host=your-db-host;Port=5432;Database=yapplr_db;Username=yapplr;Password=your-secure-password

# Redis Connection
PROD_REDIS_CONNECTION_STRING=your-redis-host:6379,password=your-redis-password
```
