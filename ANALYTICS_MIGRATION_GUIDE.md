# Analytics Data Migration Guide

This guide shows you how to migrate your existing analytics data from PostgreSQL to InfluxDB, preserving all historical data while transitioning to the new analytics system.

## 🎯 **Yes! You Can Migrate All Your Existing Data**

The migration tool will transfer:
- ✅ **All User Activities** - Login, logout, registration, content creation, etc.
- ✅ **All Content Engagements** - Likes, comments, shares, views, etc.
- ✅ **All Tag Analytics** - Tag usage, trending data, suggestions, etc.
- ✅ **All Performance Metrics** - Response times, system metrics, etc.

## 🔧 **How Migration Works**

### **Data Mapping**
```
PostgreSQL Table → InfluxDB Measurement
─────────────────────────────────────────
UserActivities   → user_activities
ContentEngagements → content_engagement  
TagAnalytics     → tag_actions
PerformanceMetrics → performance_metrics
```

### **Data Preservation**
- **All timestamps preserved** with nanosecond precision
- **All metadata preserved** including JSON fields
- **All relationships maintained** through tags and fields
- **Batch processing** to handle large datasets efficiently

## 🚀 **Migration Methods**

### **Method 1: Automated Script (Recommended)**

```bash
# Set your admin token
export ADMIN_TOKEN="your-admin-jwt-token"

# Check if everything is ready
./analytics/migrate-data.sh check

# Migrate all data
./analytics/migrate-data.sh migrate

# Validate the migration
./analytics/migrate-data.sh validate
```

### **Method 2: API Endpoints**

```bash
# Check data source status
curl -H "Authorization: Bearer $ADMIN_TOKEN" \
  http://localhost:8080/api/admin/analytics/data-source

# Start migration
curl -X POST -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"batchSize": 1000}' \
  http://localhost:8080/api/admin/analytics/migrate

# Check migration status
curl -H "Authorization: Bearer $ADMIN_TOKEN" \
  http://localhost:8080/api/admin/analytics/migration/status

# Validate migration
curl -X POST -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{}' \
  http://localhost:8080/api/admin/analytics/migration/validate
```

### **Method 3: Admin Dashboard**

Access the admin panel at `/admin/analytics` and use the migration interface (if implemented).

## 📋 **Step-by-Step Migration Process**

### **Step 1: Prerequisites**

1. **Ensure InfluxDB is running**:
   ```bash
   docker-compose -f docker-compose.local.yml up -d influxdb
   curl http://localhost:8086/ping  # Should return 204
   ```

2. **Enable dual-write** (if not already enabled):
   ```json
   {
     "Analytics": {
       "EnableDualWrite": true
     }
   }
   ```

3. **Get admin JWT token** from your application

### **Step 2: Check System Status**

```bash
# Check if InfluxDB is available and configured
./analytics/migrate-data.sh check
```

Expected output:
```json
{
  "configured_source": "Database",
  "influx_available": true,
  "actual_source": "Database",
  "dual_write_enabled": true
}
```

### **Step 3: Run Migration**

#### **Full Migration**
```bash
# Migrate all historical data
./analytics/migrate-data.sh migrate
```

#### **Date-Range Migration**
```bash
# Migrate data for specific period
./analytics/migrate-data.sh migrate 2024-01-01 2024-12-31
```

#### **Table-Specific Migration**
```bash
# Migrate only user activities
./analytics/migrate-data.sh migrate-table user-activities

# Migrate only content engagements
./analytics/migrate-data.sh migrate-table content-engagements
```

### **Step 4: Validate Migration**

```bash
# Validate all migrated data
./analytics/migrate-data.sh validate
```

Expected output:
```json
{
  "isValid": true,
  "tableValidations": [
    {
      "tableName": "UserActivities",
      "postgreSqlCount": 15420,
      "influxDbCount": 15420,
      "difference": 0
    },
    {
      "tableName": "ContentEngagements", 
      "postgreSqlCount": 8932,
      "influxDbCount": 8932,
      "difference": 0
    }
  ]
}
```

