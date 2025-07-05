'use client';

import React, { useState, useEffect } from 'react';
import { useNotification } from '@/contexts/NotificationContext';

interface NotificationProviderSettingsProps {
  onClose?: () => void;
}

export function NotificationProviderSettings({ onClose }: NotificationProviderSettingsProps) {
  const { activeNotificationProvider, isFirebaseReady, isSignalRReady } = useNotification();
  
  // Get current configuration from environment variables
  const [firebaseEnabled, setFirebaseEnabled] = useState(
    process.env.NEXT_PUBLIC_ENABLE_FIREBASE === 'true'
  );
  const [signalREnabled, setSignalREnabled] = useState(
    process.env.NEXT_PUBLIC_ENABLE_SIGNALR === 'true'
  );

  const handleSaveSettings = () => {
    // Note: In a real application, you would need to restart the app or 
    // implement dynamic configuration changes for these settings to take effect
    alert(`Settings saved! 
    
Note: To apply these changes, you need to:
1. Update your .env.local file:
   NEXT_PUBLIC_ENABLE_FIREBASE=${firebaseEnabled}
   NEXT_PUBLIC_ENABLE_SIGNALR=${signalREnabled}
2. Restart the development server

Current runtime values cannot be changed without restart.`);
    
    if (onClose) {
      onClose();
    }
  };

  const getProviderStatus = (provider: 'firebase' | 'signalr') => {
    if (provider === 'firebase') {
      if (!firebaseEnabled) return { status: 'disabled', color: 'text-gray-500' };
      if (isFirebaseReady) return { status: 'ready', color: 'text-green-600' };
      return { status: 'failed', color: 'text-red-600' };
    } else {
      if (!signalREnabled) return { status: 'disabled', color: 'text-gray-500' };
      if (isSignalRReady) return { status: 'ready', color: 'text-green-600' };
      return { status: 'failed', color: 'text-red-600' };
    }
  };

  const firebaseStatus = getProviderStatus('firebase');
  const signalRStatus = getProviderStatus('signalr');

  return (
    <div className="bg-white rounded-lg shadow-lg p-6 max-w-md mx-auto">
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-xl font-semibold text-gray-900">Notification Settings</h2>
        {onClose && (
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600"
          >
            ✕
          </button>
        )}
      </div>

      <div className="space-y-6">
        {/* Current Active Provider */}
        <div className="bg-blue-50 p-4 rounded-lg">
          <h3 className="font-medium text-blue-900 mb-2">Active Provider</h3>
          <p className="text-blue-700 capitalize">{activeNotificationProvider}</p>
        </div>

        {/* Firebase Configuration */}
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <label className="flex items-center space-x-3">
              <input
                type="checkbox"
                checked={firebaseEnabled}
                onChange={(e) => setFirebaseEnabled(e.target.checked)}
                className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              <span className="font-medium text-gray-900">Firebase</span>
            </label>
            <span className={`text-sm font-medium ${firebaseStatus.color}`}>
              {firebaseStatus.status}
            </span>
          </div>
          <p className="text-sm text-gray-600 ml-6">
            Push notifications via Firebase Cloud Messaging
          </p>
        </div>

        {/* SignalR Configuration */}
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <label className="flex items-center space-x-3">
              <input
                type="checkbox"
                checked={signalREnabled}
                onChange={(e) => setSignalREnabled(e.target.checked)}
                className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              <span className="font-medium text-gray-900">SignalR</span>
            </label>
            <span className={`text-sm font-medium ${signalRStatus.color}`}>
              {signalRStatus.status}
            </span>
          </div>
          <p className="text-sm text-gray-600 ml-6">
            Real-time notifications via WebSocket connection
          </p>
        </div>

        {/* Warning if both disabled */}
        {!firebaseEnabled && !signalREnabled && (
          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-yellow-400" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                </svg>
              </div>
              <div className="ml-3">
                <h3 className="text-sm font-medium text-yellow-800">
                  Warning
                </h3>
                <div className="mt-2 text-sm text-yellow-700">
                  <p>
                    Both notification providers are disabled. Only polling will be used for notifications.
                  </p>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Save Button */}
        <div className="flex justify-end space-x-3">
          {onClose && (
            <button
              onClick={onClose}
              className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 border border-gray-300 rounded-md hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
            >
              Cancel
            </button>
          )}
          <button
            onClick={handleSaveSettings}
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 border border-transparent rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
          >
            Save Settings
          </button>
        </div>
      </div>
    </div>
  );
}
