# Yapplr Admin System Deployment Guide

## 📋 Overview

This document provides comprehensive deployment and operational guidance for the Yapplr Admin System. The admin system includes user management, content moderation, system tags, audit logging, analytics, and appeals management.

## 🏗️ System Architecture

### Components
- **Backend API**: ASP.NET Core with admin endpoints
- **Frontend Dashboard**: Next.js React application
- **Database**: PostgreSQL/MySQL with admin tables
- **Notifications**: SignalR + Firebase integration
- **Analytics**: Real-time reporting dashboard

### Admin Features
- User role management (User → Moderator → Admin)
- Content moderation (hide, delete, tag posts/comments)
- System tags with 6 categories and 15+ default tags
- Comprehensive audit logging (25+ action types)
- User appeals system with admin review workflow
- Real-time analytics and system health monitoring

## 🔐 Security Configuration

### 1. Environment Variables

Create a `.env.production` file with the following variables:

```bash
# Database
PROD_DATABASE_CONNECTION_STRING="Server=your-db-server;Database=yapplr_prod;User Id=yapplr_admin;Password=secure_admin_password;"

# JWT Configuration
PROD_JWT_SECRET_KEY="your-super-secure-jwt-key-min-256-bits"
JWT_ISSUER="https://api.yapplr.com"
JWT_AUDIENCE="https://yapplr.com"
JWT_EXPIRY_HOURS=24

# Admin Security
ADMIN_PASSWORD_MIN_LENGTH=12
ADMIN_REQUIRE_2FA=true
ADMIN_SESSION_TIMEOUT_MINUTES=60

# Rate Limiting
ADMIN_RATE_LIMIT_REQUESTS=100
ADMIN_RATE_LIMIT_WINDOW_MINUTES=1

# Notifications
PROD_FIREBASE_PROJECT_ID="your-firebase-project"
FIREBASE_PRIVATE_KEY="your-firebase-private-key"
SIGNALR_CONNECTION_STRING="your-signalr-connection"

# Monitoring
ENABLE_ADMIN_MONITORING=true
ALERT_EMAIL="admin@yapplr.com"
```

### 2. Admin Account Security

```bash
# Create initial admin account (run once)
dotnet run --project Yapplr.Api -- create-admin \
  --username "admin" \
  --email "admin@yapplr.com" \
  --password "SecureAdminPassword123!" \
  --role "Admin"
```

### 3. CORS Configuration

Update `appsettings.Production.json`:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://yapplr.com",
      "https://admin.yapplr.com"
    ],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE", "PATCH"],
    "AllowedHeaders": ["*"],
    "AllowCredentials": true
  }
}
```

## 🗄️ Database Setup

### 1. Database Permissions

Create dedicated admin database user:

```sql
-- PostgreSQL
CREATE USER yapplr_admin WITH PASSWORD 'secure_admin_password';
GRANT ALL PRIVILEGES ON DATABASE yapplr TO yapplr_admin;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO yapplr_admin;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO yapplr_admin;

-- MySQL
CREATE USER 'yapplr_admin'@'%' IDENTIFIED BY 'secure_admin_password';
GRANT ALL PRIVILEGES ON yapplr.* TO 'yapplr_admin'@'%';
FLUSH PRIVILEGES;
```

### 2. Performance Indexes

Run these SQL commands to optimize admin queries:

```sql
-- User management indexes
CREATE INDEX IF NOT EXISTS IX_Users_Status_Role ON Users(Status, Role);
CREATE INDEX IF NOT EXISTS IX_Users_LastLoginAt ON Users(LastLoginAt);
CREATE INDEX IF NOT EXISTS IX_Users_CreatedAt ON Users(CreatedAt);

-- Content moderation indexes
CREATE INDEX IF NOT EXISTS IX_Posts_IsHidden_CreatedAt ON Posts(IsHidden, CreatedAt);
CREATE INDEX IF NOT EXISTS IX_Comments_IsHidden_CreatedAt ON Comments(IsHidden, CreatedAt);
CREATE INDEX IF NOT EXISTS IX_Posts_UserId_CreatedAt ON Posts(UserId, CreatedAt);