### **Step 5: Switch to InfluxDB**

Once validation passes, switch your admin dashboard to use InfluxDB:

```json
{
  "Analytics": {
    "UseInfluxForAdminDashboard": true
  }
}
```

## ⚙️ **Migration Options**

### **Batch Size**
- **Default**: 1000 records per batch
- **Small datasets**: Use 500-1000
- **Large datasets**: Use 2000-5000
- **Memory constrained**: Use 100-500

### **Date Filtering**
```bash
# Migrate last 30 days only
./analytics/migrate-data.sh migrate $(date -d '30 days ago' '+%Y-%m-%d') $(date '+%Y-%m-%d')

# Migrate specific year
./analytics/migrate-data.sh migrate 2024-01-01 2024-12-31

# Migrate everything (default)
./analytics/migrate-data.sh migrate
```

### **Selective Migration**
```bash
# Migrate only user activities
./analytics/migrate-data.sh migrate-table user-activities

# Migrate only content engagements  
./analytics/migrate-data.sh migrate-table content-engagements
```

## 📊 **Migration Performance**

### **Expected Performance**
- **Small dataset** (< 10K records): 1-2 minutes
- **Medium dataset** (10K-100K records): 5-15 minutes  
- **Large dataset** (100K-1M records): 30-60 minutes
- **Very large dataset** (> 1M records): 1-3 hours

### **Performance Factors**
- **Batch size**: Larger batches = faster migration
- **Network latency**: Local InfluxDB = faster
- **Database load**: Migrate during low-traffic periods
- **System resources**: More RAM/CPU = faster processing

## 🛠️ **Troubleshooting**

### **Migration Fails**

1. **Check InfluxDB connectivity**:
   ```bash
   curl http://localhost:8086/ping
   ```

2. **Check disk space**:
   ```bash
   df -h  # Ensure sufficient space for InfluxDB
   ```

3. **Check logs**:
   ```bash
   docker-compose logs influxdb
   docker-compose logs yapplr-api
   ```

### **Validation Fails**

1. **Check for data inconsistencies**:
   ```bash
   ./analytics/migrate-data.sh validate
   ```

2. **Re-run migration for failed tables**:
   ```bash
   ./analytics/migrate-data.sh migrate-table user-activities
   ```

3. **Check InfluxDB data**:
   ```bash
   curl -X POST "http://localhost:8086/api/v2/query?org=yapplr" \
     -H "Authorization: Token yapplr-analytics-token-local-dev-only" \
     -H "Content-Type: application/vnd.flux" \
     -d 'from(bucket:"analytics") |> range(start:-30d) |> count()'
   ```

### **Performance Issues**

1. **Reduce batch size**:
   ```bash
   ./analytics/migrate-data.sh migrate "" "" 500
   ```

2. **Migrate during off-peak hours**

3. **Increase InfluxDB resources**:
   ```yaml
   influxdb:
     deploy:
       resources:
         limits:
           memory: 4G
           cpus: '2'
   ```

## 🔄 **Migration Strategies**

### **Strategy 1: Full Historical Migration**
- Migrate all data at once
- Best for: Small to medium datasets
- Downtime: Minimal (read-only during migration)

### **Strategy 2: Incremental Migration**
- Migrate data in date ranges
- Best for: Large datasets
- Downtime: None (can run during business hours)

### **Strategy 3: Table-by-Table Migration**
- Migrate one table at a time
- Best for: Troubleshooting or selective migration
- Downtime: None

## ✅ **Post-Migration Checklist**

- [ ] All tables migrated successfully
- [ ] Validation passed for all tables
- [ ] Admin dashboard switched to InfluxDB
- [ ] Grafana dashboards showing data
- [ ] Performance improved
- [ ] No errors in application logs
- [ ] Backup of original PostgreSQL data created

## 🎉 **Benefits After Migration**

- **10x faster analytics queries**
- **Real-time dashboard updates**
- **Reduced database load**
- **Better scalability for future growth**
- **Advanced time-series analytics capabilities**

Your historical analytics data will be preserved and available in the new high-performance InfluxDB system!
