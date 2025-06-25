# Postr Production Deployment Guide

## Overview

This guide covers deploying your Postr application with AWS SES email functionality to production.

## Prerequisites

- AWS Account with SES configured
- Domain name
- SSL certificate
- Production database (PostgreSQL)
- Hosting environment (AWS, Azure, Docker, etc.)

## 1. AWS SES Production Setup

### 1.1 Domain Verification
```bash
# Add these DNS records to your domain:
# TXT record: _amazonses.yourdomain.com
# CNAME records for DKIM (provided by AWS)
```

### 1.2 Request Production Access
- Submit production access request in SES console
- Typical approval time: 24 hours
- Required for sending to unverified addresses

### 1.3 Configure SPF/DKIM/DMARC
```dns
# SPF Record
yourdomain.com. TXT "v=spf1 include:amazonses.com ~all"

# DMARC Record  
_dmarc.yourdomain.com. TXT "v=DMARC1; p=quarantine; rua=mailto:dmarc@yourdomain.com"
```

## 2. Environment Configuration

### 2.1 Production appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=your-prod-db;Database=Postr;Username=postr_user;Password=secure_password"
  },
  "JwtSettings": {
    "SecretKey": "your-production-jwt-secret-key-minimum-32-characters",
    "Issuer": "Postr.Api",
    "Audience": "Postr.Client",
    "ExpirationInMinutes": 60
  },
  "AwsSesSettings": {
    "Region": "us-east-1",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "Postr"
  },
  "EmailProvider": "AwsSes",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### 2.2 Environment Variables
```bash
# AWS Credentials (preferred method)
export AWS_ACCESS_KEY_ID=your_access_key
export AWS_SECRET_ACCESS_KEY=your_secret_key
export AWS_DEFAULT_REGION=us-east-1

# Database
export ConnectionStrings__DefaultConnection="Host=prod-db;Database=Postr;Username=user;Password=pass"

# JWT
export JwtSettings__SecretKey="your-production-secret-key"

# Email
export AwsSesSettings__FromEmail="noreply@yourdomain.com"
```

## 3. Database Migration

### 3.1 Production Database Setup
```bash
# Create production database
createdb -h your-db-host -U postgres Postr

# Run migrations
dotnet ef database update --connection "Host=your-db;Database=Postr;Username=user;Password=pass"
```

### 3.2 Database Security
- Use dedicated database user with minimal permissions
- Enable SSL connections
- Regular backups
- Connection pooling

## 4. Docker Deployment

### 4.1 Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Postr.Api/Postr.Api.csproj", "Postr.Api/"]
RUN dotnet restore "Postr.Api/Postr.Api.csproj"
COPY . .
WORKDIR "/src/Postr.Api"
RUN dotnet build "Postr.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Postr.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Postr.Api.dll"]
```

### 4.2 Docker Compose
```yaml
version: '3.8'
services:
  postr-api:
    build: .
    ports:
      - "80:80"
      - "443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=db;Database=Postr;Username=postr;Password=password
      - AWS_ACCESS_KEY_ID=${AWS_ACCESS_KEY_ID}
      - AWS_SECRET_ACCESS_KEY=${AWS_SECRET_ACCESS_KEY}
    depends_on:
      - db
    
  db:
    image: postgres:15
    environment:
      POSTGRES_DB: Postr
      POSTGRES_USER: postr
      POSTGRES_PASSWORD: password
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

volumes:
  postgres_data:
```

## 5. Security Considerations

### 5.1 HTTPS Configuration
```csharp
// In Program.cs
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}
```

### 5.2 CORS Configuration
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

### 5.3 Rate Limiting
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("PasswordReset", opt =>
    {
        opt.PermitLimit = 3;
        opt.Window = TimeSpan.FromMinutes(15);
    });
});
```

## 6. Monitoring and Logging

### 6.1 Application Insights (Azure)
```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### 6.2 CloudWatch (AWS)
```csharp
builder.Services.AddAWSService<IAmazonCloudWatchLogs>();
```

### 6.3 Health Checks
```csharp
builder.Services.AddHealthChecks()
    .AddDbContext<PostrDbContext>()
    .AddCheck<EmailHealthCheck>("email");
```

## 7. Performance Optimization

### 7.1 Database Indexing
```sql
-- Add indexes for frequently queried columns
CREATE INDEX idx_posts_userid_createdat ON "Posts" ("UserId", "CreatedAt" DESC);
CREATE INDEX idx_passwordresets_token ON "PasswordResets" ("Token");
CREATE INDEX idx_passwordresets_email_expiry ON "PasswordResets" ("Email", "ExpiresAt");
```

### 7.2 Caching
```csharp
builder.Services.AddMemoryCache();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "your-redis-connection-string";
});
```

## 8. Deployment Checklist

### Pre-Deployment
- [ ] AWS SES domain verified
- [ ] Production access approved
- [ ] Database migrations tested
- [ ] Environment variables configured
- [ ] SSL certificates installed
- [ ] DNS records configured

### Post-Deployment
- [ ] Health checks passing
- [ ] Email sending functional
- [ ] Database connectivity verified
- [ ] Logs monitoring setup
- [ ] Performance metrics baseline
- [ ] Backup procedures tested

## 9. Troubleshooting

### Common Issues

#### Email Not Sending
```bash
# Check SES sending statistics
aws ses get-send-statistics --region us-east-1

# Check bounce/complaint rates
aws ses get-reputation --identity yourdomain.com
```

#### Database Connection Issues
```bash
# Test database connectivity
dotnet ef database update --dry-run
```

#### Performance Issues
```bash
# Monitor application metrics
dotnet-counters monitor --process-id <pid>
```

## 10. Maintenance

### Regular Tasks
- Monitor SES reputation metrics
- Review application logs
- Update dependencies
- Database maintenance
- Security patches
- Backup verification

### Scaling Considerations
- Database read replicas
- Load balancing
- CDN for static assets
- Horizontal scaling
- Caching strategies

This guide ensures your Postr application is production-ready with reliable email functionality!
