'use client';

import React, { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { useNotification } from '@/contexts/NotificationContext';
import { NotificationProviderSettings } from '@/components/NotificationProviderSettings';
import { NotificationProviderIndicator } from '@/components/NotificationProviderIndicator';
import { notificationApi } from '@/lib/api';

export default function NotificationTestPage() {
  const { user } = useAuth();
  const { activeNotificationProvider, isFirebaseReady, isSignalRReady } = useNotification();
  const [showSettings, setShowSettings] = useState(false);
  const [testResult, setTestResult] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [backendConfig, setBackendConfig] = useState<any>(null);
  const [providerStatus, setProviderStatus] = useState<any>(null);

  // Load backend configuration
  useEffect(() => {
    const loadBackendConfig = async () => {
      try {
        const response = await fetch('/api/notification-config', {
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('token')}`
          }
        });
        if (response.ok) {
          const config = await response.json();
          setBackendConfig(config);
        }
      } catch (error) {
        console.error('Failed to load backend config:', error);
      }
    };

    const loadProviderStatus = async () => {
      try {
        const response = await fetch('/api/notification-config/status', {
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('token')}`
          }
        });
        if (response.ok) {
          const status = await response.json();
          setProviderStatus(status);
        }
      } catch (error) {
        console.error('Failed to load provider status:', error);
      }
    };

    if (user) {
      loadBackendConfig();
      loadProviderStatus();
    }
  }, [user]);

  const handleTestNotification = async () => {
    if (!user) return;

    setIsLoading(true);
    setTestResult(null);

    try {
      const response = await notificationApi.sendTestNotification();
      if (response.success) {
        setTestResult('✅ Test notification sent successfully!');
      } else {
        setTestResult('❌ Failed to send test notification');
      }
    } catch (error) {
      setTestResult('❌ Error sending test notification: ' + (error as Error).message);
    } finally {
      setIsLoading(false);
    }
  };

  if (!user) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-gray-900 mb-4">Notification Test</h1>
          <p className="text-gray-600">Please log in to test notifications.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="bg-white shadow rounded-lg">
          <div className="px-6 py-4 border-b border-gray-200">
            <div className="flex justify-between items-start">
              <div>
                <h1 className="text-2xl font-bold text-gray-900">Notification Provider Test</h1>
                <p className="mt-1 text-sm text-gray-600">
                  Test and configure notification providers (Firebase, SignalR)
                </p>
              </div>
              <NotificationProviderIndicator showDetails={false} />
            </div>
          </div>

          <div className="p-6 space-y-8">
            {/* Frontend Configuration */}
            <div className="bg-blue-50 rounded-lg p-6">
              <h2 className="text-lg font-semibold text-blue-900 mb-4">Frontend Configuration</h2>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <h3 className="font-medium text-blue-800">Platform Detection</h3>
                  <div className="text-sm space-y-1">
                    <div>Platform: <span className="font-mono">{typeof window !== 'undefined' && (/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent) || window.innerWidth <= 768) ? 'Mobile' : 'Desktop/Web'}</span></div>
                    <div>User Agent: <span className="font-mono text-xs">{typeof window !== 'undefined' ? navigator.userAgent.substring(0, 50) + '...' : 'N/A'}</span></div>
                  </div>
                </div>
                <div className="space-y-2">
                  <h3 className="font-medium text-blue-800">Environment Variables</h3>
                  <div className="text-sm space-y-1">
                    <div>Firebase (Mobile): <span className="font-mono">{process.env.NEXT_PUBLIC_ENABLE_FIREBASE}</span></div>
                    <div>Firebase (Web): <span className="font-mono">{process.env.NEXT_PUBLIC_ENABLE_FIREBASE_WEB}</span></div>
                    <div>SignalR (Web): <span className="font-mono">{process.env.NEXT_PUBLIC_ENABLE_SIGNALR}</span></div>
                    <div>SignalR (Mobile): <span className="font-mono">{process.env.NEXT_PUBLIC_ENABLE_SIGNALR_MOBILE}</span></div>
                  </div>
                </div>
                <div className="space-y-2">
                  <h3 className="font-medium text-blue-800">Runtime Status</h3>
                  <div className="text-sm space-y-1">
                    <div>Active Provider: <span className="font-mono capitalize">{activeNotificationProvider}</span></div>
                    <div>Firebase Ready: <span className={`font-mono ${isFirebaseReady ? 'text-green-600' : 'text-red-600'}`}>{isFirebaseReady ? 'Yes' : 'No'}</span></div>
                    <div>SignalR Ready: <span className={`font-mono ${isSignalRReady ? 'text-green-600' : 'text-red-600'}`}>{isSignalRReady ? 'Yes' : 'No'}</span></div>
                  </div>
                </div>
              </div>
            </div>

            {/* Backend Configuration */}
            {backendConfig && (
              <div className="bg-green-50 rounded-lg p-6">
                <h2 className="text-lg font-semibold text-green-900 mb-4">Backend Configuration</h2>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <h3 className="font-medium text-green-800">Firebase</h3>
                    <div className="text-sm space-y-1">
                      <div>Enabled: <span className={`font-mono ${backendConfig.Firebase.Enabled ? 'text-green-600' : 'text-red-600'}`}>{backendConfig.Firebase.Enabled ? 'Yes' : 'No'}</span></div>
                      <div>Project ID: <span className="font-mono">{backendConfig.Firebase.ProjectId}</span></div>
                    </div>
                  </div>
                  <div className="space-y-2">
                    <h3 className="font-medium text-green-800">SignalR</h3>
                    <div className="text-sm space-y-1">
                      <div>Enabled: <span className={`font-mono ${backendConfig.SignalR.Enabled ? 'text-green-600' : 'text-red-600'}`}>{backendConfig.SignalR.Enabled ? 'Yes' : 'No'}</span></div>
                      <div>Max Connections: <span className="font-mono">{backendConfig.SignalR.MaxConnectionsPerUser}</span></div>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {/* Provider Status */}
            {providerStatus && (
              <div className="bg-yellow-50 rounded-lg p-6">
                <h2 className="text-lg font-semibold text-yellow-900 mb-4">Provider Status</h2>
                <div className="space-y-2">
                  <div className="text-sm">
                    Available Providers: <span className="font-mono">{providerStatus.AvailableProviders} / {providerStatus.TotalProviders}</span>
                  </div>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    {providerStatus.Providers.map((provider: any, index: number) => (
                      <div key={index} className="flex justify-between items-center">
                        <span className="font-medium">{provider.Name}</span>
                        <span className={`text-sm font-mono ${provider.IsAvailable ? 'text-green-600' : 'text-red-600'}`}>
                          {provider.IsAvailable ? 'Available' : 'Unavailable'}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            )}

            {/* Test Controls */}
            <div className="bg-gray-50 rounded-lg p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">Test Controls</h2>
              <div className="space-y-4">
                <div className="flex space-x-4">
                  <button
                    onClick={handleTestNotification}
                    disabled={isLoading}
                    className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {isLoading ? 'Sending...' : 'Send Test Notification'}
                  </button>
                  <button
                    onClick={() => setShowSettings(true)}
                    className="px-4 py-2 bg-gray-600 text-white rounded-md hover:bg-gray-700"
                  >
                    Configure Providers
                  </button>
                </div>
                
                {testResult && (
                  <div className="p-4 bg-white border rounded-md">
                    <p className="text-sm">{testResult}</p>
                  </div>
                )}
              </div>
            </div>

            {/* Configuration Instructions */}
            <div className="bg-purple-50 rounded-lg p-6">
              <h2 className="text-lg font-semibold text-purple-900 mb-4">Configuration Instructions</h2>
              <div className="text-sm text-purple-800 space-y-2">
                <p><strong>To test Firebase only:</strong></p>
                <ul className="list-disc list-inside ml-4 space-y-1">
                  <li>Set <code>NEXT_PUBLIC_ENABLE_FIREBASE=true</code></li>
                  <li>Set <code>NEXT_PUBLIC_ENABLE_SIGNALR=false</code></li>
                  <li>Set backend <code>NotificationProviders:Firebase:Enabled=true</code></li>
                  <li>Set backend <code>NotificationProviders:SignalR:Enabled=false</code></li>
                </ul>
                
                <p className="pt-2"><strong>To test SignalR only:</strong></p>
                <ul className="list-disc list-inside ml-4 space-y-1">
                  <li>Set <code>NEXT_PUBLIC_ENABLE_FIREBASE=false</code></li>
                  <li>Set <code>NEXT_PUBLIC_ENABLE_SIGNALR=true</code></li>
                  <li>Set backend <code>NotificationProviders:Firebase:Enabled=false</code></li>
                  <li>Set backend <code>NotificationProviders:SignalR:Enabled=true</code></li>
                </ul>
                
                <p className="pt-2"><strong>To test both (fallback behavior):</strong></p>
                <ul className="list-disc list-inside ml-4 space-y-1">
                  <li>Set both frontend and backend providers to <code>true</code></li>
                  <li>Firebase will be tried first, SignalR as fallback</li>
                </ul>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Settings Modal */}
      {showSettings && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <NotificationProviderSettings onClose={() => setShowSettings(false)} />
        </div>
      )}
    </div>
  );
}
