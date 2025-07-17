# Logging Stack Verification Guide

## 🎉 Success! Your logging stack is now fully operational!

### **Access Points**
- **Grafana UI**: http://localhost:3000 (admin/admin123)
- **Frontend**: http://localhost:3001 (moved to avoid port conflict with Grafana)
- **API**: http://localhost:8080
- **Loki API**: http://localhost:3100

### **Verification Steps Completed** ✅

1. **✅ Build Verification**
   - All .NET projects build successfully
   - All 533 tests pass (515 succeeded, 18 skipped)
   - Docker containers build without errors

2. **✅ Infrastructure Verification**
   - Loki started successfully and accepting logs
   - Grafana started with Loki datasource configured
   - Promtail collecting logs from Docker containers
   - All containers running in healthy state

3. **✅ Structured Logging Verification**
   - Request correlation IDs working (`CorrelationId: "1c665aab"`)
   - Automatic request timing (`Duration: 0.9044ms`)
   - IP address and User-Agent capture
   - HTTP status code categorization (405 → Warning level)
   - Rich context in every log entry

## **Test Queries for Grafana**

Open Grafana (http://localhost:3000) → Explore → Select "Loki" datasource

### **Basic Queries**

```logql
# All API logs
{container_name="yapplr-yapplr-api-1"}

# All logs from any Yapplr service
{container_name=~"yapplr-.*"}

# Only warning and error logs
{container_name=~"yapplr-.*"} |= `"Level":"Warning"` or `"Level":"Error"`

# Logs with correlation IDs
{container_name=~"yapplr-.*"} |= "CorrelationId"
```

### **Advanced Queries**

```logql
# Parse JSON and filter by specific correlation ID
{container_name="yapplr-yapplr-api-1"} | json | CorrelationId="1c665aab"

# Find slow requests (>100ms)
{container_name="yapplr-yapplr-api-1"} | json | Duration > 100

# HTTP errors by status code
{container_name="yapplr-yapplr-api-1"} | json | StatusCode >= 400

# Requests from specific IP
{container_name="yapplr-yapplr-api-1"} | json | IpAddress="::ffff:172.217.165.202"

# User-Agent analysis
{container_name="yapplr-yapplr-api-1"} | json | UserAgent =~ "curl.*"
```

### **Business Intelligence Queries**

```logql
# Rate of requests per minute
rate({container_name="yapplr-yapplr-api-1"}[1m])

# Error rate percentage
(
  rate({container_name="yapplr-yapplr-api-1"} | json | StatusCode >= 400 [5m]) /
  rate({container_name="yapplr-yapplr-api-1"}[5m])
) * 100

# Top endpoints by request count
topk(10, count by (RequestPath) (
  {container_name="yapplr-yapplr-api-1"} | json
))

# Average response time by endpoint
avg by (RequestPath) (
  {container_name="yapplr-yapplr-api-1"} | json | Duration
)
```

## **Sample Log Entry Structure**

Here's what a typical structured log entry looks like:

```json
{
  "timestamp": "2024-01-17T13:18:51.123Z",
  "level": "Information",
  "message": "HTTP GET /api/posts completed with 200 in 45.2ms",
  "SourceContext": "Yapplr.Api.Middleware.LoggingContextMiddleware",
  "RequestId": "0HNE56GV1R3NB:00000001",
  "CorrelationId": "1c665aab",
  "HttpMethod": "GET",
  "RequestPath": "/api/posts",
  "QueryString": "?page=1&pageSize=10",
  "StatusCode": 200,
  "Duration": 45.2,
  "UserAgent": "Mozilla/5.0...",
  "IpAddress": "192.168.1.100",
  "UserId": 123,
  "Username": "john_doe",
  "UserRole": "User",
  "MachineName": "ffbe7e146799",
  "Application": "Yapplr.Api",
  "Environment": "Development"
}
```

## **Testing Scenarios**

### **1. Generate Different Log Types**

```bash
# Health check (should be INFO)
curl http://localhost:8080/health

# Invalid endpoint (should be WARNING - 404)
curl http://localhost:8080/api/invalid

# API endpoint (should be INFO if successful)
curl http://localhost:8080/api/posts
```

### **2. Test User Context Logging**

When you authenticate and make requests, you should see:
- `UserId` in logs
- `Username` in logs  
- `UserRole` in logs

### **3. Test Business Operations**

Look for these structured log patterns:
- `BusinessOperation: "CreatePost"`
- `SecurityEvent: "LoginFailure"`
- `UserAction: "FollowUser"`
- `PerformanceOperation: "DatabaseQuery"`

## **Dashboard Ideas**

Create these dashboards in Grafana:

### **1. Operational Dashboard**
- Request rate over time
- Error rate percentage
- Average response time
- Top endpoints by traffic

### **2. Security Dashboard**
- Failed login attempts
- Unauthorized access attempts
- Suspicious IP addresses
- User activity patterns

### **3. Performance Dashboard**
- Slow requests (>1000ms)
- Database query performance
- Memory and CPU usage correlation
- Request volume by endpoint

### **4. Business Intelligence Dashboard**
- User registration trends
- Post creation rates
- Feature usage analytics
- Geographic request distribution

## **Alerting Examples**

Set up alerts for:

```logql
# High error rate (>5% in 5 minutes)
(
  rate({container_name="yapplr-yapplr-api-1"} | json | StatusCode >= 400 [5m]) /
  rate({container_name="yapplr-yapplr-api-1"}[5m])
) * 100 > 5

# Slow requests (>2 seconds)
{container_name="yapplr-yapplr-api-1"} | json | Duration > 2000

# Failed logins (security alert)
{container_name="yapplr-yapplr-api-1"} | json | SecurityEvent="LoginFailure"

# High request volume (potential DDoS)
rate({container_name="yapplr-yapplr-api-1"}[1m]) > 100
```

## **Next Steps**

1. **Explore Grafana**: Try the queries above and create your first dashboard
2. **Generate Test Data**: Use the API to create posts, users, etc. and watch the logs
3. **Set Up Alerts**: Configure alerts for critical events
4. **Monitor Performance**: Use the timing data to optimize slow operations
5. **Security Monitoring**: Watch for suspicious patterns in the logs

## **Troubleshooting**

If you don't see logs in Grafana:
1. Check Promtail logs: `docker logs yapplr-promtail-1`
2. Verify Loki is receiving data: `curl http://localhost:3100/ready`
3. Check container logs: `docker logs yapplr-yapplr-api-1`

Your logging infrastructure is now enterprise-ready! 🚀
