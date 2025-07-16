# Self-Hosted Analytics Setup Guide

This branch (`feature/self-hosted-analytics`) implements a complete self-hosted analytics stack that disconnects metrics and analytics from your main application database.

## 🚀 Quick Start

### 1. Update Your Development Configuration

Copy the example configuration and update with your settings:

```bash
cp Yapplr.Api/appsettings.Development.example.json Yapplr.Api/appsettings.Development.json
```

Then edit `Yapplr.Api/appsettings.Development.json` with your actual credentials and settings.

**Important**: Make sure these analytics settings are included:

```json
{
  "InfluxDB": {
    "Url": "http://influxdb:8086",
    "Token": "yapplr-analytics-token-local-dev-only",
    "Organization": "yapplr",
    "Bucket": "analytics",
    "Enabled": true
  },
  "Analytics": {
    "EnableDualWrite": true
  }
}
```

### 2. Start the Analytics Stack

```bash
# Build and start all services including the new analytics stack
docker-compose -f docker-compose.local.yml up -d --build

# Check that all services are healthy
docker-compose -f docker-compose.local.yml ps
```

### 3. Verify the Setup

```bash
# Run the analytics test script
./analytics/test-analytics.sh
```

### 4. Access Your Dashboards

- **Grafana**: http://localhost:3001 (admin/yapplr123)
- **InfluxDB**: http://localhost:8086 (yapplr/yapplr123)
- **Prometheus**: http://localhost:9090

## 📊 What's New

### Services Added
- **InfluxDB** (port 8086) - Time-series database for analytics
- **Prometheus** (port 9090) - Metrics collection and monitoring
- **Grafana** (port 3001) - Visualization and dashboards

### Application Changes
- New `IExternalAnalyticsService` interface
- `InfluxAnalyticsService` implementation for InfluxDB
- Modified `AnalyticsService` with dual-write support
- Added InfluxDB client package

### Configuration Files
- `analytics/prometheus.yml` - Prometheus configuration
- `analytics/grafana/` - Grafana provisioning and dashboards
- `analytics/README.md` - Comprehensive documentation
- `analytics/test-analytics.sh` - Testing script

## 🔄 Migration Strategy

### Current State: Dual-Write Enabled
- Analytics data written to **both** PostgreSQL and InfluxDB
- Existing functionality unchanged
- Data validation possible

### Next Steps
1. **Validate Data**: Compare database vs InfluxDB analytics
2. **Update Dashboards**: Migrate to Grafana dashboards
3. **Test Thoroughly**: Ensure external analytics meets needs
4. **Switch to External-Only**: Set `EnableDualWrite=false`

## 🛠️ Troubleshooting

### Services Not Starting
```bash
# Check service logs
docker-compose -f docker-compose.local.yml logs influxdb
docker-compose -f docker-compose.local.yml logs grafana
docker-compose -f docker-compose.local.yml logs prometheus
```

### No Data in Dashboards
1. Verify dual-write is enabled in configuration
2. Generate some application activity
3. Check InfluxDB for data using the test script
4. Wait a few minutes for data to appear

### Port Conflicts
The analytics stack uses these ports:
- 8086 (InfluxDB)
- 9090 (Prometheus)  
- 3001 (Grafana)

Make sure these ports are available.

## 📈 Benefits

1. **Performance**: Removes analytics load from main database
2. **Scalability**: InfluxDB designed for high-volume time-series data
3. **Rich Visualization**: Grafana provides powerful dashboards
4. **Self-Hosted**: Complete control, no external dependencies
5. **Cost Effective**: No per-event pricing

## 📝 Documentation

- Full documentation: `analytics/README.md`
- Testing guide: `analytics/test-analytics.sh`
- Configuration examples: `Yapplr.Api/appsettings.Development.example.json`

## 🔗 Useful Links

- [InfluxDB Documentation](https://docs.influxdata.com/influxdb/v2.7/)
- [Grafana Documentation](https://grafana.com/docs/grafana/latest/)
- [Prometheus Documentation](https://prometheus.io/docs/)

---

**Ready to test?** Run `./analytics/test-analytics.sh` after starting the services!
