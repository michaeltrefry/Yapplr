# Yapplr Video Processing - Production Deployment Guide

This guide covers the complete deployment of Yapplr with video processing capabilities in a production environment.

## 🏗️ Architecture Overview

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Load Balancer │    │      Nginx       │    │   Monitoring    │
│   (Cloudflare)  │───▶│  Reverse Proxy   │───▶│   (Grafana)     │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │   Yapplr API     │
                    │   (Main App)     │
                    └──────────────────┘
                              │
                              ▼
                    ┌──────────────────┐    ┌─────────────────┐
                    │ Video Processor  │    │   PostgreSQL    │
                    │  (Microservice)  │───▶│   Database      │
                    └──────────────────┘    └─────────────────┘
                              │
                              ▼
                    ┌──────────────────┐    ┌─────────────────┐
                    │   File Storage   │    │     Redis       │
                    │   (Videos/Imgs)  │    │     Cache       │
                    └──────────────────┘    └─────────────────┘
```

## 📋 Prerequisites

### System Requirements
- **CPU**: 4+ cores (video processing is CPU intensive)
- **RAM**: 8GB+ (4GB for API, 4GB for video processing)
- **Storage**: 100GB+ SSD (for videos and database)
- **Network**: 1Gbps+ (for video uploads/streaming)

### Software Requirements
- Docker 20.10+
- Docker Compose 2.0+
- Git
- OpenSSL (for SSL certificates)

### Domain Setup
- Main domain: `yapplr.com`
- API subdomain: `api.yapplr.com`
- Wildcard SSL certificate for `*.yapplr.com`

## 🚀 Quick Deployment

### 1. Clone and Setup
```bash
git clone https://github.com/your-org/yapplr.git
cd yapplr
git checkout feature/video-processing
```

### 2. Configure Environment
```bash
# Copy environment template
cp .env.production.template .env.production

# Edit configuration (see Configuration section below)
nano .env.production
```

### 3. SSL Certificates
```bash
# Create SSL directory
mkdir -p nginx/ssl

# Copy your SSL certificates
cp your-ssl-cert.crt nginx/ssl/yapplr.com.crt
cp your-ssl-key.key nginx/ssl/yapplr.com.key
```

### 4. Deploy
```bash
# Run deployment script
./scripts/deploy-production.sh deploy
```

### 5. Verify
```bash
# Check deployment status
./scripts/deploy-production.sh status

# Test video upload
curl -X POST https://api.yapplr.com/health
```

## ⚙️ Configuration

### Environment Variables (.env.production)

#### Database Configuration
```bash
POSTGRES_DB=yapplr
POSTGRES_USER=yapplr_user
POSTGRES_PASSWORD=your_secure_password_here
DATABASE_CONNECTION_STRING=Host=postgres;Database=yapplr;Username=yapplr_user;Password=your_secure_password_here
```

#### JWT Configuration
```bash
# Generate with: openssl rand -base64 32
JWT_SECRET_KEY=your_jwt_secret_key_minimum_32_characters
JWT_ISSUER=https://api.yapplr.com
JWT_AUDIENCE=https://yapplr.com
```

#### Email Configuration (SendGrid)
```bash
SMTP_SERVER=smtp.sendgrid.net
SMTP_PORT=587
SMTP_USERNAME=apikey
SMTP_PASSWORD=your_sendgrid_api_key
FROM_EMAIL=noreply@yapplr.com
FROM_NAME=Yapplr
```

#### Video Processing Configuration
```bash
VIDEO_MAX_CONCURRENT_JOBS=2          # Adjust based on CPU cores
VIDEO_POLL_INTERVAL=5                # Seconds between status checks
VIDEO_MAX_DURATION=300               # 5 minutes max video length
VIDEO_MAX_SIZE=104857600             # 100MB max file size
VIDEO_MAX_WIDTH=1920                 # Full HD max width
VIDEO_MAX_HEIGHT=1080                # Full HD max height
VIDEO_BITRATE=2000k                  # Video bitrate
AUDIO_BITRATE=128k                   # Audio bitrate
```

## 🔧 Service Configuration

### Nginx Configuration
The nginx configuration includes:
- SSL termination
- Video streaming with range requests
- Rate limiting for uploads
- Static file serving
- WebSocket support for SignalR

### Video Processing Limits
- **File Size**: 100MB maximum
- **Duration**: 5 minutes maximum
- **Formats**: MP4, WebM, MOV, AVI, MKV
- **Output**: MP4 with H.264/AAC
- **Quality**: Up to 1080p

### Database Optimization
```sql
-- Recommended PostgreSQL settings for video workloads
shared_buffers = 256MB
effective_cache_size = 1GB
work_mem = 4MB
maintenance_work_mem = 64MB
max_connections = 100
```

## 📊 Monitoring

### Grafana Dashboards
Access Grafana at `http://your-server:3001`
- **System Metrics**: CPU, Memory, Disk usage
- **Application Metrics**: API response times, error rates
- **Video Processing**: Job queue, processing times, success rates
- **Database Metrics**: Connection pool, query performance

