# Nginx Subdomain Configuration

This document outlines the nginx configuration for all subdomains in staging and production environments.

## Staging Environment Subdomains

### Core Application
- **`stg-api.yapplr.com`** - Main API backend
  - Proxies to: `yapplr-api:8080`
  - Features: Rate limiting, SSL, health checks, SignalR support
  - Endpoints: `/api/*`, `/health`, `/swagger`, `/notificationHub`

- **`stg.yapplr.com`** - Frontend application
  - Proxies to: `yapplr-frontend:3000`
  - Features: SSL, WebSocket support for Next.js

### Analytics & Monitoring
- **`stg-logger.yapplr.com`** - Seq structured logging interface
  - Proxies to: `seq:80`
  - Features: SSL, log viewing and search capabilities
  - Access: Development team for debugging

- **`stg-grafana.yapplr.com`** - Grafana analytics dashboard
  - Proxies to: `grafana:3000`
  - Features: SSL, WebSocket support for live updates
  - Data sources: InfluxDB, Prometheus
  - Access: Admin credentials required

- **`stg-rabbitmq.yapplr.com`** - RabbitMQ management UI
  - Proxies to: `rabbitmq:15672`
  - Features: SSL, WebSocket support
  - Access: RabbitMQ admin credentials required
  - Purpose: Message queue monitoring and management

## Production Environment Subdomains

### Core Application
- **`api.yapplr.com`** - Main API backend
  - Proxies to: `yapplr-api:8080`
  - Features: Rate limiting, SSL, health checks, SignalR support
  - Endpoints: `/api/*`, `/health`, `/swagger`, `/notificationHub`

- **`yapplr.com`** / **`www.yapplr.com`** / **`app.yapplr.com`** - Frontend application
  - Proxies to: `yapplr-frontend:3000`
  - Features: SSL, WebSocket support for Next.js

### Analytics & Monitoring
- **`logger.yapplr.com`** - Seq structured logging interface
  - Proxies to: `seq:80`
  - Features: SSL, log viewing and search capabilities
  - Access: Development team for debugging

- **`grafana.yapplr.com`** - Grafana analytics dashboard
  - Proxies to: `grafana:3000`
  - Features: SSL, WebSocket support for live updates
  - Data sources: InfluxDB, Prometheus
  - Access: Admin credentials required

- **`rabbitmq.yapplr.com`** - RabbitMQ management UI
  - Proxies to: `rabbitmq:15672`
  - Features: SSL, WebSocket support
  - Access: RabbitMQ admin credentials required
  - Purpose: Message queue monitoring and management

## SSL Certificate Requirements

### Staging Certificate
The staging SSL certificate must cover:
- `stg.yapplr.com`
- `stg-api.yapplr.com`
- `stg-logger.yapplr.com`
- `stg-grafana.yapplr.com`
- `stg-rabbitmq.yapplr.com`

### Production Certificate
The production SSL certificate must cover:
- `yapplr.com`
- `www.yapplr.com`
- `app.yapplr.com`
- `api.yapplr.com`
- `logger.yapplr.com`
- `grafana.yapplr.com`
- `rabbitmq.yapplr.com`

**Recommendation**: Use wildcard certificates (`*.yapplr.com`) to cover all subdomains.

## Security Features

### Rate Limiting
- API endpoints: 10 requests/second with burst of 20
- Auth endpoints: 5 requests/second with burst of 10

### Security Headers
- `X-Frame-Options: DENY`
- `X-Content-Type-Options: nosniff`
- `X-XSS-Protection: 1; mode=block`
- `Strict-Transport-Security: max-age=63072000`
- `Referrer-Policy: strict-origin-when-cross-origin`

### Additional Security
- Gzip compression enabled
- HTTP to HTTPS redirect
- WebSocket support where needed
- Proper proxy headers for real IP forwarding

## Access Control

### Public Access
- Frontend applications (stg.yapplr.com, yapplr.com)
- API endpoints (with rate limiting)

### Restricted Access
- **Grafana**: Requires admin login credentials
- **RabbitMQ**: Requires RabbitMQ admin credentials
- **Seq Logging**: Should be restricted to development team IPs (optional)

### Optional Basic Auth
RabbitMQ and Seq can be configured with additional basic auth:
```nginx
auth_basic "Service Name";
auth_basic_user_file /etc/nginx/.htpasswd;
```

## DNS Configuration Required

Ensure the following DNS A records point to your servers:

### Staging Server
- `stg.yapplr.com` → Staging server IP
- `stg-api.yapplr.com` → Staging server IP
- `stg-logger.yapplr.com` → Staging server IP
- `stg-grafana.yapplr.com` → Staging server IP
- `stg-rabbitmq.yapplr.com` → Staging server IP

### Production Server
- `yapplr.com` → Production server IP
- `www.yapplr.com` → Production server IP
- `app.yapplr.com` → Production server IP
- `api.yapplr.com` → Production server IP
- `logger.yapplr.com` → Production server IP
- `grafana.yapplr.com` → Production server IP
- `rabbitmq.yapplr.com` → Production server IP

## Testing Subdomain Configuration

After deployment, test each subdomain:

```bash
# Core application
curl -I https://stg-api.yapplr.com/health
curl -I https://stg.yapplr.com

# Analytics & monitoring
curl -I https://stg-logger.yapplr.com
curl -I https://stg-grafana.yapplr.com
curl -I https://stg-rabbitmq.yapplr.com

# Production (replace stg- with production domains)
curl -I https://api.yapplr.com/health
curl -I https://yapplr.com
curl -I https://logger.yapplr.com
curl -I https://grafana.yapplr.com
curl -I https://rabbitmq.yapplr.com
```

## Troubleshooting

### Common Issues
1. **SSL Certificate Errors**: Ensure certificates cover all subdomains
2. **502 Bad Gateway**: Check if backend services are running
3. **Connection Refused**: Verify DNS records and firewall settings
4. **WebSocket Issues**: Ensure `Upgrade` and `Connection` headers are properly set

### Log Locations
- Nginx access logs: `/var/log/nginx/access.log`
- Nginx error logs: `/var/log/nginx/error.log`
- Container logs: `docker compose logs nginx`
