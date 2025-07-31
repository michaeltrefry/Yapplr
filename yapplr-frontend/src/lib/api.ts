import axios from 'axios';
import type {
  AuthResponse,
  RegisterResponse,
  LoginData,
  RegisterData,
  Post,
  CreatePostData,
  CreatePostWithMediaData,
  UpdatePostData,
  CreateRepostData,
  CreateRepostWithMediaData,

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
  NotificationList,
  Tag,
  SystemTag,
  AiSuggestedTag,
  CreateSystemTagDto,
  UpdateSystemTagDto,
  AdminUser,
  AdminUserDetails,
  UpdateUserRateLimitSettingsDto,
  AdminPost,
  AdminComment,
  AuditLog,
  UserAppeal,
  ModerationStats,
  ContentQueue,
  SuspendUserDto,
  BanUserDto,
  ChangeUserRoleDto,
  HideContentDto,
  ApplySystemTagDto,
  ReviewAppealDto,
  CreateAppealDto,
  MultipleFileUploadResponse,
  UploadLimits,
  SystemTagCategory,
  UserRole,
  UserStatus,
  AuditAction,
  AppealStatus,
  AppealType,
  UserGrowthStats,
  ContentStats,
  ModerationTrends,
  SystemHealth,
  TopModerators,
  ContentTrends,
  UserEngagementStats,
  UserReport,
  CreateUserReportDto,
  ReviewUserReportDto,
  HideContentFromReportDto,
  ContentPage,
  ContentPageVersion,
  CreateContentPageVersionDto,
  Group,
  GroupList,
  GroupMember,
  CreateGroup,
  UpdateGroup,
  PaginatedResult,
  SubscriptionTier,
  CreateSubscriptionTierDto,
  UpdateSubscriptionTierDto,
  UserSubscription,
  AssignSubscriptionTierDto,
} from '@/types';

import { getApiBaseUrl } from './config';

const API_BASE_URL = getApiBaseUrl();

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

// Handle auth errors and redirect to login
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Token is expired or invalid, clear it and redirect to login
      localStorage.removeItem('token');

      // Only redirect if we're in a browser environment
      if (typeof window !== 'undefined') {
        const currentPath = window.location.pathname + window.location.search;

        // Don't redirect from certain pages that should be accessible without auth
        const noRedirectPaths = ['/login', '/register', '/verify-email', '/forgot-password', '/reset-password', '/resend-verification'];
        const shouldNotRedirect = noRedirectPaths.some(path => window.location.pathname.startsWith(path));

        if (!shouldNotRedirect) {
          window.location.href = `/login?redirect=${encodeURIComponent(currentPath)}`;
        }
      }
    }
    return Promise.reject(error);
  }
);

