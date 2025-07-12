// Local types for mobile app - copied from yapplr-shared

export enum UserRole {
  User = 0,
  Moderator = 1,
  Admin = 2,
  System = 3
}

export enum UserStatus {
  Active = 0,
  Suspended = 1,
  ShadowBanned = 2,
  Banned = 3
}

export interface User {
  id: number;
  email: string;
  username: string;
  bio: string;
  birthday?: string;
  pronouns: string;
  tagline: string;
  profileImageFileName: string;
  createdAt: string;
  lastSeenAt?: string;
  emailVerified: boolean;
  role?: UserRole;
  status?: UserStatus;
  suspendedUntil?: string;
  suspensionReason?: string;
}

export interface UserProfile {
  id: number;
  username: string;
  bio: string;
  birthday?: string;
  pronouns: string;
  tagline: string;
  profileImageFileName: string;
  createdAt: string;
  postCount: number;
  followerCount: number;
  followingCount: number;
  isFollowedByCurrentUser: boolean;
  hasPendingFollowRequest: boolean;
  requiresFollowApproval: boolean;
}

export interface LoginData {
  email: string;
  password: string;
}

export interface RegisterData {
  email: string;
  password: string;
  username: string;
  acceptTerms: boolean;
  bio?: string;
  birthday?: string;
  pronouns?: string;
  tagline?: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface CreatePostData {
  content: string;
  imageFileName?: string;
  videoFileName?: string;
  privacy?: PostPrivacy;
}

export enum PostPrivacy {
  Public = 0,
  Followers = 1,
  Private = 2,
}

export enum VideoProcessingStatus {
  Pending = 0,
  Processing = 1,
  Completed = 2,
  Failed = 3,
  Cancelled = 4,
}

export interface Tag {
  id: number;
  name: string;
  postCount: number;
}

export enum LinkPreviewStatus {
  Pending = 0,
  Success = 1,
  NotFound = 2,
  Unauthorized = 3,
  Forbidden = 4,
  Timeout = 5,
  NetworkError = 6,
  InvalidUrl = 7,
  TooLarge = 8,
  UnsupportedContent = 9,
  Error = 10
}

export interface LinkPreview {
  id: number;
  url: string;
  title?: string;
  description?: string;
  imageUrl?: string;
  siteName?: string;
  status: LinkPreviewStatus;
  errorMessage?: string;
  createdAt: string;
}

export interface Post {
  id: number;
  content: string;
  createdAt: string;
  updatedAt?: string;
  isEdited: boolean;
  imageUrl?: string;
  videoUrl?: string;
  videoThumbnailUrl?: string;
  videoProcessingStatus?: VideoProcessingStatus;
  privacy: PostPrivacy;
  user: User;
  likeCount: number;
  commentCount: number;
  repostCount: number;
  tags: Tag[];
  linkPreviews: LinkPreview[];
  isLikedByCurrentUser: boolean;
  isRepostedByCurrentUser: boolean;
  moderationInfo?: PostModerationInfo;
}

export interface Comment {
  id: number;
  content: string;
  createdAt: string;
  updatedAt: string;
  user: User;
  isEdited: boolean;
}

export interface CreateCommentData {
  content: string;
}

export interface UpdateCommentData {
  content: string;
}

export interface TimelineItem {
  type: 'post' | 'repost';
  createdAt: string;
  post: Post;
  repostedBy?: User;
}

export interface Message {
  id: number;
  content: string;
  imageUrl?: string;
  createdAt: string;
  updatedAt?: string;
  isEdited: boolean;
  sender: User;
  status?: 'sent' | 'delivered' | 'read';
}

export interface ConversationListItem {
  id: number;
  createdAt: string;
  otherParticipant: User;
  lastMessage?: Message;
  unreadCount: number;
}

export interface Conversation {
  id: number;
  createdAt: string;
  updatedAt: string;
  participants: User[];
  lastMessage?: Message;
  unreadCount: number;
}

export interface CanMessageResponse {
  canMessage: boolean;
  reason?: string;
}

export interface SendMessageData {
  conversationId: number;
  content: string;
  imageFileName?: string;
  videoFileName?: string;
}

export interface FollowResponse {
  isFollowing: boolean;
  followerCount: number;
  hasPendingRequest?: boolean;
}

export interface BlockResponse {
  message: string;
}

export interface BlockStatusResponse {
  isBlocked: boolean;
}

export interface ImageUploadResponse {
  fileName: string;
  imageUrl: string;
}

export interface VideoUploadResponse {
  fileName: string;
  videoUrl: string;
  fileSizeBytes: number;
}

// Moderation and Appeal Types
export interface PostModerationInfo {
  isHidden: boolean;
  hiddenReason?: string;
  hiddenAt?: string;
  hiddenByUser?: User;
  systemTags: PostSystemTag[];
  riskScore?: number;
  riskLevel?: string;
  appealInfo?: PostAppealInfo;
}

export interface PostSystemTag {
  id: number;
  name: string;
  description: string;
  category: string;
  isVisibleToUsers: boolean;
  appliedByUser: User;
  reason?: string;
  appliedAt: string;
}

export interface PostAppealInfo {
  id: number;
  status: AppealStatus;
  reason: string;
  additionalInfo?: string;
  createdAt: string;
  reviewedAt?: string;
  reviewedByUsername?: string;
  reviewNotes?: string;
}

export enum AppealType {
  Suspension = 0,
  Ban = 1,
  ContentRemoval = 2,
  SystemTag = 3,
  Other = 4,
}

export enum AppealStatus {
  Pending = 0,
  UnderReview = 1,
  Approved = 2,
  Denied = 3,
  Escalated = 4,
}

export interface CreateAppealDto {
  type: AppealType;
  reason: string;
  additionalInfo?: string;
  postId?: number;
  commentId?: number;
}

// User Report Types
export enum UserReportStatus {
  Pending = 0,
  Reviewed = 1,
  Dismissed = 2,
  ActionTaken = 3,
}

export interface SystemTag {
  id: number;
  name: string;
  description: string;
  category: number;
  isVisibleToUsers: boolean;
  isActive: boolean;
  color: string;
  icon?: string;
  sortOrder: number;
  createdAt: string;
  updatedAt: string;
}

export interface UserReport {
  id: number;
  reportedByUsername: string;
  status: UserReportStatus;
  reason: string;
  createdAt: string;
  reviewedAt?: string;
  reviewedByUsername?: string;
  reviewNotes?: string;
  post?: Post;
  comment?: Comment;
  systemTags: SystemTag[];
}

export interface CreateUserReportDto {
  postId?: number;
  commentId?: number;
  reason: string;
  systemTagIds: number[];
}

// Notification Types
export enum NotificationType {
  Mention = 1,
  Like = 2,
  Repost = 3,
  Follow = 4,
  Comment = 5,
  FollowRequest = 6,

