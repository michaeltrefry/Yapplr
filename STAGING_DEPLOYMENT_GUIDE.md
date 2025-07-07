# Yapplr Staging Deployment Guide

## 🎯 Overview

The staging environment is designed to be **identical to production** except for one key difference: **PostgreSQL runs locally in Docker** instead of using an external database service.

## 📁 Staging Files

### **Core Files:**
- `docker-compose.staging.yml` - Staging services configuration
- `scripts/deploy-staging.sh` - Staging deployment script
- `.env.staging` - Staging environment variables
- `.env.staging.template` - Template for staging configuration

### **GitHub Actions:**
- `.github/workflows/deploy-staging.yml` - Automated staging deployment

## 🔄 Key Differences from Production

| Component | Production | Staging |
|-----------|------------|---------|
| **PostgreSQL** | External service | Local Docker container |
| **Database Port** | Internal only | Exposed on 5432 for debugging |
| **Environment** | `ASPNETCORE_ENVIRONMENT=Production` | `ASPNETCORE_ENVIRONMENT=Staging` |
| **Video Limits** | Full resolution (1920x1080) | Reduced (1280x720) |
| **Concurrent Jobs** | 2 video processing jobs | 1 video processing job |
| **Redis** | Included (if used) | Not included |
| **Monitoring** | Prometheus/Grafana/Loki | Not included |

## 🚀 Deployment Commands

### **Deploy Staging:**
```bash
./scripts/deploy-staging.sh deploy
```

### **Other Commands:**
```bash
./scripts/deploy-staging.sh stop      # Stop all services
./scripts/deploy-staging.sh restart   # Restart services
./scripts/deploy-staging.sh logs      # View logs
./scripts/deploy-staging.sh status    # Show status
./scripts/deploy-staging.sh cleanup   # Cleanup only
```

### **Health Monitoring:**
```bash
./scripts/health-check.sh staging check    # Full health check
./scripts/health-check.sh staging quick    # Quick check
./scripts/health-check.sh staging services # Service status only
```

## 🗄️ Database Access

### **Connection Details:**
- **Host:** localhost
- **Port:** 5432
- **Database:** yapplr_staging (or your configured name)
- **Username:** yapplr_staging_user (or your configured user)
- **Password:** (from your .env.staging file)

### **Connect via Docker:**
```bash
# Connect to PostgreSQL container
docker-compose -f docker-compose.staging.yml exec postgres psql -U yapplr_staging_user -d yapplr_staging

# Run SQL commands
docker-compose -f docker-compose.staging.yml exec postgres psql -U yapplr_staging_user -d yapplr_staging -c "SELECT COUNT(*) FROM \"Users\";"
```

### **Connect via External Tool:**
Use any PostgreSQL client (pgAdmin, DBeaver, etc.) with the connection details above.

## ⚙️ Environment Configuration

### **Required Variables in `.env.staging`:**

```bash
# Database (Local PostgreSQL)
POSTGRES_DB=yapplr_staging
POSTGRES_USER=yapplr_staging_user
POSTGRES_PASSWORD=your_staging_password

# JWT Configuration
JWT_SECRET_KEY=staging-jwt-secret-key
JWT_ISSUER=Yapplr.Api.Staging
JWT_AUDIENCE=Yapplr.Client.Staging

# Email Configuration (SendGrid)
EMAIL_PROVIDER=SendGrid
SENDGRID_API_KEY=your-sendgrid-staging-api-key
SENDGRID_FROM_EMAIL=staging@yapplr.com
SENDGRID_FROM_NAME=Yapplr Staging

# Domain Configuration
API_BASE_URL=http://localhost
WS_BASE_URL=ws://localhost

# Firebase (Staging Project)
FIREBASE_PROJECT_ID=yapplr-staging

# Video Processing (Reduced Limits)
VIDEO_MAX_CONCURRENT_JOBS=1
VIDEO_MAX_WIDTH=1280
VIDEO_MAX_HEIGHT=720
VIDEO_BITRATE=1000k
```

## 🔧 GitHub Actions Setup

### **Required GitHub Secrets for Staging:**

