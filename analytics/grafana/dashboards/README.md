# Yapplr Grafana Dashboards

This directory contains comprehensive Grafana dashboards for visualizing your Yapplr analytics data from InfluxDB.

## 📊 Available Dashboards

### 1. **yapplr-comprehensive-analytics.json**
**Complete analytics overview with all key metrics**

**Panels:**
- **Overview Stats**: User activities, content engagements, tag actions, performance metrics
- **User Activities Over Time**: Time series of user activities by type
- **Content Engagement by Type**: Engagement trends (likes, comments, reposts, etc.)
- **Top User Activities**: Pie chart breakdown of activity types
- **Performance Metrics Over Time**: Response times and system performance
- **Tag Usage Trends**: Hashtag usage and trending analysis
- **System Health Metrics**: Response time and error rate monitoring
- **User Engagement Heatmap**: Activity patterns by time

**Best for**: General analytics overview, daily monitoring, trend analysis

### 2. **yapplr-admin-analytics.json**
**Admin-focused dashboard matching your new InfluxDB admin endpoints**

**Panels:**
- **User Growth Statistics**: New user registrations vs active users
- **Content Creation Stats**: Daily posts and comments creation
- **System Health Overview**: 24h active users and average response time
- **User Engagement Breakdown**: Pie chart of engagement types
- **Moderation Activity Trends**: Moderation actions over time

**Best for**: Admin dashboard, user growth analysis, content moderation oversight

### 3. **yapplr-realtime-monitoring.json**
**Real-time performance and system monitoring**

**Panels:**
- **Live User Activity**: Activity in last 5 minutes
- **Response Time**: Real-time response time monitoring
- **Error Rate**: Current error count
- **Active Sessions**: Number of active user sessions
- **Real-time Activity Stream**: Minute-by-minute activity
- **Performance Metrics**: Live performance monitoring
- **Recent Activity Log**: Latest user activities

**Best for**: Real-time monitoring, performance troubleshooting, live system health

### 4. **yapplr-analytics.json** (Original)
**Basic analytics dashboard**

**Best for**: Simple overview, getting started

## 🚀 **InfluxDB Queries Used**

### **User Activities**
```flux
from(bucket: "analytics")
  |> range(start: v.timeRangeStart, stop: v.timeRangeStop)
  |> filter(fn: (r) => r._measurement == "user_activities")
  |> filter(fn: (r) => r._field == "count")
  |> group(columns: ["activity_type"])
  |> aggregateWindow(every: v.windowPeriod, fn: sum, createEmpty: false)
```

### **Content Engagement**
```flux
from(bucket: "analytics")
  |> range(start: v.timeRangeStart, stop: v.timeRangeStop)
  |> filter(fn: (r) => r._measurement == "content_engagement")
  |> filter(fn: (r) => r._field == "count")
  |> group(columns: ["engagement_type"])
  |> aggregateWindow(every: v.windowPeriod, fn: sum, createEmpty: false)
```

### **Performance Metrics**
```flux
from(bucket: "analytics")
  |> range(start: v.timeRangeStart, stop: v.timeRangeStop)
  |> filter(fn: (r) => r._measurement == "performance_metrics")
  |> filter(fn: (r) => r._field == "value")
  |> group(columns: ["metric_type"])
  |> aggregateWindow(every: v.windowPeriod, fn: mean, createEmpty: false)
```

### **Tag Analytics**
```flux
from(bucket: "analytics")
  |> range(start: v.timeRangeStart, stop: v.timeRangeStop)
  |> filter(fn: (r) => r._measurement == "tag_actions")
  |> filter(fn: (r) => r._field == "count")
  |> group(columns: ["action"])
  |> aggregateWindow(every: v.windowPeriod, fn: sum, createEmpty: false)
```

## 🔧 **Setup Instructions**

### **1. Automatic Setup (Recommended)**
The dashboards are automatically provisioned when you start Grafana with Docker Compose:

```bash
docker-compose -f docker-compose.local.yml up -d
```

### **2. Manual Import**
If you need to import manually:

1. Open Grafana at http://localhost:3001
2. Login with admin/yapplr123
3. Go to **Dashboards** → **Import**
4. Upload the JSON files from this directory

### **3. Data Source Configuration**
Ensure your InfluxDB data source is configured:
- **URL**: http://influxdb:8086
- **Organization**: yapplr
- **Token**: yapplr-analytics-token-local-dev-only
- **Default Bucket**: analytics

## 📈 **Dashboard Features**

### **Time Range Controls**
- **Comprehensive Analytics**: Last 24 hours (good for daily overview)
- **Admin Analytics**: Last 7 days (good for weekly trends)
- **Real-time Monitoring**: Last 1 hour with 5-second refresh

### **Interactive Elements**
- **Zoom**: Click and drag on time series charts
- **Legend**: Click to show/hide series
- **Tooltip**: Hover for detailed values
- **Time Range**: Use time picker for custom ranges

### **Thresholds and Alerts**
- **Green**: Normal operation
- **Yellow**: Warning levels
- **Red**: Critical levels

## 🎯 **Customization**

### **Adding New Panels**
1. Edit dashboard in Grafana UI
2. Add new panel with InfluxDB query
3. Export JSON and save to this directory

### **Modifying Queries**
Update the Flux queries to match your specific analytics needs:
- Change measurement names
- Add new filters
- Modify aggregation windows
- Add new grouping columns

### **Custom Variables**
Add template variables for:
- Time ranges
- User segments
- Content types
- Tag categories

## 🔍 **Troubleshooting**

### **No Data Showing**
1. Check InfluxDB connection in data source settings
2. Verify analytics data is being written to InfluxDB
3. Check bucket name and organization settings
4. Ensure time range includes data

### **Query Errors**
1. Verify Flux query syntax
2. Check measurement and field names
3. Ensure proper data types
4. Test queries in InfluxDB UI

### **Performance Issues**
1. Reduce time range for large datasets
2. Increase aggregation window (e.g., 1h instead of 1m)
3. Add more specific filters
4. Consider data retention policies

## 📊 **Data Mapping**

Your analytics implementation writes data to these InfluxDB measurements:

- **user_activities**: Login, logout, post creation, etc.
- **content_engagement**: Likes, comments, reposts, views
- **tag_actions**: Hashtag usage, trending analysis
- **performance_metrics**: Response times, errors, system health

Each measurement includes relevant tags and fields for detailed analysis and visualization.

## 🎉 **Ready to Use!**

All dashboards are designed to work with your complete analytics implementation and provide comprehensive insights into your Yapplr application's usage and performance.
