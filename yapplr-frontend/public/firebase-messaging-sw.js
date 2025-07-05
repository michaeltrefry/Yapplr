// Firebase Service Worker Template
// This file serves as a template for the service worker.
// The actual service worker is served from /api/firebase-messaging-sw
// with environment variables injected for security.

console.log('🔥 SERVICE WORKER: Starting Firebase service worker initialization...');

// Handle skip waiting message
self.addEventListener('message', (event) => {
  console.log('🔥 SERVICE WORKER: Received message:', event.data);
  if (event.data && event.data.type === 'SKIP_WAITING') {
    console.log('🔥 SERVICE WORKER: Skipping waiting...');
    self.skipWaiting();
  }
});

// Take control of all clients immediately
self.addEventListener('activate', (event) => {
  console.log('🔥 SERVICE WORKER: Activated');
  event.waitUntil(self.clients.claim());
});

// Declare messaging variable in global scope
let messaging;

try {
  // Import Firebase scripts for service worker (using more recent version)
  console.log('🔥 SERVICE WORKER: Importing Firebase scripts...');
  console.log('🔥 SERVICE WORKER: About to import firebase-app-compat.js');
  importScripts('https://www.gstatic.com/firebasejs/10.7.1/firebase-app-compat.js');
  console.log('🔥 SERVICE WORKER: firebase-app-compat.js imported successfully');
  console.log('🔥 SERVICE WORKER: About to import firebase-messaging-compat.js');
  importScripts('https://www.gstatic.com/firebasejs/10.7.1/firebase-messaging-compat.js');
  console.log('🔥 SERVICE WORKER: firebase-messaging-compat.js imported successfully');
  console.log('🔥 SERVICE WORKER: All Firebase scripts imported successfully');

  // Firebase configuration will be injected by the API route
  // This file should not be used directly - use /api/firebase-messaging-sw instead
  const firebaseConfig = {
    apiKey: "PLACEHOLDER_API_KEY",
    authDomain: "PLACEHOLDER_AUTH_DOMAIN",
    databaseURL: "PLACEHOLDER_DATABASE_URL",
    projectId: "PLACEHOLDER_PROJECT_ID",
    storageBucket: "PLACEHOLDER_STORAGE_BUCKET",
    messagingSenderId: "PLACEHOLDER_MESSAGING_SENDER_ID",
    appId: "PLACEHOLDER_APP_ID",
    measurementId: "PLACEHOLDER_MEASUREMENT_ID"
  };

  console.log('🔥 SERVICE WORKER: Firebase config loaded:', {
    apiKey: firebaseConfig.apiKey ? 'Set' : 'Missing',
    authDomain: firebaseConfig.authDomain ? 'Set' : 'Missing',
    projectId: firebaseConfig.projectId ? 'Set' : 'Missing',
    messagingSenderId: firebaseConfig.messagingSenderId ? 'Set' : 'Missing'
  });

  // Initialize Firebase
  console.log('🔥 SERVICE WORKER: Initializing Firebase app...');
  firebase.initializeApp(firebaseConfig);
  console.log('🔥 SERVICE WORKER: Firebase app initialized successfully');

  // Retrieve Firebase Messaging object
  console.log('🔥 SERVICE WORKER: Getting Firebase messaging instance...');
  messaging = firebase.messaging();
  console.log('🔥 SERVICE WORKER: Firebase messaging instance obtained successfully');
} catch (error) {
  console.error('🔥 SERVICE WORKER: Error during Firebase initialization:', error);
  throw error;
}

// Handle background messages
if (messaging) {
  messaging.onBackgroundMessage(function(payload) {
    console.log('🔥🔥🔥 SERVICE WORKER: FIREBASE BACKGROUND MESSAGE RECEIVED 🔥🔥🔥');
    console.log('🔥 SW: Payload:', payload);
    console.log('🔥 SW: Title:', payload.notification?.title);
    console.log('🔥 SW: Body:', payload.notification?.body);
    console.log('🔥 SW: Data:', payload.data);
    console.log('🔥 SW: Timestamp:', new Date().toISOString());

  const notificationTitle = payload.notification?.title || 'Yapplr Notification';
  const notificationOptions = {
    body: payload.notification?.body || 'You have a new notification',
    icon: '/next.svg', // You can replace this with your app icon
    badge: '/next.svg',
    tag: payload.data?.type || 'general',
    data: payload.data,
    actions: [
      {
        action: 'open',
        title: 'Open App'
      },
      {
        action: 'dismiss',
        title: 'Dismiss'
      }
    ]
  };

    console.log('🔥 SW: Showing notification:', notificationTitle, notificationOptions);
    self.registration.showNotification(notificationTitle, notificationOptions);
    console.log('🔥 SW: Background notification processing complete');
  });
} else {
  console.warn('🔥 SERVICE WORKER: Messaging not initialized, background messages will not be handled');
}

// Handle notification click events
self.addEventListener('notificationclick', function(event) {
  console.log('🔥🔥🔥 SERVICE WORKER: NOTIFICATION CLICK RECEIVED 🔥🔥🔥');
  console.log('🔥 SW: Event:', event);
  console.log('🔥 SW: Action:', event.action);
  console.log('🔥 SW: Notification data:', event.notification.data);

  event.notification.close();

  if (event.action === 'dismiss') {
    console.log('🔥 SW: Dismiss action, returning');
    return;
  }

  // Handle different notification types
  const data = event.notification.data;
  let url = '/';

  if (data) {
    switch (data.type) {
      case 'message':
        url = `/messages/${data.conversationId}`;
        break;
      case 'mention':
      case 'reply':
        url = `/posts/${data.postId}`;
        if (data.commentId) {
          url += `#comment-${data.commentId}`;
        }
        break;
      case 'follow':
        url = `/profile/${data.userId}`;
        break;
      case 'like':
      case 'repost':
        url = `/posts/${data.postId}`;
        break;
      default:
        url = '/notifications';
    }
  }

  // Open the app and navigate to the appropriate page
  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function(clientList) {
      // Check if there's already a window/tab open with the target URL
      for (let i = 0; i < clientList.length; i++) {
        const client = clientList[i];
        if (client.url.includes(url) && 'focus' in client) {
          return client.focus();
        }
      }

      // If no existing window/tab, open a new one
      if (clients.openWindow) {
        return clients.openWindow(url);
      }
    })
  );
});
