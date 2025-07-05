import { messaging, requestNotificationPermission, onMessageListener } from './firebase';
import { userApi } from './api';

export interface FirebaseNotificationPayload {
  notification?: {
    title: string;
    body: string;
    icon?: string;
  };
  data?: {
    type: 'message' | 'mention' | 'reply' | 'comment' | 'follow' | 'like' | 'repost' | 'follow_request';
    userId?: string;
    postId?: string;
    commentId?: string;
    conversationId?: string;
    [key: string]: string | undefined;
  };
}

class FirebaseMessagingService {
  private fcmToken: string | null = null;
  private isInitialized = false;
  private messageListeners: ((payload: FirebaseNotificationPayload) => void)[] = [];

  constructor() {
    console.log('🔥 FirebaseMessagingService instance created');
    console.log('🔥 Constructor called at:', new Date().toISOString());
    console.log('🔥 Window object available:', typeof window !== 'undefined');
  }

  async initialize(): Promise<boolean> {
    console.log('🔥🔥🔥 FIREBASE MESSAGING INITIALIZE CALLED 🔥🔥🔥');
    console.log('🔥 isInitialized:', this.isInitialized);
    console.log('🔥 Window available:', typeof window !== 'undefined');

    // Only initialize on client side
    if (typeof window === 'undefined') {
      console.log('🔥 Server side detected, skipping Firebase messaging initialization');
      return false;
    }

    if (this.isInitialized) {
      console.log('🔥 Firebase messaging already initialized');
      return true;
    }

    try {
      console.log('🔥🔥🔥 STARTING FIREBASE MESSAGING INITIALIZATION (CLIENT SIDE) 🔥🔥🔥');
      console.log('🔥 Current notification permission:', Notification.permission);
      console.log('🔥 Service Worker supported:', 'serviceWorker' in navigator);
      console.log('🔥 Window location:', window.location.href);
      console.log('🔥 User agent:', navigator.userAgent);

      // Register service worker from API route (with environment variables injected)
      if ('serviceWorker' in navigator) {
        console.log('Registering service worker...');
        console.log('Service worker URL: /api/firebase-messaging-sw');

        // Clear any existing service workers first to avoid conflicts
        const existingRegistrations = await navigator.serviceWorker.getRegistrations();
        console.log('Current service workers:', existingRegistrations);

        // Unregister any existing service workers for this scope
        for (const registration of existingRegistrations) {
          if (registration.scope.includes('/api/')) {
            console.log('Unregistering existing service worker:', registration.scope);
            await registration.unregister();
          }
        }

        try {
          const registration = await navigator.serviceWorker.register('/api/firebase-messaging-sw', {
            updateViaCache: 'none' // Ensure we get the latest version
          });
          console.log('Service Worker registered successfully:', registration);
          console.log('Service Worker state:', registration.installing?.state || registration.waiting?.state || registration.active?.state);

          // If there's a waiting service worker, skip waiting to activate it immediately
          if (registration.waiting) {
            console.log('Service worker is waiting, sending skipWaiting message...');
            registration.waiting.postMessage({ type: 'SKIP_WAITING' });
          }

          // Wait for service worker to be ready with proper timeout and activation handling
          console.log('🔥 Waiting for service worker to be ready...');

          const readyPromise = navigator.serviceWorker.ready;
          const timeoutPromise = new Promise((_, reject) =>
            setTimeout(() => reject(new Error('Service worker ready timeout after 10 seconds')), 10000)
          );

          try {
            const readyRegistration = await Promise.race([readyPromise, timeoutPromise]) as ServiceWorkerRegistration;
            console.log('🔥 Service Worker is ready!', {
              installing: readyRegistration.installing?.state,
              waiting: readyRegistration.waiting?.state,
              active: readyRegistration.active?.state,
              scope: readyRegistration.scope
            });
          } catch (readyError) {
            console.warn('🔥 Service Worker ready timeout, but continuing:', readyError instanceof Error ? readyError.message : readyError);
            console.warn('🔥 This was the "Service worker ready timeout" error you were seeing');

            // Check current service worker state after timeout
            const currentRegistration = await navigator.serviceWorker.getRegistration('/api/firebase-messaging-sw');
            if (currentRegistration) {
              console.warn('🔥 Service worker registration exists after timeout:', {
                installing: currentRegistration.installing?.state,
                waiting: currentRegistration.waiting?.state,
                active: currentRegistration.active?.state,
                scope: currentRegistration.scope
              });

              // If there's an active service worker, we can continue
              if (currentRegistration.active) {
                console.log('🔥 Active service worker found, continuing with Firebase initialization');
              }
            } else {
              console.warn('🔥 No service worker registration found after timeout');
            }

            // Don't throw here - continue with Firebase initialization for foreground notifications
          }
        } catch (swError) {
          console.error('Service Worker registration failed:', swError);
          console.log('Continuing without service worker - foreground notifications will still work');
          // Continue without service worker for now
        }
      } else {
        console.warn('Service Worker not supported');
      }

      console.log('🔥 Service worker registration complete, continuing with FCM token...');

      // Check if permission is already granted
      console.log('Checking notification permission...');
      if (Notification.permission === 'granted') {
        console.log('Permission already granted, getting token...');
        try {
          const token = await requestNotificationPermission();
          if (token) {
            this.fcmToken = token;
            console.log('FCM Token obtained:', token.substring(0, 20) + '...');

            // Send token to backend
            console.log('Sending token to server...');
            await this.sendTokenToServer(token);

            // Set up foreground message listener
            console.log('Setting up message listener...');
            this.setupMessageListener();

            this.isInitialized = true;
            console.log('Firebase messaging initialized successfully with notifications');
            return true;
          } else {
            console.warn('Failed to get FCM token despite granted permission');
          }
        } catch (tokenError) {
          console.error('Error getting FCM token:', tokenError);
        }
      } else {
        console.log('Notification permission not granted:', Notification.permission);
      }

      // Permission not granted, but Firebase is still initialized for manual permission request
      console.log('Firebase messaging initialized without notifications');
      this.isInitialized = true;
      return true; // Return true so polling doesn't kick in unnecessarily
    } catch (error) {
      console.error('Error initializing Firebase messaging:', error);
      return false;
    }
  }

