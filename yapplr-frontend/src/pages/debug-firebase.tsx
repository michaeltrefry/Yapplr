'use client';

import { useState, useEffect, useCallback } from 'react';
import { firebaseMessagingService } from '@/lib/firebaseMessaging';
import { requestNotificationPermission, getNotificationStatus } from '@/lib/firebase';

export default function DebugFirebase() {
  const [logs, setLogs] = useState<string[]>([]);
  const [status, setStatus] = useState<any>({});
  const [isInitialized, setIsInitialized] = useState(false);

  const addLog = (message: string) => {
    const timestamp = new Date().toLocaleTimeString();
    setLogs(prev => [...prev, `[${timestamp}] ${message}`]);
    console.log(message);
  };

  useEffect(() => {
    // Initialize Firebase only once
    initializeFirebase();
  }, []);

  const initializeFirebase = useCallback(async () => {
    addLog('Starting Firebase initialization...');
    try {
      const result = await firebaseMessagingService.initialize();
      setIsInitialized(result);
      addLog(`Firebase initialization result: ${result}`);

      // Update status
      const notificationStatus = getNotificationStatus();
      setStatus({
        ...notificationStatus,
        firebaseInitialized: result,
        fcmToken: firebaseMessagingService.getToken()
      });
    } catch (error) {
      addLog(`Firebase initialization error: ${error}`);
    }
  }, []);

  const requestPermission = async () => {
    addLog('Manually requesting notification permission...');
    try {
      // Just get the token without sending to server (to avoid 401 error)
      const token = await requestNotificationPermission();
      if (token) {
        addLog(`Permission granted! FCM Token obtained: ${token.substring(0, 50)}...`);
        addLog(`Full token length: ${token.length}`);

        // Try to send to server, but don't fail if not authenticated
        try {
          const success = await firebaseMessagingService.requestPermission();
          addLog(`Server update result: ${success}`);
        } catch (serverError) {
          addLog(`Server update failed (probably not logged in): ${serverError}`);
          addLog('Token was still generated successfully for testing!');
        }
      } else {
        addLog('Permission denied or token generation failed');
      }

      // Update status
      const notificationStatus = getNotificationStatus();
      setStatus({
        ...notificationStatus,
        firebaseInitialized: isInitialized,
        fcmToken: token
      });
    } catch (error) {
      addLog(`Permission request error: ${error}`);
    }
  };

  const testToken = async () => {
    addLog('Testing FCM token generation...');
    try {
      const token = await requestNotificationPermission();
      addLog(`Token result: ${token ? 'Success' : 'Failed'}`);
      if (token) {
        addLog(`Token: ${token.substring(0, 50)}...`);
      }
    } catch (error) {
      addLog(`Token test error: ${error}`);
    }
  };

  const clearLogs = () => {
    setLogs([]);
  };

  return (
    <div className="container mx-auto p-4">
      <h1 className="text-2xl font-bold mb-4">Firebase Debug Page</h1>
      
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
        <div className="bg-gray-100 p-4 rounded">
          <h2 className="text-lg font-semibold mb-2">Status</h2>
          <pre className="text-sm">{JSON.stringify(status, null, 2)}</pre>
        </div>
        
        <div className="bg-gray-100 p-4 rounded">
          <h2 className="text-lg font-semibold mb-2">Environment Variables</h2>
          <div className="text-sm">
            <div>API_KEY: {process.env.NEXT_PUBLIC_FIREBASE_API_KEY ? 'Set' : 'Missing'}</div>
            <div>AUTH_DOMAIN: {process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN ? 'Set' : 'Missing'}</div>
            <div>DATABASE_URL: {process.env.NEXT_PUBLIC_FIREBASE_DATABASE_URL ? 'Set' : 'Missing'}</div>
            <div>PROJECT_ID: {process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID ? 'Set' : 'Missing'}</div>
            <div>STORAGE_BUCKET: {process.env.NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET ? 'Set' : 'Missing'}</div>
            <div>MESSAGING_SENDER_ID: {process.env.NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID ? 'Set' : 'Missing'}</div>
            <div>APP_ID: {process.env.NEXT_PUBLIC_FIREBASE_APP_ID ? 'Set' : 'Missing'}</div>
            <div>VAPID_KEY: {process.env.NEXT_PUBLIC_FIREBASE_VAPID_KEY ? 'Set' : 'Missing'}</div>
          </div>
        </div>
      </div>

      <div className="mb-4">
        <button 
          onClick={requestPermission}
          className="bg-blue-500 text-white px-4 py-2 rounded mr-2"
        >
          Request Permission
        </button>
        <button 
          onClick={testToken}
          className="bg-green-500 text-white px-4 py-2 rounded mr-2"
        >
          Test Token
        </button>
        <button 
          onClick={initializeFirebase}
          className="bg-purple-500 text-white px-4 py-2 rounded mr-2"
        >
          Re-initialize Firebase
        </button>
        <button 
          onClick={clearLogs}
          className="bg-red-500 text-white px-4 py-2 rounded"
        >
          Clear Logs
        </button>
      </div>

      <div className="bg-black text-green-400 p-4 rounded h-96 overflow-y-auto">
        <h2 className="text-lg font-semibold mb-2">Logs</h2>
        {logs.map((log, index) => (
          <div key={index} className="text-sm font-mono">{log}</div>
        ))}
      </div>
    </div>
  );
}