```
# Server Access
STAGING_HOST=your-staging-server.com
STAGING_USERNAME=deploy
STAGING_SSH_KEY=your-ssh-private-key
STAGING_PORT=22

# Database
STAGING_POSTGRES_DB=yapplr_staging
STAGING_POSTGRES_USER=yapplr_staging_user
STAGING_POSTGRES_PASSWORD=secure_staging_password

# JWT
STAGING_JWT_SECRET_KEY=staging-jwt-secret-key
STAGING_JWT_ISSUER=Yapplr.Api.Staging
STAGING_JWT_AUDIENCE=Yapplr.Client.Staging

# Email (SendGrid)
STAGING_EMAIL_PROVIDER=SendGrid
STAGING_SENDGRID_API_KEY=your-sendgrid-staging-api-key
STAGING_SENDGRID_FROM_EMAIL=staging@yapplr.com
STAGING_SENDGRID_FROM_NAME=Yapplr Staging

# Domains
STAGING_API_BASE_URL=http://staging.yapplr.com
STAGING_WS_BASE_URL=ws://staging.yapplr.com

# Firebase
STAGING_FIREBASE_PROJECT_ID=yapplr-staging
STAGING_FIREBASE_SERVICE_ACCOUNT_KEY={"type":"service_account",...}
```

## 🔄 Workflow Triggers

### **Automatic Deployment:**
- Push to `develop` branch
- Push to `staging` branch
- Changes to staging-related files

### **Manual Deployment:**
- Use "Run workflow" button in GitHub Actions

## 🧪 Testing Staging Environment

### **1. Basic Health Check:**
```bash
curl http://localhost/health
```

### **2. Database Connectivity:**
```bash
./scripts/health-check.sh staging check
```

### **3. Video Processing:**
```bash
# Check video processor logs
docker-compose -f docker-compose.staging.yml logs yapplr-video-processor
```

### **4. Frontend Access:**
```bash
curl http://localhost
```

## 📊 Monitoring & Logs

### **View Logs:**
```bash
# All services
docker-compose -f docker-compose.staging.yml logs -f

# Specific service
docker-compose -f docker-compose.staging.yml logs -f yapplr-api
docker-compose -f docker-compose.staging.yml logs -f postgres
docker-compose -f docker-compose.staging.yml logs -f yapplr-video-processor
```

### **Service Status:**
```bash
docker-compose -f docker-compose.staging.yml ps
```

## 🔒 Security Considerations

### **Staging Security:**
1. **Different Credentials:** Use separate passwords from production
2. **Separate Firebase Project:** Don't use production Firebase
3. **Test Email Accounts:** Use dedicated staging email accounts
4. **Network Access:** Consider firewall rules for staging server
5. **Database Exposure:** Port 5432 is exposed for debugging - secure appropriately

## 🚨 Troubleshooting

### **Common Issues:**

#### **Database Connection Failed:**
```bash
# Check if PostgreSQL is running
docker-compose -f docker-compose.staging.yml ps postgres

# Check database logs
docker-compose -f docker-compose.staging.yml logs postgres

# Restart database
docker-compose -f docker-compose.staging.yml restart postgres
```

#### **API Not Starting:**
```bash
# Check API logs
docker-compose -f docker-compose.staging.yml logs yapplr-api

# Check if database is ready
docker-compose -f docker-compose.staging.yml exec postgres pg_isready
```

#### **Port Conflicts:**
```bash
# Check what's using port 5432
sudo lsof -i :5432

# Stop conflicting services
sudo systemctl stop postgresql  # If local PostgreSQL is running
```

## 🔄 Data Management

### **Reset Staging Database:**
```bash
# Stop services
./scripts/deploy-staging.sh stop

# Remove database volume
docker volume rm yapplr_postgres_data

# Redeploy (will create fresh database)
./scripts/deploy-staging.sh deploy
```

### **Backup Staging Database:**
```bash
# Create backup
docker-compose -f docker-compose.staging.yml exec postgres pg_dump -U yapplr_staging_user yapplr_staging > staging_backup.sql

# Restore backup
docker-compose -f docker-compose.staging.yml exec -T postgres psql -U yapplr_staging_user yapplr_staging < staging_backup.sql
```

## 🎯 Best Practices

1. **Keep staging in sync** with production code
2. **Use separate credentials** for all services
3. **Test migrations** in staging before production
4. **Monitor resource usage** (staging typically has lower limits)
5. **Regular cleanup** of old data and logs
6. **Document staging-specific configurations**

## 📞 Support

If you encounter issues with staging deployment:

1. **Check logs**: `./scripts/deploy-staging.sh logs`
2. **Check status**: `./scripts/deploy-staging.sh status`
3. **Run health check**: `./scripts/health-check.sh staging check`
4. **Manual cleanup**: `./scripts/deploy-staging.sh cleanup`
5. **Database check**: Connect to PostgreSQL and verify data

The staging environment provides a safe space to test changes before production deployment!
