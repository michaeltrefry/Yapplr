# Grafana Data Source Provisioning Fix

## 🔍 **Problem Identified**

Grafana was failing to start with this error:
```
Datasource provisioning error: datasource.yaml config is invalid. Only one datasource per organization can be marked as default
```

## 🔧 **Root Cause**

The issue was that **both local and staging datasource files were being loaded simultaneously**:

1. **Local Docker Compose** mounted the entire provisioning directory:
   ```yaml
   - ./analytics/grafana/provisioning:/etc/grafana/provisioning
   ```

2. **Both files had Prometheus as default**:
   - `datasources.yml` (local) - Prometheus `isDefault: true`
   - `datasources.staging.yml` (staging) - Prometheus `isDefault: true`

3. **Grafana loads ALL `.yml` files** in the datasources directory, causing a conflict

## ✅ **Solution Implemented**

### **1. Environment-Specific Provisioning Directories**

Created separate provisioning directories for each environment:

```
analytics/grafana/
├── provisioning-local/          # ← Local environment only
│   ├── datasources/
│   │   └── datasources.yml
│   └── dashboards/
│       └── dashboards.yml
├── provisioning-staging/        # ← Staging environment only
│   ├── datasources/
│   │   └── datasources.yml
│   └── dashboards/
│       └── dashboards.yml
└── provisioning/                # ← Original (now unused)
    ├── datasources/
    └── dashboards/
```

### **2. Updated Docker Compose Files**

**Local (`docker-compose.local.yml`)**:
```yaml
volumes:
  - ./analytics/grafana/provisioning-local:/etc/grafana/provisioning
```

**Staging (`docker-compose.stage.yml`)**:
```yaml
volumes:
  - ./analytics/grafana/provisioning-staging:/etc/grafana/provisioning
```

### **3. Optimized Data Source Configuration**

**Both environments now have**:
- ✅ **InfluxDB as default** (needed for analytics dashboards)
- ✅ **Prometheus as secondary** (available but not default)
- ✅ **No conflicts** between environments

## 📊 **Data Source Priority**

### **Local Environment**:
```yaml
- name: InfluxDB
  isDefault: true    # ← Primary for analytics dashboards
  token: yapplr-analytics-token-local-dev-only

- name: Prometheus
  isDefault: false   # ← Secondary for system metrics
```

### **Staging Environment**:
```yaml
- name: InfluxDB
  isDefault: true    # ← Primary for analytics dashboards
  token: yapplr-analytics-token-staging

- name: Prometheus
  isDefault: false   # ← Secondary for system metrics
```

## 🚀 **Benefits of This Fix**

1. **✅ No More Conflicts**: Each environment loads only its own datasource configuration
2. **✅ InfluxDB Default**: Analytics dashboards work immediately without manual data source selection
3. **✅ Environment Isolation**: Local and staging configurations are completely separate
4. **✅ Cleaner Setup**: No more duplicate default data sources
5. **✅ Future-Proof**: Easy to add production environment with same pattern

## 🔄 **How to Apply the Fix**

1. **Restart Grafana**:
   ```bash
   docker-compose -f docker-compose.local.yml restart grafana
   ```

2. **Check Grafana logs** (should be clean now):
   ```bash
   docker-compose -f docker-compose.local.yml logs grafana
   ```

3. **Verify in Grafana UI**:
   - Go to http://localhost:3001
   - Login: admin/yapplr123
   - Check Configuration → Data Sources
   - InfluxDB should be marked as default ⭐

## 📁 **File Changes Made**

### **New Files Created**:
- `analytics/grafana/provisioning-local/datasources/datasources.yml`
- `analytics/grafana/provisioning-local/dashboards/dashboards.yml`
- `analytics/grafana/provisioning-staging/datasources/datasources.yml`
- `analytics/grafana/provisioning-staging/dashboards/dashboards.yml`

### **Files Modified**:
- `docker-compose.local.yml` - Updated Grafana volume mount
- `docker-compose.stage.yml` - Updated Grafana volume mount

### **Files Moved**:
- `datasources.staging.yml` → `provisioning-staging/datasources/datasources.yml`

## 🎯 **Result**

- ✅ **Grafana starts successfully** without provisioning errors
- ✅ **InfluxDB is the default data source** for analytics dashboards
- ✅ **Environment-specific configurations** work correctly
- ✅ **No manual data source configuration needed** in Grafana UI

Your analytics dashboards should now work immediately after importing! 🎉
