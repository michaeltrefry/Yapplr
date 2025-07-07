# GitHub Actions Automatic Deployment Setup

## Overview

Your Yapplr repository has a GitHub Actions workflow that automatically deploys to Linode on every push to main. The workflow is currently failing because it needs proper configuration.

## 🔧 Required GitHub Secrets

You need to add these secrets in your GitHub repository:

### 1. Add GitHub Secrets

Go to your GitHub repository → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**

**SSH Access Secrets:**
```
PROD_SERVER_HOST=your-linode-server-ip
PROD_SERVER_USER=root
PROD_SERVER_SSH_KEY=your-private-ssh-key-content
```

**Database & Core Secrets:**
```
PROD_DATABASE_CONNECTION_STRING=Host=your-db-host;Database=yapplr;Username=yapplr_user;Password=your-password
PROD_JWT_SECRET_KEY=your-production-jwt-secret-key-minimum-32-characters
PROD_API_DOMAIN_NAME=api.yapplr.com
```

**Email Provider Secrets (SendGrid):**
```
PROD_SENDGRID_API_KEY=your-sendgrid-api-key
PROD_SENDGRID_FROM_EMAIL=noreply@yapplr.com
PROD_SENDGRID_FROM_NAME=Yapplr
PROD_EMAIL_PROVIDER=SendGrid
```

**Firebase Secrets (REQUIRED for notifications):**
```
PROD_FIREBASE_PROJECT_ID=your-firebase-project-id
PROD_FIREBASE_SERVICE_ACCOUNT_KEY={"type":"service_account","project_id":"your-project-id",...}
FIREBASE_API_KEY=your-firebase-api-key
FIREBASE_AUTH_DOMAIN=your-project.firebaseapp.com
FIREBASE_STORAGE_BUCKET=your-project.appspot.com
FIREBASE_MESSAGING_SENDER_ID=your-sender-id
FIREBASE_APP_ID=your-app-id
FIREBASE_VAPID_KEY=your-vapid-key
PROD_CERTBOT_EMAIL=your-email@domain.com
```

### 2. Get Your SSH Key

If you don't have an SSH key for your Linode server:

```bash
# Generate SSH key pair
ssh-keygen -t rsa -b 4096 -C "github-actions@yapplr.com"

# Copy public key to your Linode server
ssh-copy-id -i ~/.ssh/id_rsa.pub root@your-linode-ip

# Copy private key content for GitHub secret
cat ~/.ssh/id_rsa
```

### 3. Firebase Configuration Values

Get these from Firebase Console:

1. **Project Settings** → **General** → **Your apps** → **Web app config**
2. **Project Settings** → **Cloud Messaging** → **Web configuration** → **Generate VAPID key**
3. **Project Settings** → **Service accounts** → **Generate new private key**

## 🚀 How the Deployment Works

### Workflow Trigger
- **Automatic**: Every push to `main` branch
- **Manual**: Can be triggered from GitHub Actions tab

### Deployment Steps
1. **Checkout code** from GitHub
2. **Connect to Linode** via SSH
3. **Pull latest code** on server
4. **Build Docker images** for API and frontend
5. **Update environment variables** with secrets
6. **Run database migrations** automatically
7. **Deploy with zero downtime** using Docker Compose
8. **Health check** to verify deployment success

### What Gets Deployed
- ✅ **Yapplr API** (backend) with all latest changes
- ✅ **Yapplr Frontend** (web app) with Firebase notifications
- ✅ **Database migrations** applied automatically
- ✅ **SSL certificates** managed by Let's Encrypt
- ✅ **Nginx reverse proxy** for routing

## 🔍 Monitoring Deployment

### GitHub Actions
- Go to your repository → **Actions** tab
- Click on latest workflow run to see logs
- Green ✅ = successful deployment
- Red ❌ = failed deployment with error logs

### Deployment Logs
The workflow shows detailed logs for each step:
- SSH connection status
- Docker build progress
- Database migration results
- Health check status
- Final deployment confirmation

### Server Health Check
After deployment, the workflow automatically checks:
- API responds at `https://api.yapplr.com/health`
- Database connectivity
- Firebase initialization
- SSL certificate validity

## 🛠️ Troubleshooting

### Common Issues

**"SSH connection failed"**
- Check `PROD_SERVER_HOST` and `PROD_SERVER_USER` secrets
- Verify SSH key is correct and has access
- Ensure server is running and accessible

**"Docker build failed"**
- Check for syntax errors in recent code changes
- Verify Dockerfile is valid
- Check server has enough disk space

**"Database migration failed"**
- Verify `PROD_DATABASE_CONNECTION_STRING` is correct
- Check database server is running
- Ensure database user has migration permissions

**"Firebase initialization failed"**
- Verify `PROD_FIREBASE_SERVICE_ACCOUNT_KEY` is valid JSON
- Check `PROD_FIREBASE_PROJECT_ID` matches your project
- Ensure service account has correct permissions

**"Health check failed"**
- API may be starting slowly (normal for first deployment)
- Check server resources (CPU/memory)
- Review application logs for errors

### Manual Deployment

If GitHub Actions fails, you can deploy manually:

```bash
# SSH to your Linode server
ssh root@your-linode-ip

# Navigate to project directory
cd /opt/yapplr

# Pull latest changes
git pull origin main

# Run deployment script
cd Yapplr.Api
./deploy.sh
```

## ✅ Deployment Success

When deployment succeeds, you'll see:
- ✅ Green checkmark in GitHub Actions
- ✅ API responding at `https://api.yapplr.com`
- ✅ Frontend accessible at `https://yapplr.com`
- ✅ Firebase notifications working
- ✅ Database migrations applied

## 🎯 Next Steps

1. **Add all required GitHub secrets** (see list above)
2. **Test deployment** by pushing a small change
3. **Monitor first deployment** in GitHub Actions
4. **Verify everything works** after successful deployment

Your automatic deployment will then work on every push to main! 🚀

## 📋 Quick Setup Checklist

- [ ] Add SSH secrets (`PROD_SERVER_HOST`, `PROD_SERVER_USER`, `PROD_SERVER_SSH_KEY`)
- [ ] Add database secret (`PROD_DATABASE_CONNECTION_STRING`)
- [ ] Add JWT secret (`PROD_JWT_SECRET_KEY`)
- [ ] Add AWS SES secrets (4 variables)
- [ ] Add Firebase secrets (8 variables)
- [ ] Add domain and email secrets
- [ ] Test deployment by pushing to main
- [ ] Verify deployment success in GitHub Actions
- [ ] Check API and frontend are accessible

Once configured, your Yapplr platform will automatically deploy on every code change! 🎉
