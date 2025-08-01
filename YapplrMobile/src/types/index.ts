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
  profileImageUrl?: string;
  createdAt: string;
  lastSeenAt?: string;
  emailVerified: boolean;
  role?: UserRole;
  status?: UserStatus;
  suspendedUntil?: string;
  suspensionReason?: string;
  subscriptionTier?: SubscriptionTier;
}

export interface UserProfile {
  id: number;
  username: string;
  bio: string;
  birthday?: string;
  pronouns: string;
  tagline: string;
  profileImageUrl?: string;
  createdAt: string;
  postCount: number;
  followerCount: number;
  followingCount: number;
  isFollowedByCurrentUser: boolean;
  hasPendingFollowRequest: boolean;
  requiresFollowApproval: boolean;
  subscriptionTier?: SubscriptionTier;
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

export interface RegisterResponse {
  message: string;
  user: User;
}

// Subscription types
export interface SubscriptionTier {
  id: number;
  name: string;
  description: string;
  price: number;
  currency: string;
  billingCycleMonths: number;
  isActive: boolean;
  isDefault: boolean;
  sortOrder: number;
  showAdvertisements: boolean;
  hasVerifiedBadge: boolean;
  features?: string;
  createdAt: string;
  updatedAt: string;
}

export interface UserSubscription {
  userId: number;
  username: string;
  subscriptionTier?: SubscriptionTier;
}

export enum MediaType {
  Image = 0,
  Video = 1,
  Gif = 2,
}

export interface PostMedia {
  id: number;
  mediaType: MediaType;
  imageUrl?: string;
  videoUrl?: string;
  videoThumbnailUrl?: string;
  videoProcessingStatus?: VideoProcessingStatus;
  gifUrl?: string;
  gifPreviewUrl?: string;
  width?: number;
  height?: number;
  duration?: string; // ISO 8601 duration string
  fileSizeBytes?: number;
  format?: string;
  createdAt: string;
  videoMetadata?: VideoMetadata;
}

export interface VideoMetadata {
  processedWidth: number;
  processedHeight: number;
  processedDuration: string; // ISO 8601 duration string
  processedFileSizeBytes: number;
  processedFormat: string;
  processedBitrate: number;
  compressionRatio: number;
  originalWidth: number;
  originalHeight: number;
  originalDuration: string; // ISO 8601 duration string
  originalFileSizeBytes: number;
  originalFormat: string;
  originalBitrate: number;
}

export interface MediaFile {
  fileName?: string | null; // Optional - not required for GIFs
  mediaType: MediaType;
  width?: number;
  height?: number;
  fileSizeBytes?: number;
  duration?: string; // ISO 8601 duration string
  gifUrl?: string; // For GIF media type
  gifPreviewUrl?: string; // For GIF media type
}

export interface CreatePostData {
  content: string;
  imageFileName?: string;
  videoFileName?: string;
  privacy?: PostPrivacy;
  mediaFileNames?: string[];
  groupId?: number;
}

export interface CreatePostWithMediaData {
  content?: string;
  privacy?: PostPrivacy;
  mediaFiles?: MediaFile[];
  groupId?: number;
}

// Enhanced repost types (replaces quote tweet functionality)
export interface CreateRepostData {
  content?: string; // Optional - empty for simple reposts
  repostedPostId: number;
  privacy?: PostPrivacy;
  groupId?: number;
}

export interface CreateRepostWithMediaData {
  content?: string; // Optional - empty for simple reposts
  repostedPostId: number;
  privacy?: PostPrivacy;
  groupId?: number;
  mediaFiles?: MediaFile[];
}



export interface UploadedFile {
  fileName: string;
  fileUrl: string;
  mediaType: MediaType;
  fileSizeBytes: number;
  width?: number;
  height?: number;
  duration?: string;
}

export interface FileUploadError {
  originalFileName: string;
  errorMessage: string;
  errorCode: string;
}

export interface MultipleFileUploadResponse {
  uploadedFiles: UploadedFile[];
  errors: FileUploadError[];
  totalFiles: number;
  successfulUploads: number;
  failedUploads: number;
}

export interface UploadLimits {
  maxFiles: number;
  maxImageSizeMB: number;
  maxVideoSizeMB: number;
  supportedImageFormats: string[];
  supportedVideoFormats: string[];
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
  youTubeVideoId?: string;
  status: LinkPreviewStatus;
  errorMessage?: string;
  createdAt: string;
}

export enum GroupMemberRole {
  Member = 0,
  Moderator = 1,
  Admin = 2,
}

export interface Group {
  id: number;
  name: string;
  description: string;
  imageUrl?: string;
  createdAt: string;
  updatedAt: string;
  isOpen: boolean;
  user: User;
  memberCount: number;
  postCount: number;
  isCurrentUserMember: boolean;
}

export interface GroupList {
  id: number;
  name: string;
  description: string;
  imageUrl?: string;
  createdAt: string;
  creatorUsername: string;
  memberCount: number;
  postCount: number;
  isCurrentUserMember: boolean;
}

export interface GroupMember {
  id: number;
  joinedAt: string;
  role: GroupMemberRole;
  user: User;
}

export interface CreateGroup {
  name: string;
  description?: string;
  imageUrl?: string;
}

export interface UpdateGroup {
  name: string;
  description: string;
  imageUrl?: string;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export enum ReactionType {
  Heart = 1,
  ThumbsUp = 2,
  Laugh = 3,
  Surprised = 4,
  Sad = 5,
  Angry = 6
}

export enum PostType {
  Post = 0,
  Comment = 1,
  Repost = 2
}

export interface ReactionCount {
  reactionType: ReactionType;
  emoji: string;
  displayName: string;
  count: number;
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
  videoWidth?: number;
  videoHeight?: number;
  privacy: PostPrivacy;
  user: User;
  group?: Group;
  likeCount: number; // Legacy - will be replaced by reactionCounts
  commentCount: number;
  repostCount: number;
  quoteTweetCount: number;
  tags: Tag[];
  linkPreviews: LinkPreview[];
  isLikedByCurrentUser: boolean; // Legacy - will be replaced by currentUserReaction
  isRepostedByCurrentUser: boolean;
  moderationInfo?: PostModerationInfo;
  mediaItems?: PostMedia[];
  reactionCounts?: ReactionCount[];
  currentUserReaction?: ReactionType | null;
  totalReactionCount?: number;
  postType?: PostType;
  quotedPost?: Post;
  repostedPost?: Post; // The original post that was reposted
}

export interface Comment {
  id: number;
  content: string;
  createdAt: string;
  updatedAt: string;
  user: User;
  isEdited: boolean;
  likeCount: number; // Legacy - will be replaced by reactionCounts
  isLikedByCurrentUser: boolean; // Legacy - will be replaced by currentUserReaction
  reactionCounts?: ReactionCount[];
  currentUserReaction?: ReactionType | null;
  totalReactionCount?: number;
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
  Message = 7, // Private message notifications (excluded from main notifications list)

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
  isSeen: boolean;
  createdAt: string;
  readAt?: string;
  seenAt?: string;
  status?: string;
  actorUser?: User;
  post?: Post;
  comment?: Comment;
  mention?: {
    id: number;
    mentioningUserId: number;
    postId: number; // The post/comment where mention occurred
    isCommentMention: boolean; // True if mentioned in a comment, false if in a post
    parentPostId?: number; // If comment mention, this is the parent post ID
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
    register: (data: RegisterData) => Promise<RegisterResponse>;
    getCurrentUser: () => Promise<User>;
    forgotPassword: (email: string) => Promise<{ message: string }>;
    resetPassword: (token: string, newPassword: string) => Promise<{ message: string }>;
    verifyEmail: (token: string) => Promise<{ message: string }>;
    resendVerification: (email: string) => Promise<{ message: string }>;
  };
  posts: {
    getTimeline: (page: number, limit: number) => Promise<TimelineItem[]>;
    createPost: (data: CreatePostData) => Promise<Post>;
    createPostWithMedia: (data: CreatePostWithMediaData) => Promise<Post>;
    likePost: (postId: number) => Promise<void>;
    reactToPost: (postId: number, reactionType: number) => Promise<void>;
    removePostReaction: (postId: number) => Promise<void>;
    repostPost: (postId: number) => Promise<void>;
    deletePost: (postId: number) => Promise<void>;
    unrepost: (postId: number) => Promise<void>;
    createRepost: (data: CreateRepostData) => Promise<Post>;
    createRepostWithMedia: (data: CreateRepostWithMediaData) => Promise<Post>;
    getReposts: (postId: number, page?: number, pageSize?: number) => Promise<Post[]>;
    getPost: (postId: number) => Promise<Post>;

    getUserTimeline: (userId: number, page: number, limit: number) => Promise<TimelineItem[]>;
    getComments: (postId: number) => Promise<Comment[]>;
    addComment: (postId: number, data: CreateCommentData) => Promise<Comment>;
    likeComment: (postId: number, commentId: number) => Promise<void>;
    unlikeComment: (postId: number, commentId: number) => Promise<void>;
    reactToComment: (postId: number, commentId: number, reactionType: number) => Promise<void>;
    removeCommentReaction: (postId: number, commentId: number) => Promise<void>;
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
  uploads: {
    uploadMultipleFiles: (files: Array<{ uri: string; fileName: string; type: string }>) => Promise<MultipleFileUploadResponse>;
    getUploadLimits: () => Promise<UploadLimits>;
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
  groups: {
    getGroups: (page?: number, pageSize?: number) => Promise<PaginatedResult<GroupList>>;
    searchGroups: (query: string, page?: number, pageSize?: number) => Promise<PaginatedResult<GroupList>>;
    getGroup: (id: number) => Promise<Group>;
    getGroupByName: (name: string) => Promise<Group>;
    createGroup: (data: CreateGroup) => Promise<Group>;
    updateGroup: (id: number, data: UpdateGroup) => Promise<Group>;
    deleteGroup: (id: number) => Promise<void>;
    joinGroup: (id: number) => Promise<{ message: string }>;
    leaveGroup: (id: number) => Promise<{ message: string }>;
    getGroupMembers: (id: number, page?: number, pageSize?: number) => Promise<PaginatedResult<GroupMember>>;
    getGroupPosts: (id: number, page?: number, pageSize?: number) => Promise<PaginatedResult<Post>>;
    getUserGroups: (userId: number, page?: number, pageSize?: number) => Promise<PaginatedResult<GroupList>>;
    getMyGroups: (page?: number, pageSize?: number) => Promise<PaginatedResult<GroupList>>;
    uploadGroupImage: (uri: string, fileName: string, type: string) => Promise<{ fileName: string; imageUrl: string }>;
  };
  // Discovery & Personalization APIs
  personalization: {
    getProfile: () => Promise<UserPersonalizationProfileDto>;
    updateProfile: (forceRebuild?: boolean) => Promise<UserPersonalizationProfileDto>;
    getInsights: () => Promise<PersonalizationInsightsDto>;
    trackInteraction: (interaction: UserInteractionEventDto) => Promise<void>;
    getRecommendations: (contentType: string, limit?: number) => Promise<PersonalizedRecommendationDto[]>;
    getPersonalizedFeed: (config?: Partial<PersonalizedFeedConfigDto>) => Promise<PersonalizedRecommendationDto[]>;
    getPersonalizedSearch: (query: string, contentTypes?: string[], limit?: number) => Promise<PersonalizedSearchResultDto>;
    findSimilarUsers: (limit?: number, minSimilarity?: number) => Promise<UserSimilarityDto[]>;
  };
  topics: {
    getTopics: (category?: string, featured?: boolean) => Promise<TopicDto[]>;
    getTopic: (identifier: string) => Promise<TopicDto>;
    searchTopics: (query: string, limit?: number) => Promise<TopicSearchResultDto>;
    getTopicRecommendations: (limit?: number) => Promise<TopicRecommendationDto[]>;
    followTopic: (data: CreateTopicFollowDto) => Promise<TopicFollowDto>;
    unfollowTopic: (topicName: string) => Promise<void>;
    updateTopicFollow: (topicName: string, data: UpdateTopicFollowDto) => Promise<TopicFollowDto>;
    getUserTopics: (includeInMainFeed?: boolean) => Promise<TopicFollowDto[]>;
    isFollowingTopic: (topicName: string) => Promise<{ topicName: string; isFollowing: boolean }>;
    getTopicFeed: (topicName: string, config?: Partial<TopicFeedConfigDto>) => Promise<TopicFeedDto>;
    getPersonalizedTopicFeed: (config?: Partial<TopicFeedConfigDto>) => Promise<PersonalizedTopicFeedDto>;
    getMixedTopicFeed: (config?: Partial<TopicFeedConfigDto>) => Promise<Post[]>;
    getTrendingTopics: (timeWindow?: number, limit?: number, category?: string) => Promise<TopicTrendingDto[]>;
  };
  explore: {
    getExplorePage: (config?: Partial<ExploreConfigDto>) => Promise<ExplorePageDto>;
    getUserRecommendations: (limit?: number, minSimilarityScore?: number) => Promise<UserRecommendationDto[]>;
    getSimilarUsers: (limit?: number) => Promise<SimilarUserDto[]>;
    getNetworkBasedUsers: (maxDegrees?: number, limit?: number) => Promise<NetworkBasedUserDto[]>;
    getContentClusters: (limit?: number) => Promise<ContentClusterDto[]>;
    getInterestBasedContent: (limit?: number) => Promise<InterestBasedContentDto[]>;
    getTrendingTopics: (timeWindow?: number, limit?: number) => Promise<TrendingTopicDto[]>;
    getQuickTrendingPosts: () => Promise<Post[]>;
    getQuickTrendingHashtags: () => Promise<TrendingHashtagDto[]>;
    getQuickUserRecommendations: () => Promise<UserRecommendationDto[]>;
  };
}

// ===== DISCOVERY & PERSONALIZATION TYPES =====

// Trending Dashboard Types
export interface TrendingHashtagDto {
  name: string;
  postCount: number;
  velocity: number;
  velocityScore: number;
  engagementRate: number;
  category?: string;
  isGrowing: boolean;
  trendingScore: number;
  previousPostCount: number;
  growthRate: number;
  qualityScore: number;
  uniqueUsers: number;
  totalEngagement: number;
  averageEngagementPerPost: number;
  peakHour?: number;
  relatedHashtags: string[];
  samplePosts: Post[];
  lastUpdated: string;
}

export interface CategoryTrendingDto {
  category: string;
  trendingScore: number;
  postCount: number;
  topHashtags: TrendingHashtagDto[];
  growthRate: number;
  description?: string;
}

// Personalization Types
export interface UserPersonalizationProfileDto {
  userId: number;
  interestScores: Record<string, number>;
  contentTypePreferences: Record<string, number>;
  engagementPatterns: Record<string, number>;
  similarUsers: Record<string, number>;
  personalizationConfidence: number;
  diversityPreference: number;
  noveltyPreference: number;
  socialInfluenceFactor: number;
  qualityThreshold: number;
  lastMLUpdate: string;
  dataPointCount: number;
  algorithmVersion: string;
}

export interface PersonalizedRecommendationDto {
  content: any;
  contentType: string;
  recommendationScore: number;
  primaryReason: string;
  reasonTags: string[];
  scoreBreakdown: Record<string, number>;
  confidenceLevel: number;
  isExperimental: boolean;
  generatedAt: string;
}

export interface InterestInsightDto {
  interest: string;
  score: number;
  trendDirection: number;
  postCount: number;
  engagementCount: number;
  category: string;
  isGrowing: boolean;
}

export interface ContentTypeInsightDto {
  contentType: string;
  preferenceScore: number;
  engagementRate: number;
  viewCount: number;
  interactionCount: number;
  averageViewTime: string;
}

export interface EngagementPatternDto {
  timeOfDay: string;
  engagementScore: number;
  activityCount: number;
  preferredContentTypes: string[];
  averageSessionDuration: number;
}

export interface UserSimilarityDto {
  similarUser: User;
  similarityScore: number;
  commonInterests: string[];
  sharedFollows: string[];
  similarityReason: string;
}

export interface PersonalizationStatsDto {
  overallConfidence: number;
  totalInteractions: number;
  uniqueInterests: number;
  similarUsersCount: number;
  diversityScore: number;
  noveltyScore: number;
  profileCreatedAt: string;
  lastUpdated: string;
}

export interface PersonalizationInsightsDto {
  userId: number;
  topInterests: InterestInsightDto[];
  contentPreferences: ContentTypeInsightDto[];
  engagementPatterns: EngagementPatternDto[];
  similarUsers: UserSimilarityDto[];
  stats: PersonalizationStatsDto;
  recommendationTips: string[];
  generatedAt: string;
}

export interface PersonalizedFeedConfigDto {
  userId: number;
  postLimit: number;
  diversityWeight: number;
  noveltyWeight: number;
  socialWeight: number;
  qualityThreshold: number;
  includeExperimental: boolean;
  preferredContentTypes: string[];
  excludedTopics: string[];
  feedType: string;
}

export interface UserInteractionEventDto {
  userId: number;
  interactionType: string;
  targetEntityType?: string;
  targetEntityId?: number;
  interactionStrength: number;
  durationMs?: number;
  context?: string;
  deviceInfo?: string;
  sessionId?: string;
  isImplicit: boolean;
  sentiment: number;
}

// Topic Types
export interface TopicDto {
  id: number;
  name: string;
  description: string;
  category: string;
  relatedHashtags: string[];
  slug: string;
  icon?: string;
  color?: string;
  isFeatured: boolean;
  followerCount: number;
  isFollowedByCurrentUser: boolean;
  createdAt: string;
}

export interface TopicFollowDto {
  id: number;
  userId: number;
  topicName: string;
  topicDescription?: string;
  category: string;
  relatedHashtags: string[];
  interestLevel: number;
  includeInMainFeed: boolean;
  enableNotifications: boolean;
  notificationThreshold: number;
  createdAt: string;
}

export interface CreateTopicFollowDto {
  topicName: string;
  topicDescription?: string;
  category: string;
  relatedHashtags: string[];
  interestLevel?: number;
  includeInMainFeed?: boolean;
  enableNotifications?: boolean;
  notificationThreshold?: number;
}

export interface UpdateTopicFollowDto {
  interestLevel?: number;
  includeInMainFeed?: boolean;
  enableNotifications?: boolean;
  notificationThreshold?: number;
}

export interface TopicFeedMetricsDto {
  totalPosts: number;
  totalEngagement: number;
  uniqueContributors: number;
  avgEngagementRate: number;
  trendingScore: number;
  growthRate: number;
  generationTime: string;
}

export interface TopicFeedDto {
  topicName: string;
  category: string;
  posts: Post[];
  trendingHashtags: TrendingHashtagDto[];
  topContributors: User[];
  metrics: TopicFeedMetricsDto;
  generatedAt: string;
}

export interface PersonalizedFeedMetricsDto {
  totalTopicsFollowed: number;
  totalPosts: number;
  personalizationScore: number;
  activeTopics: string[];
  generationTime: string;
}

export interface PersonalizedTopicFeedDto {
  userId: number;
  topicFeeds: TopicFeedDto[];
  mixedFeed: Post[];
  metrics: PersonalizedFeedMetricsDto;
  generatedAt: string;
}

export interface TopicRecommendationDto {
  topic: TopicDto;
  recommendationScore: number;
  recommendationReason: string;
  matchingInterests: string[];
  samplePosts: Post[];
  isPersonalized: boolean;
}

export interface TopicFeedConfigDto {
  postsPerTopic: number;
  maxTopics: number;
  includeTrendingContent: boolean;
  includePersonalizedContent: boolean;
  minInterestLevel: number;
  timeWindowHours: number;
  sortBy: string;
}

export interface TopicTrendingDto {
  topicName: string;
  category: string;
  currentTrendingScore: number;
  previousTrendingScore: number;
  velocityScore: number;
  currentPosts: number;
  previousPosts: number;
  drivingHashtags: TrendingHashtagDto[];
  analyzedAt: string;
}

export interface TopicSearchResultDto {
  exactMatches: TopicDto[];
  partialMatches: TopicDto[];
  recommendations: TopicRecommendationDto[];
  suggestedHashtags: string[];
  totalResults: number;
}

// Explore Types
export interface ExploreMetricsDto {
  totalTrendingPosts: number;
  totalTrendingHashtags: number;
  totalRecommendedUsers: number;
  averageEngagementRate: number;
  personalizationScore: number;
  generationTime: string;
  algorithmVersion: string;
}

export interface ExplorePageDto {
  trendingPosts: Post[];
  trendingHashtags: TrendingHashtagDto[];
  trendingCategories: CategoryTrendingDto[];
  recommendedUsers: UserRecommendationDto[];
  personalizedPosts: Post[];
  metrics: ExploreMetricsDto;
  generatedAt: string;
}

export interface UserRecommendationDto {
  user: User;
  similarityScore: number;
  recommendationReason: string;
  commonInterests: string[];
  mutualFollows: User[];
  isNewUser: boolean;
  activityScore: number;
}

export interface ContentClusterDto {
  topic: string;
  description: string;
  posts: Post[];
  relatedHashtags: TrendingHashtagDto[];
  topContributors: User[];
  clusterScore: number;
  totalPosts: number;
}

export interface SimilarUserDto {
  user: User;
  similarityScore: number;
  sharedInterests: string[];
  mutualConnections: User[];
  similarityReason: string;
}

export interface InterestBasedContentDto {
  interest: string;
  recommendedPosts: Post[];
  topCreators: User[];
  interestStrength: number;
  isGrowing: boolean;
}

export interface TrendingTopicDto {
  topic: string;
  trendingPosts: Post[];
  relatedHashtags: TrendingHashtagDto[];
  topContributors: User[];
  topicScore: number;
  growthRate: number;
  category: string;
}

export interface NetworkBasedUserDto {
  user: User;
  networkScore: number;
  connectionPath: User[];
  discoveryMethod: string;
  degreesOfSeparation: number;
}

export interface ExploreConfigDto {
  trendingPostsLimit: number;
  trendingHashtagsLimit: number;
  recommendedUsersLimit: number;
  timeWindowHours: number;
  includePersonalizedContent: boolean;
  includeUserRecommendations: boolean;
  preferredCategories: string[];
  minSimilarityScore: number;
}

export interface PersonalizedSearchResultDto {
  query: string;
  results: PersonalizedRecommendationDto[];
  queryExpansion: Record<string, number>;
  personalizationStrength: number;
  totalResults: number;
  searchedAt: string;
}
