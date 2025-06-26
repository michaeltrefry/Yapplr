import axios, { AxiosInstance } from 'axios';
import { YapplrApi, LoginData, RegisterData, AuthResponse, User, UserProfile, TimelineItem, ConversationListItem, CreatePostData, Post, ImageUploadResponse } from '../types';

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

      getUserTimeline: async (userId: number, page: number, limit: number): Promise<TimelineItem[]> => {
        const response = await client.get(`/api/posts/user/${userId}/timeline?page=${page}&pageSize=${limit}`);
        return response.data;
      },
    },

    users: {
      searchUsers: async (query: string): Promise<User[]> => {
        const response = await client.get(`/api/users/search?q=${encodeURIComponent(query)}`);
        return response.data;
      },

      getUserProfile: async (username: string): Promise<UserProfile> => {
        const response = await client.get(`/api/users/${username}`);
        return response.data;
      },
    },

    messages: {
      getConversations: async (): Promise<ConversationListItem[]> => {
        const response = await client.get('/api/messages/conversations');
        return response.data;
      },
    },

    images: {
      uploadImage: async (uri: string, fileName: string, type: string): Promise<ImageUploadResponse> => {
        const formData = new FormData();
        formData.append('file', {
          uri,
          name: fileName,
          type,
        } as any);

        const response = await client.post('/api/images/upload', formData, {
          headers: {
            'Content-Type': 'multipart/form-data',
          },
        });
        return response.data;
      },

      deleteImage: async (fileName: string): Promise<void> => {
        await client.delete(`/api/images/${fileName}`);
      },
    },
  };
}
