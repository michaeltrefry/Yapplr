/**
 * Platform-Specific Notification Configuration for React Native
 * 
 * This configuration ensures that:
 * - Mobile apps use Firebase for push notifications
 * - Web apps use SignalR for real-time notifications
 */

export const NOTIFICATION_CONFIG = {
  // Mobile (React Native) - Use Firebase
  FIREBASE_ENABLED: true,
  SIGNALR_ENABLED: false,
  
  // Platform detection
  PLATFORM: 'mobile',
  
  // Firebase configuration for mobile
  FIREBASE_CONFIG: {
    // These should match your Firebase project
    projectId: process.env.FIREBASE_PROJECT_ID || 'yapplr-bd41a',
    appId: process.env.FIREBASE_APP_ID || '1:320574424098:android:your-android-app-id',
    messagingSenderId: process.env.FIREBASE_MESSAGING_SENDER_ID || '320574424098',
    apiKey: process.env.FIREBASE_API_KEY || 'your-api-key-here',
  },
  
  // Notification preferences
  PREFERENCES: {
    // Request permission on app start
    REQUEST_PERMISSION_ON_START: true,
    
    // Show notifications when app is in foreground
    SHOW_FOREGROUND_NOTIFICATIONS: true,
    
    // Handle background notifications
    HANDLE_BACKGROUND_NOTIFICATIONS: true,
    
    // Notification channels (Android)
    CHANNELS: {
      DEFAULT: {
        id: 'yapplr-default',
        name: 'Yapplr Notifications',
        description: 'General notifications from Yapplr',
        importance: 'high',
      },
      MESSAGES: {
        id: 'yapplr-messages',
        name: 'Messages',
        description: 'New message notifications',
        importance: 'high',
      },
      SOCIAL: {
        id: 'yapplr-social',
        name: 'Social',
        description: 'Likes, follows, and social interactions',
        importance: 'default',
      }
    }
  }
};

/**
 * Get the appropriate notification service for the current platform
 */
export const getNotificationService = () => {
  if (NOTIFICATION_CONFIG.FIREBASE_ENABLED) {
    return 'firebase';
  } else if (NOTIFICATION_CONFIG.SIGNALR_ENABLED) {
    return 'signalr';
  } else {
    return 'polling';
  }
};

/**
 * Check if push notifications are supported on this platform
 */
export const isPushNotificationSupported = () => {
  return NOTIFICATION_CONFIG.FIREBASE_ENABLED;
};

/**
 * Get platform-specific notification configuration
 */
export const getPlatformConfig = () => {
  return {
    platform: NOTIFICATION_CONFIG.PLATFORM,
    service: getNotificationService(),
    firebaseEnabled: NOTIFICATION_CONFIG.FIREBASE_ENABLED,
    signalrEnabled: NOTIFICATION_CONFIG.SIGNALR_ENABLED,
    pushSupported: isPushNotificationSupported(),
  };
};
