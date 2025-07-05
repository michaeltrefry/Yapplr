import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { AppState, AppStateStatus } from 'react-native';
import { useAuth } from './AuthContext';
import SignalRService, { SignalRNotificationPayload, SignalRConnectionStatus } from '../services/SignalRService';

interface NotificationContextType {
  signalRStatus: SignalRConnectionStatus;
  isSignalRReady: boolean;
  sendTestNotification: () => Promise<void>;
  refreshSignalRConnection: () => Promise<void>;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

interface NotificationProviderProps {
  children: ReactNode;
  baseURL: string;
}

export function NotificationProvider({ children, baseURL }: NotificationProviderProps) {
  const { user } = useAuth();
  const [signalRService] = useState(() => new SignalRService(baseURL));
  const [signalRStatus, setSignalRStatus] = useState<SignalRConnectionStatus>({
    connected: false,
    connectionState: 'Disconnected'
  });
  const [isSignalRReady, setIsSignalRReady] = useState(false);

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

  // Listen for SignalR notifications
  useEffect(() => {
    if (!isSignalRReady) return;

    const handleSignalRNotification = (payload: SignalRNotificationPayload) => {
      console.log('📱🔔 Received mobile notification:', payload);
      
      // Here you could:
      // 1. Show a local notification
      // 2. Update badge counts
      // 3. Refresh data
      // 4. Navigate to relevant screen
      
      // For now, just log it
      console.log('📱🔔 Processing notification:', {
        type: payload.type,
        title: payload.title,
        body: payload.body,
        data: payload.data
      });
    };

    signalRService.addMessageListener(handleSignalRNotification);

    return () => {
      signalRService.removeMessageListener(handleSignalRNotification);
    };
  }, [isSignalRReady, signalRService]);

  // Handle app state changes (foreground/background)
  useEffect(() => {
    const handleAppStateChange = (nextAppState: AppStateStatus) => {
      console.log('📱🔔 App state changed to:', nextAppState);
      
      if (nextAppState === 'active' && user && !isSignalRReady) {
        // App came to foreground and SignalR is not connected, try to reconnect
        console.log('📱🔔 App became active, attempting SignalR reconnection...');
        signalRService.initialize().then(success => {
          if (success) {
            setIsSignalRReady(true);
            console.log('📱🔔 SignalR reconnected successfully');
          }
        });
      } else if (nextAppState === 'background') {
        // App went to background
        console.log('📱🔔 App went to background');
        // Note: We don't disconnect SignalR here to maintain real-time notifications
        // The connection will be maintained in the background
      }
    };

    const subscription = AppState.addEventListener('change', handleAppStateChange);

    return () => {
      subscription?.remove();
    };
  }, [user, isSignalRReady, signalRService]);

  // Test notification function
  const sendTestNotification = async (): Promise<void> => {
    try {
      await signalRService.ping();
      console.log('📱🔔 Test notification sent');
    } catch (error) {
      console.error('📱🔔 Failed to send test notification:', error);
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

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      signalRService.disconnect();
    };
  }, [signalRService]);

  const value: NotificationContextType = {
    signalRStatus,
    isSignalRReady,
    sendTestNotification,
    refreshSignalRConnection,
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
