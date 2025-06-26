'use client';

import React, { createContext, useContext, useEffect, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { messageApi } from '@/lib/api';
import { useAuth } from './AuthContext';

interface NotificationContextType {
  unreadMessageCount: number;
  refreshUnreadCount: () => void;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export function NotificationProvider({ children }: { children: React.ReactNode }) {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const { data: unreadData, refetch } = useQuery({
    queryKey: ['unreadMessageCount'],
    queryFn: messageApi.getUnreadCount,
    enabled: !!user,
    refetchInterval: 30000, // Refresh every 30 seconds
    refetchIntervalInBackground: false,
  });

  const unreadMessageCount = unreadData?.unreadCount || 0;

  const refreshUnreadCount = () => {
    refetch();
  };

  // Listen for changes in conversations to refresh unread count
  useEffect(() => {
    const handleConversationsChange = () => {
      refreshUnreadCount();
    };

    // Listen for query invalidations that might affect unread count
    const unsubscribe = queryClient.getQueryCache().subscribe((event) => {
      if (event.type === 'updated' && event.query.queryKey[0] === 'conversations') {
        refreshUnreadCount();
      }
    });

    return unsubscribe;
  }, [queryClient]);

  return (
    <NotificationContext.Provider
      value={{
        unreadMessageCount,
        refreshUnreadCount,
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
