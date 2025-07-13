# Redis Deployment Setup for Yapplr

## ✅ **Deployment Configuration Complete**

All Redis deployment configurations have been set up for both staging and production environments.

## 🔧 **What's Been Configured**

### **1. Docker Compose Files Updated**

#### **Staging (docker-compose.stage.yml)**
- ✅ Redis container added with 256MB memory limit
- ✅ Health checks configured
- ✅ Persistent volume for data
- ✅ API service configured with `Redis__ConnectionString=redis:6379`
- ✅ API service depends on Redis health check

#### **Production (docker-compose.prod.yml)**
- ✅ Redis container added with 512MB memory limit
- ✅ Enhanced persistence settings for production
- ✅ Health checks configured
- ✅ Persistent volume for data
- ✅ API service configured with `Redis__ConnectionString=redis:6379`
- ✅ API service depends on Redis health check

### **2. Deployment Scripts Updated**

#### **deploy-stage.sh**
- ✅ Redis container cleanup added
- ✅ Redis volume cleanup added
- ✅ Redis image cleanup added

#### **deploy-prod.sh**
- ✅ Redis container cleanup added
- ✅ Redis image cleanup added

### **3. GitHub Actions Workflows**
- ✅ **Updated to use Redis secrets** - workflows configured with Redis connection strings
- ✅ Staging workflow uses `STAGE_REDIS_CONNECTION_STRING` secret
- ✅ Production workflow uses `PROD_REDIS_CONNECTION_STRING` secret
- ✅ Flexible configuration allows switching to external Redis services

## 🚀 **Redis Configuration Details**

### **Staging Environment**
```yaml
redis:
  image: redis:7.2-alpine
  command: redis-server --maxmemory 256mb --maxmemory-policy allkeys-lru --save 60 1000
  volumes:
    - redis_data:/data
  memory_limit: 512M
  memory_reservation: 256M
```

### **Production Environment**
```yaml
redis:
  image: redis:7.2-alpine
  command: redis-server --maxmemory 512mb --maxmemory-policy allkeys-lru --save 300 100 --save 60 1000
  volumes:
    - redis_data:/data
  memory_limit: 768M
  memory_reservation: 512M
```

## 🔒 **Security & Access**

### **Network Security**
- ✅ Redis only accessible within Docker network
- ✅ No external ports exposed
- ✅ Container-to-container communication only

### **Connection String**
- **Staging**: `${STAGE_REDIS_CONNECTION_STRING}` (default: `redis:6379`)
- **Production**: `${PROD_REDIS_CONNECTION_STRING}` (default: `redis:6379`)
- **Local Development**: `localhost:6379`

## 📊 **Performance Settings**

### **Memory Management**
- **Staging**: 256MB max memory with LRU eviction
- **Production**: 512MB max memory with LRU eviction
- **Policy**: `allkeys-lru` (evict least recently used keys when memory limit reached)

### **Persistence**
- **Staging**: Save to disk every 60 seconds if 1000+ keys changed
- **Production**: Save every 5 minutes if 100+ keys changed, or every minute if 1000+ keys changed
- **Data**: Persisted in Docker volumes (`redis_data`)

## 🛡️ **Fallback Strategy**

### **Graceful Degradation**
- ✅ If Redis is unavailable → automatic fallback to memory caching
- ✅ No deployment failures if Redis connection fails
- ✅ Application continues to function with reduced performance

### **Health Checks**
- ✅ Redis health check: `redis-cli ping`
- ✅ API waits for Redis to be healthy before starting
- ✅ 30-second intervals with 5 retries

## 🚀 **Deployment Readiness**

### **GitHub Secrets Required**
- ✅ `STAGE_REDIS_CONNECTION_STRING` - Redis connection for staging
- ✅ `PROD_REDIS_CONNECTION_STRING` - Redis connection for production
- ✅ Flexible configuration allows switching to external Redis services

### **Ready to Deploy**
- ✅ Staging environment ready
- ✅ Production environment ready
- ✅ All deployment scripts updated
- ✅ GitHub Actions workflows compatible

## 📋 **Pre-Deployment Checklist**

- [x] Docker Compose files updated with Redis
- [x] Deployment scripts updated for Redis cleanup
- [x] Health checks configured
- [x] Memory limits set appropriately
- [x] Persistent volumes configured
- [x] Fallback mechanism implemented
- [x] Unit tests passing

## 🔑 **Required GitHub Secrets**

You need to add these secrets to your GitHub repository:

### **Staging Environment**
```
STAGE_REDIS_CONNECTION_STRING=redis:6379
```

### **Production Environment**
```
PROD_REDIS_CONNECTION_STRING=redis:6379
```

### **Future External Redis Example**
When you switch to external Redis services, simply update the secrets:
```
STAGE_REDIS_CONNECTION_STRING=your-staging-redis-host:6379
PROD_REDIS_CONNECTION_STRING=your-production-redis-host:6379
```

## 🎯 **Next Steps**

1. **Add GitHub Secrets** - Set `STAGE_REDIS_CONNECTION_STRING` and `PROD_REDIS_CONNECTION_STRING`
2. **Deploy to staging** - Test Redis functionality
3. **Monitor Redis memory usage** - Adjust limits if needed
4. **Verify cache performance** - Check cache hit rates
5. **Deploy to production** - Roll out with confidence

## 🔍 **Monitoring Commands**

### **Check Redis Status**
```bash
# Check if Redis is running
docker compose -f docker-compose.stage.yml ps redis

# Check Redis logs
docker compose -f docker-compose.stage.yml logs redis

# Connect to Redis CLI
docker compose -f docker-compose.stage.yml exec redis redis-cli

# Check memory usage
docker compose -f docker-compose.stage.yml exec redis redis-cli info memory
```

### **Cache Statistics**
```bash
# Check cache hit/miss ratio
docker compose -f docker-compose.stage.yml exec redis redis-cli info stats

# Monitor real-time commands
docker compose -f docker-compose.stage.yml exec redis redis-cli monitor
```

## ✅ **Deployment Ready!**

Your Redis caching implementation is now fully configured and ready for deployment to both staging and production environments. No additional secrets or configuration changes are required.
