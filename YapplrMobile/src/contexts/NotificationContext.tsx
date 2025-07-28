import React, { createContext, useContext, useEffect, useState, ReactNode, useRef, useCallback } from 'react';
import { AppState, AppStateStatus } from 'react-native';
import { useAuth } from './AuthContext';
import { useQueryClient } from '@tanstack/react-query';
import SignalRService, { SignalRNotificationPayload, SignalRConnectionStatus } from '../services/SignalRService';
import ExpoNotificationService from '../services/ExpoNotificationService';

// Safely import Notifications with fallback
let Notifications: any = null;
try {
  Notifications = require('expo-notifications');
} catch (error) {
  console.warn('📱🔔 expo-notifications not available in NotificationContext');
}

// Navigation context for smart notification handling
interface NavigationContext {
  currentScreen?: string;
  currentRoute?: any;
  conversationId?: number;
  postId?: number;
  otherUserId?: number;
}

interface NotificationContextType {
  signalRStatus: SignalRConnectionStatus;
  isSignalRReady: boolean;
  isExpoNotificationReady: boolean;
  expoPushToken: string | null;
  sendTestNotification: () => Promise<void>;
  sendTestExpoNotification: () => Promise<void>;
  refreshSignalRConnection: () => Promise<void>;
  refreshExpoNotifications: () => Promise<void>;
  // New context-aware notification methods
  showNotificationBanner: (payload: SignalRNotificationPayload) => void;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

interface NotificationProviderProps {
  children: ReactNode;
  baseURL: string;
  apiClient?: any;
}

export function NotificationProvider({ children, baseURL, apiClient }: NotificationProviderProps) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [signalRService] = useState(() => new SignalRService(baseURL));
  const [expoNotificationService] = useState(() => {
    const service = new ExpoNotificationService();
    if (apiClient) {
      service.setApiClient(apiClient);
    }
    return service;
  });
  const [signalRStatus, setSignalRStatus] = useState<SignalRConnectionStatus>({
    connected: false,
    connectionState: 'Disconnected'
  });
  const [isSignalRReady, setIsSignalRReady] = useState(false);
  const [isExpoNotificationReady, setIsExpoNotificationReady] = useState(false);
  const [expoPushToken, setExpoPushToken] = useState<string | null>(null);

  // Track navigation context for smart notification handling
  const navigationContext = useRef<NavigationContext>({});

  // Expose navigation context setter globally for the navigation tracker
  useEffect(() => {
    (global as any).setNavigationContext = (context: NavigationContext) => {
      navigationContext.current = context;
      console.log('📱🔔 Navigation context updated:', context);
    };

    return () => {
      (global as any).setNavigationContext = undefined;
    };
  }, []);

  // Smart notification banner display
  const showNotificationBanner = (payload: SignalRNotificationPayload) => {
    console.log('📱🔔 Showing notification banner:', payload);
    console.log('📱🔔 Global banner function available:', typeof (global as any).showNotificationBanner);

    // Use the global banner manager function if available
    if ((global as any).showNotificationBanner) {
      console.log('📱🔔 Calling global banner function...');
      (global as any).showNotificationBanner(payload);
    } else {
      console.warn('📱🔔 Banner manager not available, banner not shown');
    }
  };

  // Simplified notification handler (without navigation context for now)
  const handleContextAwareNotification = useCallback((payload: SignalRNotificationPayload) => {
    console.log('📱🔔 Received SignalR notification:', payload);

    const appState = AppState.currentState;

    // For now, always show banner when app is active and invalidate relevant queries
    let shouldShowBanner = appState === 'active';
    let shouldShowLocalNotification = appState !== 'active';

    // Invalidate relevant queries based on notification type
    switch (payload.type) {
      case 'message':
        console.log('📱🔔 Message notification, updating conversations and counts');
        queryClient.invalidateQueries({ queryKey: ['conversations'] });
        queryClient.invalidateQueries({ queryKey: ['unreadMessageCount'] });
        break;
      case 'VideoProcessingCompleted':
        console.log('📱🔔 Video processing completed notification, refreshing posts');
        queryClient.invalidateQueries({ queryKey: ['posts'] });
        queryClient.invalidateQueries({ queryKey: ['userPosts'] });
        queryClient.invalidateQueries({ queryKey: ['feedPosts'] });
        queryClient.invalidateQueries({ queryKey: ['timeline'] });
        queryClient.invalidateQueries({ queryKey: ['userTimeline'] });
        // If we have a specific post ID, we could invalidate just that post
        if (payload.data?.postId) {
          queryClient.invalidateQueries({ queryKey: ['post', payload.data.postId] });
        }
        break;

      case 'mention':
      case 'reply':
      case 'comment':
      case 'like':
      case 'repost':
      case 'follow':
      case 'follow_request':
      case 'test':
        console.log('📱🔔 Social/interaction notification, updating notifications and posts');
        queryClient.invalidateQueries({ queryKey: ['notifications'] });
        queryClient.invalidateQueries({ queryKey: ['notificationUnreadCount'] });
        queryClient.invalidateQueries({ queryKey: ['posts'] });
        queryClient.invalidateQueries({ queryKey: ['timeline'] });
        break;

      case 'generic':
      case 'systemMessage':
        console.log('📱🔔 System notification, updating notifications and timeline');
        queryClient.invalidateQueries({ queryKey: ['notifications'] });
        queryClient.invalidateQueries({ queryKey: ['notificationUnreadCount'] });
        queryClient.invalidateQueries({ queryKey: ['timeline'] });
        break;

      default:
        console.log('📱🔔 Unknown notification type, updating notifications');
        queryClient.invalidateQueries({ queryKey: ['notifications'] });
        queryClient.invalidateQueries({ queryKey: ['notificationUnreadCount'] });
        break;
    }

    // Show appropriate UI feedback
    if (shouldShowBanner) {
      showNotificationBanner(payload);
    }

    if (shouldShowLocalNotification && Notifications) {
      // Show local notification when app is in background
      Notifications.scheduleNotificationAsync({
        content: {
          title: payload.title,
          body: payload.body,
          data: payload.data,
        },
        trigger: null, // Show immediately
      }).catch((error: any) => {
        console.warn('📱🔔 Failed to show local notification:', error);
      });
    }

    console.log('📱🔔 Notification handling complete:', {
      type: payload.type,
      showedBanner: shouldShowBanner,
      showedLocalNotification: shouldShowLocalNotification,
    });
  }, [queryClient, showNotificationBanner]);

