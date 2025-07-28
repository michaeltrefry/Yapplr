import { Platform } from 'react-native';
import Constants from 'expo-constants';

// Safely import modules with fallbacks
let Device: any = null;
let Notifications: any = null;

try {
  Device = require('expo-device');
} catch (error) {
  console.warn('📱🔔 expo-device not available, using fallback');
}

try {
  Notifications = require('expo-notifications');
} catch (error) {
  console.warn('📱🔔 expo-notifications not available, using fallback');
}

export interface ExpoPushToken {
  data: string;
  type: 'expo';
}

export interface NotificationContent {
  title: string;
  body: string;
  data?: any;
}

export interface NotificationResponse {
  notification: any; // Notifications.Notification
  actionIdentifier: string;
  userText?: string;
}

class ExpoNotificationService {
  private expoPushToken: string | null = null;
  private isInitialized = false;
  private apiClient: any = null;

  constructor() {
    this.setupNotificationHandler();
  }

  /**
   * Set the API client for token registration
   */
  setApiClient(apiClient: any) {
    this.apiClient = apiClient;
  }

  /**
   * Configure how notifications are handled when the app is in the foreground
   */
  private setupNotificationHandler() {
    if (Notifications && Notifications.setNotificationHandler) {
      Notifications.setNotificationHandler({
        handleNotification: async () => ({
          shouldPlaySound: true,
          shouldSetBadge: true,
          shouldShowBanner: true,
          shouldShowList: true,
        }),
      });
    } else {
      console.warn('📱🔔 Notifications.setNotificationHandler not available');
    }
  }

  /**
   * Initialize the notification service and register for push notifications
   */
  async initialize(): Promise<boolean> {
    try {
      console.log('📱🔔 Initializing Expo notification service...');

      // Check if Notifications module is available
      if (!Notifications) {
        console.warn('📱🔔 Expo notifications module not available');
        if (__DEV__) {
          console.log('📱🔔 Development mode: using mock token for testing');
          // Generate a mock token for development testing
          this.expoPushToken = 'ExponentPushToken[DEVELOPMENT_MOCK_TOKEN_' + Date.now() + ']';
          this.isInitialized = true;

          // Register mock token with backend
          await this.registerTokenWithBackend(this.expoPushToken);

          console.log('📱🔔 Mock Expo notification service initialized');
          console.log('📱🔔 Mock push token:', this.expoPushToken);
          return true;
        } else {
          return false;
        }
      }

      // Check if running on a physical device
      if (!Device || !Device.isDevice) {
        console.warn('📱🔔 Push notifications only work on physical devices or Device module not available');
        // In development, we can still continue for testing purposes
        if (__DEV__) {
          console.log('📱🔔 Development mode: continuing without device check');
        } else {
          return false;
        }
      }

      // Set up Android notification channel
      if (Platform.OS === 'android') {
        await this.setupAndroidNotificationChannel();
      }

      // Register for push notifications
      const token = await this.registerForPushNotifications();
      if (token) {
        this.expoPushToken = token;
        this.isInitialized = true;
        console.log('📱🔔 Expo notification service initialized successfully');
        console.log('📱🔔 Push token:', token);

        // Register token with backend
        await this.registerTokenWithBackend(token);

        return true;
      }

      return false;
    } catch (error) {
      console.error('📱🔔 Failed to initialize Expo notification service:', error);
      return false;
    }
  }

  /**
   * Set up Android notification channel for proper notification display
   */
  private async setupAndroidNotificationChannel() {
    if (Notifications && Notifications.setNotificationChannelAsync) {
      await Notifications.setNotificationChannelAsync('default', {
        name: 'Default',
        importance: Notifications.AndroidImportance?.MAX || 4,
        vibrationPattern: [0, 250, 250, 250],
        lightColor: '#FF231F7C',
        sound: 'default',
        enableLights: true,
        enableVibrate: true,
        showBadge: true,
      });
    } else {
      console.warn('📱🔔 Notifications.setNotificationChannelAsync not available');
    }
  }

