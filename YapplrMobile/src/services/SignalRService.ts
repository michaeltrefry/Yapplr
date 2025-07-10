import * as signalR from '@microsoft/signalr';
import AsyncStorage from '@react-native-async-storage/async-storage';
import 'react-native-url-polyfill/auto'; // Required for SignalR in React Native

export interface SignalRNotificationPayload {
  type: 'message' | 'mention' | 'reply' | 'comment' | 'follow' | 'like' | 'repost' | 'follow_request' | 'test' | 'generic';
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

export interface SignalRConnectionStatus {
  connected: boolean;
  connectionState: string;
  lastConnected?: Date;
  lastError?: string;
}

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private isInitialized = false;
  private isConnected = false;
  private messageListeners: ((payload: SignalRNotificationPayload) => void)[] = [];
  private connectionListeners: ((status: SignalRConnectionStatus) => void)[] = [];
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private baseURL: string;

  constructor(baseURL: string) {
    this.baseURL = baseURL;
    console.log('📱📡 SignalRService instance created for mobile');
  }

  async initialize(): Promise<boolean> {
    console.log('📱📡📡📡 MOBILE SIGNALR INITIALIZE CALLED 📡📡📡');
    console.log('📱📡 isInitialized:', this.isInitialized);

    if (this.isInitialized) {
      console.log('📱📡 SignalR already initialized');
      return this.isConnected;
    }

    try {
      console.log('📱📡📡📡 STARTING MOBILE SIGNALR INITIALIZATION 📡📡📡');
      
      const token = await AsyncStorage.getItem('yapplr_token');
      if (!token) {
        console.warn('📱📡 No auth token available, cannot connect to SignalR');
        return false;
      }

      // Create SignalR connection with React Native specific configuration
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(`${this.baseURL}/notificationHub`, {
          accessTokenFactory: () => token,
          // React Native specific transport configuration
          transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
          skipNegotiation: false,
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: (retryContext) => {
            // Exponential backoff: 1s, 2s, 4s, 8s, 16s
            const delay = Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 16000);
            console.log(`📱📡 Reconnect attempt ${retryContext.previousRetryCount + 1}, delay: ${delay}ms`);
            return delay;
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Set up event handlers
      this.setupEventHandlers();

      // Start the connection
      await this.connection.start();
      this.isConnected = true;
      this.isInitialized = true;
      this.reconnectAttempts = 0;

      console.log('📱📡 Mobile SignalR connection established successfully');
      this.notifyConnectionListeners({
        connected: true,
        connectionState: this.connection.state,
        lastConnected: new Date()
      });

      return true;
    } catch (error) {
      console.error('📱📡 Failed to initialize mobile SignalR:', error);
      this.isConnected = false;
      this.notifyConnectionListeners({
        connected: false,
        connectionState: 'Disconnected',
        lastError: error instanceof Error ? error.message : 'Unknown error'
      });
      return false;
    }
  }

  private setupEventHandlers(): void {
    if (!this.connection) return;

    // Handle incoming notifications
    this.connection.on('Notification', (payload: SignalRNotificationPayload) => {
      console.log('📱📡 Received mobile SignalR notification:', payload);
      this.notifyMessageListeners(payload);
    });

    // Handle connection events
    this.connection.on('Connected', (data: any) => {
      console.log('📱📡 Mobile SignalR Connected event received:', data);
    });

    // Handle ping/pong for testing
    this.connection.on('Pong', (data: any) => {
      console.log('📱📡 Mobile SignalR Pong received:', data);
    });

    // Handle conversation events
    this.connection.on('JoinedConversation', (conversationId: number) => {
      console.log('📱📡 Joined conversation:', conversationId);
    });

    this.connection.on('LeftConversation', (conversationId: number) => {
      console.log('📱📡 Left conversation:', conversationId);
    });

    this.connection.on('NewMessage', (messageData: any) => {
      console.log('📱📡 New message in conversation:', messageData);
      // This could trigger a refresh of the conversation
    });

    // Handle errors
    this.connection.on('Error', (error: string) => {
      console.error('📱📡 Mobile SignalR error:', error);
    });

    // Connection state change handlers
    this.connection.onclose((error) => {
      console.log('📱📡 Mobile SignalR connection closed:', error);
      this.isConnected = false;
      this.notifyConnectionListeners({
        connected: false,
        connectionState: 'Disconnected',
        lastError: error?.message
      });
    });

    this.connection.onreconnecting((error) => {
      console.log('📱📡 Mobile SignalR reconnecting:', error);
      this.isConnected = false;
      this.notifyConnectionListeners({
        connected: false,
        connectionState: 'Reconnecting',
        lastError: error?.message
      });
    });

    this.connection.onreconnected((connectionId) => {
      console.log('📱📡 Mobile SignalR reconnected with connection ID:', connectionId);
      this.isConnected = true;
      this.reconnectAttempts = 0;
      this.notifyConnectionListeners({
        connected: true,
        connectionState: this.connection?.state || 'Connected',
        lastConnected: new Date()
      });
    });
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop();
        console.log('📱📡 Mobile SignalR connection stopped');
      } catch (error) {
        console.error('📱📡 Error stopping mobile SignalR connection:', error);
      }
      this.connection = null;
    }
    this.isConnected = false;
    this.isInitialized = false;
    this.notifyConnectionListeners({
      connected: false,
      connectionState: 'Disconnected'
    });
  }