  // Initialize SignalR when user is authenticated
  useEffect(() => {
    if (!user) {
      // Disconnect when user logs out
      signalRService.disconnect();
      setIsSignalRReady(false);
      return;
    }

    const initializeSignalR = async () => {
      console.log('📱🔔 Initializing mobile SignalR...');

      try {
        const success = await signalRService.initialize();
        if (success) {
          setIsSignalRReady(true);
          console.log('📱🔔 Mobile SignalR initialized successfully');
        } else {
          console.warn('📱🔔 Mobile SignalR initialization failed');
          setIsSignalRReady(false);
        }
      } catch (error) {
        console.error('📱🔔 Mobile SignalR initialization error:', error);
        setIsSignalRReady(false);
      }
    };

    initializeSignalR();
  }, [user, signalRService]);

  // Initialize Expo notifications when user is authenticated
  useEffect(() => {
    if (!user) {
      // Cleanup when user logs out
      expoNotificationService.cleanup();
      setIsExpoNotificationReady(false);
      setExpoPushToken(null);
      return;
    }

    const initializeExpoNotifications = async () => {
      console.log('📱🔔 Initializing Expo notifications...');

      try {
        const success = await expoNotificationService.initialize();
        if (success) {
          const token = expoNotificationService.getExpoPushToken();
          setExpoPushToken(token);
          setIsExpoNotificationReady(true);
          console.log('📱🔔 Expo notifications initialized successfully');
          console.log('📱🔔 Expo push token:', token);
        } else {
          console.warn('📱🔔 Expo notifications initialization failed');
          setIsExpoNotificationReady(false);
          setExpoPushToken(null);
        }
      } catch (error) {
        console.error('📱🔔 Expo notifications initialization error:', error);
        // Don't fail completely - just disable Expo notifications
        setIsExpoNotificationReady(false);
        setExpoPushToken(null);
      }
    };

    // Initialize Expo notifications after a small delay to let SignalR stabilize
    const timer = setTimeout(() => {
      initializeExpoNotifications();
    }, 2000); // 2 second delay

    return () => clearTimeout(timer);
  }, [user, expoNotificationService]);

  // Listen for SignalR connection status changes
  useEffect(() => {
    const handleConnectionStatusChange = (status: SignalRConnectionStatus) => {
      console.log('📱🔔 SignalR connection status changed:', status);
      setSignalRStatus(status);
      setIsSignalRReady(status.connected);
    };

    signalRService.addConnectionListener(handleConnectionStatusChange);

    return () => {
      signalRService.removeConnectionListener(handleConnectionStatusChange);
    };
  }, [signalRService]);

  // Listen for SignalR notifications with context-aware handling
  useEffect(() => {
    if (!isSignalRReady) return;

    signalRService.addMessageListener(handleContextAwareNotification);

    return () => {
      signalRService.removeMessageListener(handleContextAwareNotification);
    };
  }, [isSignalRReady, signalRService, handleContextAwareNotification]);

