# Deployment Verification Checklist

Use this checklist to verify that your deployment is working correctly after applying the analytics stack updates.

## Pre-Deployment Setup

### GitHub Secrets Configuration
- [ ] All required secrets added to GitHub repository (see GITHUB_SECRETS_REQUIRED.md)
- [ ] Staging secrets configured with `STAGE_` prefix
- [ ] Production secrets configured with `PROD_` prefix
- [ ] SSH keys and server access configured

### Server Preparation
- [ ] Staging server has sufficient disk space for analytics data
- [ ] Production server has sufficient disk space for analytics data
- [ ] Server firewall allows necessary ports (if applicable)

## Post-Deployment Verification

### Core Application Services
- [ ] API health check responds: `curl https://your-domain/health`
- [ ] Frontend loads correctly
- [ ] User registration/login works
- [ ] Database migrations completed successfully

### Infrastructure Services
- [ ] PostgreSQL database accessible
- [ ] Redis cache working
- [ ] RabbitMQ message queue operational
- [ ] Video processing service running

### Analytics Stack
- [ ] InfluxDB accessible and initialized
- [ ] Prometheus scraping metrics from API
- [ ] Grafana dashboard accessible
- [ ] Analytics data being written to InfluxDB

### Logging Stack
- [ ] Seq logging interface accessible
- [ ] Structured logs appearing in Seq
- [ ] Log correlation IDs working
- [ ] Error logs being captured

## Service URLs to Test

### Staging Environment
- [ ] API: `https://stg-api.yapplr.com/health`
- [ ] Frontend: `https://stg.yapplr.com`
- [ ] Grafana: `https://stg-grafana.yapplr.com` (if exposed)
- [ ] Seq Logs: `https://stg-api.yapplr.com:5341` (if exposed)

### Production Environment
- [ ] API: `https://api.yapplr.com/health`
- [ ] Frontend: `https://yapplr.com`
- [ ] Grafana: `https://grafana.yapplr.com` (if exposed)
- [ ] Seq Logs: Internal access only

## Analytics Verification

### InfluxDB Data
```bash
# Check if data is being written
curl -H "Authorization: Token YOUR_INFLUXDB_TOKEN" \
  "http://your-influxdb:8086/api/v2/query?org=yapplr" \
  --data-urlencode 'q=from(bucket:"analytics") |> range(start:-1h) |> limit(n:10)'
```

### Prometheus Metrics
```bash
# Check if API metrics are being scraped
curl http://your-prometheus:9090/api/v1/query?query=up
```

### Grafana Dashboards
- [ ] Login to Grafana with admin credentials
- [ ] Verify InfluxDB datasource connected
- [ ] Verify Prometheus datasource connected
- [ ] Check that dashboards show data

## Performance Verification

### Resource Usage
- [ ] CPU usage within acceptable limits
- [ ] Memory usage within acceptable limits
- [ ] Disk space sufficient for analytics data retention
- [ ] Network connectivity between services

### Analytics Performance
- [ ] InfluxDB queries respond quickly
- [ ] Grafana dashboards load within 5 seconds
- [ ] Prometheus scraping interval working
- [ ] No excessive log volume

## Troubleshooting Commands

### Check Service Status
```bash
# View all running containers
docker compose -f docker-compose.prod.yml ps

# Check specific service logs
docker compose -f docker-compose.prod.yml logs yapplr-api
docker compose -f docker-compose.prod.yml logs influxdb
docker compose -f docker-compose.prod.yml logs grafana
docker compose -f docker-compose.prod.yml logs prometheus
docker compose -f docker-compose.prod.yml logs seq
```

### Check Service Health
```bash
# API health
curl -f https://your-domain/health

# InfluxDB health
curl -f http://your-influxdb:8086/ping

# Prometheus health
curl -f http://your-prometheus:9090/-/healthy

# Grafana health
curl -f http://your-grafana:3000/api/health
```

### Check Data Flow
```bash
# Check if metrics endpoint is working
curl https://your-domain/metrics

# Check if analytics data is being written
# (requires InfluxDB token and proper query)
```

## Common Issues and Solutions

### InfluxDB Not Starting
- Check if token and credentials are properly set
- Verify storage directory permissions
- Check if initialization completed successfully

### Grafana Can't Connect to Datasources
- Verify InfluxDB token in datasource configuration
- Check network connectivity between services
- Ensure InfluxDB is fully initialized before Grafana starts

### Prometheus Not Scraping Metrics
- Verify API `/metrics` endpoint is accessible
- Check Prometheus configuration file
- Ensure API service is running and healthy

### Seq Not Receiving Logs
- Check if Seq URL is correctly configured in API
- Verify network connectivity to Seq service
- Check API logging configuration

## Success Criteria

Deployment is considered successful when:
- [ ] All core application features work
- [ ] All analytics services are running and healthy
- [ ] Data is flowing through the analytics pipeline
- [ ] Logs are being captured and structured properly
- [ ] Performance is within acceptable parameters
- [ ] No critical errors in any service logs

## Rollback Plan

If issues are encountered:
1. Check service logs for specific errors
2. Verify all secrets are correctly configured
3. If analytics services are causing issues, they can be temporarily disabled
4. Core application should continue working even if analytics fails
5. Contact development team if issues persist

## Monitoring Setup

After successful deployment:
- [ ] Set up alerts for service health
- [ ] Configure log retention policies
- [ ] Set up automated backups for analytics data
- [ ] Document access credentials and procedures