  async ping(): Promise<void> {
    if (this.connection && this.isConnected) {
      try {
        await this.connection.invoke('Ping');
        console.log('📱📡 Ping sent to mobile SignalR hub');
      } catch (error) {
        console.error('📱📡 Error sending ping:', error);
      }
    }
  }

  async joinConversation(conversationId: number): Promise<void> {
    if (this.connection && this.isConnected) {
      try {
        await this.connection.invoke('JoinConversation', conversationId);
        console.log('📱📡 Joined conversation:', conversationId);
      } catch (error) {
        console.error('📱📡 Error joining conversation:', error);
      }
    }
  }

  async leaveConversation(conversationId: number): Promise<void> {
    if (this.connection && this.isConnected) {
      try {
        await this.connection.invoke('LeaveConversation', conversationId);
        console.log('📱📡 Left conversation:', conversationId);
      } catch (error) {
        console.error('📱📡 Error leaving conversation:', error);
      }
    }
  }

  // Listener management
  addMessageListener(callback: (payload: SignalRNotificationPayload) => void): void {
    this.messageListeners.push(callback);
    console.log('📱📡 Added mobile SignalR message listener, total:', this.messageListeners.length);
  }

  removeMessageListener(callback: (payload: SignalRNotificationPayload) => void): void {
    const index = this.messageListeners.indexOf(callback);
    if (index > -1) {
      this.messageListeners.splice(index, 1);
      console.log('📱📡 Removed mobile SignalR message listener, total:', this.messageListeners.length);
    }
  }

  addConnectionListener(callback: (status: SignalRConnectionStatus) => void): void {
    this.connectionListeners.push(callback);
    console.log('📱📡 Added mobile SignalR connection listener, total:', this.connectionListeners.length);
  }

  removeConnectionListener(callback: (status: SignalRConnectionStatus) => void): void {
    const index = this.connectionListeners.indexOf(callback);
    if (index > -1) {
      this.connectionListeners.splice(index, 1);
      console.log('📱📡 Removed mobile SignalR connection listener, total:', this.connectionListeners.length);
    }
  }

  private notifyMessageListeners(payload: SignalRNotificationPayload): void {
    this.messageListeners.forEach(listener => {
      try {
        listener(payload);
      } catch (error) {
        console.error('📱📡 Error in mobile SignalR message listener:', error);
      }
    });
  }

  private notifyConnectionListeners(status: SignalRConnectionStatus): void {
    this.connectionListeners.forEach(listener => {
      try {
        listener(status);
      } catch (error) {
        console.error('📱📡 Error in mobile SignalR connection listener:', error);
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

  getStatus(): SignalRConnectionStatus {
    return {
      connected: this.isConnected,
      connectionState: this.connectionState,
      lastConnected: this.isConnected ? new Date() : undefined
    };
  }
}

export default SignalRService;
