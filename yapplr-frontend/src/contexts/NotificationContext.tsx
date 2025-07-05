'use client';

import React, { createContext, useContext, useEffect, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { messageApi, notificationApi } from '@/lib/api';
import { useAuth } from './AuthContext';
import { firebaseMessagingService, FirebaseNotificationPayload } from '@/lib/firebaseMessaging';
import { signalRMessagingService, SignalRNotificationPayload } from '@/lib/signalRMessaging';

interface NotificationContextType {
  unreadMessageCount: number;
  unreadNotificationCount: number;
  refreshUnreadCount: () => void;
  refreshNotificationCount: () => void;
  isFirebaseReady: boolean;
  isSignalRReady: boolean;
  activeNotificationProvider: 'firebase' | 'signalr' | 'polling' | 'none';
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export function NotificationProvider({ children }: { children: React.ReactNode }) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [isFirebaseReady, setIsFirebaseReady] = useState(false);
  const [isSignalRReady, setIsSignalRReady] = useState(false);
  const [activeNotificationProvider, setActiveNotificationProvider] = useState<'firebase' | 'signalr' | 'polling' | 'none'>('none');
  const [retryCount, setRetryCount] = useState(0);
  const maxRetries = 3;

  // Platform detection
  const isMobile = typeof window !== 'undefined' &&
    (/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent) ||
     window.innerWidth <= 768);

  // Platform-specific configuration
  const isFirebaseEnabled = isMobile ?
    process.env.NEXT_PUBLIC_ENABLE_FIREBASE === 'true' :
    process.env.NEXT_PUBLIC_ENABLE_FIREBASE_WEB === 'true';

  const isSignalREnabled = !isMobile ?
    process.env.NEXT_PUBLIC_ENABLE_SIGNALR === 'true' :
    process.env.NEXT_PUBLIC_ENABLE_SIGNALR_MOBILE === 'true';

  // Determine polling interval based on active notification provider
  const hasRealtimeNotifications = isFirebaseReady || isSignalRReady;
  const pollingInterval = hasRealtimeNotifications ? 60000 : 30000; // Slower polling when real-time is available

  const { data: unreadData, refetch } = useQuery({
    queryKey: ['unreadMessageCount'],
    queryFn: messageApi.getUnreadCount,
    enabled: !!user,
    refetchInterval: pollingInterval,
    refetchIntervalInBackground: false,
  });

  const { data: notificationData, refetch: refetchNotifications } = useQuery({
    queryKey: ['unreadNotificationCount'],
    queryFn: notificationApi.getUnreadCount,
    enabled: !!user,
    refetchInterval: pollingInterval,
    refetchIntervalInBackground: false,
  });

  const unreadMessageCount = unreadData?.unreadCount || 0;
  const unreadNotificationCount = notificationData?.unreadCount || 0;

  const refreshUnreadCount = () => {
    refetch();
  };

  const refreshNotificationCount = () => {
    refetchNotifications();
  };

  // Initialize notification providers when user is authenticated
  useEffect(() => {
    if (!user) return;

    const initializeNotificationProviders = async () => {
      console.log('🔔 Initializing notification providers...');
      console.log('🔔 Configuration - Firebase enabled:', isFirebaseEnabled, 'SignalR enabled:', isSignalREnabled);

      // Try Firebase first (but only if enabled and permissions might be available)
      if (isFirebaseEnabled && !isFirebaseReady) {
        console.log('🔥 Attempting Firebase initialization...');

        // Check if notifications are supported and not denied
        const notificationPermission = typeof window !== 'undefined' && 'Notification' in window
          ? Notification.permission
          : 'default';

        if (notificationPermission === 'denied') {
          console.warn('🔥 Firebase skipped: Notification permission denied, trying SignalR...');
        } else {
          const firebaseSuccess = await firebaseMessagingService.initialize();
          if (firebaseSuccess) {
            setIsFirebaseReady(true);
            setActiveNotificationProvider('firebase');
            console.log('🔥 Firebase messaging initialized successfully');
            return; // Firebase is working, no need to try SignalR
          } else {
            console.warn('🔥 Firebase messaging initialization failed, trying SignalR...');
          }
        }
      } else if (!isFirebaseEnabled) {
        console.log('🔥 Firebase is disabled via configuration');
      }

      // If Firebase failed, is denied, disabled, or is not ready, try SignalR
      if (isSignalREnabled && !isSignalRReady) {
        console.log('📡 Attempting SignalR initialization as fallback...');
        try {
          // Check if SignalR is already ready first
          if (signalRMessagingService.isReady()) {
            console.log('📡 SignalR is already ready');
            setIsSignalRReady(true);
            setActiveNotificationProvider('signalr');
            return;
          }

          const signalRSuccess = await signalRMessagingService.initialize();
          if (signalRSuccess) {
            setIsSignalRReady(true);
            setActiveNotificationProvider('signalr');
            console.log('📡 SignalR messaging initialized successfully');
            return;
          } else {
            console.warn('📡 SignalR messaging initialization failed');
          }
        } catch (error) {
          console.error('📡 SignalR initialization error:', error);
        }
      } else if (!isSignalREnabled) {
        console.log('📡 SignalR is disabled via configuration');
      }

      // If both failed or are disabled, fall back to polling
      if (!isFirebaseEnabled && !isSignalREnabled) {
        console.warn('🔔 Both Firebase and SignalR are disabled, using polling only');
      } else {
        console.warn('🔔 All enabled notification providers failed, using polling only');
      }
      setActiveNotificationProvider('polling');
    };

    initializeNotificationProviders();
  }, [user]); // Only depend on user, not on the ready states to avoid loops

  // Monitor SignalR connection and retry if it disconnects
  useEffect(() => {
    if (!isSignalRReady) return;

    const handleSignalRConnectionChange = (connected: boolean) => {
      console.log('📡 SignalR connection status changed:', connected);
      if (!connected && retryCount < maxRetries) {
        console.log(`📡 SignalR disconnected, attempting retry ${retryCount + 1}/${maxRetries}`);
        setRetryCount(prev => prev + 1);

        // Retry after a delay
        setTimeout(async () => {
          const success = await signalRMessagingService.initialize();
          if (!success) {
            console.warn('📡 SignalR retry failed, falling back to polling');
            setIsSignalRReady(false);
            setActiveNotificationProvider('polling');
          }
        }, 2000 * (retryCount + 1)); // Exponential backoff
      } else if (!connected && retryCount >= maxRetries) {
        console.warn('📡 SignalR max retries reached, falling back to polling');
        setIsSignalRReady(false);
        setActiveNotificationProvider('polling');
      }
    };

    signalRMessagingService.addConnectionListener(handleSignalRConnectionChange);

    return () => {
      signalRMessagingService.removeConnectionListener(handleSignalRConnectionChange);
    };
  }, [isSignalRReady, retryCount, maxRetries]);

  // Generic notification handler for both Firebase and SignalR
  const handleNotificationMessage = (type: string, provider: string) => {
    console.log(`🔔 NOTIFICATION CONTEXT RECEIVED ${provider.toUpperCase()} MESSAGE`);
    console.log(`🔔 Message type: ${type}`);
    console.log('🔔 Timestamp:', new Date().toISOString());

    // Refresh counts based on notification type
    if (type === 'message') {
      console.log('🔔 Refreshing unread message count');
      refreshUnreadCount();
    } else if (['mention', 'reply', 'comment', 'follow', 'like', 'repost', 'follow_request'].includes(type)) {
      console.log('🔔 Refreshing notification count');
      refreshNotificationCount();
    }

    // Invalidate relevant queries to refresh data
    console.log('🔔 Invalidating queries');
    queryClient.invalidateQueries({ queryKey: ['conversations'] });
    queryClient.invalidateQueries({ queryKey: ['notifications'] });

    console.log(`🔔 ${provider} message handling complete`);
  };

  // Listen for Firebase messages
  useEffect(() => {
    if (!isFirebaseReady) return;

    const handleFirebaseMessage = (payload: FirebaseNotificationPayload) => {
      handleNotificationMessage(payload.data?.type || 'unknown', 'Firebase');
    };

    firebaseMessagingService.addMessageListener(handleFirebaseMessage);

    return () => {
      firebaseMessagingService.removeMessageListener(handleFirebaseMessage);
    };
  }, [isFirebaseReady, queryClient, refreshUnreadCount, refreshNotificationCount]);

  // Listen for SignalR messages
  useEffect(() => {
    if (!isSignalRReady) return;

    const handleSignalRMessage = (payload: SignalRNotificationPayload) => {
      handleNotificationMessage(payload.type, 'SignalR');
    };

    signalRMessagingService.addMessageListener(handleSignalRMessage);

    return () => {
      signalRMessagingService.removeMessageListener(handleSignalRMessage);
    };
  }, [isSignalRReady, queryClient, refreshUnreadCount, refreshNotificationCount]);

  // Listen for changes in conversations to refresh unread count (fallback for non-Firebase updates)
  useEffect(() => {
    // Listen for query invalidations that might affect unread count
    const unsubscribe = queryClient.getQueryCache().subscribe((event) => {
      if (event.type === 'updated' && event.query.queryKey[0] === 'conversations') {
        refreshUnreadCount();
      }
    });

    return unsubscribe;
  }, [queryClient, refreshUnreadCount]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (isFirebaseReady) {
        firebaseMessagingService.disconnect?.();
      }
      if (isSignalRReady) {
        signalRMessagingService.disconnect();
      }
    };
  }, []);

  return (
    <NotificationContext.Provider
      value={{
        unreadMessageCount,
        unreadNotificationCount,
        refreshUnreadCount,
        refreshNotificationCount,
        isFirebaseReady,
        isSignalRReady,
        activeNotificationProvider,
      }}
    >
      {children}
    </NotificationContext.Provider>
  );
}

export function useNotifications() {
  const context = useContext(NotificationContext);
  if (context === undefined) {
    throw new Error('useNotifications must be used within a NotificationProvider');
  }
  return context;
}

// Export singular version for consistency
export const useNotification = useNotifications;
