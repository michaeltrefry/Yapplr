import { messaging, requestNotificationPermission, onMessageListener } from './firebase';
import { userApi } from './api';

export interface FirebaseNotificationPayload {
  notification?: {
    title: string;
    body: string;
    icon?: string;
  };
  data?: {
    type: 'message' | 'mention' | 'reply' | 'follow' | 'like' | 'repost';
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

  async initialize(): Promise<boolean> {
    if (this.isInitialized) {
      return true;
    }

    try {
      // Register service worker from API route (with environment variables injected)
      if ('serviceWorker' in navigator) {
        const registration = await navigator.serviceWorker.register('/api/firebase-messaging-sw');
        console.log('Service Worker registered:', registration);
      }

      // Request notification permission and get token
      const token = await requestNotificationPermission();
      if (token) {
        this.fcmToken = token;
        console.log('FCM Token:', token);
        
        // Send token to backend
        await this.sendTokenToServer(token);
        
        // Set up foreground message listener
        this.setupMessageListener();
        
        this.isInitialized = true;
        return true;
      } else {
        console.warn('Failed to get FCM token');
        return false;
      }
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
    onMessageListener()
      .then((payload: any) => {
        console.log('Received foreground message:', payload);
        
        // Show notification if the app is in foreground
        this.showForegroundNotification(payload);
        
        // Notify all listeners
        this.messageListeners.forEach(listener => listener(payload));
      })
      .catch((error) => {
        console.error('Error setting up message listener:', error);
      });
  }

  private showForegroundNotification(payload: FirebaseNotificationPayload): void {
    if (!('Notification' in window) || Notification.permission !== 'granted') {
      return;
    }

    const title = payload.notification?.title || 'Yapplr Notification';
    const options = {
      body: payload.notification?.body || 'You have a new notification',
      icon: payload.notification?.icon || '/next.svg',
      tag: payload.data?.type || 'general',
      data: payload.data,
    };

    const notification = new Notification(title, options);
    
    notification.onclick = () => {
      window.focus();
      notification.close();
      
      // Navigate to appropriate page based on notification type
      this.handleNotificationClick(payload.data);
    };
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

  isReady(): boolean {
    return this.isInitialized && this.fcmToken !== null;
  }
}

// Export singleton instance
export const firebaseMessagingService = new FirebaseMessagingService();