### Prometheus Metrics
Access Prometheus at `http://your-server:9090`
- API endpoint metrics
- Video processing job metrics
- System resource metrics
- Database performance metrics

### Log Management
Logs are collected by Promtail and stored in Loki:
- API logs: `/var/log/yapplr/api/`
- Video processor logs: `/var/log/yapplr/video-processor/`
- Container logs: Automatically collected

## 🔄 Backup and Recovery

### Automated Backups
```bash
# Create backup
./scripts/backup.sh backup

# Schedule daily backups (add to crontab)
0 2 * * * /path/to/yapplr/scripts/backup.sh backup
```

### Restore from Backup
```bash
# List available backups
./scripts/backup.sh list

# Restore specific backup
./scripts/backup.sh restore 20240106_020000
```

### S3 Backup (Optional)
Configure AWS credentials in `.env.production`:
```bash
BACKUP_S3_BUCKET=yapplr-backups
AWS_ACCESS_KEY_ID=your_access_key
AWS_SECRET_ACCESS_KEY=your_secret_key
AWS_REGION=us-east-1
```

## 🔒 Security

### SSL/TLS Configuration
- TLS 1.2+ only
- Strong cipher suites
- HSTS headers
- Certificate pinning recommended

### Rate Limiting
- API: 10 requests/second per IP
- Video uploads: 2 requests/second per IP
- Burst allowance: 20 requests

### File Upload Security
- File type validation
- Size limits enforced
- Virus scanning recommended
- Content-Type verification

## 🚨 Troubleshooting

### Common Issues

#### Video Processing Fails
```bash
# Check FFmpeg installation
docker-compose -f docker-compose.production.yml exec yapplr-video-processor ffmpeg -version

# Check processing logs
docker-compose -f docker-compose.production.yml logs yapplr-video-processor

# Check disk space
df -h
```

#### High CPU Usage
```bash
# Reduce concurrent video jobs
# Edit .env.production:
VIDEO_MAX_CONCURRENT_JOBS=1

# Restart video processor
docker-compose -f docker-compose.production.yml restart yapplr-video-processor
```

#### Database Connection Issues
```bash
# Check database status
docker-compose -f docker-compose.production.yml exec postgres pg_isready

# Check connection string
docker-compose -f docker-compose.production.yml logs yapplr-api | grep -i database
```

### Performance Tuning

#### Video Processing Optimization
- Use SSD storage for temp files
- Increase `VIDEO_MAX_CONCURRENT_JOBS` on powerful servers
- Consider GPU acceleration for large-scale deployments

#### Database Optimization
- Regular VACUUM and ANALYZE
- Monitor slow queries
- Consider read replicas for high traffic

## 📈 Scaling

### Horizontal Scaling
- Multiple video processor instances
- Load balancer for API instances
- Database read replicas
- CDN for video delivery

### Vertical Scaling
- Increase CPU cores for video processing
- Add RAM for database caching
- Use faster storage (NVMe SSD)

## 🔄 Updates and Maintenance

### Rolling Updates
```bash
# Pull latest changes
git pull origin feature/video-processing

# Rebuild and restart services
docker-compose -f docker-compose.production.yml build --no-cache
docker-compose -f docker-compose.production.yml up -d
```

### Database Migrations
```bash
# Run migrations
docker-compose -f docker-compose.production.yml exec yapplr-api dotnet ef database update
```

### Health Checks
```bash
# Check all services
./scripts/deploy-production.sh status

# Individual service health
curl https://api.yapplr.com/health
```

## 📞 Support

For deployment issues:
1. Check logs: `./scripts/deploy-production.sh logs`
2. Verify configuration: `./scripts/deploy-production.sh status`
3. Review monitoring dashboards
4. Check system resources: `htop`, `df -h`, `free -h`

## 🎯 Next Steps

After successful deployment:
1. Configure monitoring alerts
2. Set up automated backups
3. Implement CDN for video delivery
4. Configure log rotation
5. Set up SSL certificate auto-renewal
6. Plan capacity scaling based on usage

## 📋 Deployment Checklist

- [ ] Server meets minimum requirements
- [ ] Domain and SSL certificates configured
- [ ] Environment variables set in `.env.production`
- [ ] Database connection tested
- [ ] Email configuration verified
- [ ] Video processing limits configured
- [ ] Monitoring dashboards accessible
- [ ] Backup strategy implemented
- [ ] Security headers configured
- [ ] Rate limiting tested
- [ ] Health checks passing
- [ ] Log collection working
- [ ] Performance baseline established
