# Staging Analytics Setup Guide

This guide covers deploying the analytics stack to your staging environment.

## 🎯 **What's Included in Staging**

### **Analytics Services**
- **InfluxDB**: Time-series analytics database
- **Prometheus**: Metrics collection and monitoring  
- **Grafana**: Analytics dashboards and visualization

### **Access URLs**
- **API**: https://stg-api.yapplr.com
- **Grafana**: https://stg-grafana.yapplr.com
- **RabbitMQ Management**: https://stg-rabbitmq.yapplr.com

## 🔧 **Required Environment Variables**

Add these to your GitHub Secrets for staging deployment:

### **InfluxDB Configuration**
```bash
STAGE_INFLUXDB_USER=yapplr
STAGE_INFLUXDB_PASSWORD=your-secure-password
STAGE_INFLUXDB_ORG=yapplr
STAGE_INFLUXDB_BUCKET=analytics
STAGE_INFLUXDB_TOKEN=your-secure-token-here
```

### **Grafana Configuration**
```bash
STAGE_GRAFANA_USER=admin
STAGE_GRAFANA_PASSWORD=your-secure-password
STAGE_GRAFANA_DOMAIN=stg-grafana.yapplr.com
```

## 🚀 **Deployment Steps**

### **1. Set Environment Variables**
Add the required secrets to your GitHub repository:
- Go to Settings → Secrets and variables → Actions
- Add each environment variable listed above

### **2. Update DNS Records**
Add DNS A records for the analytics services:
```
stg-grafana.yapplr.com → your-staging-server-ip
stg-rabbitmq.yapplr.com → your-staging-server-ip
```

### **3. Deploy to Staging**
```bash
# Deploy with the updated analytics stack
./deploy-stage.sh
```

### **4. SSL Certificate Setup**
The SSL certificate for `stg.yapplr.com` should cover the Grafana subdomain. If not, you may need to update your certificate to include `stg-grafana.yapplr.com`.

## 📊 **Post-Deployment Verification**

### **Check Service Health**
```bash
# SSH to your staging server
ssh your-staging-server

# Check all services are running
docker-compose -f docker-compose.stage.yml ps

# Check analytics services specifically
docker-compose -f docker-compose.stage.yml logs influxdb
docker-compose -f docker-compose.stage.yml logs prometheus
docker-compose -f docker-compose.stage.yml logs grafana
```

### **Test Analytics Endpoints**
```bash
# Check InfluxDB health
curl https://stg-api.yapplr.com/health

# Test Grafana access
curl -I https://stg-grafana.yapplr.com

# Check Prometheus metrics
curl https://stg-api.yapplr.com/metrics
```

## 🔍 **Access Your Analytics**

### **Grafana Dashboard**
1. Go to: https://stg-grafana.yapplr.com
2. Login with your configured credentials
3. Navigate to dashboards to view analytics

### **Data Sources**
The following data sources are automatically configured:
- **Prometheus**: HTTP metrics and system monitoring
- **InfluxDB**: User activities and content engagement analytics

## 🛠️ **Configuration Differences from Local**

### **Security**
- All services run behind nginx proxy
- No direct port exposure except through HTTPS
- Production-grade SSL configuration

### **Performance**
- Resource limits configured for staging environment
- Optimized retention policies (30 days for Prometheus)
- Memory limits to prevent resource exhaustion

### **Monitoring**
- RabbitMQ Management UI exposed for debugging
- Comprehensive health checks for all services
- Automatic restart policies

## 📈 **Analytics Features Available**

### **Real-time Metrics**
- HTTP request metrics via Prometheus
- Application performance monitoring
- System resource utilization

### **User Analytics**
- User activity tracking in InfluxDB
- Content engagement metrics
- Tag usage analytics
- Performance metrics

### **Dashboards**
- Pre-configured Grafana dashboards
- Real-time data visualization
- Historical trend analysis

## 🔧 **Troubleshooting**

### **If Grafana Won't Start**
```bash
# Check Grafana logs
docker-compose -f docker-compose.stage.yml logs grafana

# Restart Grafana service
docker-compose -f docker-compose.stage.yml restart grafana
```

### **If InfluxDB Connection Fails**
```bash
# Check InfluxDB health
docker-compose -f docker-compose.stage.yml exec influxdb curl http://localhost:8086/ping

# Verify environment variables
docker-compose -f docker-compose.stage.yml exec yapplr-api env | grep INFLUX
```

### **If SSL Certificate Issues**
```bash
# Check certificate validity
openssl s_client -connect stg-grafana.yapplr.com:443 -servername stg-grafana.yapplr.com

# Renew certificate if needed
certbot renew --dry-run
```

## 🎯 **Next Steps**

1. **Generate Analytics Data**: Use your staging application to create analytics data
2. **Configure Dashboards**: Customize Grafana dashboards for your needs
3. **Set Up Alerts**: Configure Grafana alerts for important metrics
4. **Monitor Performance**: Watch for improvements in analytics query speed

Your staging environment now has the same powerful analytics stack as your local development environment!