-- System tags indexes
CREATE INDEX IF NOT EXISTS IX_PostSystemTags_TagId ON PostSystemTags(TagId);
CREATE INDEX IF NOT EXISTS IX_CommentSystemTags_TagId ON CommentSystemTags(TagId);
CREATE INDEX IF NOT EXISTS IX_SystemTags_Category_IsActive ON SystemTags(Category, IsActive);

-- Audit log indexes
CREATE INDEX IF NOT EXISTS IX_AuditLogs_CreatedAt ON AuditLogs(CreatedAt);
CREATE INDEX IF NOT EXISTS IX_AuditLogs_PerformedByUserId ON AuditLogs(PerformedByUserId);
CREATE INDEX IF NOT EXISTS IX_AuditLogs_Action ON AuditLogs(Action);

-- Appeals indexes
CREATE INDEX IF NOT EXISTS IX_UserAppeals_Status_CreatedAt ON UserAppeals(Status, CreatedAt);
CREATE INDEX IF NOT EXISTS IX_UserAppeals_UserId ON UserAppeals(UserId);

-- Notifications indexes
CREATE INDEX IF NOT EXISTS IX_Notifications_UserId_IsRead ON Notifications(UserId, IsRead);
CREATE INDEX IF NOT EXISTS IX_Notifications_Type_CreatedAt ON Notifications(Type, CreatedAt);
```

### 3. Database Migrations

Run migrations in production:

```bash
# Apply all admin-related migrations
dotnet ef database update --project Yapplr.Api --configuration Production

# Verify migrations applied
dotnet ef migrations list --project Yapplr.Api --configuration Production
```

## 🚀 Backend Deployment

### 1. Build Configuration

Update `appsettings.Production.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Yapplr.Api.Services.AdminService": "Information"
    }
  },
  "AdminFeatures": {
    "BulkOperationsEnabled": true,
    "AdvancedAnalyticsEnabled": true,
    "AutoModerationEnabled": false,
    "MaxBulkOperationSize": 100,
    "AuditLogRetentionDays": 2555,
    "AppealReviewTimeoutHours": 48
  },
  "SystemTags": {
    "AutoCreateDefaults": true,
    "AllowUserVisibleTags": true,
    "MaxTagsPerContent": 5
  },
  "RateLimiting": {
    "AdminEndpoints": {
      "RequestsPerMinute": 100,
      "BurstLimit": 200
    }
  }
}
```

### 2. Docker Configuration

Create `Dockerfile.admin` for admin-specific deployment:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Yapplr.Api/Yapplr.Api.csproj", "Yapplr.Api/"]
RUN dotnet restore "Yapplr.Api/Yapplr.Api.csproj"
COPY . .
WORKDIR "/src/Yapplr.Api"
RUN dotnet build "Yapplr.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Yapplr.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Health check for admin endpoints
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/health/admin || exit 1

ENTRYPOINT ["dotnet", "Yapplr.Api.dll"]
```

### 3. Health Checks

Add admin-specific health checks:

```csharp
// In Program.cs
builder.Services.AddHealthChecks()
    .AddDbContext<YapplrDbContext>()
    .AddCheck<AdminHealthCheck>("admin-services")
    .AddCheck<AuditLogHealthCheck>("audit-logging");

// Map health check endpoints
app.MapHealthChecks("/health/admin", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("admin"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

## 🌐 Frontend Deployment

### 1. Environment Configuration

Create `.env.production` for the frontend:

```bash
# API Configuration
NEXT_PUBLIC_API_URL=https://api.yapplr.com
NEXT_PUBLIC_ADMIN_API_URL=https://api.yapplr.com/admin

# Authentication
NEXT_PUBLIC_JWT_STORAGE_KEY=yapplr_admin_token
NEXT_PUBLIC_SESSION_TIMEOUT=3600000

# Features
NEXT_PUBLIC_ENABLE_ANALYTICS=true
NEXT_PUBLIC_ENABLE_BULK_OPERATIONS=true
NEXT_PUBLIC_MAX_BULK_SIZE=100

# Monitoring
NEXT_PUBLIC_ENABLE_ERROR_REPORTING=true
NEXT_PUBLIC_SENTRY_DSN=your-sentry-dsn
```

### 2. Build and Deploy

```bash
# Install dependencies
cd yapplr-frontend
npm ci --production

# Build for production
npm run build

# Start production server
npm start

