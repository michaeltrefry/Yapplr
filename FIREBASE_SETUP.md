# Firebase Real-time Notifications Setup Guide

This guide explains how to set up Firebase for real-time messaging and notifications in the Yapplr application.

## What's Been Implemented

### Frontend (React/Next.js)
1. **Firebase SDK Integration**: Added Firebase web SDK for messaging
2. **Service Worker**: Created `firebase-messaging-sw.js` for background notifications
3. **Firebase Messaging Service**: Handles token registration and message listening
4. **NotificationContext Integration**: Updated to use Firebase instead of polling
5. **Real-time Updates**: Automatic refresh of message and notification counts

### Backend (.NET API)
1. **Firebase Admin SDK**: Added for sending push notifications
2. **FCM Token Management**: Added FCM token field to User model and API endpoint
3. **Notification Integration**: Firebase notifications sent for:
   - New messages
   - Mentions in posts/comments
   - Replies to comments
   - New followers
4. **Database Migration**: Added FCM token storage

## Firebase Project Setup

### 1. Create Firebase Project
1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Click "Create a project"
3. Enter project name (e.g., "yapplr-notifications")
4. Enable Google Analytics (optional)
5. Create project

### 2. Enable Cloud Messaging
1. In Firebase Console, go to "Project Settings" (gear icon)
2. Click on "Cloud Messaging" tab
3. Note down the "Server key" (for backend)

### 3. Add Web App
1. In Firebase Console, click "Add app" and select Web (</>) 
2. Register app with nickname (e.g., "yapplr-web")
3. Copy the Firebase configuration object

### 4. Generate VAPID Key
1. In Firebase Console, go to "Project Settings" > "Cloud Messaging"
2. In "Web configuration" section, click "Generate key pair"
3. Copy the VAPID key

### 5. Set Up Authentication (for Backend)
Since service account keys are disabled in your organization, we'll use Application Default Credentials (ADC), which is more secure.

**For Development:**
1. Install Google Cloud CLI: https://cloud.google.com/sdk/docs/install
2. Run: `gcloud auth application-default login`
3. Select your Google account that has access to the Firebase project

**For Production:**
- Use workload identity or service account impersonation
- Or run on Google Cloud services (App Engine, Cloud Run, GKE) which provide automatic credentials

## Configuration

### Frontend Environment Variables
Update `yapplr-frontend/.env.local`:

```env
NEXT_PUBLIC_API_URL=http://localhost:5161

# Firebase Configuration
NEXT_PUBLIC_FIREBASE_API_KEY=your-api-key-here
NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN=your-project.firebaseapp.com
NEXT_PUBLIC_FIREBASE_PROJECT_ID=your-project-id
NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET=your-project.appspot.com
NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID=your-sender-id
NEXT_PUBLIC_FIREBASE_APP_ID=your-app-id
NEXT_PUBLIC_FIREBASE_MEASUREMENT_ID=your-measurement-id
NEXT_PUBLIC_FIREBASE_VAPID_KEY=your-vapid-key-here
```

### Backend Configuration
Update `Yapplr.Api/appsettings.json`:

```json
{
  "Firebase": {
    "ProjectId": "yapplr"
  }
}
```

That's it! No credentials needed in the configuration since we're using Application Default Credentials.

### Service Worker Configuration
Update `yapplr-frontend/public/firebase-messaging-sw.js` with your Firebase config:

```javascript
const firebaseConfig = {
  apiKey: "your-api-key",
  authDomain: "your-project.firebaseapp.com",
  projectId: "your-project-id",
  storageBucket: "your-project.appspot.com",
  messagingSenderId: "your-sender-id",
  appId: "your-app-id",
  measurementId: "your-measurement-id"
};
```

## Testing

### 1. Start the Applications
```bash
# Backend
cd Yapplr.Api
dotnet run

# Frontend
cd yapplr-frontend
npm run dev
```

### 2. Test Notification Permission
1. Open browser to `http://localhost:3000`
2. Login to the application
3. Check browser console for Firebase initialization messages
4. Browser should prompt for notification permission

### 3. Test Real-time Notifications
1. Open two browser windows/tabs with different users
2. Send a message from one user to another
3. Mention a user in a post (@username)
4. Reply to a comment
5. Follow another user

### 4. Test Background Notifications
1. Minimize or switch away from the browser tab
2. Perform actions that should trigger notifications
3. Check if notifications appear in the system notification area

## Troubleshooting

### Common Issues
1. **Service Worker Not Registering**: Check browser console for errors
2. **No Notification Permission**: Ensure HTTPS in production
3. **Firebase Initialization Errors**: Verify configuration values
4. **Backend Firebase Errors**: Check service account key path and permissions

### Debug Steps
1. Check browser console for Firebase-related errors
2. Verify FCM tokens are being saved to the database
3. Check backend logs for Firebase notification sending attempts
4. Test with Firebase Console's "Cloud Messaging" test feature

## Production Deployment

### Security Considerations
1. Store service account key securely (environment variables or secure file storage)
2. Use HTTPS for web push notifications
3. Implement proper error handling and retry logic
4. Monitor Firebase usage and quotas

### Performance Optimization
1. Batch notifications when possible
2. Implement exponential backoff for failed sends
3. Clean up invalid FCM tokens
4. Monitor notification delivery rates

## Features Implemented

- ✅ Real-time message notifications
- ✅ Mention notifications (@username)
- ✅ Reply notifications
- ✅ Follow notifications
- ✅ Background notification support
- ✅ Automatic notification count updates
- ✅ Cross-browser compatibility
- ✅ Fallback to polling if Firebase fails
