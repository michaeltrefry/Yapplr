# Redis Secrets Setup Guide

## 🔑 **GitHub Secrets to Add**

You need to add these two secrets to your GitHub repository before deploying:

### **1. Staging Redis Connection**
- **Secret Name**: `STAGE_REDIS_CONNECTION_STRING`
- **Secret Value**: `redis:6379`

### **2. Production Redis Connection**
- **Secret Name**: `PROD_REDIS_CONNECTION_STRING`
- **Secret Value**: `redis:6379`

## 📋 **How to Add GitHub Secrets**

1. Go to your GitHub repository
2. Click **Settings** tab
3. Click **Secrets and variables** → **Actions**
4. Click **New repository secret**
5. Add each secret:

### **Secret 1:**
```
Name: STAGE_REDIS_CONNECTION_STRING
Value: redis:6379
```

### **Secret 2:**
```
Name: PROD_REDIS_CONNECTION_STRING
Value: redis:6379
```

## 🔄 **Future Flexibility**

When you decide to use external Redis services, simply update these secrets:

### **Example with AWS ElastiCache:**
```
STAGE_REDIS_CONNECTION_STRING=your-staging-redis.cache.amazonaws.com:6379
PROD_REDIS_CONNECTION_STRING=your-production-redis.cache.amazonaws.com:6379
```

### **Example with Azure Redis:**
```
STAGE_REDIS_CONNECTION_STRING=your-staging-redis.redis.cache.windows.net:6380,ssl=True
PROD_REDIS_CONNECTION_STRING=your-production-redis.redis.cache.windows.net:6380,ssl=True
```

## ✅ **After Adding Secrets**

Once you've added both secrets:

1. ✅ Your deployments will work correctly
2. ✅ Redis containers will be used initially
3. ✅ You can switch to external Redis anytime by updating the secrets
4. ✅ No code changes needed when switching

## 🚀 **Ready to Deploy**

After adding these secrets, your Redis caching implementation is ready for deployment!

## 🔍 **Verification**

You can verify the secrets are set correctly by:

1. Going to **Settings** → **Secrets and variables** → **Actions**
2. You should see both secrets listed:
   - `STAGE_REDIS_CONNECTION_STRING`
   - `PROD_REDIS_CONNECTION_STRING`

## ⚠️ **Important Notes**

- **Container Redis**: Use `redis:6379` for Docker container communication
- **External Redis**: Use full hostname and port for external services
- **SSL/TLS**: Add `,ssl=True` parameter if external Redis requires encryption
- **Authentication**: Add username/password if required: `username:password@host:port`

## 🎯 **Next Steps**

1. Add the two GitHub secrets above
2. Deploy to staging to test Redis functionality
3. Monitor performance and cache hit rates
4. Deploy to production when ready
