import axios, { AxiosInstance } from 'axios';
import { YapplrApi, LoginData, RegisterData, AuthResponse, RegisterResponse, User, UserProfile, TimelineItem, ConversationListItem, Conversation, CanMessageResponse, Message, SendMessageData, FollowResponse, CreatePostData, CreatePostWithMediaData, CreateRepostData, CreateRepostWithMediaData, Post, ImageUploadResponse, VideoUploadResponse, Comment, CreateCommentData, UpdateCommentData, BlockResponse, BlockStatusResponse, NotificationList, UnreadCountResponse, CreateAppealDto, CreateUserReportDto, UserReport, SystemTag, ContentPageVersion, MultipleFileUploadResponse, UploadLimits, Group, GroupList, GroupMember, CreateGroup, UpdateGroup, PaginatedResult } from '../types';

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
    baseURL: config.baseURL,
    getToken: config.getToken,
    auth: {
      login: async (data: LoginData): Promise<AuthResponse> => {
        const response = await client.post('/api/auth/login', data);
        return response.data;
      },

      register: async (data: RegisterData): Promise<RegisterResponse> => {
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
        const response = await client.get(`/api/posts/timeline?page=${page}&pageSize=${limit}`);

        // Debug logging for video URLs in timeline
        console.log(`🎥 API Timeline: Mobile app connecting to: ${config.baseURL}`);
        response.data.forEach((item: TimelineItem, index: number) => {
          if (item.post.mediaItems && item.post.mediaItems.length > 0) {
            const videoItems = item.post.mediaItems.filter(media => media.mediaType === 1); // Video type
            if (videoItems.length > 0) {
              console.log(`🎥 API Timeline: Post ${item.post.id} video media:`, videoItems.map(v => ({
                id: v.id,
                videoUrl: v.videoUrl,
                videoThumbnailUrl: v.videoThumbnailUrl,
                videoProcessingStatus: v.videoProcessingStatus,
                hasVideoUrl: !!v.videoUrl,
                videoUrlLength: v.videoUrl?.length || 0,
                urlStartsWith: v.videoUrl?.substring(0, 30) || 'N/A'
              })));
            }
          }

          // Also check legacy video fields
          if (item.post.videoUrl) {
            console.log(`🎥 API Timeline: Post ${item.post.id} legacy video:`, {
              videoUrl: item.post.videoUrl,
              videoThumbnailUrl: item.post.videoThumbnailUrl,
              videoProcessingStatus: item.post.videoProcessingStatus,
              hasVideoUrl: !!item.post.videoUrl,
              videoUrlLength: item.post.videoUrl?.length || 0,
              urlStartsWith: item.post.videoUrl?.substring(0, 30) || 'N/A'
            });
          }
        });

        return response.data;
      },

      createPost: async (data: CreatePostData): Promise<Post> => {
        const response = await client.post('/api/posts', data);
        return response.data;
      },

      createPostWithMedia: async (data: CreatePostWithMediaData): Promise<Post> => {
        const response = await client.post('/api/posts/with-media', data);
        return response.data;
      },

      likePost: async (postId: number): Promise<void> => {
        await client.post(`/api/posts/${postId}/like`);
      },

      reactToPost: async (postId: number, reactionType: number): Promise<void> => {
        await client.post(`/api/posts/${postId}/react`, { reactionType });
      },

      removePostReaction: async (postId: number): Promise<void> => {
        await client.delete(`/api/posts/${postId}/react`);
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

      // Enhanced repost functionality (replaces simple repost and quote tweet)
      createRepost: async (data: CreateRepostData): Promise<Post> => {
        const response = await client.post('/api/posts/repost', data);
        return response.data;
      },

      createRepostWithMedia: async (data: CreateRepostWithMediaData): Promise<Post> => {
        const response = await client.post('/api/posts/repost-with-media', data);
        return response.data;
      },

      getReposts: async (postId: number, page: number = 1, pageSize: number = 20): Promise<Post[]> => {
        const response = await client.get(`/api/posts/${postId}/reposts`, {
          params: { page, pageSize }
        });
        return response.data;
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

      likeComment: async (postId: number, commentId: number): Promise<void> => {
        await client.post(`/api/posts/${postId}/comments/${commentId}/like`);
      },

      unlikeComment: async (postId: number, commentId: number): Promise<void> => {
        await client.delete(`/api/posts/${postId}/comments/${commentId}/like`);
      },

      reactToComment: async (postId: number, commentId: number, reactionType: number): Promise<void> => {
        await client.post(`/api/posts/${postId}/comments/${commentId}/react`, { reactionType });
      },

      removeCommentReaction: async (postId: number, commentId: number): Promise<void> => {
        await client.delete(`/api/posts/${postId}/comments/${commentId}/react`);
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

      updateExpoPushToken: async (data: { token: string }): Promise<{ message: string }> => {
        const response = await client.post('/api/users/me/expo-push-token', data);
        return response.data;
      },

      clearExpoPushToken: async (): Promise<{ message: string }> => {
        const response = await client.delete('/api/users/me/expo-push-token');
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

    videos: {
      uploadVideo: async (uri: string, fileName: string, type: string): Promise<VideoUploadResponse> => {
        console.log('Uploading video:', { uri, fileName, type });

        const formData = new FormData();

        // React Native specific FormData format - ensure proper MIME type
        const mimeType = type.startsWith('video/') ? type : `video/${type}`;

        formData.append('file', {
          uri: uri,
          type: mimeType,
          name: fileName,
        } as any);

        console.log('FormData created with MIME type:', mimeType);

        try {
          const response = await client.post('/api/videos/upload', formData, {
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

          console.log('Video upload successful:', response.data);
          return response.data;
        } catch (error: any) {
          console.error('Video upload error:', error.message);
          throw error;
        }
      },

      deleteVideo: async (fileName: string): Promise<void> => {
        await client.delete(`/api/videos/${fileName}`);
      },
    },

    uploads: {
      uploadMultipleFiles: async (files: Array<{ uri: string; fileName: string; type: string }>): Promise<MultipleFileUploadResponse> => {
        console.log('Uploading multiple files:', files.length);

        const formData = new FormData();

        files.forEach((file, index) => {
          const mimeType = file.type;
          formData.append('files', {
            uri: file.uri,
            type: mimeType,
            name: file.fileName,
          } as any);
        });

        try {
          const response = await client.post('/api/uploads/media', formData, {
            headers: {
              'Content-Type': 'multipart/form-data',
            },
          });

          if (response.status >= 400) {
            const errorMessage = typeof response.data === 'string' ? response.data : JSON.stringify(response.data);
            console.error('API error response:', errorMessage);
            throw new Error(`Upload failed with status ${response.status}: ${errorMessage}`);
          }

          console.log('Multiple files upload successful:', response.data);
          return response.data;
        } catch (error: any) {
          console.error('Multiple files upload error:', error.message);
          throw error;
        }
      },

      getUploadLimits: async (): Promise<UploadLimits> => {
        const response = await client.get('/api/uploads/limits');
        return response.data;
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

    tags: {
      getTrendingTags: async (limit: number) => {
        const response = await client.get(`/api/tags/trending?limit=${limit}`);
        return response.data;
      },
    },

    userReports: {
      createReport: async (data: CreateUserReportDto): Promise<UserReport> => {
        const response = await client.post('/api/reports', data);
        return response.data;
      },

      getMyReports: async (page: number = 1, pageSize: number = 25): Promise<UserReport[]> => {
        const response = await client.get(`/api/reports/my-reports?page=${page}&pageSize=${pageSize}`);
        return response.data;
      },

      getSystemTags: async (): Promise<SystemTag[]> => {
        const response = await client.get('/api/admin/system-tags');
        return response.data;
      },
    },

    content: {
      getTermsOfService: async (): Promise<ContentPageVersion> => {
        const response = await client.get('/api/content/terms');
        return response.data;
      },

      getPrivacyPolicy: async (): Promise<ContentPageVersion> => {
        const response = await client.get('/api/content/privacy');
        return response.data;
      },

      getPublishedContentBySlug: async (slug: string): Promise<ContentPageVersion> => {
        const response = await client.get(`/api/content/pages/${slug}`);
        return response.data;
      },
    },

    groups: {
      // Get all groups (paginated)
      getGroups: async (page: number = 1, pageSize: number = 20): Promise<PaginatedResult<GroupList>> => {
        const response = await client.get(`/api/groups?page=${page}&pageSize=${pageSize}`);
        return response.data;
      },

      // Search groups
      searchGroups: async (query: string, page: number = 1, pageSize: number = 20): Promise<PaginatedResult<GroupList>> => {
        const response = await client.get(`/api/groups/search?query=${encodeURIComponent(query)}&page=${page}&pageSize=${pageSize}`);
        return response.data;
      },

      // Get group by ID
      getGroup: async (id: number): Promise<Group> => {
        const response = await client.get(`/api/groups/${id}`);
        return response.data;
      },

      // Get group by name
      getGroupByName: async (name: string): Promise<Group> => {
        const response = await client.get(`/api/groups/name/${encodeURIComponent(name)}`);
        return response.data;
      },

      // Create group
      createGroup: async (data: CreateGroup): Promise<Group> => {
        const response = await client.post('/api/groups', data);
        return response.data;
      },

      // Update group
      updateGroup: async (id: number, data: UpdateGroup): Promise<Group> => {
        const response = await client.put(`/api/groups/${id}`, data);
        return response.data;
      },

      // Delete group
      deleteGroup: async (id: number): Promise<void> => {
        await client.delete(`/api/groups/${id}`);
      },

      // Join group
      joinGroup: async (id: number): Promise<{ message: string }> => {
        const response = await client.post(`/api/groups/${id}/join`);
        return response.data;
      },

      // Leave group
      leaveGroup: async (id: number): Promise<{ message: string }> => {
        const response = await client.post(`/api/groups/${id}/leave`);
        return response.data;
      },

      // Get group members
      getGroupMembers: async (id: number, page: number = 1, pageSize: number = 20): Promise<PaginatedResult<GroupMember>> => {
        const response = await client.get(`/api/groups/${id}/members?page=${page}&pageSize=${pageSize}`);
        return response.data;
      },

      // Get group posts
      getGroupPosts: async (id: number, page: number = 1, pageSize: number = 20): Promise<PaginatedResult<Post>> => {
        const response = await client.get(`/api/groups/${id}/posts?page=${page}&pageSize=${pageSize}`);
        return response.data;
      },

      // Get user's groups
      getUserGroups: async (userId: number, page: number = 1, pageSize: number = 20): Promise<PaginatedResult<GroupList>> => {
        const response = await client.get(`/api/groups/user/${userId}?page=${page}&pageSize=${pageSize}`);
        return response.data;
      },

      // Get current user's groups
      getMyGroups: async (page: number = 1, pageSize: number = 20): Promise<PaginatedResult<GroupList>> => {
        const response = await client.get(`/api/groups/me?page=${page}&pageSize=${pageSize}`);
        return response.data;
      },

      // Upload group image
      uploadGroupImage: async (uri: string, fileName: string, type: string): Promise<{ fileName: string; imageUrl: string }> => {
        const formData = new FormData();
        formData.append('file', {
          uri,
          name: fileName,
          type,
        } as any);
        const response = await client.post('/api/groups/upload-image', formData, {
          headers: {
            'Content-Type': 'multipart/form-data',
          },
        });
        return response.data;
      },
    },
  };
}