  private async sendTokenToServer(token: string): Promise<void> {
    try {
      // Send the FCM token to your backend API
      await userApi.updateFcmToken(token);
      console.log('FCM token sent to server successfully');
    } catch (error) {
      console.error('Error sending FCM token to server:', error);
    }
  }

  private setupMessageListener(): void {
    console.log('🔥 Setting up Firebase message listener...');
    console.log('🔥 Message listener setup timestamp:', new Date().toISOString());

    // Use the callback-based listener that persists for multiple messages
    let messageCount = 0;
    onMessageListener((payload: any) => {
      try {
        messageCount++;
        console.log('🔥🔥🔥 FIREBASE NOTIFICATION RECEIVED (FOREGROUND) #' + messageCount + ' 🔥🔥🔥');
        console.log('🔥 Message listener still active at:', new Date().toISOString());
        console.log('🔥 Notification title:', payload.notification?.title);
        console.log('🔥 Notification body:', payload.notification?.body);
        console.log('🔥 Notification data:', payload.data);
        console.log('🔥 Full payload:', JSON.stringify(payload, null, 2));
        console.log('🔥 Timestamp:', new Date().toISOString());

        // Show notification if the app is in foreground
        try {
          this.showForegroundNotification(payload);
          console.log('🔥 Foreground notification shown successfully');
        } catch (notificationError) {
          console.error('🔥 Error showing foreground notification:', notificationError);
        }

        // Notify all listeners
        console.log('🔥 Notifying', this.messageListeners.length, 'listeners');
        this.messageListeners.forEach((listener, index) => {
          try {
            console.log('🔥 Calling listener', index + 1);
            listener(payload);
            console.log('🔥 Listener', index + 1, 'completed successfully');
          } catch (error) {
            console.error('🔥 Error in message listener', index + 1, ':', error);
          }
        });

        console.log('🔥 Firebase notification processing complete');
        console.log('🔥 Message listener should still be active for next message');

        // Log current FCM token status
        console.log('🔥 Current FCM token:', this.fcmToken ? this.fcmToken.substring(0, 20) + '...' : 'null');

        // Auto-refresh FCM token every 3 messages (Safari limitation workaround)
        if (messageCount % 3 === 0) {
          console.log(`🔥 REACHED ${messageCount} MESSAGES - Auto-refreshing FCM token (Safari workaround)...`);
          setTimeout(async () => {
            try {
              console.log('🔥 Attempting to refresh FCM token...');
              const newToken = await requestNotificationPermission();
              if (newToken) {
                console.log('🔥 FCM token refreshed:', newToken.substring(0, 20) + '...');
                this.fcmToken = newToken;
                await this.sendTokenToServer(newToken);
                console.log('🔥 Refreshed FCM token sent to server - ready for next batch of messages');
              } else {
                console.log('🔥 FCM token refresh failed');
              }
            } catch (error) {
              console.error('🔥 Error refreshing FCM token:', error);
            }
          }, 2000); // Wait 2 seconds after every 3rd message
        }
      } catch (error) {
        console.error('🔥 CRITICAL ERROR in Firebase message handler:', error);
        console.error('🔥 This error should not break the message listener');
      }
    });

    console.log('🔥 Firebase message listener setup complete - ready for multiple messages');

    // Add a heartbeat to monitor listener status
    const heartbeatInterval = setInterval(() => {
      console.log('🔥 HEARTBEAT: Message listener still active, received', messageCount, 'messages so far');
      console.log('🔥 HEARTBEAT: Current time:', new Date().toISOString());
      console.log('🔥 HEARTBEAT: FCM token still valid:', this.fcmToken ? this.fcmToken.substring(0, 20) + '...' : 'null');

      // Check if we're stuck at multiples of 3 (Safari FCM limitation)
      if (messageCount > 0 && messageCount % 3 === 0) {
        console.log(`🔥 HEARTBEAT: At ${messageCount} messages - Safari FCM may need token refresh soon`);
      }
    }, 30000); // Every 30 seconds

    // Store the interval so we can clear it later if needed
    (window as any).firebaseHeartbeat = heartbeatInterval;
  }

