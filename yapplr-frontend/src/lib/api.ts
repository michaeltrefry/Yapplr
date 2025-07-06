import axios from 'axios';
import type {
  AuthResponse,
  LoginData,
  RegisterData,
  Post,
  CreatePostData,
  UpdatePostData,
  Comment,
  CreateCommentData,
  UpdateCommentData,
  User,
  UserWithOnlineStatus,
  UserProfile,
  UpdateUserData,
  FollowResponse,
  FollowRequest,
  TimelineItem,
  BlockResponse,
  BlockStatusResponse,
  Message,
  Conversation,
  ConversationListItem,
  CreateMessageData,
  SendMessageData,
  CanMessageResponse,
  UnreadCountResponse,
  Notification,
  NotificationList,
} from '@/types';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5161';

const api = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add auth token to requests
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Auth API
export const authApi = {
  register: async (data: RegisterData): Promise<AuthResponse> => {
    const response = await api.post('/auth/register', data);
    return response.data;
  },

  login: async (data: LoginData): Promise<AuthResponse> => {
    const response = await api.post('/auth/login', data);
    return response.data;
  },

  forgotPassword: async (email: string): Promise<{ message: string }> => {
    const response = await api.post('/auth/forgot-password', { email });
    return response.data;
  },

  resetPassword: async (token: string, newPassword: string): Promise<{ message: string }> => {
    const response = await api.post('/auth/reset-password', { token, newPassword });
    return response.data;
  },

  verifyEmail: async (token: string): Promise<{ message: string }> => {
    const response = await api.post('/auth/verify-email', { token });
    return response.data;
  },

  resendVerification: async (email: string): Promise<{ message: string }> => {
    const response = await api.post('/auth/resend-verification', { email });
    return response.data;
  },
};

