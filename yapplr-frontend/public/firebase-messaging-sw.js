// Import Firebase scripts for service worker
importScripts('https://www.gstatic.com/firebasejs/9.0.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/9.0.0/firebase-messaging-compat.js');

// Firebase configuration
const firebaseConfig = {
  apiKey: "AIzaSyB630nQ2IY3s39oj23J0R2jmRmT0WlKm5k",
  authDomain: "yapplr.firebaseapp.com",
  databaseURL: "https://yapplr-default-rtdb.firebaseio.com",
  projectId: "yapplr",
  storageBucket: "yapplr.firebasestorage.app",
  messagingSenderId: "508987117866",
  appId: "1:508987117866:web:2c39ac91dce61d4e548e53",
  measurementId: "G-GH2MX6V68B"
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
