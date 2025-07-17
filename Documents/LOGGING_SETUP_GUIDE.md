# Yapplr Logging Setup Guide

## Overview

Yapplr now includes a comprehensive logging stack using **Grafana Loki** for log aggregation and **Grafana** for log visualization. This setup provides:

- **Centralized logging** from all services (API, Video Processor, etc.)
- **Structured logging** with JSON format for better searchability
- **Real-time log streaming** and historical log storage
- **Powerful filtering and search** capabilities through LogQL
- **Beautiful dashboards** and alerting through Grafana

## Architecture

The logging stack consists of:

1. **Serilog** - Structured logging library in .NET applications
2. **Promtail** - Log collection agent that scrapes Docker container logs
3. **Loki** - Log aggregation system that stores and indexes logs
4. **Grafana** - Web UI for querying, visualizing, and alerting on logs

## Accessing the Logging UI

### Local Development
- **Grafana UI**: http://localhost:3000
- **Loki API**: http://localhost:3100
- **Default credentials**: admin / admin123

### Staging Environment
- **Grafana UI**: https://stg-api.yapplr.com:3000
- **Loki API**: https://stg-api.yapplr.com:3100
- **Credentials**: admin / ${STAGE_GRAFANA_ADMIN_PASSWORD}

### Production Environment
- **Grafana UI**: https://api.yapplr.com:3000
- **Loki API**: https://api.yapplr.com:3100
- **Credentials**: admin / ${PROD_GRAFANA_ADMIN_PASSWORD}

## Using the Log Viewer

### Basic Log Exploration

1. Open Grafana in your browser
2. Go to **Explore** (compass icon in sidebar)
3. Select **Loki** as the data source
4. Use the log browser or write LogQL queries

### Common LogQL Queries

#### View all logs from a specific service:
```logql
{service="yapplr-api"}
```

#### Filter by log level:
```logql
{service="yapplr-api"} |= "ERROR"
```

#### Search for specific text:
```logql
{service="yapplr-api"} |= "user login"
```

#### Filter by time range and level:
```logql
{service="yapplr-api", level="Error"} [5m]
```

#### Search for exceptions:
```logql
{service="yapplr-api"} |= "Exception"
```

#### Filter by user ID (if logged):
```logql
{service="yapplr-api"} | json | user_id="12345"
```

#### Video processing logs:
```logql
{service="yapplr-video-processor"}
```

#### Rate of errors per minute:
```logql
rate({service="yapplr-api", level="Error"}[1m])
```

### Advanced Filtering

#### Multiple conditions:
```logql
{service="yapplr-api"} |= "ERROR" |= "database"
```

#### Exclude certain logs:
```logql
{service="yapplr-api"} != "health check"
```

#### JSON field extraction:
```logql
{service="yapplr-api"} | json | request_id="abc-123"
```

## Log Structure

### .NET Application Logs

The applications use Serilog with structured logging. Each log entry includes:

- **Timestamp**: When the log was created
- **Level**: Debug, Information, Warning, Error, Fatal
- **Message**: The log message
- **Properties**: Additional structured data
- **Exception**: Stack trace if an exception occurred

### Labels Available

- **app**: Application name (Yapplr.Api, Yapplr.VideoProcessor)
- **environment**: Development, Staging, Production
- **service**: Service identifier
- **container_name**: Docker container name
- **level**: Log level
- **request_id**: Request correlation ID (if available)
- **user_id**: User ID (if available)

## Creating Dashboards

1. In Grafana, go to **Dashboards** → **New Dashboard**
2. Add a new panel
3. Select **Loki** as data source
4. Write your LogQL query
5. Choose visualization type (Logs, Time series, etc.)
6. Save the dashboard

### Example Dashboard Panels

#### Error Rate Over Time:
```logql
sum(rate({service="yapplr-api", level="Error"}[5m])) by (service)
```

#### Top Error Messages:
```logql
topk(10, count by (message) ({service="yapplr-api", level="Error"}))
```

## Log Retention

- **Local Development**: Logs are kept until containers are removed
- **Staging**: 7 days retention
- **Production**: 30 days retention (configurable in loki-config.yml)

## Troubleshooting

### No logs appearing in Grafana

1. Check if Loki is running: `docker ps | grep loki`
2. Check Promtail logs: `docker logs yapplr-promtail-1`
3. Verify Loki is receiving logs: `curl http://localhost:3100/ready`

### Application not sending logs to Loki

1. Check application logs for Serilog errors
2. Verify Loki URL in configuration
3. Check network connectivity between containers

### Performance Issues

1. Reduce log level in production (Warning or Error only)
2. Limit log retention period
3. Use log sampling for high-volume applications

## Configuration Files

- **loki-config.yml**: Loki server configuration
- **promtail-config.yml**: Log collection configuration
- **grafana-datasources.yml**: Grafana data source setup
- **appsettings.json**: Application logging configuration

## Environment Variables

### Grafana
- `PROD_GRAFANA_ADMIN_PASSWORD`: Admin password for Grafana UI

### Loki
- `Logging:Loki:Url`: Loki server URL for applications

## Best Practices

1. **Use structured logging** with meaningful properties
2. **Include correlation IDs** for request tracing
3. **Set appropriate log levels** (avoid Debug in production)
4. **Use consistent property names** across services
5. **Include user context** when available
6. **Log business events** not just technical events
7. **Use log sampling** for high-frequency events

## Example Structured Log

```csharp
logger.LogInformation("User {UserId} created post {PostId} with {MediaCount} media files", 
    userId, postId, mediaFiles.Count);
```

This creates a searchable log entry with structured properties that can be filtered and aggregated in Grafana.