  // Listen for Expo push notifications
  useEffect(() => {
    if (!isExpoNotificationReady || !Notifications) return;

    const handleExpoNotification = (notification: any) => {
      console.log('📱🔔 Received Expo notification:', notification);

      // Process the notification
      console.log('📱🔔 Processing Expo notification:', {
        title: notification.request.content.title,
        body: notification.request.content.body,
        data: notification.request.content.data
      });
    };

    const handleExpoNotificationResponse = (response: any) => {
      console.log('📱🔔 User tapped on notification:', response);

      // Handle navigation or other actions based on notification data
      const data = response.notification.request.content.data;
      if (data) {
        console.log('📱🔔 Notification data:', data);
        // TODO: Add navigation logic based on notification type
      }
    };

    try {
      const notificationListener = expoNotificationService.addNotificationReceivedListener(handleExpoNotification);
      const responseListener = expoNotificationService.addNotificationResponseListener(handleExpoNotificationResponse);

      return () => {
        notificationListener?.remove();
        responseListener?.remove();
      };
    } catch (error) {
      console.warn('📱🔔 Failed to set up Expo notification listeners:', error);
    }
  }, [isExpoNotificationReady, expoNotificationService]);

  // Handle app state changes (foreground/background)
  useEffect(() => {
    const handleAppStateChange = (nextAppState: AppStateStatus) => {
      console.log('📱🔔 App state changed to:', nextAppState);

      if (nextAppState === 'active' && user) {
        // App came to foreground, try to reconnect services if needed
        if (!isSignalRReady) {
          console.log('📱🔔 App became active, attempting SignalR reconnection...');
          signalRService.initialize().then(success => {
            if (success) {
              setIsSignalRReady(true);
              console.log('📱🔔 SignalR reconnected successfully');
            }
          });
        }

        if (!isExpoNotificationReady) {
          console.log('📱🔔 App became active, attempting Expo notifications reconnection...');
          expoNotificationService.initialize().then(success => {
            if (success) {
              const token = expoNotificationService.getExpoPushToken();
              setExpoPushToken(token);
              setIsExpoNotificationReady(true);
              console.log('📱🔔 Expo notifications reconnected successfully');
            }
          });
        }
      } else if (nextAppState === 'background') {
        // App went to background
        console.log('📱🔔 App went to background');
        // Note: We don't disconnect services here to maintain notifications
        // The connections will be maintained in the background
      }
    };

    const subscription = AppState.addEventListener('change', handleAppStateChange);

    return () => {
      subscription?.remove();
    };
  }, [user, isSignalRReady, isExpoNotificationReady, signalRService, expoNotificationService]);

  // Test SignalR notification function
  const sendTestNotification = async (): Promise<void> => {
    try {
      await signalRService.ping();
      console.log('📱🔔 Test SignalR notification sent');
    } catch (error) {
      console.error('📱🔔 Failed to send test SignalR notification:', error);
      throw error;
    }
  };

  // Test Expo notification function
  const sendTestExpoNotification = async (): Promise<void> => {
    try {
      await expoNotificationService.sendTestNotification();
      console.log('📱🔔 Test Expo notification sent');
    } catch (error) {
      console.error('📱🔔 Failed to send test Expo notification:', error);
      throw error;
    }
  };

  // Refresh SignalR connection
  const refreshSignalRConnection = async (): Promise<void> => {
    try {
      console.log('📱🔔 Refreshing SignalR connection...');
      await signalRService.disconnect();
      const success = await signalRService.initialize();
      setIsSignalRReady(success);

      if (success) {
        console.log('📱🔔 SignalR connection refreshed successfully');
      } else {
        console.warn('📱🔔 SignalR connection refresh failed');
      }
    } catch (error) {
      console.error('📱🔔 Error refreshing SignalR connection:', error);
      setIsSignalRReady(false);
      throw error;
    }
  };

  // Refresh Expo notifications
  const refreshExpoNotifications = async (): Promise<void> => {
    try {
      console.log('📱🔔 Refreshing Expo notifications...');
      expoNotificationService.cleanup();
      const success = await expoNotificationService.initialize();

      if (success) {
        const token = expoNotificationService.getExpoPushToken();
        setExpoPushToken(token);
        setIsExpoNotificationReady(true);
        console.log('📱🔔 Expo notifications refreshed successfully');
      } else {
        setIsExpoNotificationReady(false);
        setExpoPushToken(null);
        console.warn('📱🔔 Expo notifications refresh failed');
      }
    } catch (error) {
      console.error('📱🔔 Error refreshing Expo notifications:', error);
      setIsExpoNotificationReady(false);
      setExpoPushToken(null);
      throw error;
    }
  };

  // Cleanup on unmount only
  useEffect(() => {
    return () => {
      console.log('📱🔔 NotificationProvider unmounting, cleaning up services...');
      signalRService.disconnect();
      expoNotificationService.cleanup();
    };
  }, []); // Empty dependency array - only run on mount/unmount

  const value: NotificationContextType = {
    signalRStatus,
    isSignalRReady,
    isExpoNotificationReady,
    expoPushToken,
    sendTestNotification,
    sendTestExpoNotification,
    refreshSignalRConnection,
    refreshExpoNotifications,
    showNotificationBanner,
  };

  return (
    <NotificationContext.Provider value={value}>
      {children}
    </NotificationContext.Provider>
  );
}

export function useNotifications(): NotificationContextType {
  const context = useContext(NotificationContext);
  if (context === undefined) {
    throw new Error('useNotifications must be used within a NotificationProvider');
  }
  return context;
}

export default NotificationContext;
