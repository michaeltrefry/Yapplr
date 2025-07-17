# Yapplr Logging Implementation Summary

## What Was Implemented

### 1. Grafana Loki Stack
- **Loki**: Log aggregation system for storing and indexing logs
- **Grafana**: Web UI for querying, visualizing, and creating dashboards
- **Promtail**: Log collection agent that scrapes Docker container logs

### 2. Structured Logging with Serilog
- Added Serilog packages to both API and VideoProcessor projects
- Configured structured JSON logging with multiple sinks:
  - Console output for development
  - File logging with daily rotation
  - Grafana Loki sink for centralized aggregation

### 3. Enhanced Logging Service
- Created `LoggingEnhancementService` for advanced logging patterns
- Added extension methods for business operations, user actions, security events
- Implemented logging scopes with correlation IDs and user context

### 4. Docker Integration
- Added logging services to all Docker Compose environments (local, staging, production)
- Configured persistent volumes for log storage
- Set up proper networking between services

## Files Added/Modified

### New Configuration Files
- `loki-config.yml` - Loki server configuration
- `promtail-config.yml` - Log collection configuration  
- `grafana-datasources.yml` - Grafana data source setup

### New Services
- `Yapplr.Api/Services/LoggingEnhancementService.cs` - Enhanced logging patterns

### Modified Files
- `Yapplr.Api/Yapplr.Api.csproj` - Added Serilog packages
- `Yapplr.VideoProcessor/Yapplr.VideoProcessor.csproj` - Added Serilog packages
- `Yapplr.Api/Program.cs` - Configured Serilog
- `Yapplr.VideoProcessor/Program.cs` - Configured Serilog
- `Yapplr.Api/appsettings.json` - Added logging configuration
- `Yapplr.Api/appsettings.Production.json` - Added production logging config
- `Yapplr.VideoProcessor/appsettings.json` - Added logging configuration
- `Yapplr.Api/Dockerfile` - Added logs directory
- `Yapplr.VideoProcessor/Dockerfile` - Added logs directory
- `docker-compose.prod.yml` - Added logging stack
- `docker-compose.stage.yml` - Added logging stack
- `docker-compose.local.yml` - Added logging stack
- `Yapplr.Api/Extensions/ServiceCollectionExtensions.cs` - Registered logging service
- `Yapplr.Api/Common/BaseService.cs` - Enhanced logging methods

### New Documentation
- `Documents/LOGGING_SETUP_GUIDE.md` - Complete usage guide
- `Documents/LOGGING_IMPLEMENTATION_SUMMARY.md` - This summary
- `start-logging.sh` - Startup script for logging stack

## Key Features

### 1. Multi-Environment Support
- **Local Development**: http://localhost:3000 (Grafana), http://localhost:3100 (Loki)
- **Staging**: https://stg-api.yapplr.com:3000, https://stg-api.yapplr.com:3100
- **Production**: https://api.yapplr.com:3000, https://api.yapplr.com:3100

### 2. Structured Logging Labels
- `app` - Application name (Yapplr.Api, Yapplr.VideoProcessor)
- `environment` - Development, Staging, Production
- `service` - Service identifier
- `level` - Log level (Debug, Information, Warning, Error, Fatal)
- `user_id` - User ID when available
- `request_id` - Request correlation ID
- `correlation_id` - Cross-service correlation

### 3. Log Storage
- **Local**: Docker volumes (temporary)
- **Staging**: Docker volumes (temporary)
- **Production**: Persistent storage at `/mnt/yapplr-prod-storage/`

### 4. Log Retention
- **File logs**: 7 days rotation
- **Loki storage**: Configurable (default: based on available storage)

## Getting Started

### 1. Start Logging Stack Only
```bash
./start-logging.sh local
```

### 2. Start Full Application with Logging
```bash
docker-compose -f docker-compose.local.yml up -d
```

### 3. Access Grafana
- Open http://localhost:3000
- Login: admin / admin123
- Go to Explore → Select Loki data source

### 4. Example Queries
```logql
# All API logs
{service="yapplr-api"}

# Error logs only
{service="yapplr-api", level="Error"}

# User-specific logs
{service="yapplr-api"} | json | user_id="123"

# Video processing logs
{service="yapplr-video-processor"}
```

## Enhanced Logging Usage

### In Service Classes
```csharp
// Business operation logging
LogOperation("CreatePost", new { userId, mediaCount });

// User action logging
LogUserAction(userId, "PostCreated", new { postId, mediaCount });

// Entity operation logging
LogEntityOperation("Post", postId, "Update", new { changes });

// Error logging
LogError(ex, "CreatePost", new { userId, mediaCount });
```

### With Logging Scopes
```csharp
using var userScope = _loggingService.CreateUserScope(user);
using var requestScope = _loggingService.CreateRequestScope();
using var operationScope = _loggingService.CreateOperationScope("CreatePost");

// All logs within this scope will include user, request, and operation context
_logger.LogInformation("Creating post with {MediaCount} files", mediaCount);
```

## Benefits

### 1. Centralized Logging
- All application logs in one place
- Easy correlation across services
- Historical log storage and analysis

### 2. Powerful Search & Filtering
- LogQL query language for complex searches
- Filter by user, time range, log level, service
- Real-time log streaming

### 3. Operational Insights
- Monitor application health and performance
- Track user behavior and system usage
- Debug issues across distributed services

### 4. Alerting & Dashboards
- Create custom dashboards for key metrics
- Set up alerts for error rates, performance issues
- Monitor business KPIs through logs

## Next Steps

1. **Test the Setup**: Start with local environment and verify logs appear
2. **Create Dashboards**: Build operational dashboards for key metrics
3. **Set Up Alerts**: Configure alerts for critical errors and performance issues
4. **Enhance Logging**: Add more structured logging throughout the application
5. **Performance Tuning**: Adjust log levels and retention based on usage

## Troubleshooting

### No Logs in Grafana
1. Check if services are running: `docker ps`
2. Check Promtail logs: `docker logs yapplr-promtail-1`
3. Verify Loki connectivity: `curl http://localhost:3100/ready`

### High Resource Usage
1. Reduce log levels in production
2. Adjust retention periods in `loki-config.yml`
3. Use log sampling for high-volume events

### Network Issues
1. Ensure all services are on the same Docker network
2. Check firewall settings for external access
3. Verify DNS resolution between containers
