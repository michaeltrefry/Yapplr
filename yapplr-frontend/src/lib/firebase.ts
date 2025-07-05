import { initializeApp, getApps } from 'firebase/app';
import { getMessaging, getToken, onMessage, isSupported } from 'firebase/messaging';

const firebaseConfig = {
  apiKey: process.env.NEXT_PUBLIC_FIREBASE_API_KEY,
  authDomain: process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN,
  databaseURL: process.env.NEXT_PUBLIC_FIREBASE_DATABASE_URL,
  projectId: process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID,
  storageBucket: process.env.NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET,
  messagingSenderId: process.env.NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID,
  appId: process.env.NEXT_PUBLIC_FIREBASE_APP_ID,
  measurementId: process.env.NEXT_PUBLIC_FIREBASE_MEASUREMENT_ID,
};

// Debug Firebase configuration
console.log('Firebase config loaded:', {
  apiKey: firebaseConfig.apiKey ? 'Set' : 'Missing',
  authDomain: firebaseConfig.authDomain ? 'Set' : 'Missing',
  databaseURL: firebaseConfig.databaseURL ? 'Set' : 'Missing',
  projectId: firebaseConfig.projectId ? 'Set' : 'Missing',
  storageBucket: firebaseConfig.storageBucket ? 'Set' : 'Missing',
  messagingSenderId: firebaseConfig.messagingSenderId ? 'Set' : 'Missing',
  appId: firebaseConfig.appId ? 'Set' : 'Missing',
  measurementId: firebaseConfig.measurementId ? 'Set' : 'Missing',
});

// Initialize Firebase
const app = getApps().length === 0 ? initializeApp(firebaseConfig) : getApps()[0];
console.log('Firebase app initialized:', app.name);

// Initialize Firebase Cloud Messaging and get a reference to the service
let messaging: any = null;
let messagingPromise: Promise<any> | null = null;

if (typeof window !== 'undefined') {
  // Only initialize messaging on client side
  messagingPromise = isSupported().then((supported) => {
    if (supported) {
      messaging = getMessaging(app);
      return messaging;
    }
    return null;
  });
}

export { app, messaging };

// VAPID key for web push notifications
// This should be generated in Firebase Console -> Project Settings -> Cloud Messaging -> Web configuration
export const VAPID_KEY = process.env.NEXT_PUBLIC_FIREBASE_VAPID_KEY || '';

// Get messaging instance (wait for initialization if needed)
const getMessagingInstance = async () => {
  if (messagingPromise) {
    await messagingPromise;
  }
  return messaging;
};

// Request notification permission and get FCM token (must be called from user gesture)
export const requestNotificationPermission = async (): Promise<string | null> => {
  try {
    console.log('Requesting notification permission...');
    console.log('VAPID key available:', VAPID_KEY ? 'Yes' : 'No');

    const messagingInstance = await getMessagingInstance();
    if (!messagingInstance) {
      console.warn('Firebase messaging not supported');
      return null;
    }
    console.log('Firebase messaging instance obtained');

    // Check current permission status first
    console.log('Current permission status:', Notification.permission);
    if (Notification.permission === 'granted') {
      // Permission already granted, just get token
      console.log('Permission already granted, getting token...');
      const token = await getToken(messagingInstance, {
        vapidKey: VAPID_KEY,
      });
      console.log('Token obtained:', token ? 'Success' : 'Failed');
      return token;
    } else if (Notification.permission === 'denied') {
      console.warn('Notification permission previously denied');
      return null;
    }

    // Request permission (must be from user gesture)
    console.log('Requesting permission from user...');
    const permission = await Notification.requestPermission();
    console.log('Permission result:', permission);
    if (permission === 'granted') {
      console.log('Permission granted, getting token...');
      const token = await getToken(messagingInstance, {
        vapidKey: VAPID_KEY,
      });
      console.log('Token obtained:', token ? 'Success' : 'Failed');
      return token;
    } else {
      console.warn('Notification permission denied');
      return null;
    }
  } catch (error) {
    console.error('Error getting notification permission:', error);
    return null;
  }
};

// Check if notifications are supported and permission status
export const getNotificationStatus = () => {
  return {
    supported: 'Notification' in window,
    permission: Notification.permission,
    canRequest: Notification.permission === 'default'
  };
};

// Listen for foreground messages - callback-based for multiple messages
export const onMessageListener = (callback: (payload: any) => void) => {
  const setupListener = async () => {
    try {
      const messagingInstance = await getMessagingInstance();
      if (!messagingInstance) {
        console.warn('Firebase messaging instance not available');
        return;
      }

      console.log('🔥 Setting up persistent Firebase message listener...');
      onMessage(messagingInstance, (payload) => {
        console.log('🔥 Firebase onMessage triggered:', payload);
        callback(payload);
      });
      console.log('🔥 Persistent Firebase message listener setup complete');
    } catch (error) {
      console.error('Error setting up message listener:', error);
    }
  };

  setupListener();
};