// User API
export const userApi = {
  getCurrentUser: async (): Promise<User> => {
    const response = await api.get('/users/me');
    return response.data;
  },

  updateCurrentUser: async (data: UpdateUserData): Promise<User> => {
    const response = await api.put('/users/me', data);
    return response.data;
  },

  getUserProfile: async (username: string): Promise<UserProfile> => {
    const response = await api.get(`/users/${username}`);
    return response.data;
  },

  searchUsers: async (query: string): Promise<User[]> => {
    const response = await api.get(`/users/search/${query}`);
    return response.data;
  },

  uploadProfileImage: async (file: File): Promise<User> => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post('/users/me/profile-image', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  removeProfileImage: async (): Promise<User> => {
    const response = await api.delete('/users/me/profile-image');
    return response.data;
  },

  followUser: async (userId: number): Promise<FollowResponse> => {
    const response = await api.post(`/users/${userId}/follow`);
    return response.data;
  },

  unfollowUser: async (userId: number): Promise<FollowResponse> => {
    const response = await api.delete(`/users/${userId}/follow`);
    return response.data;
  },

  getFollowing: async (): Promise<User[]> => {
    const response = await api.get('/users/me/following');
    return response.data;
  },

  getFollowingWithOnlineStatus: async (): Promise<UserWithOnlineStatus[]> => {
    const response = await api.get('/users/me/following/online-status');
    return response.data;
  },

  getUserFollowing: async (userId: number): Promise<User[]> => {
    const response = await api.get(`/users/${userId}/following`);
    return response.data;
  },

  getUserFollowers: async (userId: number): Promise<User[]> => {
    const response = await api.get(`/users/${userId}/followers`);
    return response.data;
  },

  updateFcmToken: async (token: string): Promise<void> => {
    await api.post('/users/me/fcm-token', { token });
  },
};

// Post API
export const postApi = {
  createPost: async (data: CreatePostData): Promise<Post> => {
    const response = await api.post('/posts', data);
    return response.data;
  },

  getPost: async (id: number): Promise<Post> => {
    const response = await api.get(`/posts/${id}`);
    return response.data;
  },

  getTimeline: async (page = 1, pageSize = 25): Promise<TimelineItem[]> => {
    const response = await api.get(`/posts/timeline?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  getPublicTimeline: async (page = 1, pageSize = 25): Promise<TimelineItem[]> => {
    const response = await api.get(`/posts/public?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  getUserPosts: async (userId: number, page = 1, pageSize = 25): Promise<Post[]> => {
    const response = await api.get(`/posts/user/${userId}?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  getUserTimeline: async (userId: number, page = 1, pageSize = 25): Promise<TimelineItem[]> => {
    const response = await api.get(`/posts/user/${userId}/timeline?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  updatePost: async (id: number, data: UpdatePostData): Promise<Post> => {
    const response = await api.put(`/posts/${id}`, data);
    return response.data;
  },

  deletePost: async (id: number): Promise<void> => {
    await api.delete(`/posts/${id}`);
  },

  likePost: async (id: number): Promise<void> => {
    await api.post(`/posts/${id}/like`);
  },

  unlikePost: async (id: number): Promise<void> => {
    await api.delete(`/posts/${id}/like`);
  },

  repost: async (id: number): Promise<void> => {
    await api.post(`/posts/${id}/repost`);
  },

  unrepost: async (id: number): Promise<void> => {
    await api.delete(`/posts/${id}/repost`);
  },

  addComment: async (postId: number, data: CreateCommentData): Promise<Comment> => {
    const response = await api.post(`/posts/${postId}/comments`, data);
    return response.data;
  },

  getComments: async (postId: number): Promise<Comment[]> => {
    const response = await api.get(`/posts/${postId}/comments`);
    return response.data;
  },

  updateComment: async (commentId: number, data: UpdateCommentData): Promise<Comment> => {
    const response = await api.put(`/posts/comments/${commentId}`, data);
    return response.data;
  },

  deleteComment: async (commentId: number): Promise<void> => {
    await api.delete(`/posts/comments/${commentId}`);
  },
};

// Image API
export const imageApi = {
  uploadImage: async (file: File): Promise<{ fileName: string; imageUrl: string }> => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await api.post('/images/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  deleteImage: async (fileName: string): Promise<void> => {
    await api.delete(`/images/${fileName}`);
  },
};

// Messaging API
export const messageApi = {
  sendMessage: async (data: CreateMessageData): Promise<Message> => {
    const response = await api.post('/messages', data);
    return response.data;
  },

  sendMessageToConversation: async (data: SendMessageData): Promise<Message> => {
    const response = await api.post('/messages/conversation', data);
    return response.data;
  },

  getConversations: async (page = 1, pageSize = 25): Promise<ConversationListItem[]> => {
    const response = await api.get(`/messages/conversations?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  getConversation: async (conversationId: number): Promise<Conversation> => {
    const response = await api.get(`/messages/conversations/${conversationId}`);
    return response.data;
  },

  getMessages: async (conversationId: number, page = 1, pageSize = 25): Promise<Message[]> => {
    const response = await api.get(`/messages/conversations/${conversationId}/messages?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  markConversationAsRead: async (conversationId: number): Promise<void> => {
    await api.post(`/messages/conversations/${conversationId}/read`);
  },

  canMessage: async (userId: number): Promise<CanMessageResponse> => {
    const response = await api.get(`/messages/can-message/${userId}`);
    return response.data;
  },

  getOrCreateConversation: async (userId: number): Promise<Conversation> => {
    const response = await api.post(`/messages/conversations/with/${userId}`);
    return response.data;
  },

  getUnreadCount: async (): Promise<UnreadCountResponse> => {
    const response = await api.get('/messages/unread-count');
    return response.data;
  },
};

// Block API
export const blockApi = {
  blockUser: async (userId: number): Promise<BlockResponse> => {
    const response = await api.post(`/blocks/users/${userId}`);
    return response.data;
  },

  unblockUser: async (userId: number): Promise<BlockResponse> => {
    const response = await api.delete(`/blocks/users/${userId}`);
    return response.data;
  },

  getBlockStatus: async (userId: number): Promise<BlockStatusResponse> => {
    const response = await api.get(`/blocks/users/${userId}/status`);
    return response.data;
  },

  getBlockedUsers: async (): Promise<User[]> => {
    const response = await api.get('/blocks');
    return response.data;
  },
};

// Preferences API
export const preferencesApi = {
  get: async (): Promise<{ darkMode: boolean; requireFollowApproval: boolean }> => {
    const response = await api.get('/preferences');
    return response.data;
  },

  update: async (preferences: { darkMode?: boolean; requireFollowApproval?: boolean }): Promise<{ darkMode: boolean; requireFollowApproval: boolean }> => {
    const response = await api.put('/preferences', preferences);
    return response.data;
  },
};

// Follow Requests API
export const followRequestsApi = {
  getPending: async (): Promise<FollowRequest[]> => {
    const response = await api.get('/users/me/follow-requests');
    return response.data;
  },

  approve: async (requestId: number): Promise<FollowResponse> => {
    const response = await api.post(`/users/follow-requests/${requestId}/approve`);
    return response.data;
  },

  deny: async (requestId: number): Promise<FollowResponse> => {
    const response = await api.post(`/users/follow-requests/${requestId}/deny`);
    return response.data;
  },

  approveByUserId: async (requesterId: number): Promise<FollowResponse> => {
    const response = await api.post(`/users/follow-requests/approve-by-user/${requesterId}`);
    return response.data;
  },

  denyByUserId: async (requesterId: number): Promise<FollowResponse> => {
    const response = await api.post(`/users/follow-requests/deny-by-user/${requesterId}`);
    return response.data;
  },
};

export const notificationApi = {
  getNotifications: async (page: number = 1, pageSize: number = 25): Promise<NotificationList> => {
    const response = await api.get(`/notifications?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  getUnreadCount: async (): Promise<UnreadCountResponse> => {
    const response = await api.get('/notifications/unread-count');
    return response.data;
  },

  markAsRead: async (notificationId: number): Promise<{ message: string }> => {
    const response = await api.put(`/notifications/${notificationId}/read`);
    return response.data;
  },

  markAllAsRead: async (): Promise<{ message: string }> => {
    const response = await api.put('/notifications/read-all');
    return response.data;
  },

  sendTestNotification: async (): Promise<{ success: boolean; message: string }> => {
    const response = await api.post('/notification-config/test/current-user');
    return response.data;
  },
};

export default api;
