'use client';

import { useState } from 'react';
import { Bell, BellOff } from 'lucide-react';
import { firebaseMessagingService } from '@/lib/firebaseMessaging';
import { getNotificationStatus } from '@/lib/firebase';

export default function NotificationPermissionButton() {
  const [isRequesting, setIsRequesting] = useState(false);
  const notificationStatus = getNotificationStatus();

  const handleEnableNotifications = async () => {
    setIsRequesting(true);
    try {
      const success = await firebaseMessagingService.requestPermission();
      if (success) {
        // Refresh the page to update the notification status
        window.location.reload();
      } else {
        alert('Failed to enable notifications. Please check your browser settings.');
      }
    } catch (error) {
      console.error('Error enabling notifications:', error);
      alert('Failed to enable notifications. Please try again.');
    } finally {
      setIsRequesting(false);
    }
  };

  // Don't show button if notifications aren't supported
  if (!notificationStatus.supported) {
    return null;
  }

  // Don't show button if permission is already granted
  if (notificationStatus.permission === 'granted') {
    return (
      <div className="flex items-center gap-2 text-green-600 text-sm">
        <Bell className="w-4 h-4" />
        <span>Notifications enabled</span>
      </div>
    );
  }

  // Don't show button if permission is permanently denied
  if (notificationStatus.permission === 'denied') {
    return (
      <div className="flex items-center gap-2 text-gray-500 text-sm">
        <BellOff className="w-4 h-4" />
        <span>Notifications blocked. Enable in browser settings.</span>
      </div>
    );
  }

  // Show enable button if permission can be requested
  return (
    <button
      onClick={handleEnableNotifications}
      disabled={isRequesting}
      className="flex items-center gap-2 px-3 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed text-sm"
    >
      <Bell className="w-4 h-4" />
      {isRequesting ? 'Enabling...' : 'Enable Notifications'}
    </button>
  );
}
