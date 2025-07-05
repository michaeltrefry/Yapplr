'use client';

import React, { createContext, useContext, useEffect, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { messageApi, notificationApi } from '@/lib/api';
import { useAuth } from './AuthContext';
import { firebaseMessagingService, FirebaseNotificationPayload } from '@/lib/firebaseMessaging';

interface NotificationContextType {
  unreadMessageCount: number;
  unreadNotificationCount: number;
  refreshUnreadCount: () => void;
  refreshNotificationCount: () => void;
  isFirebaseReady: boolean;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export function NotificationProvider({ children }: { children: React.ReactNode }) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const [isFirebaseReady, setIsFirebaseReady] = useState(false);

  const { data: unreadData, refetch } = useQuery({
    queryKey: ['unreadMessageCount'],
    queryFn: messageApi.getUnreadCount,
    enabled: !!user,
    refetchInterval: isFirebaseReady ? 60000 : 30000, // Slower polling when Firebase is ready, but keep it as backup
    refetchIntervalInBackground: false,
  });

  const { data: notificationData, refetch: refetchNotifications } = useQuery({
    queryKey: ['unreadNotificationCount'],
    queryFn: notificationApi.getUnreadCount,
    enabled: !!user,
    refetchInterval: isFirebaseReady ? 60000 : 30000, // Slower polling when Firebase is ready, but keep it as backup
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

  // Initialize Firebase messaging when user is authenticated
  useEffect(() => {
    if (user && !isFirebaseReady) {
      firebaseMessagingService.initialize().then((success) => {
        if (success) {
          setIsFirebaseReady(true);
          console.log('Firebase messaging initialized successfully');
        } else {
          console.warn('Firebase messaging initialization failed, falling back to polling');
        }
      });
    }
  }, [user, isFirebaseReady]);

  // Listen for Firebase messages and update counts
  useEffect(() => {
    if (!isFirebaseReady) return;

    const handleFirebaseMessage = (payload: FirebaseNotificationPayload) => {
      console.log('🔥🔥🔥 NOTIFICATION CONTEXT RECEIVED FIREBASE MESSAGE 🔥🔥🔥');
      console.log('🔥 Payload:', payload);
      console.log('🔥 Message type:', payload.data?.type);
      console.log('🔥 Timestamp:', new Date().toISOString());

      // Refresh counts based on notification type
      if (payload.data?.type === 'message') {
        console.log('🔥 Refreshing unread message count');
        refreshUnreadCount();
      } else if (payload.data?.type === 'mention' || payload.data?.type === 'reply' || payload.data?.type === 'comment' || payload.data?.type === 'follow' || payload.data?.type === 'like' || payload.data?.type === 'repost' || payload.data?.type === 'follow_request') {
        console.log('🔥 Refreshing notification count');
        refreshNotificationCount();
      }

      // Invalidate relevant queries to refresh data
      console.log('🔥 Invalidating queries');
      queryClient.invalidateQueries({ queryKey: ['conversations'] });
      queryClient.invalidateQueries({ queryKey: ['notifications'] });

      console.log('🔥 Firebase message handling complete');
    };

    firebaseMessagingService.addMessageListener(handleFirebaseMessage);

    return () => {
      firebaseMessagingService.removeMessageListener(handleFirebaseMessage);
    };
  }, [isFirebaseReady, queryClient, refreshUnreadCount, refreshNotificationCount]);

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

  return (
    <NotificationContext.Provider
      value={{
        unreadMessageCount,
        unreadNotificationCount,
        refreshUnreadCount,
        refreshNotificationCount,
        isFirebaseReady,
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
