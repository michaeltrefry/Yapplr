import axios, { AxiosInstance } from 'axios';
import { YapplrApi, LoginData, RegisterData, AuthResponse, User, UserProfile, TimelineItem, ConversationListItem, Conversation, CanMessageResponse, Message, SendMessageData, FollowResponse, CreatePostData, Post, ImageUploadResponse, Comment, CreateCommentData, UpdateCommentData, BlockResponse, BlockStatusResponse, NotificationList, UnreadCountResponse, CreateAppealDto } from '../types';

interface ApiConfig {
  baseURL: string;
  getToken: () => string | null;
  onUnauthorized: () => void;
}

export function createYapplrApi(config: ApiConfig): YapplrApi {
  const client: AxiosInstance = axios.create({
    baseURL: config.baseURL,
    timeout: 30000, // Increased timeout
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    },
    // React Native specific configurations
    validateStatus: (status) => status < 500, // Don't throw on 4xx errors
  });

  // Request interceptor to add auth token
  client.interceptors.request.use((requestConfig) => {
    const token = config.getToken();
    if (token) {
      requestConfig.headers.Authorization = `Bearer ${token}`;
    }
    console.log('API Request:', requestConfig.method?.toUpperCase(), requestConfig.url);
    return requestConfig;
  });

  // Response interceptor to handle unauthorized responses
  client.interceptors.response.use(
    (response) => {
      console.log('API Response:', response.status, response.config.url);
      return response;
    },
    (error) => {
      console.error('API Error:', error.message, error.config?.url);

      // Enhanced error logging for network issues
      if (error.code === 'NETWORK_ERROR' || error.message.includes('Network Error')) {
        console.error('Network connection issue detected');
        console.error('Base URL:', config.baseURL);
        console.error('Full URL:', error.config?.url);
      }

      if (error.response) {
        console.error('Response status:', error.response.status);
        console.error('Response data:', error.response.data);
      } else if (error.request) {
        console.error('No response received - possible network issue');
        console.error('Request details:', {
          url: error.config?.url,
          method: error.config?.method,
          baseURL: error.config?.baseURL
        });
      }

      if (error.response?.status === 401) {
        config.onUnauthorized();
      }

      // Transform network errors to be more user-friendly
      if (!error.response && error.request) {
        error.message = 'Network connection failed. Please check your internet connection and try again.';
      }

      return Promise.reject(error);
    }
  );

  return {
    auth: {
      login: async (data: LoginData): Promise<AuthResponse> => {
        const response = await client.post('/api/auth/login', data);
        return response.data;
      },

      register: async (data: RegisterData): Promise<AuthResponse> => {
        const response = await client.post('/api/auth/register', data);
        return response.data;
      },

      getCurrentUser: async (): Promise<User> => {
        const response = await client.get('/api/users/me');
        return response.data;
      },

      forgotPassword: async (email: string): Promise<{ message: string }> => {
        const response = await client.post('/api/auth/forgot-password', { email });
        return response.data;
      },

      resetPassword: async (token: string, newPassword: string): Promise<{ message: string }> => {
        const response = await client.post('/api/auth/reset-password', { token, newPassword });
        return response.data;
      },

      verifyEmail: async (token: string): Promise<{ message: string }> => {
        const response = await client.post('/api/auth/verify-email', { token });
        return response.data;
      },

      resendVerification: async (email: string): Promise<{ message: string }> => {
        const response = await client.post('/api/auth/resend-verification', { email });
        return response.data;
      },
    },

    posts: {
      getTimeline: async (page: number, limit: number): Promise<TimelineItem[]> => {
        const response = await client.get(`/api/posts/timeline?page=${page}&limit=${limit}`);
        return response.data;
      },

      createPost: async (data: CreatePostData): Promise<Post> => {
        const response = await client.post('/api/posts', data);
        return response.data;
      },

      likePost: async (postId: number): Promise<void> => {
        await client.post(`/api/posts/${postId}/like`);
      },

      repostPost: async (postId: number): Promise<void> => {
        await client.post(`/api/posts/${postId}/repost`);
      },

      deletePost: async (postId: number): Promise<void> => {
        await client.delete(`/api/posts/${postId}`);
      },

      unrepost: async (postId: number): Promise<void> => {
        await client.delete(`/api/posts/${postId}/repost`);
      },

      getUserTimeline: async (userId: number, page: number, limit: number): Promise<TimelineItem[]> => {
        const response = await client.get(`/api/posts/user/${userId}/timeline?page=${page}&pageSize=${limit}`);
        return response.data;
      },

      getComments: async (postId: number): Promise<Comment[]> => {
        const response = await client.get(`/api/posts/${postId}/comments`);
        return response.data;
      },

      addComment: async (postId: number, data: CreateCommentData): Promise<Comment> => {
        const response = await client.post(`/api/posts/${postId}/comments`, data);
        return response.data;
      },

      updateComment: async (commentId: number, data: UpdateCommentData): Promise<Comment> => {
        const response = await client.put(`/api/posts/comments/${commentId}`, data);
        return response.data;
      },

      deleteComment: async (commentId: number): Promise<void> => {
        await client.delete(`/api/posts/comments/${commentId}`);
      },
    },

    users: {
      searchUsers: async (query: string): Promise<User[]> => {
        const response = await client.get(`/api/users/search/${encodeURIComponent(query)}`);
        return response.data;
      },

      getUserProfile: async (username: string): Promise<UserProfile> => {
        const response = await client.get(`/api/users/${username}`);
        return response.data;
      },

      updateProfile: async (data: { bio?: string; pronouns?: string; tagline?: string; birthday?: string }): Promise<User> => {
        const response = await client.put('/api/users/me', data);
        return response.data;
      },

      updateFcmToken: async (data: { token: string }): Promise<{ message: string }> => {
        const response = await client.post('/api/users/me/fcm-token', data);
        return response.data;
      },

      clearFcmToken: async (): Promise<{ message: string }> => {
        const response = await client.delete('/api/users/me/fcm-token');
        return response.data;
      },

      getFollowing: async (): Promise<User[]> => {
        const response = await client.get('/api/users/me/following');
        return response.data;
      },

      getFollowers: async (): Promise<User[]> => {
        const response = await client.get('/api/users/me/followers');
        return response.data;
      },

      getUserFollowing: async (userId: number): Promise<User[]> => {
        const response = await client.get(`/api/users/${userId}/following`);
        return response.data;
      },

      getUserFollowers: async (userId: number): Promise<User[]> => {
        const response = await client.get(`/api/users/${userId}/followers`);
        return response.data;
      },

      follow: async (userId: number): Promise<FollowResponse> => {
        const response = await client.post(`/api/users/${userId}/follow`);
        return response.data;
      },

      unfollow: async (userId: number): Promise<FollowResponse> => {
        const response = await client.delete(`/api/users/${userId}/follow`);
        return response.data;
      },

      uploadProfileImage: async (uri: string, fileName: string, type: string): Promise<User> => {
        const formData = new FormData();
        formData.append('file', {
          uri,
          name: fileName,
          type,
        } as any);

        const response = await client.post('/api/users/me/profile-image', formData, {
          headers: {
            'Content-Type': 'multipart/form-data',
          },
        });
        return response.data;
      },

      blockUser: async (userId: number): Promise<BlockResponse> => {
        const response = await client.post(`/api/blocks/users/${userId}`);
        return response.data;
      },

      unblockUser: async (userId: number): Promise<BlockResponse> => {
        const response = await client.delete(`/api/blocks/users/${userId}`);
        return response.data;
      },

      getBlockStatus: async (userId: number): Promise<BlockStatusResponse> => {
        const response = await client.get(`/api/blocks/users/${userId}/status`);
        return response.data;
      },

      getBlockedUsers: async (): Promise<User[]> => {
        const response = await client.get('/api/blocks/');
        return response.data;
      },
    },

    preferences: {
      get: async (): Promise<{ darkMode: boolean }> => {
        const response = await client.get('/api/preferences');
        return response.data;
      },

      update: async (preferences: { darkMode?: boolean }): Promise<{ darkMode: boolean }> => {
        const response = await client.put('/api/preferences', preferences);
        return response.data;
      },
    },

    messages: {
      getConversations: async (): Promise<ConversationListItem[]> => {
        const response = await client.get('/api/messages/conversations');
        return response.data;
      },

      canMessage: async (userId: number): Promise<CanMessageResponse> => {
        const response = await client.get(`/api/messages/can-message/${userId}`);
        return response.data;
      },

      getOrCreateConversation: async (userId: number): Promise<Conversation> => {
        const response = await client.post(`/api/messages/conversations/with/${userId}`);
        return response.data;
      },

      getMessages: async (conversationId: number, page: number, limit: number): Promise<Message[]> => {
        const response = await client.get(`/api/messages/conversations/${conversationId}/messages?page=${page}&pageSize=${limit}`);
        return response.data;
      },

      sendMessageToConversation: async (data: SendMessageData): Promise<Message> => {
        const response = await client.post('/api/messages/conversation', data);
        return response.data;
      },

      getUnreadCount: async (): Promise<{ unreadCount: number }> => {
        const response = await client.get('/api/messages/unread-count');
        return response.data;
      },

      markConversationAsRead: async (conversationId: number): Promise<void> => {
        await client.post(`/api/messages/conversations/${conversationId}/read`);
      },
    },

    images: {
      uploadImage: async (uri: string, fileName: string, type: string): Promise<ImageUploadResponse> => {
        console.log('Uploading image:', { uri, fileName, type });

        const formData = new FormData();

        // React Native specific FormData format - ensure proper MIME type
        const mimeType = type.startsWith('image/') ? type : `image/${type}`;

        formData.append('file', {
          uri: uri,
          type: mimeType,
          name: fileName,
        } as any);

        console.log('FormData created with MIME type:', mimeType);

        try {
          const response = await client.post('/api/images/upload', formData, {
            headers: {
              'Content-Type': 'multipart/form-data',
            },
          });

          // Check if the response is actually successful (since we don't throw on 4xx)
          if (response.status >= 400) {
            const errorMessage = typeof response.data === 'string' ? response.data : JSON.stringify(response.data);
            console.error('API error response:', errorMessage);
            throw new Error(`Upload failed with status ${response.status}: ${errorMessage}`);
          }

          if (!response.data || !response.data.fileName) {
            throw new Error('Upload succeeded but no file information returned');
          }

          console.log('Image upload successful:', response.data);
          return response.data;
        } catch (error: any) {
          console.error('Image upload error:', error.message);
          throw error;
        }
      },

      deleteImage: async (fileName: string): Promise<void> => {
        await client.delete(`/api/images/${fileName}`);
      },
    },

    notifications: {
      getNotifications: async (page: number = 1, pageSize: number = 25): Promise<NotificationList> => {
        const response = await client.get(`/api/notifications?page=${page}&pageSize=${pageSize}`);
        return response.data;
      },

      getUnreadCount: async (): Promise<UnreadCountResponse> => {
        const response = await client.get('/api/notifications/unread-count');
        return response.data;
      },

      markAsRead: async (notificationId: number): Promise<{ message: string }> => {
        const response = await client.put(`/api/notifications/${notificationId}/read`);
        return response.data;
      },

      markAllAsRead: async (): Promise<{ message: string }> => {
        const response = await client.put('/api/notifications/read-all');
        return response.data;
      },
    },

    appeals: {
      submitAppeal: async (data: CreateAppealDto): Promise<void> => {
        await client.post('/api/appeals', data);
      },
    },
  };
}