  /**
   * Register for push notifications and get the Expo push token
   */
  private async registerForPushNotifications(): Promise<string | null> {
    if (!Notifications) {
      console.warn('📱🔔 Notifications module not available for registration');
      return null;
    }

    try {
      // Check existing permissions
      const { status: existingStatus } = await Notifications.getPermissionsAsync();
      let finalStatus = existingStatus;

      // Request permissions if not already granted
      if (existingStatus !== 'granted') {
        const { status } = await Notifications.requestPermissionsAsync();
        finalStatus = status;
      }

      if (finalStatus !== 'granted') {
        console.warn('📱🔔 Permission not granted for push notifications');
        return null;
      }

      // Get the project ID from app config
      const projectId = this.getProjectId();
      if (!projectId) {
        console.error('📱🔔 Project ID not found in app config');
        return null;
      }

      // Get the Expo push token
      const pushTokenData = await Notifications.getExpoPushTokenAsync({
        projectId,
      });

      return pushTokenData.data;
    } catch (error) {
      console.error('📱🔔 Error registering for push notifications:', error);
      return null;
    }
  }

  /**
   * Get the project ID from the app configuration
   */
  private getProjectId(): string | null {
    const projectId = 
      Constants?.expoConfig?.extra?.eas?.projectId ?? 
      Constants?.easConfig?.projectId;
    
    if (!projectId) {
      console.error('📱🔔 Project ID not found. Make sure your app.json has the EAS project ID configured.');
    }

    return projectId;
  }

  /**
   * Get the current Expo push token
   */
  getExpoPushToken(): string | null {
    return this.expoPushToken;
  }

  /**
   * Check if the service is initialized and ready
   */
  isReady(): boolean {
    return this.isInitialized && this.expoPushToken !== null;
  }

  /**
   * Add a listener for incoming notifications when the app is in the foreground
   */
  addNotificationReceivedListener(
    listener: (notification: any) => void
  ): any {
    if (Notifications && Notifications.addNotificationReceivedListener) {
      return Notifications.addNotificationReceivedListener(listener);
    } else {
      console.log('📱🔔 Mock notification listener added');
      return { remove: () => console.log('📱🔔 Mock notification listener removed') };
    }
  }

  /**
   * Add a listener for notification responses (when user taps on a notification)
   */
  addNotificationResponseListener(
    listener: (response: any) => void
  ): any {
    if (Notifications && Notifications.addNotificationResponseReceivedListener) {
      return Notifications.addNotificationResponseReceivedListener(listener);
    } else {
      console.log('📱🔔 Mock notification response listener added');
      return { remove: () => console.log('📱🔔 Mock notification response listener removed') };
    }
  }

  /**
   * Send a test notification (for development/testing purposes)
   */
  async sendTestNotification(): Promise<void> {
    if (!this.expoPushToken) {
      throw new Error('No push token available. Initialize the service first.');
    }

    const message = {
      to: this.expoPushToken,
      sound: 'default',
      title: 'Test Notification',
      body: 'This is a test notification from your Yapplr app!',
      data: { 
        type: 'test',
        timestamp: new Date().toISOString()
      },
    };

    try {
      const response = await fetch('https://exp.host/--/api/v2/push/send', {
        method: 'POST',
        headers: {
          Accept: 'application/json',
          'Accept-encoding': 'gzip, deflate',
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(message),
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const result = await response.json();
      console.log('📱🔔 Test notification sent successfully:', result);
    } catch (error) {
      console.error('📱🔔 Failed to send test notification:', error);
      throw error;
    }
  }

  /**
   * Get current notification permissions status
   */
  async getPermissionsStatus(): Promise<any> { // Notifications.NotificationPermissionsStatus
    return await Notifications.getPermissionsAsync();
  }

  /**
   * Request notification permissions
   */
  async requestPermissions(): Promise<any> { // Notifications.NotificationPermissionsStatus
    return await Notifications.requestPermissionsAsync();
  }

  /**
   * Register the Expo push token with the backend
   */
  private async registerTokenWithBackend(token: string): Promise<void> {
    if (!this.apiClient) {
      console.warn('📱🔔 No API client available for token registration');
      return;
    }

    try {
      console.log('📱🔔 Registering Expo push token with backend...');
      await this.apiClient.users.updateExpoPushToken({ token });
      console.log('📱🔔 Expo push token registered successfully with backend');
    } catch (error) {
      console.error('📱🔔 Failed to register Expo push token with backend:', error);

      // Check if it's an authentication error
      if (error && typeof error === 'object' && 'status' in error && error.status === 401) {
        console.error('📱🔔 Authentication failed - user may not be properly logged in');
      }

      // Don't throw error here as the local token is still valid for local notifications
    }
  }

  /**
   * Cleanup method to remove listeners and reset state
   */
  cleanup() {
    this.expoPushToken = null;
    this.isInitialized = false;
    console.log('📱🔔 Expo notification service cleaned up');
  }
}

export default ExpoNotificationService;
