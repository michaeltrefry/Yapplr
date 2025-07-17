# Yapplr Complete Analytics Stack

This directory contains the configuration for a **complete self-hosted analytics solution** that provides comprehensive metrics, monitoring, and data migration capabilities.

## 🏗️ Architecture

- **InfluxDB**: Time-series database for storing analytics data
- **Prometheus**: Metrics collection and monitoring
- **Grafana**: Visualization and dashboards
- **Dual-Write Pattern**: Writes to both database and external analytics
- **Admin Analytics**: InfluxDB-powered admin dashboard analytics
- **Data Migration**: Tools for migrating existing analytics data
- **Health Monitoring**: Comprehensive system health checks

## ✅ **IMPLEMENTATION STATUS: COMPLETE**

All analytics components are now fully implemented and functional:

### ✅ **Core Services**
- ✅ `InfluxAnalyticsService` - External analytics data collection
- ✅ `InfluxAdminAnalyticsService` - Admin dashboard analytics from InfluxDB
- ✅ `AnalyticsMigrationService` - Data migration utilities
- ✅ `NoOpAnalyticsService` - Fallback when InfluxDB disabled

### ✅ **API Endpoints**
- ✅ Admin analytics endpoints (`/api/admin/analytics/*`)
- ✅ Migration endpoints (`/api/admin/analytics/migrate/*`)
- ✅ Health check endpoints (`/api/admin/analytics/health`)
- ✅ Metrics endpoints (`/api/metrics/*`)
- ✅ Tag analytics endpoints (`/api/tags/*/analytics`)

### ✅ **Data Collection**
- ✅ User activity tracking
- ✅ Content engagement metrics
- ✅ Tag usage analytics
- ✅ Performance metrics
- ✅ System health monitoring

### ✅ **Infrastructure**
- ✅ Docker configuration for all services
- ✅ Grafana dashboards and data sources
- ✅ Prometheus metrics collection
- ✅ Environment-specific configurations

## 🚀 Quick Start

### 1. Start the Complete Analytics Stack

```bash
# Start all services including the complete analytics stack
docker-compose -f docker-compose.local.yml up -d --build

# Check that all services are healthy
docker-compose -f docker-compose.local.yml ps

# Verify analytics services are running
./analytics/test-complete-analytics.sh
```

### 2. Access Your Analytics Dashboards

- **Grafana**: http://localhost:3001
  - Username: `admin`
  - Password: `yapplr123`
- **InfluxDB**: http://localhost:8086
  - Username: `yapplr`
  - Password: `yapplr123`
  - Organization: `yapplr`
- **Prometheus**: http://localhost:9090

### 3. Test All Analytics Features

```bash
# Run the comprehensive analytics test
./analytics/test-complete-analytics.sh

# Test specific components
./analytics/test-analytics.sh  # Basic analytics test
```

### 4. Access Admin Analytics (Requires Authentication)

The complete analytics implementation includes powerful admin endpoints:

```bash
# Get admin token first, then access:
# GET /api/admin/analytics/data-source      - Data source information
# GET /api/admin/analytics/health           - Comprehensive health check
# GET /api/admin/analytics/user-growth-influx?days=30  - User growth from InfluxDB
# GET /api/admin/analytics/migration/status - Migration status
# POST /api/admin/analytics/migrate         - Migrate all data to InfluxDB
```

### 5. Explore Your Analytics Dashboards

We've created **4 comprehensive Grafana dashboards** for your InfluxDB data:

#### **📊 Available Dashboards**
1. **Yapplr Comprehensive Analytics** - Complete overview with all metrics
2. **Yapplr Admin Analytics** - Admin-focused dashboard matching your API endpoints
3. **Yapplr Real-time Monitoring** - Live performance and system monitoring
4. **Yapplr Analytics** - Original basic dashboard

#### **🎨 Test Your Dashboards**
```bash
# Test all dashboards and validate setup
./analytics/test-dashboards.sh
```

#### **🔧 Fix Empty Dashboards**
If your dashboards are empty, it's likely because InfluxDB is disabled:

```bash
# Enable InfluxDB in your application configuration
./analytics/enable-influxdb.sh

# Debug data issues
./analytics/debug-influxdb-data.sh

# Follow the complete fix guide
cat analytics/FIX_EMPTY_DASHBOARDS.md
```

#### **🔍 Dashboard Features**
- **Real-time data** from InfluxDB with Flux queries
- **User activity tracking** - logins, posts, comments, engagement
- **Performance monitoring** - response times, errors, system health
- **Content analytics** - engagement trends, tag usage
- **Admin insights** - user growth, moderation trends
- **Interactive visualizations** - time series, pie charts, heatmaps

### 6. Verify Analytics Data

1. Use your application to generate some activity (login, create posts, etc.)
2. Check InfluxDB for data:
   ```bash
   # Query user activities
   curl -X POST "http://localhost:8086/api/v2/query?org=yapplr" \
     -H "Authorization: Token yapplr-analytics-token-local-dev-only" \
     -H "Content-Type: application/vnd.flux" \
     -d 'from(bucket:"analytics") |> range(start:-1h) |> filter(fn:(r) => r._measurement == "user_activities")'
   ```
3. **View your dashboards** at http://localhost:3001 (admin/yapplr123)
4. Access admin analytics through the API endpoints

## 🆕 **NEW FEATURES COMPLETED**

