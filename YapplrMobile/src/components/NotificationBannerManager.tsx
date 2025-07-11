import React, { useState, useCallback, useEffect } from 'react';
import NotificationBanner from './NotificationBanner';
import { SignalRNotificationPayload } from '../services/SignalRService';
import NotificationNavigationService from '../services/NotificationNavigationService';
import NotificationFeedbackService from '../services/NotificationFeedbackService';
import { useAuth } from '../contexts/AuthContext';

interface NotificationBannerManagerProps {
  children: React.ReactNode;
}

export default function NotificationBannerManager({ children }: NotificationBannerManagerProps) {
  const { api } = useAuth();
  const [currentNotification, setCurrentNotification] = useState<SignalRNotificationPayload | null>(null);
  const [notificationQueue, setNotificationQueue] = useState<SignalRNotificationPayload[]>([]);
  const [isProcessingQueue, setIsProcessingQueue] = useState(false);

  // Set the API client in the navigation service
  useEffect(() => {
    if (api) {
      NotificationNavigationService.setApiClient(api);
      console.log('📱🔔 API client set in navigation service');
    }
  }, [api]);

  // Process the next notification in the queue
  const processNextNotification = useCallback(() => {
    setNotificationQueue(queue => {
      if (queue.length === 0) {
        setIsProcessingQueue(false);
        return queue;
      }

      const [nextNotification, ...remainingQueue] = queue;
      console.log('📱🔔 Processing next notification from queue:', nextNotification);
      setCurrentNotification(nextNotification);

      return remainingQueue;
    });
  }, []);

  const showNotification = useCallback(async (notification: SignalRNotificationPayload) => {
    console.log('📱🔔 Banner manager received notification:', notification);

    // Provide feedback for the notification
    try {
      await NotificationFeedbackService.provideFeedback(notification);
    } catch (error) {
      console.warn('📱🔔 Error providing notification feedback:', error);
    }

    if (currentNotification) {
      // If there's already a notification showing, add to queue
      console.log('📱🔔 Adding notification to queue (current notification showing)');
      setNotificationQueue(queue => [...queue, notification]);
      setIsProcessingQueue(true);
    } else {
      // Show immediately if no notification is currently displayed
      console.log('📱🔔 Showing notification immediately');
      setCurrentNotification(notification);
    }
  }, [currentNotification]);

  const handleNotificationPress = useCallback(() => {
    if (!currentNotification) return;

    console.log('📱🔔 Notification banner pressed:', currentNotification);

    // Store the notification for navigation
    const notificationToNavigate = currentNotification;

    // Dismiss the notification first to avoid state conflicts
    setCurrentNotification(null);

    // Defer navigation and queue processing to avoid useInsertionEffect conflicts
    setTimeout(() => {
      try {
        // Try to navigate based on notification data
        const navigated = NotificationNavigationService.navigateFromNotification(notificationToNavigate);

        if (navigated) {
          console.log('📱🔔 Successfully navigated from notification');
        } else {
          console.log('📱🔔 No navigation performed for notification');
        }
      } catch (error) {
        console.error('📱🔔 Error during navigation:', error);
      }

      // Process next notification in queue
      processNextNotification();
    }, 100); // Small delay to allow state to settle
  }, [currentNotification, processNextNotification]);

  const handleNotificationDismiss = useCallback(() => {
    console.log('📱🔔 Notification banner dismissed');
    setCurrentNotification(null);

    // Process next notification in queue after a short delay
    setTimeout(() => {
      processNextNotification();
    }, 300); // Small delay to allow for smooth transitions
  }, [processNextNotification]);

  const handleMarkAsRead = useCallback(async () => {
    if (!currentNotification) return;

    console.log('📱🔔 Mark as read action triggered for notification:', currentNotification);

    // Provide haptic feedback for the action
    try {
      await NotificationFeedbackService.provideSwipeFeedback('markAsRead');
    } catch (error) {
      console.warn('📱🔔 Error providing mark as read feedback:', error);
    }

    // TODO: Implement mark as read API call
    // For now, just dismiss the notification
  }, [currentNotification]);

  const handleReply = useCallback(async () => {
    if (!currentNotification) return;

    console.log('📱🔔 Reply action triggered for notification:', currentNotification);

    // Provide haptic feedback for the action
    try {
      await NotificationFeedbackService.provideSwipeFeedback('reply');
    } catch (error) {
      console.warn('📱🔔 Error providing reply feedback:', error);
    }

    // TODO: Implement reply functionality (navigate to conversation with reply intent)
    // For now, just navigate normally
    NotificationNavigationService.navigateFromNotification(currentNotification);
  }, [currentNotification]);

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
        onMarkAsRead={handleMarkAsRead}
        onReply={handleReply}
        duration={5000} // Show for 5 seconds
      />
    </>
  );
}
