import React, { useState, useCallback, useEffect } from 'react';
import NotificationBanner from './NotificationBanner';
import { SignalRNotificationPayload } from '../services/SignalRService';

interface NotificationBannerManagerProps {
  children: React.ReactNode;
}

export default function NotificationBannerManager({ children }: NotificationBannerManagerProps) {
  const [currentNotification, setCurrentNotification] = useState<SignalRNotificationPayload | null>(null);

  const showNotification = useCallback((notification: SignalRNotificationPayload) => {
    console.log('📱🔔 Banner manager showing notification:', notification);
    setCurrentNotification(notification);
  }, []);

  const handleNotificationPress = useCallback(() => {
    if (!currentNotification) return;

    // For now, just dismiss the notification
    // TODO: Add navigation logic here later
    console.log('📱🔔 Notification banner pressed:', currentNotification);
    setCurrentNotification(null);
  }, [currentNotification]);

  const handleNotificationDismiss = useCallback(() => {
    console.log('📱🔔 Notification banner dismissed');
    setCurrentNotification(null);
  }, []);

  // Expose the showNotification function globally
  useEffect(() => {
    console.log('📱🔔 Banner manager registering global function');
    (global as any).showNotificationBanner = showNotification;

    return () => {
      console.log('📱🔔 Banner manager unregistering global function');
      (global as any).showNotificationBanner = undefined;
    };
  }, [showNotification]);

  return (
    <>
      {children}
      <NotificationBanner
        notification={currentNotification}
        onPress={handleNotificationPress}
        onDismiss={handleNotificationDismiss}
        duration={5000} // Show for 5 seconds
      />
    </>
  );
}
