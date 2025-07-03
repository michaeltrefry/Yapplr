# Yapplr Production Deployment Guide

## Overview

This guide covers deploying your Yapplr application with AWS SES email functionality and Firebase real-time notifications to production.

## Prerequisites

- AWS Account with SES configured
- Firebase project with Cloud Messaging enabled
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

## 2. Firebase Production Setup

### 2.1 Create Firebase Service Account
1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Select your project
3. Go to **Project Settings** → **Service Accounts**
4. Click **Generate New Private Key**
5. Download the JSON file (keep it secure!)

### 2.2 Configure Firebase Environment Variables
```bash
# Required Firebase environment variables for production
Firebase__ProjectId=your-firebase-project-id
Firebase__ServiceAccountKey='{"type":"service_account","project_id":"your-project-id",...}'
```

**Important**: The `Firebase__ServiceAccountKey` should be the entire JSON content as a single-line string.

## 3. Environment Configuration

### 3.1 Production appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=your-prod-db;Database=yapplr;Username=yapplr_user;Password=secure_password"
  },
  "JwtSettings": {
    "SecretKey": "your-production-jwt-secret-key-minimum-32-characters",
    "Issuer": "Yapplr.Api",
    "Audience": "Yapplr.Client",
    "ExpirationInMinutes": 60
  },
  "AwsSesSettings": {
    "Region": "us-east-1",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "Yapplr"
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

### 3.2 Environment Variables
```bash
# AWS Credentials (preferred method)
export AWS_ACCESS_KEY_ID=your_access_key
export AWS_SECRET_ACCESS_KEY=your_secret_key
export AWS_DEFAULT_REGION=us-east-1

# Database
export ConnectionStrings__DefaultConnection="Host=prod-db;Database=yapplr;Username=user;Password=pass"

# JWT
export JwtSettings__SecretKey="your-production-secret-key"

# Email
export AwsSesSettings__FromEmail="noreply@yourdomain.com"

# Firebase (REQUIRED for notifications)
export Firebase__ProjectId="your-firebase-project-id"
export Firebase__ServiceAccountKey='{"type":"service_account","project_id":"your-project-id",...}'
```

## 4. Database Migration

### 4.1 Production Database Setup
```bash
# Create production database
createdb -h your-db-host -U postgres yapplr

# Run migrations
dotnet ef database update --connection "Host=your-db;Database=yapplr;Username=user;Password=pass"
```

### 4.2 Database Security
- Use dedicated database user with minimal permissions
- Enable SSL connections
- Regular backups
- Connection pooling

## 5. Docker Deployment

### 5.1 Dockerfile
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
ENTRYPOINT ["dotnet", "Yapplr.Api.dll"]
```

### 5.2 Docker Compose
```yaml
version: '3.8'
services:
  yapplr-api:
    build: .
    ports:
      - "80:80"
      - "443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=db;Database=yapplr;Username=yapplr;Password=password
      - AWS_ACCESS_KEY_ID=${AWS_ACCESS_KEY_ID}
      - AWS_SECRET_ACCESS_KEY=${AWS_SECRET_ACCESS_KEY}
      # Firebase configuration (REQUIRED)
      - Firebase__ProjectId=${FIREBASE_PROJECT_ID}
      - Firebase__ServiceAccountKey=${FIREBASE_SERVICE_ACCOUNT_KEY}
    depends_on:
      - db

  db:
    image: postgres:15
    environment:
      POSTGRES_DB: yapplr
      POSTGRES_USER: yapplr
      POSTGRES_PASSWORD: password
    volumes:
      - postgres_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

volumes:
  postgres_data:
```

## 6. Frontend Firebase Configuration

### 6.1 Production Environment Variables
Create a `.env.production` file in your frontend directory:

```bash
# Frontend Firebase Configuration (REQUIRED for notifications)
NEXT_PUBLIC_FIREBASE_API_KEY=your-api-key
NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN=your-project.firebaseapp.com
NEXT_PUBLIC_FIREBASE_PROJECT_ID=your-project-id
NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET=your-project.appspot.com
NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID=your-sender-id
NEXT_PUBLIC_FIREBASE_APP_ID=your-app-id
NEXT_PUBLIC_FIREBASE_VAPID_KEY=your-vapid-key
```

### 6.2 Get Firebase Configuration Values
1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Select your project
3. Go to **Project Settings** → **General** → **Your apps**
4. Select your web app or create one
5. Copy the configuration values

### 6.3 Generate VAPID Key
1. In Firebase Console, go to **Project Settings** → **Cloud Messaging**
2. Under **Web configuration**, click **Generate key pair**
3. Copy the VAPID key to `NEXT_PUBLIC_FIREBASE_VAPID_KEY`

### 6.4 Frontend Deployment
```bash
# Build frontend with production environment
npm run build

# Deploy to your hosting provider (Vercel, Netlify, etc.)
# Make sure to set the environment variables in your hosting platform
```

## 7. Security Considerations

### 7.1 HTTPS Configuration
```csharp
// In Program.cs
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}
```

### 7.2 CORS Configuration
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

### 7.3 Rate Limiting
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

## 8. Monitoring and Logging

### 8.1 Application Insights (Azure)
```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### 8.2 CloudWatch (AWS)
```csharp
builder.Services.AddAWSService<IAmazonCloudWatchLogs>();
```

### 8.3 Health Checks
```csharp
builder.Services.AddHealthChecks()
    .AddDbContext<YapplrDbContext>()
    .AddCheck<EmailHealthCheck>("email");
```

### 8.4 Firebase Monitoring
Monitor Firebase notification delivery in your Firebase Console:
- Go to **Cloud Messaging** → **Reports**
- Check delivery rates and error logs
- Monitor FCM token registration success

## 9. Performance Optimization

### 9.1 Database Indexing
The application includes comprehensive performance indexes. See [Database Performance Analysis](Database-Performance-Analysis.md) for details.

### 9.2 Caching
```csharp
builder.Services.AddMemoryCache();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "your-redis-connection-string";
});
```

## 10. Deployment Checklist

### Pre-Deployment
- [ ] AWS SES domain verified
- [ ] Production access approved
- [ ] Firebase project configured
- [ ] Firebase service account key generated
- [ ] Frontend Firebase environment variables set
- [ ] Database migrations tested
- [ ] Environment variables configured
- [ ] SSL certificates installed
- [ ] DNS records configured

### Post-Deployment
- [ ] Health checks passing
- [ ] Email sending functional
- [ ] Firebase notifications working
- [ ] Push notifications delivering to browsers
- [ ] FCM token registration successful
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

This guide ensures your Yapplr application is production-ready with reliable email functionality!
