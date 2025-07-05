'use client';

import React from 'react';
import { useNotifications } from '@/contexts/NotificationContext';
import { Wifi, WifiOff, Smartphone, Bell, Clock } from 'lucide-react';

interface NotificationStatusProps {
  showDetails?: boolean;
  className?: string;
}

export function NotificationStatus({ showDetails = false, className = '' }: NotificationStatusProps) {
  const { activeNotificationProvider, isFirebaseReady, isSignalRReady } = useNotifications();

  const getStatusInfo = () => {
    switch (activeNotificationProvider) {
      case 'firebase':
        return {
          icon: <Smartphone className="w-4 h-4 text-green-500" />,
          label: 'Firebase',
          description: 'Push notifications active',
          color: 'text-green-600',
          bgColor: 'bg-green-50',
          borderColor: 'border-green-200'
        };
      case 'signalr':
        return {
          icon: <Wifi className="w-4 h-4 text-blue-500" />,
          label: 'SignalR',
          description: 'Real-time notifications active',
          color: 'text-blue-600',
          bgColor: 'bg-blue-50',
          borderColor: 'border-blue-200'
        };
      case 'polling':
        return {
          icon: <Clock className="w-4 h-4 text-yellow-500" />,
          label: 'Polling',
          description: 'Checking for updates every 30s',
          color: 'text-yellow-600',
          bgColor: 'bg-yellow-50',
          borderColor: 'border-yellow-200'
        };
      default:
        return {
          icon: <WifiOff className="w-4 h-4 text-gray-500" />,
          label: 'Offline',
          description: 'No notifications available',
          color: 'text-gray-600',
          bgColor: 'bg-gray-50',
          borderColor: 'border-gray-200'
        };
    }
  };

  const statusInfo = getStatusInfo();

  if (!showDetails) {
    return (
      <div className={`flex items-center gap-1 ${className}`} title={statusInfo.description}>
        {statusInfo.icon}
        <span className={`text-xs ${statusInfo.color}`}>{statusInfo.label}</span>
      </div>
    );
  }

  return (
    <div className={`rounded-lg border p-3 ${statusInfo.bgColor} ${statusInfo.borderColor} ${className}`}>
      <div className="flex items-center gap-2 mb-2">
        {statusInfo.icon}
        <span className={`font-medium ${statusInfo.color}`}>
          Notifications: {statusInfo.label}
        </span>
      </div>
      <p className={`text-sm ${statusInfo.color} opacity-80`}>
        {statusInfo.description}
      </p>
      
      {showDetails && (
        <div className="mt-3 space-y-1">
          <div className="flex items-center gap-2 text-xs">
            <div className={`w-2 h-2 rounded-full ${isFirebaseReady ? 'bg-green-500' : 'bg-gray-300'}`} />
            <span className={isFirebaseReady ? 'text-green-600' : 'text-gray-500'}>
              Firebase {isFirebaseReady ? 'Connected' : 'Disconnected'}
            </span>
          </div>
          <div className="flex items-center gap-2 text-xs">
            <div className={`w-2 h-2 rounded-full ${isSignalRReady ? 'bg-blue-500' : 'bg-gray-300'}`} />
            <span className={isSignalRReady ? 'text-blue-600' : 'text-gray-500'}>
              SignalR {isSignalRReady ? 'Connected' : 'Disconnected'}
            </span>
          </div>
        </div>
      )}
    </div>
  );
}

export function NotificationStatusBadge() {
  const { activeNotificationProvider } = useNotifications();
  
  if (activeNotificationProvider === 'none') {
    return null;
  }

  const getStatusColor = () => {
    switch (activeNotificationProvider) {
      case 'firebase':
        return 'bg-green-500';
      case 'signalr':
        return 'bg-blue-500';
      case 'polling':
        return 'bg-yellow-500';
      default:
        return 'bg-gray-500';
    }
  };

  return (
    <div className="fixed bottom-4 right-4 z-50">
      <div className={`w-3 h-3 rounded-full ${getStatusColor()} animate-pulse`} 
           title={`Notifications via ${activeNotificationProvider}`} />
    </div>
  );
}
