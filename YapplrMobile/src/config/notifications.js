/**
 * Platform-Specific Notification Configuration for React Native
 *
 * This configuration ensures that:
 * - Mobile apps use SignalR for real-time notifications
 * - No Firebase dependencies
 */

export const NOTIFICATION_CONFIG = {
  // Mobile (React Native) - Use SignalR
  FIREBASE_ENABLED: false,
  SIGNALR_ENABLED: true,

  // Platform detection
  PLATFORM: 'mobile',

  // Notification preferences
  PREFERENCES: {
    // Show notifications when app is in foreground
    SHOW_FOREGROUND_NOTIFICATIONS: true,

    // Handle background notifications
    HANDLE_BACKGROUND_NOTIFICATIONS: true,
  }
};

/**
 * Get the appropriate notification service for the current platform
 */
export const getNotificationService = () => {
  if (NOTIFICATION_CONFIG.SIGNALR_ENABLED) {
    return 'signalr';
  } else {
    return 'polling';
  }
};

/**
 * Check if real-time notifications are supported on this platform
 */
export const isRealTimeNotificationSupported = () => {
  return NOTIFICATION_CONFIG.SIGNALR_ENABLED;
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
    realTimeSupported: isRealTimeNotificationSupported(),
  };
};
