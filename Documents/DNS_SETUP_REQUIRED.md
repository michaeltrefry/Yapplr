# DNS Setup Required for Subdomain Configuration

## Overview
The nginx configuration now includes subdomains for analytics and logging services. You'll need to configure DNS records to point these subdomains to your servers.

## Required DNS Records

### Staging Environment
Add these A records pointing to your **staging server IP**:

```
stg-logger.yapplr.com     → [STAGING_SERVER_IP]
stg-grafana.yapplr.com    → [STAGING_SERVER_IP]  
stg-rabbitmq.yapplr.com   → [STAGING_SERVER_IP]
```

### Production Environment
Add these A records pointing to your **production server IP**:

```
logger.yapplr.com         → [PRODUCTION_SERVER_IP]
grafana.yapplr.com        → [PRODUCTION_SERVER_IP]
rabbitmq.yapplr.com       → [PRODUCTION_SERVER_IP]
```

## SSL Certificate Updates

### Option 1: Wildcard Certificate (Recommended)
Request wildcard certificates that cover all subdomains:
- Staging: `*.yapplr.com` 
- Production: `*.yapplr.com`

### Option 2: Multi-Domain Certificate
Update your existing certificates to include the new subdomains:

**Staging Certificate should cover:**
- `stg.yapplr.com`
- `stg-api.yapplr.com`
- `stg-logger.yapplr.com`
- `stg-grafana.yapplr.com`
- `stg-rabbitmq.yapplr.com`

**Production Certificate should cover:**
- `yapplr.com`
- `www.yapplr.com`
- `app.yapplr.com`
- `api.yapplr.com`
- `logger.yapplr.com`
- `grafana.yapplr.com`
- `rabbitmq.yapplr.com`

## Let's Encrypt Configuration

If using Let's Encrypt, update your certificate requests:

### Staging
```bash
certbot certonly --webroot -w /var/www/certbot \
  -d stg.yapplr.com \
  -d stg-api.yapplr.com \
  -d stg-logger.yapplr.com \
  -d stg-grafana.yapplr.com \
  -d stg-rabbitmq.yapplr.com
```

### Production
```bash
certbot certonly --webroot -w /var/www/certbot \
  -d yapplr.com \
  -d www.yapplr.com \
  -d app.yapplr.com \
  -d api.yapplr.com \
  -d logger.yapplr.com \
  -d grafana.yapplr.com \
  -d rabbitmq.yapplr.com
```

## Testing DNS Configuration

After setting up DNS records, test resolution:

```bash
# Test staging subdomains
nslookup stg-logger.yapplr.com
nslookup stg-grafana.yapplr.com
nslookup stg-rabbitmq.yapplr.com

# Test production subdomains
nslookup logger.yapplr.com
nslookup grafana.yapplr.com
nslookup rabbitmq.yapplr.com
```

## Verification After Deployment

Once DNS and certificates are configured, test each subdomain:

### Staging
```bash
curl -I https://stg-logger.yapplr.com
curl -I https://stg-grafana.yapplr.com
curl -I https://stg-rabbitmq.yapplr.com
```

### Production
```bash
curl -I https://logger.yapplr.com
curl -I https://grafana.yapplr.com
curl -I https://rabbitmq.yapplr.com
```

## Access Information

After successful deployment, these services will be available at:

### Staging
- **Seq Logs**: https://stg-logger.yapplr.com
- **Grafana Analytics**: https://stg-grafana.yapplr.com
- **RabbitMQ Management**: https://stg-rabbitmq.yapplr.com

### Production
- **Seq Logs**: https://logger.yapplr.com
- **Grafana Analytics**: https://grafana.yapplr.com
- **RabbitMQ Management**: https://rabbitmq.yapplr.com

## Security Notes

1. **Grafana**: Requires admin login (configured via GitHub secrets)
2. **RabbitMQ**: Requires RabbitMQ admin credentials
3. **Seq**: Consider restricting access to development team IPs
4. **All services**: Protected by SSL and security headers

## Next Steps

1. ✅ Configure DNS records for new subdomains
2. ✅ Update SSL certificates to include new subdomains
3. ✅ Deploy the updated configuration
4. ✅ Test all subdomain access
5. ✅ Configure access credentials for team members