  // Moderation notifications
  UserSuspended = 100,
  UserBanned = 101,
  UserUnsuspended = 102,
  UserUnbanned = 103,
  ContentHidden = 104,
  ContentDeleted = 105,
  ContentRestored = 106,
  AppealApproved = 107,
  AppealDenied = 108,
  SystemMessage = 109,

  // Video processing notifications
  VideoProcessingCompleted = 110,
}

export interface Notification {
  id: number;
  type: NotificationType;
  message: string;
  isRead: boolean;
  createdAt: string;
  readAt?: string;
  status?: string;
  actorUser?: User;
  post?: Post;
  comment?: Comment;
  mention?: {
    id: number;
    mentioningUserId: number;
    postId?: number;
    commentId?: number;
  };
}

export interface NotificationList {
  notifications: Notification[];
  totalCount: number;
  unreadCount: number;
  hasMore: boolean;
}

export interface UnreadCountResponse {
  unreadCount: number;
}

// Content Management Types
export enum ContentPageType {
  TermsOfService = 0,
  PrivacyPolicy = 1,
  CommunityGuidelines = 2,
  AboutUs = 3,
  Help = 4,
}

export interface ContentPageVersion {
  id: number;
  contentPageId: number;
  content: string;
  changeNotes?: string;
  versionNumber: number;
  isPublished: boolean;
  publishedAt?: string;
  publishedByUsername?: string;
  createdByUsername: string;
  createdAt: string;
}

// API Client interface
export interface YapplrApi {
  baseURL: string;
  getToken: () => string | null;
  auth: {
    login: (data: LoginData) => Promise<AuthResponse>;
    register: (data: RegisterData) => Promise<AuthResponse>;
    getCurrentUser: () => Promise<User>;
    forgotPassword: (email: string) => Promise<{ message: string }>;
    resetPassword: (token: string, newPassword: string) => Promise<{ message: string }>;
    verifyEmail: (token: string) => Promise<{ message: string }>;
    resendVerification: (email: string) => Promise<{ message: string }>;
  };
  posts: {
    getTimeline: (page: number, limit: number) => Promise<TimelineItem[]>;
    createPost: (data: CreatePostData) => Promise<Post>;
    likePost: (postId: number) => Promise<void>;
    repostPost: (postId: number) => Promise<void>;
    deletePost: (postId: number) => Promise<void>;
    unrepost: (postId: number) => Promise<void>;
    getUserTimeline: (userId: number, page: number, limit: number) => Promise<TimelineItem[]>;
    getComments: (postId: number) => Promise<Comment[]>;
    addComment: (postId: number, data: CreateCommentData) => Promise<Comment>;
    updateComment: (commentId: number, data: UpdateCommentData) => Promise<Comment>;
    deleteComment: (commentId: number) => Promise<void>;
  };
  users: {
    searchUsers: (query: string) => Promise<User[]>;
    getUserProfile: (username: string) => Promise<UserProfile>;
    updateProfile: (data: { bio?: string; pronouns?: string; tagline?: string; birthday?: string }) => Promise<User>;
    getFollowing: () => Promise<User[]>;
    getFollowers: () => Promise<User[]>;
    getUserFollowing: (userId: number) => Promise<User[]>;
    getUserFollowers: (userId: number) => Promise<User[]>;
    follow: (userId: number) => Promise<FollowResponse>;
    unfollow: (userId: number) => Promise<FollowResponse>;
    uploadProfileImage: (uri: string, fileName: string, type: string) => Promise<User>;
    blockUser: (userId: number) => Promise<BlockResponse>;
    unblockUser: (userId: number) => Promise<BlockResponse>;
    getBlockStatus: (userId: number) => Promise<BlockStatusResponse>;
    getBlockedUsers: () => Promise<User[]>;
    updateFcmToken: (data: { token: string }) => Promise<{ message: string }>;
    clearFcmToken: () => Promise<{ message: string }>;
    updateExpoPushToken: (data: { token: string }) => Promise<{ message: string }>;
    clearExpoPushToken: () => Promise<{ message: string }>;
  };
  messages: {
    getConversations: () => Promise<ConversationListItem[]>;
    canMessage: (userId: number) => Promise<CanMessageResponse>;
    getOrCreateConversation: (userId: number) => Promise<Conversation>;
    getMessages: (conversationId: number, page: number, limit: number) => Promise<Message[]>;
    sendMessageToConversation: (data: SendMessageData) => Promise<Message>;
    getUnreadCount: () => Promise<{ unreadCount: number }>;
    markConversationAsRead: (conversationId: number) => Promise<void>;
  };
  images: {
    uploadImage: (uri: string, fileName: string, type: string) => Promise<ImageUploadResponse>;
    deleteImage: (fileName: string) => Promise<void>;
  };
  videos: {
    uploadVideo: (uri: string, fileName: string, type: string) => Promise<VideoUploadResponse>;
    deleteVideo: (fileName: string) => Promise<void>;
  };
  preferences: {
    get: () => Promise<{ darkMode: boolean }>;
    update: (preferences: { darkMode?: boolean }) => Promise<{ darkMode: boolean }>;
  };
  notifications: {
    getNotifications: (page: number, pageSize: number) => Promise<NotificationList>;
    getUnreadCount: () => Promise<UnreadCountResponse>;
    markAsRead: (notificationId: number) => Promise<{ message: string }>;
    markAllAsRead: () => Promise<{ message: string }>;
  };
  appeals: {
    submitAppeal: (data: CreateAppealDto) => Promise<void>;
  };
  tags: {
    getTrendingTags: (limit: number) => Promise<Tag[]>;
  };
  userReports: {
    createReport: (data: CreateUserReportDto) => Promise<UserReport>;
    getMyReports: (page: number, pageSize: number) => Promise<UserReport[]>;
    getSystemTags: () => Promise<SystemTag[]>;
  };
  content: {
    getTermsOfService: () => Promise<ContentPageVersion>;
    getPrivacyPolicy: () => Promise<ContentPageVersion>;
    getPublishedContentBySlug: (slug: string) => Promise<ContentPageVersion>;
  };
}
