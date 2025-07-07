# GitHub Actions Deployment Strategy

## 🎯 **Deployment Workflow Structure**

The project now uses separate GitHub Actions workflows for independent API and frontend deployments:

### **📁 Workflow Files:**
- **`deploy-api.yml`**: Backend API deployment only
- **`deploy-frontend.yml`**: Frontend deployment with SignalR-only configuration

## 🔧 **API Deployment (deploy-api.yml)**

Triggers on changes to `Yapplr.Api/**` and deploys only the backend API.

### **API Deployment Flow**

When changes are pushed to the `Yapplr.Api/**` directory on the `main` branch:

1. **🏗️ Build API**: Creates Docker image for the .NET API
2. **🔄 Pull Latest Code**: Updates the repository on the Linode server
3. **⚙️ Configure Environment**: Sets up API secrets and configuration
4. **🚀 Deploy API**: Starts the new API container
5. **🩺 Health Check**: Verifies API accessibility at `/health` endpoint

### **API Configuration**

The API deployment includes:
- **Database Connection**: PostgreSQL connection string
- **Authentication**: JWT secret key configuration
- **Email Service**: AWS SES configuration for password reset
- **Notifications**: Firebase service account for backend notifications
- **SignalR**: Real-time notification hub configuration

## 🎯 **Frontend Deployment (deploy-frontend.yml)**

The frontend workflow is configured to automatically deploy with **SignalR-only** configuration for optimal web performance.

### **Deployment Flow**

When changes are pushed to the `yapplr-frontend/**` directory on the `main` branch:

1. **🔄 Pull Latest Code**: Updates the repository on the Linode server
2. **🔧 Configure SignalR-Only**: Runs `node configure-notifications.js signalr-only`
3. **✅ Verify Configuration**: Runs `node configure-notifications.js status` to confirm
4. **🏗️ Build Frontend**: Builds Docker image with SignalR-only configuration
5. **🚀 Deploy**: Starts the new frontend container

### **Configuration Applied**

```bash
# Automatically applied during deployment
NEXT_PUBLIC_ENABLE_FIREBASE=false
NEXT_PUBLIC_ENABLE_FIREBASE_WEB=false
NEXT_PUBLIC_ENABLE_SIGNALR=true
NEXT_PUBLIC_ENABLE_SIGNALR_MOBILE=false
```

### **Why SignalR-Only for Production Web?**

#### ✅ **Advantages for Web Users:**
- **Real-time Performance**: Instant WebSocket notifications
- **Lower Latency**: Direct connection to server
- **Better UX**: Immediate feedback for web interactions
- **No Permission Prompts**: Works without browser notification permissions
- **Reliable Connection**: Persistent connection while browsing

#### 🎯 **Platform Strategy:**
- **Web Frontend (Production)**: SignalR-only via GitHub Actions
- **Mobile Apps**: Firebase push notifications (deployed separately)
- **Development**: Configurable via npm scripts for testing

### **Manual Override**

If you need to deploy with a different configuration:

```bash
# On the server, before deployment
cd /opt/Yapplr
node configure-notifications.js [configuration]

# Available options:
# - signalr-only (default for production web)
# - firebase-only
# - both
# - platform-optimized
# - none
```

### **Monitoring Deployment**

The workflow includes:
- **Configuration Verification**: Confirms SignalR-only is applied
- **Health Checks**: Verifies frontend accessibility
- **Deployment Logs**: Shows configuration and build progress

### **Environment Variables**

The workflow sets these environment variables during build:

```yaml
# Firebase variables (available but disabled in SignalR-only mode)
NEXT_PUBLIC_FIREBASE_API_KEY
NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN
NEXT_PUBLIC_FIREBASE_DATABASE_URL
NEXT_PUBLIC_PROD_FIREBASE_PROJECT_ID
NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET
NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID
NEXT_PUBLIC_FIREBASE_APP_ID
NEXT_PUBLIC_FIREBASE_MEASUREMENT_ID
NEXT_PUBLIC_FIREBASE_VAPID_KEY

# Notification Provider Configuration (SignalR-only)
NEXT_PUBLIC_ENABLE_FIREBASE=false
NEXT_PUBLIC_ENABLE_FIREBASE_WEB=false
NEXT_PUBLIC_ENABLE_SIGNALR=true
NEXT_PUBLIC_ENABLE_SIGNALR_MOBILE=false
```

### **Workflow Triggers**

**API Deployment (`deploy-api.yml`)** runs when:
- **Push to main**: Changes in `Yapplr.Api/**` directory
- **Manual trigger**: Via GitHub Actions "workflow_dispatch"

**Frontend Deployment (`deploy-frontend.yml`)** runs when:
- **Push to main**: Changes in `yapplr-frontend/**` directory
- **Manual trigger**: Via GitHub Actions "workflow_dispatch"

### **Expected Behavior**

After deployment, the production frontend will:
- ✅ Use SignalR for real-time notifications
- ✅ Connect via WebSocket to the backend
- ✅ Provide instant notification delivery for web users
- ✅ Fall back to polling if SignalR fails
- ❌ Not attempt Firebase initialization (disabled)

This ensures optimal performance for web users while maintaining the flexibility to use Firebase for mobile applications deployed through other channels.

## 🔧 **Backend Configuration**

The backend continues to support both providers, allowing:
- **Web clients**: Connect via SignalR
- **Mobile clients**: Connect via Firebase (when deployed separately)
- **Flexibility**: Easy switching between configurations for testing

This deployment strategy provides the best user experience for each platform while maintaining a unified backend system.