// Auth API
export const authApi = {
  register: async (data: RegisterData): Promise<RegisterResponse> => {
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

  getTopFollowingWithOnlineStatus: async (limit: number = 10): Promise<UserWithOnlineStatus[]> => {
    const response = await api.get(`/users/me/following/top?limit=${limit}`);
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

  createPostWithMedia: async (data: CreatePostWithMediaData): Promise<Post> => {
    const response = await api.post('/posts/with-media', data);
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

  getUserPhotos: async (userId: number, page = 1, pageSize = 25): Promise<Post[]> => {
    const response = await api.get(`/posts/user/${userId}/photos?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  getUserVideos: async (userId: number, page = 1, pageSize = 25): Promise<Post[]> => {
    const response = await api.get(`/posts/user/${userId}/videos?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  updatePost: async (id: number, data: UpdatePostData): Promise<Post> => {
    const response = await api.put(`/posts/${id}`, data);
    return response.data;
  },

  deletePost: async (id: number): Promise<void> => {
    await api.delete(`/posts/${id}`);
  },



  reactToPost: async (id: number, reactionType: number): Promise<void> => {
    await api.post(`/posts/${id}/react`, { reactionType });
  },

  removePostReaction: async (id: number): Promise<void> => {
    await api.delete(`/posts/${id}/react`);
  },

  reactToComment: async (postId: number, commentId: number, reactionType: number): Promise<void> => {
    await api.post(`/posts/${postId}/comments/${commentId}/react`, { reactionType });
  },

  removeCommentReaction: async (postId: number, commentId: number): Promise<void> => {
    await api.delete(`/posts/${postId}/comments/${commentId}/react`);
  },

  // Enhanced repost functionality (replaces simple repost and quote tweet)
  createRepost: async (data: CreateRepostData): Promise<Post> => {
    const response = await api.post('/posts/repost', data);
    return response.data;
  },

  createRepostWithMedia: async (data: CreateRepostWithMediaData): Promise<Post> => {
    const response = await api.post('/posts/repost-with-media', data);
    return response.data;
  },

  getReposts: async (postId: number, page: number = 1, pageSize: number = 20): Promise<Post[]> => {
    const response = await api.get(`/posts/${postId}/reposts`, {
      params: { page, pageSize }
    });
    return response.data;
  },

  // Legacy simple repost methods (for backward compatibility)
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

// Video API
export const videoApi = {
  uploadVideo: async (file: File): Promise<{ fileName: string; videoUrl: string; fileSizeBytes: number }> => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await api.post('/videos/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  deleteVideo: async (fileName: string): Promise<void> => {
    await api.delete(`/videos/${fileName}`);
  },
};

// Multiple File Upload API
export const multipleUploadApi = {
  uploadMultipleFiles: async (files: File[]): Promise<MultipleFileUploadResponse> => {
    const formData = new FormData();
    files.forEach(file => {
      formData.append('files', file);
    });

    const response = await api.post('/uploads/media', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  getUploadLimits: async (): Promise<UploadLimits> => {
    const response = await api.get('/uploads/limits');
    return response.data;
  },

  // Get current upload settings for validation
  getMaxVideoSize: async (): Promise<{ maxVideoSizeBytes: number }> => {
    const response = await api.get('/admin/upload-settings/max-video-size');
    return response.data;
  },

  getMaxImageSize: async (): Promise<{ maxImageSizeBytes: number }> => {
    const response = await api.get('/admin/upload-settings/max-image-size');
    return response.data;
  },

  getAllowedExtensions: async (): Promise<{ allowedImageExtensions: string[], allowedVideoExtensions: string[] }> => {
    const response = await api.get('/admin/upload-settings/allowed-extensions');
    return response.data;
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

// Subscription API
export const subscriptionApi = {
  getActiveSubscriptionTiers: async (): Promise<SubscriptionTier[]> => {
    const response = await api.get('/subscriptions/tiers');
    return response.data;
  },

  getMySubscription: async (): Promise<UserSubscription> => {
    const response = await api.get('/subscriptions/my-subscription');
    return response.data;
  },

  assignSubscriptionTier: async (subscriptionTierId: number): Promise<void> => {
    await api.post('/subscriptions/assign-tier', { subscriptionTierId });
  },

  removeSubscription: async (): Promise<void> => {
    await api.delete('/subscriptions/remove-subscription');
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

  markAsSeen: async (notificationId: number): Promise<{ message: string }> => {
    const response = await api.put(`/notifications/${notificationId}/seen`);
    return response.data;
  },

  markMultipleAsSeen: async (notificationIds: number[]): Promise<{ message: string }> => {
    const response = await api.put('/notifications/seen', notificationIds);
    return response.data;
  },

  markAllAsSeen: async (): Promise<{ message: string }> => {
    const response = await api.put('/notifications/seen-all');
    return response.data;
  },

  sendTestNotification: async (): Promise<{ success: boolean; message: string }> => {
    const response = await api.post('/notification-config/test/current-user');
    return response.data;
  },
};

// Tag API
export const tagApi = {
  searchTags: async (query: string, limit = 20): Promise<Tag[]> => {
    const response = await api.get(`/tags/search/${encodeURIComponent(query)}?limit=${limit}`);
    return response.data;
  },

  getTrendingTags: async (limit = 10): Promise<Tag[]> => {
    const response = await api.get(`/tags/trending?limit=${limit}`);
    return response.data;
  },

  getTag: async (tagName: string): Promise<Tag> => {
    const response = await api.get(`/tags/tag/${encodeURIComponent(tagName)}`);
    return response.data;
  },

  getPostsByTag: async (tagName: string, page = 1, pageSize = 25): Promise<Post[]> => {
    const response = await api.get(`/tags/tag/${encodeURIComponent(tagName)}/posts?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },
};

// User Report API
export const userReportApi = {
  createReport: async (data: CreateUserReportDto): Promise<UserReport> => {
    const response = await api.post('/reports', data);
    return response.data;
  },

  getMyReports: async (page: number = 1, pageSize: number = 25): Promise<UserReport[]> => {
    const response = await api.get(`/reports/my-reports?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },
};

// Group API
export const groupApi = {
  // Get all groups (paginated)
  getGroups: async (page = 1, pageSize = 20): Promise<PaginatedResult<GroupList>> => {
    const response = await api.get(`/groups?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  // Search groups
  searchGroups: async (query: string, page = 1, pageSize = 20): Promise<PaginatedResult<GroupList>> => {
    const response = await api.get(`/groups/search?query=${encodeURIComponent(query)}&page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  // Get group by ID
  getGroup: async (id: number): Promise<Group> => {
    const response = await api.get(`/groups/${id}`);
    return response.data;
  },

  // Get group by name
  getGroupByName: async (name: string): Promise<Group> => {
    const response = await api.get(`/groups/name/${encodeURIComponent(name)}`);
    return response.data;
  },

  // Create group
  createGroup: async (data: CreateGroup): Promise<Group> => {
    const response = await api.post('/groups', data);
    return response.data;
  },

  // Update group
  updateGroup: async (id: number, data: UpdateGroup): Promise<Group> => {
    const response = await api.put(`/groups/${id}`, data);
    return response.data;
  },

  // Delete group
  deleteGroup: async (id: number): Promise<void> => {
    await api.delete(`/groups/${id}`);
  },

  // Join group
  joinGroup: async (id: number): Promise<{ message: string }> => {
    const response = await api.post(`/groups/${id}/join`);
    return response.data;
  },

  // Leave group
  leaveGroup: async (id: number): Promise<{ message: string }> => {
    const response = await api.post(`/groups/${id}/leave`);
    return response.data;
  },

  // Get group members
  getGroupMembers: async (id: number, page = 1, pageSize = 20): Promise<PaginatedResult<GroupMember>> => {
    const response = await api.get(`/groups/${id}/members?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  // Get specific group post
  getGroupPost: async (groupId: number, postId: number): Promise<Post> => {
    const response = await api.get(`/groups/${groupId}/posts/${postId}`);
    return response.data;
  },

  // Get group posts
  getGroupPosts: async (id: number, page = 1, pageSize = 20): Promise<PaginatedResult<Post>> => {
    const response = await api.get(`/groups/${id}/posts?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  // Get user's groups
  getUserGroups: async (userId: number, page = 1, pageSize = 20): Promise<PaginatedResult<GroupList>> => {
    const response = await api.get(`/groups/user/${userId}?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  // Get current user's groups
  getMyGroups: async (page = 1, pageSize = 20): Promise<PaginatedResult<GroupList>> => {
    const response = await api.get(`/groups/me?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  // Upload group image
  uploadGroupImage: async (file: File): Promise<{ fileName: string; imageUrl: string }> => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post('/images/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },
};

// Admin API
export const adminApi = {
  // System Tags
  getSystemTags: async (category?: SystemTagCategory, isActive?: boolean): Promise<SystemTag[]> => {
    const params = new URLSearchParams();
    if (category !== undefined) params.append('category', category.toString());
    if (isActive !== undefined) params.append('isActive', isActive.toString());

    const response = await api.get(`/admin/system-tags?${params.toString()}`);
    return response.data;
  },

  getSystemTag: async (id: number): Promise<SystemTag> => {
    const response = await api.get(`/admin/system-tags/${id}`);
    return response.data;
  },

  createSystemTag: async (data: CreateSystemTagDto): Promise<SystemTag> => {
    const response = await api.post('/admin/system-tags', data);
    return response.data;
  },

  updateSystemTag: async (id: number, data: UpdateSystemTagDto): Promise<SystemTag> => {
    const response = await api.put(`/admin/system-tags/${id}`, data);
    return response.data;
  },

  deleteSystemTag: async (id: number): Promise<void> => {
    await api.delete(`/admin/system-tags/${id}`);
  },

  // User Management
  getUsers: async (page = 1, pageSize = 25, status?: UserStatus, role?: UserRole): Promise<AdminUser[]> => {
    const params = new URLSearchParams();
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());
    if (status !== undefined) params.append('status', status.toString());
    if (role !== undefined) params.append('role', role.toString());

    const response = await api.get(`/admin/users?${params.toString()}`);
    return response.data;
  },

  getUser: async (id: number): Promise<AdminUser> => {
    const response = await api.get(`/admin/users/${id}`);
    return response.data;
  },

  getUserDetails: async (id: number): Promise<AdminUserDetails> => {
    const response = await api.get(`/admin/users/${id}`);
    return response.data;
  },

  suspendUser: async (id: number, data: SuspendUserDto): Promise<void> => {
    await api.post(`/admin/users/${id}/suspend`, data);
  },

  unsuspendUser: async (id: number): Promise<void> => {
    await api.post(`/admin/users/${id}/unsuspend`);
  },

  banUser: async (id: number, data: BanUserDto): Promise<void> => {
    await api.post(`/admin/users/${id}/ban`, data);
  },

  unbanUser: async (id: number): Promise<void> => {
    await api.post(`/admin/users/${id}/unban`);
  },

  changeUserRole: async (id: number, data: ChangeUserRoleDto): Promise<void> => {
    await api.post(`/admin/users/${id}/change-role`, data);
  },

  updateUserRateLimitSettings: async (id: number, data: UpdateUserRateLimitSettingsDto): Promise<void> => {
    await api.put(`/admin/users/${id}/rate-limiting`, data);
  },

  // Content Moderation
  getPosts: async (page = 1, pageSize = 25, isHidden?: boolean): Promise<AdminPost[]> => {
    const params = new URLSearchParams();
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());
    if (isHidden !== undefined) params.append('isHidden', isHidden.toString());

    const response = await api.get(`/admin/posts?${params.toString()}`);
    return response.data;
  },

  getPost: async (id: number): Promise<AdminPost> => {
    const response = await api.get(`/admin/posts/${id}`);
    return response.data;
  },

  hidePost: async (id: number, data: HideContentDto): Promise<void> => {
    await api.post(`/admin/posts/${id}/hide`, data);
  },

  unhidePost: async (id: number): Promise<void> => {
    await api.post(`/admin/posts/${id}/unhide`);
  },



  applySystemTagToPost: async (id: number, data: ApplySystemTagDto): Promise<void> => {
    await api.post(`/admin/posts/${id}/system-tags`, data);
  },

  removeSystemTagFromPost: async (id: number, tagId: number): Promise<void> => {
    await api.delete(`/admin/posts/${id}/system-tags/${tagId}`);
  },

  // Comment Moderation
  getComments: async (page = 1, pageSize = 25, isHidden?: boolean): Promise<AdminComment[]> => {
    const params = new URLSearchParams();
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());
    if (isHidden !== undefined) params.append('isHidden', isHidden.toString());

    const response = await api.get(`/admin/comments?${params.toString()}`);
    return response.data;
  },

  getComment: async (id: number): Promise<AdminComment> => {
    const response = await api.get(`/admin/comments/${id}`);
    return response.data;
  },

  hideComment: async (id: number, data: HideContentDto): Promise<void> => {
    await api.post(`/admin/comments/${id}/hide`, data);
  },

  unhideComment: async (id: number): Promise<void> => {
    await api.post(`/admin/comments/${id}/unhide`);
  },

  applySystemTagToComment: async (id: number, data: ApplySystemTagDto): Promise<void> => {
    await api.post(`/admin/comments/${id}/system-tags`, data);
  },



  // Analytics and Reporting
  getStats: async (): Promise<ModerationStats> => {
    const response = await api.get('/admin/stats');
    return response.data;
  },

  getContentQueue: async (): Promise<ContentQueue> => {
    const response = await api.get('/admin/queue');
    return response.data;
  },

  getAuditLogs: async (page = 1, pageSize = 25, action?: AuditAction, performedByUserId?: number, targetUserId?: number): Promise<AuditLog[]> => {
    const params = new URLSearchParams();
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());
    if (action !== undefined) params.append('action', action.toString());
    if (performedByUserId !== undefined) params.append('performedByUserId', performedByUserId.toString());
    if (targetUserId !== undefined) params.append('targetUserId', targetUserId.toString());

    const response = await api.get(`/admin/audit-logs?${params.toString()}`);
    return response.data;
  },

  // User Appeals
  getAppeals: async (page = 1, pageSize = 25, status?: AppealStatus, type?: AppealType, userId?: number): Promise<UserAppeal[]> => {
    const params = new URLSearchParams();
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());
    if (status !== undefined) params.append('status', status.toString());
    if (type !== undefined) params.append('type', type.toString());
    if (userId !== undefined) params.append('userId', userId.toString());

    const response = await api.get(`/admin/appeals?${params.toString()}`);
    return response.data;
  },

  getAppeal: async (id: number): Promise<UserAppeal> => {
    const response = await api.get(`/admin/appeals/${id}`);
    return response.data;
  },

  reviewAppeal: async (id: number, data: ReviewAppealDto): Promise<UserAppeal> => {
    const response = await api.post(`/admin/appeals/${id}/review`, data);
    return response.data;
  },

  createUserAppeal: async (data: CreateAppealDto): Promise<UserAppeal> => {
    const response = await api.post('/admin/appeals', data);
    return response.data;
  },

  // User Reports
  getAllUserReports: async (page: number = 1, pageSize: number = 50): Promise<UserReport[]> => {
    const response = await api.get(`/admin/reports?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  getUserReport: async (id: number): Promise<UserReport> => {
    const response = await api.get(`/admin/reports/${id}`);
    return response.data;
  },

  reviewUserReport: async (id: number, data: ReviewUserReportDto): Promise<UserReport> => {
    const response = await api.post(`/admin/reports/${id}/review`, data);
    return response.data;
  },

  hideContentFromReport: async (id: number, data: HideContentFromReportDto): Promise<void> => {
    await api.post(`/admin/reports/${id}/hide-content`, data);
  },

  // Enhanced Analytics
  getUserGrowthStats: async (days: number = 30): Promise<UserGrowthStats> => {
    const response = await api.get(`/admin/analytics/user-growth?days=${days}`);
    return response.data;
  },

  getContentStats: async (days: number = 30): Promise<ContentStats> => {
    const response = await api.get(`/admin/analytics/content-stats?days=${days}`);
    return response.data;
  },

  getModerationTrends: async (days: number = 30): Promise<ModerationTrends> => {
    const response = await api.get(`/admin/analytics/moderation-trends?days=${days}`);
    return response.data;
  },

  getSystemHealth: async (): Promise<SystemHealth> => {
    const response = await api.get('/admin/analytics/system-health');
    return response.data;
  },

  getTopModerators: async (days: number = 30, limit: number = 10): Promise<TopModerators> => {
    const response = await api.get(`/admin/analytics/top-moderators?days=${days}&limit=${limit}`);
    return response.data;
  },

  getContentTrends: async (days: number = 30): Promise<ContentTrends> => {
    const response = await api.get(`/admin/analytics/content-trends?days=${days}`);
    return response.data;
  },

  getUserEngagementStats: async (days: number = 30): Promise<UserEngagementStats> => {
    const response = await api.get(`/admin/analytics/user-engagement?days=${days}`);
    return response.data;
  },

  // Bulk Actions
  bulkHidePosts: async (postIds: number[], reason: string): Promise<{ count: number }> => {
    const response = await api.post('/admin/bulk/hide-posts', { postIds, reason });
    return response.data;
  },



  bulkApplySystemTag: async (postIds: number[], systemTagId: number, reason?: string): Promise<{ count: number }> => {
    const response = await api.post('/admin/bulk/apply-system-tag', { postIds, systemTagId, reason });
    return response.data;
  },

  // AI Suggested Tags
  getPendingAiSuggestions: async (postId?: number, commentId?: number, page = 1, pageSize = 25): Promise<AiSuggestedTag[]> => {
    const params = new URLSearchParams();
    if (postId !== undefined) params.append('postId', postId.toString());
    if (commentId !== undefined) params.append('commentId', commentId.toString());
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());

    const response = await api.get(`/admin/ai-suggestions?${params}`);
    return response.data;
  },

  approveAiSuggestedTag: async (tagId: number, reason?: string): Promise<void> => {
    await api.post(`/admin/ai-suggestions/${tagId}/approve`, { reason });
  },

  rejectAiSuggestedTag: async (tagId: number, reason?: string): Promise<void> => {
    await api.post(`/admin/ai-suggestions/${tagId}/reject`, { reason });
  },

  bulkApproveAiSuggestedTags: async (tagIds: number[], reason?: string): Promise<void> => {
    await api.post('/admin/ai-suggestions/bulk-approve', { suggestedTagIds: tagIds, reason });
  },

  bulkRejectAiSuggestedTags: async (tagIds: number[], reason?: string): Promise<void> => {
    await api.post('/admin/ai-suggestions/bulk-reject', { suggestedTagIds: tagIds, reason });
  },

  // Content Management
  getContentPages: async (): Promise<ContentPage[]> => {
    const response = await api.get('/admin/content-pages');
    return response.data;
  },

  getContentPage: async (id: number): Promise<ContentPage> => {
    const response = await api.get(`/admin/content-pages/${id}`);
    return response.data;
  },

  getContentPageVersions: async (contentPageId: number): Promise<ContentPageVersion[]> => {
    const response = await api.get(`/admin/content-pages/${contentPageId}/versions`);
    return response.data;
  },

  getContentPageVersion: async (versionId: number): Promise<ContentPageVersion> => {
    const response = await api.get(`/admin/content-pages/versions/${versionId}`);
    return response.data;
  },

  createContentPageVersion: async (contentPageId: number, data: CreateContentPageVersionDto): Promise<ContentPageVersion> => {
    const response = await api.post(`/admin/content-pages/${contentPageId}/versions`, data);
    return response.data;
  },

  publishContentPageVersion: async (contentPageId: number, versionId: number): Promise<void> => {
    await api.post(`/admin/content-pages/${contentPageId}/publish`, { versionId });
  },

  // Subscription Management
  getSubscriptionTiers: async (includeInactive = false): Promise<SubscriptionTier[]> => {
    const params = new URLSearchParams();
    if (includeInactive) params.append('includeInactive', 'true');

    const response = await api.get(`/admin/subscriptions/tiers?${params.toString()}`);
    return response.data;
  },

  createSubscriptionTier: async (data: CreateSubscriptionTierDto): Promise<SubscriptionTier> => {
    const response = await api.post('/admin/subscriptions/tiers', data);
    return response.data;
  },

  updateSubscriptionTier: async (id: number, data: UpdateSubscriptionTierDto): Promise<SubscriptionTier> => {
    const response = await api.put(`/admin/subscriptions/tiers/${id}`, data);
    return response.data;
  },

  deleteSubscriptionTier: async (id: number): Promise<void> => {
    await api.delete(`/admin/subscriptions/tiers/${id}`);
  },

  getUserSubscription: async (userId: number): Promise<UserSubscription> => {
    const response = await api.get(`/admin/subscriptions/users/${userId}/subscription`);
    return response.data;
  },

  assignSubscriptionTier: async (userId: number, data: AssignSubscriptionTierDto): Promise<void> => {
    await api.post(`/admin/subscriptions/users/${userId}/assign-tier`, data);
  },

  getSubscriptionTierUserCount: async (tierId: number): Promise<{ tierId: number; userCount: number }> => {
    const response = await api.get(`/admin/subscriptions/tiers/${tierId}/users/count`);
    return response.data;
  },
};

// Public Content API
export const contentApi = {
  getTermsOfService: async (): Promise<ContentPageVersion> => {
    const response = await api.get('/content/terms');
    return response.data;
  },

  getPrivacyPolicy: async (): Promise<ContentPageVersion> => {
    const response = await api.get('/content/privacy');
    return response.data;
  },

  getPublishedContentBySlug: async (slug: string): Promise<ContentPageVersion> => {
    const response = await api.get(`/content/pages/${slug}`);
    return response.data;
  },
};

export default api;