# Or deploy to static hosting
npm run export
```

### 3. Nginx Configuration

Configure Nginx for admin routes:

```nginx
server {
    listen 443 ssl;
    server_name admin.yapplr.com;

    ssl_certificate /path/to/ssl/cert.pem;
    ssl_certificate_key /path/to/ssl/key.pem;

    # Admin route protection
    location /admin {
        # Rate limiting for admin routes
        limit_req zone=admin burst=20 nodelay;
        
        # Proxy to Next.js
        proxy_pass http://localhost:3000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }

    # API proxy
    location /api/ {
        proxy_pass https://api.yapplr.com/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}

# Rate limiting zones
http {
    limit_req_zone $binary_remote_addr zone=admin:10m rate=10r/m;
}
```

## 📊 Monitoring & Alerting

### 1. Application Monitoring

Configure monitoring for admin actions:

```csharp
// Add to your monitoring service
public class AdminMonitoringService
{
    public async Task LogSuspiciousActivity(string action, int userId, string details)
    {
        if (IsSuspiciousActivity(action, userId))
        {
            await _alertService.SendAlert(new AdminAlert
            {
                Type = "SuspiciousActivity",
                Message = $"Unusual admin activity: {action} by user {userId}",
                Details = details,
                Severity = AlertSeverity.High
            });
        }
    }

    private bool IsSuspiciousActivity(string action, int userId)
    {
        // Check for bulk operations outside business hours
        // Check for rapid successive actions
        // Check for actions by recently created admin accounts
        return false; // Implement your logic
    }
}
```

### 2. Database Monitoring

Monitor admin-related database metrics:

```sql
-- Monitor audit log growth
SELECT 
    DATE(CreatedAt) as Date,
    COUNT(*) as ActionCount,
    COUNT(DISTINCT PerformedByUserId) as UniqueAdmins
FROM AuditLogs 
WHERE CreatedAt >= DATE_SUB(NOW(), INTERVAL 7 DAY)
GROUP BY DATE(CreatedAt)
ORDER BY Date DESC;

-- Monitor appeal processing times
SELECT 
    AVG(TIMESTAMPDIFF(HOUR, CreatedAt, ReviewedAt)) as AvgProcessingHours,
    COUNT(*) as TotalAppeals
FROM UserAppeals 
WHERE Status != 0 AND ReviewedAt IS NOT NULL
AND CreatedAt >= DATE_SUB(NOW(), INTERVAL 30 DAY);
```

### 3. System Health Alerts

Set up alerts for:

```bash
# Failed admin login attempts (>5 in 10 minutes)
# Bulk operations (>50 items)
# Appeal backlog (>100 pending appeals)
# System tag usage spikes
# Database connection issues
# High memory/CPU usage during admin operations
```

## 🔄 Backup & Recovery

### 1. Database Backup Strategy

```bash
#!/bin/bash
# Admin data backup script

# Backup admin-critical tables
pg_dump -h $DB_HOST -U $DB_USER -d yapplr \
  --table=Users \
  --table=AuditLogs \
  --table=UserAppeals \
  --table=SystemTags \
  --table=PostSystemTags \
  --table=CommentSystemTags \
  --table=Notifications \
  > admin_backup_$(date +%Y%m%d_%H%M%S).sql

# Compress and upload to secure storage
gzip admin_backup_*.sql
aws s3 cp admin_backup_*.sql.gz s3://yapplr-admin-backups/
```

### 2. Configuration Backup

```bash
# Backup admin configuration files
tar -czf admin_config_$(date +%Y%m%d).tar.gz \
  appsettings.Production.json \
  .env.production \
  nginx.conf \
  docker-compose.yml
```

## 🚨 Emergency Procedures

### 1. Emergency Admin Access

Create emergency admin script:

```csharp
// EmergencyAdminCreator.cs
public class EmergencyAdminCreator
{
    public static async Task CreateEmergencyAdmin(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: dotnet run emergency-admin <username> <email> <password>");
            return;
        }

        var username = args[0];
        var email = args[1];
        var password = args[2];

        // Create emergency admin with full privileges
        var user = new User
        {
            Username = username,
            Email = email,
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        // Save to database and log
        await CreateUserWithAuditLog(user, password, "EMERGENCY_ADMIN_CREATION");
        
        Console.WriteLine($"Emergency admin created: {username}");
    }
}
```

### 2. System Lockdown

```csharp
public async Task EnableEmergencyLockdown(int adminUserId, string reason)
{
    // Disable new user registrations
    await _configService.SetAsync("REGISTRATION_ENABLED", false);
    
    // Enable content pre-approval
    await _configService.SetAsync("REQUIRE_CONTENT_APPROVAL", true);
    
    // Increase moderation sensitivity
    await _configService.SetAsync("AUTO_MODERATION_THRESHOLD", 0.3);
    
    // Log emergency action
    await _auditService.LogAsync(AuditAction.EmergencyLockdownEnabled, adminUserId, reason);
    
    // Send alerts
    await _alertService.SendCriticalAlert("Emergency lockdown enabled", reason);
}
```

## 📋 Pre-Deployment Checklist

### Security
- [ ] Admin accounts created with strong passwords (12+ chars, mixed case, numbers, symbols)
- [ ] 2FA enabled for all admin accounts
- [ ] JWT secrets are production-grade (256+ bits)
- [ ] Database admin user has minimal required permissions
- [ ] CORS configured for production domains only
- [ ] Rate limiting enabled for admin endpoints
- [ ] SSL certificates installed and configured

### Database
- [ ] All migrations applied successfully
- [ ] Performance indexes created
- [ ] Database backup strategy implemented
- [ ] Audit log retention policy configured
- [ ] Connection pooling optimized for admin load

### Application
- [ ] Production configuration files updated
- [ ] Feature flags configured appropriately
- [ ] Health checks implemented and tested
- [ ] Logging configured for admin actions
- [ ] Error handling and monitoring in place

### Frontend
- [ ] Production build tested
- [ ] Admin routes properly protected
- [ ] API endpoints configured for production
- [ ] Error boundaries implemented
- [ ] Performance optimizations applied

### Monitoring
- [ ] Admin action monitoring configured
- [ ] Database performance monitoring enabled
- [ ] Alert thresholds configured
- [ ] Log aggregation set up
- [ ] Health check endpoints monitored

### Documentation
- [ ] Admin user guide created
- [ ] Emergency procedures documented
- [ ] Backup/recovery procedures tested
- [ ] Monitoring runbooks created
- [ ] Incident response plan documented

## 🔧 Post-Deployment Tasks

### Immediate (Day 1)
- [ ] Verify all admin endpoints are accessible
- [ ] Test admin login and role-based access
- [ ] Confirm audit logging is working
- [ ] Test notification system
- [ ] Verify database performance

### Short-term (Week 1)
- [ ] Monitor admin action patterns
- [ ] Review audit logs for anomalies
- [ ] Test appeal submission and review workflow
- [ ] Validate analytics data accuracy
- [ ] Optimize any performance issues

### Ongoing Maintenance
- [ ] Weekly audit log review
- [ ] Monthly admin access review
- [ ] Quarterly security assessment
- [ ] Regular backup testing
- [ ] Performance optimization as needed

## 📞 Support & Troubleshooting

### Common Issues

**Admin Login Issues**
```bash
# Check JWT configuration
dotnet run --project Yapplr.Api -- validate-jwt

# Reset admin password
dotnet run --project Yapplr.Api -- reset-admin-password <username>
```

**Database Performance**
```sql
-- Check slow admin queries
SELECT query, mean_time, calls 
FROM pg_stat_statements 
WHERE query LIKE '%admin%' 
ORDER BY mean_time DESC LIMIT 10;
```

**Audit Log Issues**
```bash
# Check audit service health
curl -f https://api.yapplr.com/health/audit

# Verify audit log retention
SELECT COUNT(*), MIN(CreatedAt), MAX(CreatedAt) FROM AuditLogs;
```

### Emergency Contacts
- **System Administrator**: admin@yapplr.com
- **Database Administrator**: dba@yapplr.com  
- **Security Team**: security@yapplr.com
- **On-call Engineer**: +1-555-YAPPLR-1

---

## 📚 Additional Resources

- [Admin User Guide](./ADMIN_USER_GUIDE.md)
- [API Documentation](./API_DOCUMENTATION.md)
- [Security Best Practices](./SECURITY_GUIDE.md)
- [Monitoring Runbook](./MONITORING_RUNBOOK.md)
- [Incident Response Plan](./INCIDENT_RESPONSE.md)

---

**Document Version**: 1.0  
**Last Updated**: December 2024  
**Next Review**: March 2025
