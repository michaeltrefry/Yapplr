# Yapplr API Deployment Checklist

## Pre-Deployment Setup

### ✅ Linode Account & Server
- [ ] Create Linode account at cloud.linode.com
- [ ] Create Ubuntu 22.04 LTS server (minimum 2GB RAM recommended)
- [ ] Configure SSH access
- [ ] Note down server IP address

### ✅ Domain & DNS
- [ ] Purchase domain name
- [ ] Point A record to Linode server IP
- [ ] Verify DNS propagation with `nslookup yourdomain.com`

### ✅ Database Setup
- [ ] Set up PostgreSQL database (Linode Managed Database recommended)
- [ ] Create database user and password
- [ ] Note connection details
- [ ] Test connectivity

### ✅ Email Service (AWS SES)
- [ ] Create AWS account
- [ ] Set up AWS SES in your region
- [ ] Verify your sending domain/email
- [ ] Create IAM user with SES permissions
- [ ] Note access key and secret key

### ✅ SSL Certificate Email
- [ ] Choose email for Let's Encrypt notifications
- [ ] Ensure email is accessible

## Deployment Steps

### 1. Server Preparation
```bash
# SSH into server
ssh root@your-server-ip

# Update system
apt update && apt upgrade -y

# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# Install Docker Compose
apt install docker-compose-plugin -y

# Install additional tools
apt install -y curl wget git nginx certbot python3-certbot-nginx ufw

# Configure firewall
ufw enable
ufw allow ssh
ufw allow 80/tcp
ufw allow 443/tcp
```

### 2. Application Deployment
```bash
# Clone repository
cd /opt
git clone https://github.com/yourusername/yapplr.git
cd yapplr/Yapplr.Api

# Configure environment
cp .env.example .env
nano .env  # Fill in your configuration

# Update nginx configuration
sed -i 's/your-domain.com/yourdomain.com/g' nginx.conf

# Deploy
./deploy.sh
```

### 3. SSL Certificate Setup
```bash
# Generate certificate
docker-compose -f docker-compose.prod.yml run --rm certbot

# Set up auto-renewal
crontab -e
# Add: 0 12 * * * /usr/bin/docker-compose -f /opt/yapplr/Yapplr.Api/docker-compose.prod.yml run --rm certbot renew --quiet
```

### 4. Verification
- [ ] Check API health: `curl https://yourdomain.com/health`
- [ ] Test API endpoints
- [ ] Verify SSL certificate
- [ ] Check logs: `docker-compose -f docker-compose.prod.yml logs`

## Post-Deployment

### ✅ Monitoring Setup
- [ ] Set up monitoring script: `./monitor.sh`
- [ ] Configure log rotation
- [ ] Set up backup schedule: `./backup.sh`

### ✅ Security
- [ ] Change default passwords
- [ ] Configure fail2ban (optional)
- [ ] Set up monitoring alerts
- [ ] Review firewall rules

### ✅ Backup Strategy
- [ ] Test backup script
- [ ] Set up automated backups
- [ ] Test restore procedure
- [ ] Configure off-site backup storage (optional)

## Environment Variables Checklist

Required in `.env` file:
- [ ] `PROD_DATABASE_CONNECTION_STRING` - PostgreSQL connection
- [ ] `PROD_JWT_SECRET_KEY` - 32+ character secret key
- [ ] `PROD_SENDGRID_API_KEY` - SendGrid API key
- [ ] `PROD_SENDGRID_FROM_EMAIL` - Verified sender email
- [ ] `PROD_SENDGRID_FROM_NAME` - Sender name
- [ ] `PROD_EMAIL_PROVIDER` - Set to `SendGrid`
- [ ] `PROD_FIREBASE_PROJECT_ID` - Firebase project
- [ ] `PROD_FIREBASE_SERVICE_ACCOUNT_KEY` - Firebase service account key
- [ ] `PROD_API_DOMAIN_NAME` - Your domain name
- [ ] `PROD_CERTBOT_DOMAIN` - Your domain name
- [ ] `PROD_CERTBOT_EMAIL` - Email for SSL notifications

## Useful Commands

### Container Management
```bash
# View running containers
docker-compose -f docker-compose.prod.yml ps

# View logs
docker-compose -f docker-compose.prod.yml logs -f yapplr-api

# Restart services
docker-compose -f docker-compose.prod.yml restart

# Update application
git pull origin main && ./deploy.sh
```

### Monitoring
```bash
# Run health check
./monitor.sh

# Check resource usage
docker stats

# View system resources
htop
```

### Backup & Restore
```bash
# Create backup
./backup.sh

# List backups
ls -la /opt/backups/yapplr/

# Restore database (example)
gunzip -c backup_file.sql.gz | psql -h host -U user -d database
```

## Troubleshooting

### Common Issues
1. **502 Bad Gateway**: API container not running
2. **Database connection failed**: Check connection string
3. **SSL certificate issues**: Verify domain DNS and port 80 access
4. **File upload issues**: Check uploads directory permissions

### Getting Help
1. Check application logs
2. Check system logs: `journalctl -u docker`
3. Verify all environment variables
4. Test individual components

## Cost Estimate (Monthly)
- Linode 2GB Server: ~$12
- Managed PostgreSQL: ~$15 (optional)
- Domain: ~$1 (annual cost divided by 12)
- **Total: ~$25-30/month**

## Success Criteria
- [ ] API responds to health checks
- [ ] HTTPS works with valid certificate
- [ ] Database connections successful
- [ ] Email functionality working
- [ ] File uploads working
- [ ] Monitoring and backups configured
