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

// Initialize Firebase
const app = getApps().length === 0 ? initializeApp(firebaseConfig) : getApps()[0];

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

// Request notification permission and get FCM token
export const requestNotificationPermission = async (): Promise<string | null> => {
  try {
    const messagingInstance = await getMessagingInstance();
    if (!messagingInstance) {
      console.warn('Firebase messaging not supported');
      return null;
    }

    const permission = await Notification.requestPermission();
    if (permission === 'granted') {
      const token = await getToken(messagingInstance, {
        vapidKey: VAPID_KEY,
      });
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

// Listen for foreground messages
export const onMessageListener = () =>
  new Promise(async (resolve) => {
    try {
      const messagingInstance = await getMessagingInstance();
      if (!messagingInstance) {
        return;
      }

      onMessage(messagingInstance, (payload) => {
        resolve(payload);
      });
    } catch (error) {
      console.error('Error setting up message listener:', error);
    }
  });
