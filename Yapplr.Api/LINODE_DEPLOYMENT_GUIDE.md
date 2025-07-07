# Yapplr API Deployment Guide for Linode

This guide will walk you through deploying the Yapplr API to Linode using Docker containers.

## Prerequisites

1. **Linode Account**: Sign up at [cloud.linode.com](https://cloud.linode.com)
2. **Domain Name**: You'll need a domain name for SSL certificates
3. **Database**: Set up a PostgreSQL database (can use Linode's managed database)
4. **AWS SES**: Configure AWS Simple Email Service for email functionality

## Step 1: Create a Linode Instance

1. Log into your Linode account
2. Click "Create Linode"
3. Choose:
   - **Distribution**: Ubuntu 22.04 LTS
   - **Region**: Choose closest to your users
   - **Plan**: Shared CPU - Nanode 1GB (minimum) or Linode 2GB (recommended)
4. Set a root password
5. Add your SSH key (recommended)
6. Click "Create Linode"

## Step 2: Initial Server Setup

SSH into your server:
```bash
ssh root@your-server-ip
```

Update the system:
```bash
apt update && apt upgrade -y
```

Install Docker and Docker Compose:
```bash
# Install Docker
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

# Install Docker Compose
apt install docker-compose-plugin -y

# Start Docker service
systemctl start docker
systemctl enable docker
```

Install additional tools:
```bash
apt install -y curl wget git nginx certbot python3-certbot-nginx ufw
```

## Step 3: Configure Firewall

```bash
# Enable UFW
ufw enable

# Allow SSH, HTTP, and HTTPS
ufw allow ssh
ufw allow 80/tcp
ufw allow 443/tcp

# Check status
ufw status
```

## Step 4: Set Up Domain and DNS

1. Point your domain's A record to your Linode's IP address
2. Wait for DNS propagation (can take up to 24 hours)
3. Verify with: `nslookup yourdomain.com`

## Step 5: Deploy the Application

Clone your repository:
```bash
cd /opt
git clone https://github.com/yourusername/yapplr.git
cd yapplr/Yapplr.Api
```

Create environment file:
```bash
cp .env.example .env
nano .env
```

Configure your `.env` file with:
```env
PROD_DATABASE_CONNECTION_STRING=Host=your-db-host;Port=5432;Database=yapplr_db;Username=your-username;Password=your-password
PROD_JWT_SECRET_KEY=your-super-secret-jwt-key-that-should-be-at-least-32-characters-long
PROD_SENDGRID_API_KEY=your-sendgrid-api-key
PROD_SENDGRID_FROM_EMAIL=noreply@yourdomain.com
PROD_SENDGRID_FROM_NAME=Your Name
PROD_EMAIL_PROVIDER=SendGrid
PROD_FIREBASE_PROJECT_ID=your-firebase-project-id
PROD_FIREBASE_SERVICE_ACCOUNT_KEY={"type":"service_account","project_id":"your-project-id",...}
PROD_API_DOMAIN_NAME=yourdomain.com
PROD_CERTBOT_DOMAIN=yourdomain.com
PROD_CERTBOT_EMAIL=admin@yourdomain.com
```

Update nginx.conf with your domain:
```bash
sed -i 's/your-domain.com/yourdomain.com/g' nginx.conf
```

Run the deployment script:
```bash
./deploy.sh
```

## Step 6: Set Up SSL Certificate

Generate SSL certificate:
```bash
docker-compose -f docker-compose.prod.yml run --rm certbot
```

Set up automatic renewal:
```bash
# Add to crontab
crontab -e

# Add this line for automatic renewal
0 12 * * * /usr/bin/docker-compose -f /opt/yapplr/Yapplr.Api/docker-compose.prod.yml run --rm certbot renew --quiet
```

## Step 7: Database Setup

If using Linode's managed database:
1. Create a PostgreSQL database in Linode
2. Note the connection details
3. Update your `.env` file with the connection string

If setting up your own database:
```bash
# Run PostgreSQL container
docker run -d \
  --name postgres \
  -e POSTGRES_DB=yapplr_db \
  -e POSTGRES_USER=yapplr \
  -e POSTGRES_PASSWORD=your-secure-password \
  -v postgres_data:/var/lib/postgresql/data \
  -p 5432:5432 \
  postgres:16-alpine
```

## Step 8: Monitoring and Maintenance

### View logs:
```bash
# API logs
docker-compose -f docker-compose.prod.yml logs -f yapplr-api

# Nginx logs
docker-compose -f docker-compose.prod.yml logs -f nginx
```

### Update the application:
```bash
cd /opt/yapplr/Yapplr.Api
git pull origin main
./deploy.sh
```

### Backup database:
```bash
# If using managed database, use Linode's backup features
# If using container database:
docker exec postgres pg_dump -U yapplr yapplr_db > backup_$(date +%Y%m%d_%H%M%S).sql
```

## Troubleshooting

### Common Issues:

1. **502 Bad Gateway**: Check if the API container is running
   ```bash
   docker-compose -f docker-compose.prod.yml ps
   ```

2. **Database connection issues**: Verify connection string and database accessibility

3. **SSL certificate issues**: Ensure domain points to server and port 80 is accessible

4. **File upload issues**: Check if uploads directory has proper permissions
   ```bash
   docker-compose -f docker-compose.prod.yml exec yapplr-api ls -la /app/uploads
   ```

### Health Checks:
```bash
# Check API health
curl https://yourdomain.com/health

# Check container status
docker-compose -f docker-compose.prod.yml ps

# Check resource usage
docker stats
```

## Security Considerations

1. **Regular Updates**: Keep your system and containers updated
2. **Firewall**: Only open necessary ports
3. **SSL**: Always use HTTPS in production
4. **Secrets**: Never commit secrets to version control
5. **Backups**: Regular database and file backups
6. **Monitoring**: Set up monitoring and alerting

## Cost Optimization

- **Linode 2GB**: ~$12/month (recommended for production)
- **Managed Database**: ~$15/month (optional, for better reliability)
- **Domain**: ~$10-15/year
- **Total**: ~$25-30/month for a production setup

## Support

For issues with this deployment:
1. Check the logs first
2. Verify all environment variables are set correctly
3. Ensure your domain DNS is properly configured
4. Check Linode's status page for any service issues
