// Firebase Service Worker Template
// This file serves as a template for the service worker.
// The actual service worker is served from /api/firebase-messaging-sw
// with environment variables injected for security.

// Import Firebase scripts for service worker
importScripts('https://www.gstatic.com/firebasejs/9.0.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/9.0.0/firebase-messaging-compat.js');

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

// Initialize Firebase
firebase.initializeApp(firebaseConfig);

// Retrieve Firebase Messaging object
const messaging = firebase.messaging();

// Handle background messages
messaging.onBackgroundMessage(function(payload) {
  console.log('[firebase-messaging-sw.js] Received background message ', payload);
  
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

  self.registration.showNotification(notificationTitle, notificationOptions);
});

// Handle notification click events
self.addEventListener('notificationclick', function(event) {
  console.log('[firebase-messaging-sw.js] Notification click received.');

  event.notification.close();

  if (event.action === 'dismiss') {
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
