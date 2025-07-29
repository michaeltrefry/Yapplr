import * as signalR from '@microsoft/signalr';

export interface SignalRNotificationPayload {
  type: 'message' | 'mention' | 'reply' | 'comment' | 'follow' | 'like' | 'repost' | 'follow_request' | 'test' | 'generic' | 'VideoProcessingCompleted' | 'systemMessage';
  title: string;
  body: string;
  data?: {
    userId?: string;
    postId?: string;
    commentId?: string;
    conversationId?: string;
    [key: string]: string | undefined;
  };
  timestamp: string;
}

class SignalRMessagingService {
  private connection: signalR.HubConnection | null = null;
  private isInitialized = false;
  private isConnected = false;
  private messageListeners: ((payload: SignalRNotificationPayload) => void)[] = [];
  private connectionListeners: ((connected: boolean) => void)[] = [];
  private typingListeners: ((action: 'started' | 'stopped', data: any) => void)[] = [];
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 1000; // Start with 1 second

  constructor() {
    console.log('📡 SignalRMessagingService instance created');
  }

  async initialize(): Promise<boolean> {
    console.log('📡📡📡 SIGNALR MESSAGING INITIALIZE CALLED 📡📡📡');
    console.log('📡 isInitialized:', this.isInitialized);
    console.log('📡 Window available:', typeof window !== 'undefined');

    // Only initialize on client side
    if (typeof window === 'undefined') {
      console.log('📡 Server side detected, skipping SignalR messaging initialization');
      return false;
    }

    if (this.isInitialized) {
      console.log('📡 SignalR messaging already initialized, connection state:', this.isConnected);
      // If initialized but not connected, try to reconnect
      if (!this.isConnected && this.connection) {
        try {
          console.log('📡 Attempting to restart existing SignalR connection...');
          await this.connection.start();
          this.isConnected = true;
          this.notifyConnectionListeners(true);
          console.log('📡 SignalR connection restarted successfully');
          return true;
        } catch (error) {
          console.error('📡 Failed to restart SignalR connection:', error);
          // Reset and try full initialization
          this.isInitialized = false;
          this.connection = null;
        }
      } else if (this.isConnected) {
        return true;
      }
    }

    try {
      console.log('📡📡📡 STARTING SIGNALR MESSAGING INITIALIZATION (CLIENT SIDE) 📡📡📡');

      const token = localStorage.getItem('token');
      if (!token) {
        console.warn('📡 No auth token available, cannot connect to SignalR');
        return false;
      }

      // Create SignalR connection with explicit transport configuration
      const hubUrl = `${process.env.NEXT_PUBLIC_API_URL}/notificationHub`;
      console.log('📡 Connecting to SignalR hub at:', hubUrl);

      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          accessTokenFactory: () => token,
          transport: signalR.HttpTransportType.LongPolling, // Use LongPolling only to avoid WebSocket issues
          skipNegotiation: false
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            // Exponential backoff: 1s, 2s, 4s, 8s, 16s
            const delay = Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 16000);
            console.log(`📡 Reconnect attempt ${retryContext.previousRetryCount + 1}, delay: ${delay}ms`);
            return delay;
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Set up event handlers
      this.setupEventHandlers();

      // Start the connection
      console.log('📡 Starting SignalR connection...');
      await this.connection.start();
      this.isConnected = true;
      this.isInitialized = true;
      this.reconnectAttempts = 0;

      console.log('📡 SignalR connection established successfully');
      console.log('📡 Connection state:', this.connection.state);
      console.log('📡 Connection ID:', this.connection.connectionId);
      this.notifyConnectionListeners(true);

      return true;
    } catch (error) {
      console.error('📡 Failed to initialize SignalR messaging:', error);
      console.error('📡 Error details:', {
        message: error instanceof Error ? error.message : 'Unknown error',
        hubUrl: `${process.env.NEXT_PUBLIC_API_URL}/notificationHub`,
        hasToken: !!localStorage.getItem('token')
      });

      // Check if it's a specific transport error
      if (error instanceof Error && error.message.includes('ServerSentEvents')) {
        console.error('📡 ServerSentEvents transport failed. This might be due to:');
        console.error('📡 1. Backend server not running');
        console.error('📡 2. CORS configuration issues');
        console.error('📡 3. SignalR hub not properly configured');
        console.error('📡 4. Network connectivity issues');
      }

      this.isConnected = false;
      this.notifyConnectionListeners(false);
      return false;
    }
  }

  private setupEventHandlers(): void {
    if (!this.connection) return;

    // Handle incoming notifications
    this.connection.on('Notification', (payload: SignalRNotificationPayload) => {
      console.log('📡 Received SignalR notification:', payload);
      this.notifyMessageListeners(payload);
    });

    // Handle connection events
    this.connection.on('Connected', (data: unknown) => {
      console.log('📡 SignalR Connected event received:', data);
    });

    // Handle ping/pong for testing
    this.connection.on('Pong', (data: unknown) => {
      console.log('📡 SignalR Pong received:', data);
    });

    // Handle conversation events
    this.connection.on('JoinedConversation', (conversationId: number) => {
      console.log('📡 Joined conversation:', conversationId);
    });

    this.connection.on('LeftConversation', (conversationId: number) => {
      console.log('📡 Left conversation:', conversationId);
    });

    // Handle typing indicator events
    this.connection.on('UserStartedTyping', (data: any) => {
      console.log('📡 User started typing:', data);
      this.notifyTypingListeners('started', data);
    });

    this.connection.on('UserStoppedTyping', (data: any) => {
      console.log('📡 User stopped typing:', data);
      this.notifyTypingListeners('stopped', data);
    });

    this.connection.on('NewMessage', (messageData: any) => {
      console.log('📡 New message in conversation:', messageData);

      // Convert NewMessage event to a notification payload for consistency
      if (messageData && messageData.conversationId) {
        const notificationPayload: SignalRNotificationPayload = {
          type: 'message',
          title: 'New Message',
          body: 'You have a new message',
          data: {
            conversationId: messageData.conversationId.toString(),
            userId: messageData.senderId?.toString(),
          },
          timestamp: new Date().toISOString()
        };

        console.log('📡 Converting NewMessage to notification payload:', notificationPayload);
        this.notifyMessageListeners(notificationPayload);
      }
    });

    // Handle errors
    this.connection.on('Error', (error: string) => {
      console.error('📡 SignalR error:', error);
    });

    // Connection state change handlers
    this.connection.onclose((error) => {
      console.log('📡 SignalR connection closed:', error);
      this.isConnected = false;
      this.notifyConnectionListeners(false);
    });

    this.connection.onreconnecting((error) => {
      console.log('📡 SignalR reconnecting:', error);
      this.isConnected = false;
      this.notifyConnectionListeners(false);
    });

    this.connection.onreconnected((connectionId) => {
      console.log('📡 SignalR reconnected with connection ID:', connectionId);
      this.isConnected = true;
      this.reconnectAttempts = 0;
      this.notifyConnectionListeners(true);
    });
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop();
        console.log('📡 SignalR connection stopped');
      } catch (error) {
        console.error('📡 Error stopping SignalR connection:', error);
      }
      this.connection = null;
    }
    this.isConnected = false;
    this.isInitialized = false;
    this.notifyConnectionListeners(false);
  }

  isReady(): boolean {
    return this.isInitialized && this.isConnected;
  }

  async ping(): Promise<void> {
    if (this.connection && this.isConnected) {
      try {
        await this.connection.invoke('Ping');
        console.log('📡 Ping sent to SignalR hub');
      } catch (error) {
        console.error('📡 Error sending ping:', error);
      }
    }
  }

  async joinConversation(conversationId: number): Promise<void> {
    if (this.connection && this.isConnected) {
      try {
        await this.connection.invoke('JoinConversation', conversationId);
        console.log('📡 Joined conversation:', conversationId);
      } catch (error) {
        console.error('📡 Error joining conversation:', error);
      }
    }
  }

  async leaveConversation(conversationId: number): Promise<void> {
    if (this.connection && this.isConnected) {
      try {
        await this.connection.invoke('LeaveConversation', conversationId);
        console.log('📡 Left conversation:', conversationId);
      } catch (error) {
        console.error('📡 Error leaving conversation:', error);
      }
    }
  }

  async startTyping(conversationId: number): Promise<void> {
    if (this.connection && this.isConnected) {
      try {
        await this.connection.invoke('StartTyping', conversationId);
        console.log('📡 Started typing in conversation:', conversationId);
      } catch (error) {
        console.error('📡 Error starting typing indicator:', error);
      }
    }
  }

  async stopTyping(conversationId: number): Promise<void> {
    if (this.connection && this.isConnected) {
      try {
        await this.connection.invoke('StopTyping', conversationId);
        console.log('📡 Stopped typing in conversation:', conversationId);
      } catch (error) {
        console.error('📡 Error stopping typing indicator:', error);
      }
    }
  }

  // Listener management
  addMessageListener(callback: (payload: SignalRNotificationPayload) => void): void {
    this.messageListeners.push(callback);
    console.log('📡 Added SignalR message listener, total:', this.messageListeners.length);
  }

  removeMessageListener(callback: (payload: SignalRNotificationPayload) => void): void {
    const index = this.messageListeners.indexOf(callback);
    if (index > -1) {
      this.messageListeners.splice(index, 1);
      console.log('📡 Removed SignalR message listener, total:', this.messageListeners.length);
    }
  }

  addConnectionListener(callback: (connected: boolean) => void): void {
    this.connectionListeners.push(callback);
    console.log('📡 Added SignalR connection listener, total:', this.connectionListeners.length);
  }

  removeConnectionListener(callback: (connected: boolean) => void): void {
    const index = this.connectionListeners.indexOf(callback);
    if (index > -1) {
      this.connectionListeners.splice(index, 1);
      console.log('📡 Removed SignalR connection listener, total:', this.connectionListeners.length);
    }
  }

  addTypingListener(callback: (action: 'started' | 'stopped', data: any) => void): void {
    this.typingListeners.push(callback);
    console.log('📡 Added SignalR typing listener, total:', this.typingListeners.length);
  }

  removeTypingListener(callback: (action: 'started' | 'stopped', data: any) => void): void {
    const index = this.typingListeners.indexOf(callback);
    if (index > -1) {
      this.typingListeners.splice(index, 1);
      console.log('📡 Removed SignalR typing listener, total:', this.typingListeners.length);
    }
  }

  private notifyMessageListeners(payload: SignalRNotificationPayload): void {
    this.messageListeners.forEach(listener => {
      try {
        listener(payload);
      } catch (error) {
        console.error('📡 Error in SignalR message listener:', error);
      }
    });
  }

  private notifyConnectionListeners(connected: boolean): void {
    this.connectionListeners.forEach(listener => {
      try {
        listener(connected);
      } catch (error) {
        console.error('📡 Error in SignalR connection listener:', error);
      }
    });
  }

  private notifyTypingListeners(action: 'started' | 'stopped', data: any): void {
    this.typingListeners.forEach(listener => {
      try {
        listener(action, data);
      } catch (error) {
        console.error('📡 Error in SignalR typing listener:', error);
      }
    });
  }

  // Getters
  get connected(): boolean {
    return this.isConnected;
  }

  get initialized(): boolean {
    return this.isInitialized;
  }

  get connectionState(): string {
    return this.connection?.state ?? 'Disconnected';
  }
}

// Export singleton instance
export const signalRMessagingService = new SignalRMessagingService();