### **Admin Analytics Service**
- **InfluxAdminAnalyticsService**: Query analytics data directly from InfluxDB for admin dashboards
- **Real-time analytics**: Get user growth, content stats, and system health from time-series data
- **Performance optimized**: Bypass database for analytics queries

### **Data Migration Service**
- **AnalyticsMigrationService**: Migrate existing database analytics to InfluxDB
- **Batch processing**: Configurable batch sizes for large data migrations
- **Progress tracking**: Real-time migration status and progress monitoring
- **Data validation**: Verify data integrity after migration

### **Enhanced API Endpoints**
- **Admin analytics endpoints**: `/api/admin/analytics/*` for comprehensive analytics
- **Migration endpoints**: `/api/admin/analytics/migrate/*` for data migration
- **Health monitoring**: `/api/admin/analytics/health` for system status
- **InfluxDB-powered analytics**: Separate endpoints for database vs InfluxDB analytics

### **Comprehensive Testing**
- **test-complete-analytics.sh**: Test all analytics components
- **Health checks**: Verify all services are working correctly
- **API validation**: Test all endpoints and authentication

## Configuration

### Environment Variables

The following environment variables control the analytics behavior:

```bash
# InfluxDB Configuration
InfluxDB__Url=http://influxdb:8086
InfluxDB__Token=yapplr-analytics-token-local-dev-only
InfluxDB__Organization=yapplr
InfluxDB__Bucket=analytics
InfluxDB__Enabled=true

# Analytics Configuration
Analytics__EnableDualWrite=true  # Write to both DB and InfluxDB
```

### Dual-Write Pattern

Currently configured to write analytics data to both:
1. **PostgreSQL Database** (existing behavior)
2. **InfluxDB** (new external analytics)

This allows you to:
- Validate external analytics data against database data
- Gradually migrate dashboards and reports
- Safely switch to external-only analytics

### Migration Strategy

1. **Phase 1** (Current): Dual-write enabled
   - All analytics written to both database and InfluxDB
   - Existing functionality unchanged
   - Validate data consistency

2. **Phase 2**: Update reports and dashboards
   - Migrate Grafana dashboards to use InfluxDB
   - Update any analytics queries in your application
   - Test external analytics thoroughly

3. **Phase 3**: External-only analytics
   - Set `Analytics__EnableDualWrite=false`
   - Remove database analytics writes
   - Archive old analytics tables

## Data Models

### InfluxDB Measurements

- **user_activities**: User behavior tracking
- **content_engagement**: Content interaction metrics
- **tag_actions**: Tag usage analytics
- **performance_metrics**: System performance data
- **events**: Custom event tracking
- **metrics**: Custom numeric metrics

### Example Queries

```flux
// User activities in the last hour
from(bucket: "analytics")
  |> range(start: -1h)
  |> filter(fn: (r) => r._measurement == "user_activities")
  |> group(columns: ["activity_type"])
  |> count()

// Average response time by endpoint
from(bucket: "analytics")
  |> range(start: -24h)
  |> filter(fn: (r) => r._measurement == "performance_metrics")
  |> filter(fn: (r) => r.metric_type == "ResponseTime")
  |> group(columns: ["operation"])
  |> mean()

// Content engagement trends
from(bucket: "analytics")
  |> range(start: -7d)
  |> filter(fn: (r) => r._measurement == "content_engagement")
  |> aggregateWindow(every: 1h, fn: count)
```

## Monitoring and Alerting

### Prometheus Metrics

The stack automatically collects:
- Application metrics from `/metrics` endpoint
- System metrics (if node_exporter is added)
- Database metrics (if postgres_exporter is added)

### Grafana Alerts

You can configure alerts in Grafana for:
- High error rates
- Slow response times
- Low user engagement
- System resource usage

## Scaling and Production

### For Production Use:

1. **Security**:
   - Change default passwords
   - Use proper authentication tokens
   - Enable HTTPS
   - Restrict network access

2. **Performance**:
   - Configure InfluxDB retention policies
   - Set up data downsampling
   - Monitor disk usage
   - Configure backup strategies

3. **High Availability**:
   - Use InfluxDB clustering
   - Set up Grafana load balancing
   - Configure Prometheus federation

### Resource Requirements

- **InfluxDB**: 2GB RAM, 50GB storage (minimum)
- **Grafana**: 512MB RAM, 1GB storage
- **Prometheus**: 1GB RAM, 10GB storage

## Troubleshooting

### Common Issues

1. **InfluxDB connection failed**:
   ```bash
   # Check InfluxDB health
   curl http://localhost:8086/ping
   
   # Check logs
   docker-compose logs influxdb
   ```

2. **No data in Grafana**:
   - Verify InfluxDB datasource configuration
   - Check that dual-write is enabled
   - Generate some application activity

3. **High memory usage**:
   - Configure InfluxDB retention policies
   - Reduce Prometheus scrape frequency
   - Monitor query performance

### Useful Commands

```bash
# View analytics service logs
docker-compose logs yapplr-api | grep -i analytics

# Check InfluxDB buckets
docker exec -it yapplr_influxdb_1 influx bucket list

# Restart analytics stack
docker-compose restart influxdb prometheus grafana
```

## Next Steps

1. **Customize Dashboards**: Modify the Grafana dashboard to show your specific metrics
2. **Add Alerts**: Configure Grafana alerts for important metrics
3. **Optimize Queries**: Tune InfluxDB queries for better performance
4. **Plan Migration**: Decide when to disable dual-write and remove database analytics
