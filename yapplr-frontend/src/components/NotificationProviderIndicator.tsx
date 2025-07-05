'use client';

import React from 'react';
import { useNotification } from '@/contexts/NotificationContext';

interface NotificationProviderIndicatorProps {
  showDetails?: boolean;
  className?: string;
}

export function NotificationProviderIndicator({ 
  showDetails = false, 
  className = '' 
}: NotificationProviderIndicatorProps) {
  const { activeNotificationProvider, isFirebaseReady, isSignalRReady } = useNotification();

  const getProviderIcon = () => {
    switch (activeNotificationProvider) {
      case 'firebase':
        return '🔥';
      case 'signalr':
        return '📡';
      case 'polling':
        return '🔄';
      default:
        return '❌';
    }
  };

  const getProviderColor = () => {
    switch (activeNotificationProvider) {
      case 'firebase':
      case 'signalr':
        return 'text-green-600';
      case 'polling':
        return 'text-yellow-600';
      default:
        return 'text-red-600';
    }
  };

  const getProviderName = () => {
    switch (activeNotificationProvider) {
      case 'firebase':
        return 'Firebase';
      case 'signalr':
        return 'SignalR';
      case 'polling':
        return 'Polling';
      default:
        return 'None';
    }
  };

  if (!showDetails) {
    return (
      <div className={`inline-flex items-center space-x-1 ${className}`}>
        <span className="text-sm">{getProviderIcon()}</span>
        <span className={`text-xs font-medium ${getProviderColor()}`}>
          {getProviderName()}
        </span>
      </div>
    );
  }

  return (
    <div className={`bg-white rounded-lg border border-gray-200 p-3 ${className}`}>
      <div className="flex items-center justify-between mb-2">
        <h3 className="text-sm font-medium text-gray-900">Notification Status</h3>
        <span className="text-lg">{getProviderIcon()}</span>
      </div>
      
      <div className="space-y-2">
        <div className="flex justify-between items-center">
          <span className="text-sm text-gray-600">Active Provider:</span>
          <span className={`text-sm font-medium ${getProviderColor()}`}>
            {getProviderName()}
          </span>
        </div>
        
        <div className="grid grid-cols-2 gap-2 text-xs">
          <div className="flex justify-between">
            <span className="text-gray-500">Firebase:</span>
            <span className={isFirebaseReady ? 'text-green-600' : 'text-gray-400'}>
              {isFirebaseReady ? '✓' : '✗'}
            </span>
          </div>
          <div className="flex justify-between">
            <span className="text-gray-500">SignalR:</span>
            <span className={isSignalRReady ? 'text-green-600' : 'text-gray-400'}>
              {isSignalRReady ? '✓' : '✗'}
            </span>
          </div>
        </div>
        
        {activeNotificationProvider === 'polling' && (
          <div className="text-xs text-yellow-600 bg-yellow-50 rounded p-2 mt-2">
            Real-time notifications unavailable
          </div>
        )}
        
        {activeNotificationProvider === 'none' && (
          <div className="text-xs text-red-600 bg-red-50 rounded p-2 mt-2">
            No notification providers available
          </div>
        )}
      </div>
    </div>
  );
}
