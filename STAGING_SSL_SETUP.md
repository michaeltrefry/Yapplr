# Staging SSL Certificate Setup Guide

## 🔒 Overview

This guide helps you set up SSL certificates for your staging environment using Let's Encrypt and Certbot.

## 🎯 Staging Domains

- **API:** `stg-api.yapplr.com`
- **Frontend:** `stg.yapplr.com`

## 🚀 Quick Setup

### **1. Run the SSL Setup Script:**
```bash
sudo ./scripts/setup-staging-ssl.sh setup
```

This script will:
- Install Certbot
- Generate staging SSL certificates
- Create nginx SSL configuration
- Set up automatic renewal

### **2. Update Environment Variables:**
Update your `.env.staging` file:
```bash
API_BASE_URL=https://stg-api.yapplr.com
WS_BASE_URL=wss://stg-api.yapplr.com
FRONTEND_URL=https://stg.yapplr.com
```

### **3. Update Docker Compose:**
Modify `docker-compose.staging.yml` to use the SSL nginx config:
```yaml
nginx:
  volumes:
    - ./nginx/nginx-staging-ssl.conf:/etc/nginx/nginx.conf:ro
    - ./nginx/ssl:/etc/nginx/ssl:ro
```

### **4. Restart Staging:**
```bash
./scripts/deploy-staging.sh deploy
```

## 🔧 Manual Certbot Commands

### **Generate Certificates (Staging):**
```bash
# For API domain
sudo certbot certonly \
  --standalone \
  --email your-email@example.com \
  --agree-tos \
  --staging \
  -d stg-api.yapplr.com

# For Frontend domain  
sudo certbot certonly \
  --standalone \
  --email your-email@example.com \
  --agree-tos \
  --staging \
  -d stg.yapplr.com
```

### **Generate Production Certificates:**
```bash
# Remove --staging flag for production certificates
sudo certbot certonly \
  --standalone \
  --email your-email@example.com \
  --agree-tos \
  -d stg-api.yapplr.com
```

### **Renew Certificates:**
```bash
sudo certbot renew --staging  # For staging certs
sudo certbot renew            # For production certs
```

### **List Certificates:**
```bash
sudo certbot certificates
```

### **Test Configuration:**
```bash
sudo certbot --staging --dry-run renew
```

## 📁 Certificate Locations

### **Let's Encrypt Certificates:**
```
/etc/letsencrypt/live/stg-api.yapplr.com/
├── fullchain.pem    # Certificate + intermediate
├── privkey.pem      # Private key
├── cert.pem         # Certificate only
└── chain.pem        # Intermediate only
```

### **Nginx Certificates (copied by script):**
```
./nginx/ssl/
├── stg-api.yapplr.com.crt
├── stg-api.yapplr.com.key
├── stg.yapplr.com.crt
└── stg.yapplr.com.key
```

## 🔄 Certificate Renewal

### **Automatic Renewal:**
The setup script creates a cron job:
```bash
# Check cron job
sudo cat /etc/cron.d/certbot-staging

# Manual renewal test
sudo certbot renew --staging --dry-run
```

### **Manual Renewal:**
```bash
# Renew and restart nginx
sudo certbot renew --staging
sudo systemctl reload nginx

# Or restart Docker containers
sudo certbot renew --staging
./scripts/deploy-staging.sh restart
```

## 🌐 DNS Requirements

### **Before Running Certbot:**
Ensure your domains point to your staging server:
```bash
# Check DNS resolution
nslookup stg-api.yapplr.com
nslookup stg.yapplr.com

# Test connectivity
curl -I http://stg-api.yapplr.com
curl -I http://stg.yapplr.com
```

### **DNS Records Needed:**
```
Type: A
Name: stg-api
Value: YOUR_STAGING_SERVER_IP

Type: A  
Name: stg
Value: YOUR_STAGING_SERVER_IP
```

## 🚨 Troubleshooting

### **Common Issues:**

#### **Port 80/443 Already in Use:**
```bash
# Stop nginx temporarily
sudo systemctl stop nginx
# Or stop Docker containers
./scripts/deploy-staging.sh stop

# Run certbot
sudo certbot certonly --standalone ...

# Restart services
./scripts/deploy-staging.sh deploy
```

#### **DNS Not Resolving:**
```bash
# Check DNS propagation
dig stg-api.yapplr.com
dig stg.yapplr.com

# Wait for DNS propagation (can take up to 48 hours)
```

#### **Certificate Validation Failed:**
```bash
# Check if domain is accessible
curl -I http://stg-api.yapplr.com/.well-known/acme-challenge/test

# Check firewall
sudo ufw status
sudo iptables -L
```

#### **Permission Issues:**
```bash
# Fix certificate permissions
sudo chmod 644 /etc/letsencrypt/live/*/fullchain.pem
sudo chmod 600 /etc/letsencrypt/live/*/privkey.pem

# Fix nginx ssl directory
sudo chown -R root:root ./nginx/ssl/
sudo chmod 644 ./nginx/ssl/*.crt
sudo chmod 600 ./nginx/ssl/*.key
```

## 🔍 Verification

### **Test SSL Configuration:**
```bash
# Test SSL connection
openssl s_client -connect stg-api.yapplr.com:443 -servername stg-api.yapplr.com

# Check certificate details
echo | openssl s_client -connect stg-api.yapplr.com:443 2>/dev/null | openssl x509 -noout -text
```

### **Test API with SSL:**
```bash
curl -k https://stg-api.yapplr.com/health
curl -k https://stg.yapplr.com
```

## 📝 Notes

### **Staging vs Production Certificates:**
- **Staging certificates:** Use `--staging` flag, not trusted by browsers
- **Production certificates:** Remove `--staging` flag, trusted by browsers
- **Rate limits:** Staging has higher rate limits for testing

### **Certificate Validity:**
- **Duration:** 90 days
- **Renewal:** Recommended at 60 days
- **Automatic renewal:** Set up via cron job

### **Security:**
- **Private keys:** Keep secure, never share
- **Permissions:** 600 for private keys, 644 for certificates
- **Backup:** Consider backing up `/etc/letsencrypt/`

## 🎯 Quick Commands Reference

```bash
# Setup SSL for staging
sudo ./scripts/setup-staging-ssl.sh setup

# Renew certificates
sudo ./scripts/setup-staging-ssl.sh renew

# Test configuration
sudo ./scripts/setup-staging-ssl.sh test

# Check certificate status
sudo certbot certificates

# Test renewal (dry run)
sudo certbot renew --staging --dry-run
```
