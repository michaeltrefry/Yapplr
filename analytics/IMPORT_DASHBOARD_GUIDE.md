# How to Import Grafana Dashboards - Step by Step

## 🎯 **Quick Fix for Empty Dashboards**

Your dashboards were empty because of JSON format issues. I've fixed them! Here's how to import them correctly.

## 📊 **Start with the Test Dashboard**

Use this dashboard first to verify everything works:
**File**: `analytics/grafana/dashboards/yapplr-test-working.json`

## 🚀 **Step-by-Step Import Process**

### **Step 1: Access Grafana**
1. Open your browser to http://localhost:3001
2. Login with:
   - **Username**: admin
   - **Password**: yapplr123

### **Step 2: Import Dashboard**
1. Click **"+"** in the left sidebar
2. Select **"Import"**
3. Click **"Upload JSON file"**
4. Select `analytics/grafana/dashboards/yapplr-test-working.json`
5. Click **"Load"**

### **Step 3: Configure Data Source**
When the import screen appears:

1. **Dashboard name**: Should show "Yapplr Analytics Test Dashboard"
2. **Data source dropdown**: Select **"InfluxDB"** (should be marked as default ⭐)
3. **Important**: Make sure InfluxDB is selected for all panels
4. Click **"Import"**

### **Step 4: Verify Dashboard Works**
You should see:
- ✅ **"Total User Activities (24h)"** panel (should show a number)
- ✅ **"User Activities Over Time"** chart
- ✅ **"Recent Analytics Data (Raw)"** table

### **Step 5: Troubleshoot if Empty**

#### **If panels show "No data"**:

1. **Check time range**: Set to "Last 6 hours" or "Last 24 hours"
2. **Check data source**: Each panel should use "InfluxDB"
3. **Generate test data**: Use your application (login, create posts, etc.)
4. **Check InfluxDB**: Run `./analytics/debug-influxdb-data.sh`

#### **If panels show "Query error"**:

1. **Check data source configuration**:
   - Go to **Configuration** → **Data Sources** → **InfluxDB**
   - **URL**: `http://influxdb:8086`
   - **Organization**: `yapplr`
   - **Token**: `yapplr-analytics-token-local-dev-only`
   - **Default Bucket**: `analytics`
   - Click **"Save & Test"** - should show green checkmark

2. **Test query manually**:
   - Go to **Explore** in Grafana
   - Select **InfluxDB** data source
   - Try this simple query:
     ```flux
     from(bucket: "analytics")
       |> range(start: -1h)
       |> limit(n: 10)
     ```

## 📁 **Available Dashboards**

Once the test dashboard works, import these others:

1. **yapplr-test-working.json** ⭐ - **Start here** (3 panels, simple)
2. **yapplr-simple-working.json** - Basic analytics (7 panels)
3. **yapplr-comprehensive-analytics.json** - Complete overview (11 panels)
4. **yapplr-admin-analytics.json** - Admin-focused (5 panels)
5. **yapplr-realtime-monitoring.json** - Real-time monitoring (7 panels)

## 🔧 **Common Issues and Fixes**

### **Issue: "Dashboard title cannot be empty"**
- **Cause**: JSON format problem
- **Fix**: Use the fixed JSON files (already done)

### **Issue: Empty dashboard after import**
- **Cause**: Wrong data source or no data
- **Fix**: 
  1. Enable InfluxDB: `./analytics/enable-influxdb.sh`
  2. Restart app: `docker-compose -f docker-compose.local.yml restart yapplr-api`
  3. Generate test data in your application

### **Issue: "Query error" in panels**
- **Cause**: Data source configuration
- **Fix**: Verify InfluxDB data source settings (see Step 5 above)

### **Issue: Panels show "N/A"**
- **Cause**: No data in InfluxDB
- **Fix**: 
  1. Check if InfluxDB is enabled in your app
  2. Use your application to generate activity
  3. Run debug script: `./analytics/debug-influxdb-data.sh`

## 🎯 **Expected Results**

When working correctly, you should see:

### **Test Dashboard**:
- **Total User Activities**: Shows a number (e.g., "15")
- **User Activities Over Time**: Shows activity lines/curves
- **Recent Analytics Data**: Shows table with recent records

### **Data in Table Should Look Like**:
```
_time                | _measurement     | activity_type | user_id | _field | _value
2025-07-17T10:30:00Z | user_activities  | Login        | 123     | count  | 1
2025-07-17T10:25:00Z | user_activities  | CreatePost   | 456     | count  | 1
```

## 🚀 **Next Steps After Success**

1. **Import other dashboards** using the same process
2. **Customize panels** as needed
3. **Set up alerts** if desired
4. **Share dashboards** with your team

## 🆘 **Still Having Issues?**

If dashboards are still empty after following this guide:

1. **Run the debug script**: `./analytics/debug-influxdb-data.sh`
2. **Check application logs** for InfluxDB errors
3. **Verify Docker services**: `docker-compose -f docker-compose.local.yml ps`
4. **Test InfluxDB directly**: Go to http://localhost:8086

The key is making sure:
- ✅ InfluxDB is enabled in your application
- ✅ Data is being written to InfluxDB
- ✅ Grafana data source is configured correctly
- ✅ Dashboard JSON format is correct (now fixed)

**Your dashboards should now work perfectly!** 🎉