  private showForegroundNotification(payload: FirebaseNotificationPayload): void {
    console.log('🔥 Attempting to show foreground notification');
    console.log('🔥 Notification support:', 'Notification' in window);
    console.log('🔥 Notification permission:', Notification.permission);

    if (!('Notification' in window) || Notification.permission !== 'granted') {
      console.log('🔥 Cannot show notification - no permission or support');
      return;
    }

    const title = payload.notification?.title || 'Yapplr Notification';
    const options = {
      body: payload.notification?.body || 'You have a new notification',
      icon: payload.notification?.icon || '/next.svg',
      tag: payload.data?.type || 'general',
      data: payload.data,
    };

    console.log('🔥 Creating browser notification:', title, options);
    const notification = new Notification(title, options);

    notification.onclick = () => {
      console.log('🔥 Notification clicked');
      window.focus();
      notification.close();

      // Navigate to appropriate page based on notification type
      this.handleNotificationClick(payload.data);
    };

    console.log('🔥 Browser notification created successfully');
  }

  private handleNotificationClick(data?: FirebaseNotificationPayload['data']): void {
    if (!data) {
      window.location.href = '/notifications';
      return;
    }

    switch (data.type) {
      case 'message':
        if (data.conversationId) {
          window.location.href = `/messages/${data.conversationId}`;
        } else {
          window.location.href = '/messages';
        }
        break;
      case 'mention':
      case 'reply':
      case 'comment':
        if (data.postId) {
          let url = `/posts/${data.postId}`;
          if (data.commentId) {
            url += `#comment-${data.commentId}`;
          }
          window.location.href = url;
        } else {
          window.location.href = '/notifications';
        }
        break;
      case 'follow':
        if (data.userId) {
          window.location.href = `/profile/${data.userId}`;
        } else {
          window.location.href = '/notifications';
        }
        break;
      case 'like':
      case 'repost':
        if (data.postId) {
          window.location.href = `/posts/${data.postId}`;
        } else {
          window.location.href = '/notifications';
        }
        break;
      default:
        window.location.href = '/notifications';
    }
  }

  addMessageListener(listener: (payload: FirebaseNotificationPayload) => void): void {
    this.messageListeners.push(listener);
  }

  removeMessageListener(listener: (payload: FirebaseNotificationPayload) => void): void {
    const index = this.messageListeners.indexOf(listener);
    if (index > -1) {
      this.messageListeners.splice(index, 1);
    }
  }

  async refreshToken(): Promise<string | null> {
    try {
      const token = await requestNotificationPermission();
      if (token && token !== this.fcmToken) {
        this.fcmToken = token;
        await this.sendTokenToServer(token);
      }
      return token;
    } catch (error) {
      console.error('Error refreshing FCM token:', error);
      return null;
    }
  }

  getToken(): string | null {
    return this.fcmToken;
  }

  // Method to manually request notification permission (must be called from user gesture)
  async requestPermission(): Promise<boolean> {
    try {
      console.log('Manually requesting notification permission...');
      const token = await requestNotificationPermission();
      if (token) {
        this.fcmToken = token;
        console.log('FCM Token obtained:', token.substring(0, 20) + '...');

        // Send token to backend
        await this.sendTokenToServer(token);

        // Set up foreground message listener if not already done
        if (!this.messageListeners.length) {
          this.setupMessageListener();
        }

        console.log('Notifications enabled successfully');
        return true;
      }
      return false;
    } catch (error) {
      console.error('Error requesting notification permission:', error);
      return false;
    }
  }

  isReady(): boolean {
    return this.isInitialized && this.fcmToken !== null;
  }
}

// Export singleton instance
console.log('🔥 Creating FirebaseMessagingService singleton');
export const firebaseMessagingService = new FirebaseMessagingService();
console.log('🔥 FirebaseMessagingService singleton created and exported');
