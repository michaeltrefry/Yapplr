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

export enum UserRole {
  User = 0,
  Moderator = 1,
  Admin = 2,
  System = 3,
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
  emailVerified: boolean;
  role?: UserRole;
  status?: UserStatus;
  suspendedUntil?: string;
  suspensionReason?: string;
}

export interface UserWithOnlineStatus {
  id: number;
  email: string;
  username: string;
  bio: string;
  birthday?: string;
  pronouns: string;
  tagline: string;
  profileImageFileName: string;
  createdAt: string;
  emailVerified: boolean;
  isOnline: boolean;
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

export interface Post {
  id: number;
  content: string;
  imageUrl?: string;
  videoUrl?: string;
  videoThumbnailUrl?: string;
  videoProcessingStatus?: VideoProcessingStatus;
  privacy: PostPrivacy;
  createdAt: string;
  updatedAt: string;
  user: User;
  likeCount: number;
  commentCount: number;
  repostCount: number;
  tags: Tag[];
  linkPreviews: LinkPreview[];
  isLikedByCurrentUser: boolean;
  isRepostedByCurrentUser: boolean;
  isEdited: boolean;
  moderationInfo?: PostModerationInfo;
}

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
  color: string;
  icon?: string;
  reason?: string;
  appliedAt: string;
  appliedByUser: User;
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

export interface Comment {
  id: number;
  content: string;
  createdAt: string;
  updatedAt: string;
  user: User;
  isEdited: boolean;
}

export interface FollowResponse {
  isFollowing: boolean;
  followerCount: number;
  hasPendingRequest?: boolean;
}

export interface FollowRequest {
  id: number;
  createdAt: string;
  requester: User;
  requested: User;
}

export interface TimelineItem {
  type: 'post' | 'repost';
  createdAt: string;
  post: Post;
  repostedBy?: User;
}

export interface AuthResponse {
  token: string;
  user: User;
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

export interface LoginData {
  email: string;
  password: string;
}

export interface CreatePostData {
  content: string;
  imageFileName?: string;
  videoFileName?: string;
  privacy?: PostPrivacy;
}

export interface UpdatePostData {
  content: string;
  privacy?: PostPrivacy;
}

export interface CreateCommentData {
  content: string;
}

export interface UpdateCommentData {
  content: string;
}

export interface BlockResponse {
  message: string;
}

export interface BlockStatusResponse {
  isBlocked: boolean;
}

// Messaging types
export enum MessageStatusType {
  Sent = 0,
  Delivered = 1,
  Read = 2
}

export interface Message {
  id: number;
  content: string;
  imageUrl?: string;
  createdAt: string;
  updatedAt: string;
  isEdited: boolean;
  isDeleted: boolean;
  conversationId: number;
  sender: User;
  status?: MessageStatusType;
}

export interface Conversation {
  id: number;
  createdAt: string;
  updatedAt: string;
  participants: User[];
  lastMessage?: Message;
  unreadCount: number;
}

export interface ConversationListItem {
  id: number;
  createdAt: string;
  updatedAt: string;
  otherParticipant: User;
  lastMessage?: Message;
  unreadCount: number;
}

export interface CreateMessageData {
  recipientId: number;
  content?: string;
  imageFileName?: string;
}

export interface SendMessageData {
  conversationId: number;
  content?: string;
  imageFileName?: string;
  videoFileName?: string;
}

export interface CanMessageResponse {
  canMessage: boolean;
}

export interface UnreadCountResponse {
  unreadCount: number;
}

export interface UpdateUserData {
  bio?: string;
  birthday?: string;
  pronouns?: string;
  tagline?: string;
}

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
  mention?: Mention;
}

export interface Mention {
  id: number;
  username: string;
  createdAt: string;
  mentionedUserId: number;
  mentioningUserId: number;
  postId?: number;
  commentId?: number;
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

// Admin Types
export enum SystemTagCategory {
  ContentWarning = 0,
  Violation = 1,
  ModerationStatus = 2,
  Quality = 3,
  Legal = 4,
  Safety = 5,
}

export enum AuditAction {
  UserSuspended = 100,
  UserBanned = 101,
  UserShadowBanned = 102,
  UserUnsuspended = 103,
  UserUnbanned = 104,
  UserRoleChanged = 105,
  UserForcePasswordReset = 106,
  UserEmailVerificationToggled = 107,
  PostHidden = 200,
  PostDeleted = 201,
  PostRestored = 202,
  PostSystemTagAdded = 203,
  PostSystemTagRemoved = 204,
  CommentHidden = 210,
  CommentDeleted = 211,
  CommentRestored = 212,
  CommentSystemTagAdded = 213,
  CommentSystemTagRemoved = 214,
  SystemTagCreated = 300,
  SystemTagUpdated = 301,
  SystemTagDeleted = 302,
  IpBlocked = 400,
  IpUnblocked = 401,
  SecurityIncidentReported = 402,
  UserReportCreated = 450,
  UserReportReviewed = 451,
  AppealCreated = 460,
  AppealApproved = 461,
  AppealDenied = 462,
  AppealEscalated = 463,
  BulkContentDeleted = 500,
  BulkContentHidden = 501,
  BulkUsersActioned = 502,
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

export interface SystemTag {
  id: number;
  name: string;
  description: string;
  category: SystemTagCategory;
  isVisibleToUsers: boolean;
  isActive: boolean;
  color: string;
  icon?: string;
  sortOrder: number;
  createdAt: string;
  updatedAt: string;
}

export interface AiSuggestedTag {
  id: number;
  tagName: string;
  category: string;
  confidence: number;
  riskLevel: string;
  requiresReview: boolean;
  suggestedAt: string;
  isApproved: boolean;
  isRejected: boolean;
  approvedByUserId?: number;
  approvedByUsername?: string;
  approvedAt?: string;
  approvalReason?: string;
}

export interface AdminUser {
  id: number;
  username: string;
  email: string;
  role: UserRole;
  status: UserStatus;
  suspendedUntil?: string;
  suspensionReason?: string;
  suspendedByUsername?: string;
  createdAt: string;
  lastLoginAt?: string;
  lastLoginIp?: string;
  emailVerified: boolean;
  postCount: number;
  followerCount: number;
  followingCount: number;
}

export interface TrustScoreFactors {
  userId: number;
  username: string;
  currentScore: number;
  factors: Record<string, any>;
}

export interface TrustScoreHistory {
  id: number;
  userId: number;
  username: string;
  previousScore: number;
  newScore: number;
  scoreChange: number;
  reason: string;
  details?: string;
  relatedEntityType?: string;
  relatedEntityId?: number;
  triggeredByUsername?: string;
  calculatedBy?: string;
  isAutomatic: boolean;
  confidence?: number;
  createdAt: string;
}

export interface AdminUserDetails {
  // Basic Profile Information
  id: number;
  username: string;
  email: string;
  bio: string;
  birthday?: string;
  pronouns: string;
  tagline: string;
  profileImageFileName: string;
  createdAt: string;
  updatedAt: string;
  lastSeenAt: string;
  emailVerified: boolean;
  termsAcceptedAt?: string;

  // Admin/Moderation Information
  role: UserRole;
  status: UserStatus;
  suspendedUntil?: string;
  suspensionReason?: string;
  suspendedByUsername?: string;
  lastLoginAt?: string;
  lastLoginIp?: string;

  // Trust Score Information
  trustScore: number;
  trustScoreFactors?: TrustScoreFactors;
  recentTrustScoreHistory: TrustScoreHistory[];

  // Rate Limiting Settings
  rateLimitingEnabled?: boolean;
  trustBasedRateLimitingEnabled?: boolean;
  isCurrentlyRateLimited: boolean;
  rateLimitedUntil?: string;
  recentRateLimitViolations: number;

  // Activity Statistics
  postCount: number;
  commentCount: number;
  likeCount: number;
  followerCount: number;
  followingCount: number;
  reportCount: number;
  moderationActionCount: number;

  // Recent Moderation Actions
  recentModerationActions: AuditLog[];
}

export interface UpdateUserRateLimitSettingsDto {
  rateLimitingEnabled?: boolean | null;
  trustBasedRateLimitingEnabled?: boolean | null;
  reason?: string;
}

export interface AdminPost {
  id: number;
  content: string;
  imageFileName?: string;
  privacy: PostPrivacy;
  isHidden: boolean;
  hiddenReason?: string;
  hiddenAt?: string;
  hiddenByUsername?: string;
  createdAt: string;
  updatedAt: string;
  user: User;
  likeCount: number;
  commentCount: number;
  repostCount: number;
  systemTags: SystemTag[];
  aiSuggestedTags: AiSuggestedTag[];
}

export interface AdminComment {
  id: number;
  content: string;
  isHidden: boolean;
  hiddenReason?: string;
  hiddenAt?: string;
  hiddenByUsername?: string;
  createdAt: string;
  updatedAt: string;
  user: User;
  postId: number;
  systemTags: SystemTag[];
  aiSuggestedTags: AiSuggestedTag[];
}

export interface AuditLog {
  id: number;
  action: AuditAction;
  userId: number;
  performedByUsername: string;
  targetUsername?: string;
  targetUserId?: number;
  targetPostId?: number;
  targetCommentId?: number;
  reason?: string;
  details?: string;
  ipAddress?: string;
  createdAt: string;
}

export interface UserAppeal {
  id: number;
  username: string;
  type: AppealType;
  status: AppealStatus;
  reason: string;
  additionalInfo?: string;
  targetPostId?: number;
  targetCommentId?: number;
  reviewedByUsername?: string;
  reviewNotes?: string;
  reviewedAt?: string;
  createdAt: string;
}

export interface ModerationStats {
  totalUsers: number;
  activeUsers: number;
  suspendedUsers: number;
  bannedUsers: number;
  shadowBannedUsers: number;
  totalPosts: number;
  hiddenPosts: number;
  totalComments: number;
  hiddenComments: number;
  pendingAppeals: number;
  todayActions: number;
  weekActions: number;
  monthActions: number;
}

export interface ContentQueue {
  flaggedPosts: AdminPost[];
  flaggedComments: AdminComment[];
  pendingAppeals: UserAppeal[];
  userReports: UserReport[];
  totalFlaggedContent: number;
}

// Enhanced Analytics Types
export interface UserGrowthStats {
  dailyStats: DailyStats[];
  totalNewUsers: number;
  totalActiveUsers: number;
  growthRate: number;
  peakDayNewUsers: number;
  peakDate: string;
}

export interface ContentStats {
  dailyPosts: DailyStats[];
  dailyComments: DailyStats[];
  totalPosts: number;
  totalComments: number;
  postsGrowthRate: number;
  commentsGrowthRate: number;
  averagePostsPerDay: number;
  averageCommentsPerDay: number;
}

export interface ModerationTrends {
  dailyActions: DailyStats[];
  actionBreakdown: ActionTypeStats[];
  totalActions: number;
  actionsGrowthRate: number;
  peakDayActions: number;
  peakDate: string;
}

export interface SystemHealth {
  uptimePercentage: number;
  activeUsers24h: number;
  errorCount24h: number;
  averageResponseTime: number;
  databaseConnections: number;
  memoryUsage: number;
  cpuUsage: number;
  alerts: SystemAlert[];
}

export interface TopModerators {
  moderators: ModeratorStats[];
  totalModerators: number;
  totalActions: number;
}

export interface ContentTrends {
  trendingHashtags: HashtagStats[];
  engagementTrends: DailyStats[];
  totalHashtags: number;
  averageEngagementRate: number;
}

export interface UserEngagementStats {
  dailyEngagement: DailyStats[];
  averageSessionDuration: number;
  totalSessions: number;
  retentionRate: number;
  engagementBreakdown: EngagementTypeStats[];
}

// Supporting Types
export interface DailyStats {
  date: string;
  count: number;
  label: string;
}

export interface ActionTypeStats {
  actionType: string;
  count: number;
  percentage: number;
}

export interface SystemAlert {
  type: string;
  message: string;
  severity: string;
  createdAt: string;
}

export interface ModeratorStats {
  username: string;
  role: UserRole;
  totalActions: number;
  userActions: number;
  contentActions: number;
  successRate: number;
  lastActive: string;
}

export interface HashtagStats {
  hashtag: string;
  count: number;
  growthRate: number;
  uniqueUsers: number;
}

export interface EngagementTypeStats {
  type: string;
  count: number;
  percentage: number;
}

// Admin API DTOs
export interface CreateSystemTagDto {
  name: string;
  description: string;
  category: SystemTagCategory;
  isVisibleToUsers?: boolean;
  color?: string;
  icon?: string;
  sortOrder?: number;
}

export interface UpdateSystemTagDto {
  name?: string;
  description?: string;
  category?: SystemTagCategory;
  isVisibleToUsers?: boolean;
  isActive?: boolean;
  color?: string;
  icon?: string;
  sortOrder?: number;
}

export interface SuspendUserDto {
  reason: string;
  suspendedUntil?: string;
}

export interface BanUserDto {
  reason: string;
  isShadowBan?: boolean;
}

export interface ChangeUserRoleDto {
  role: UserRole;
  reason: string;
}

export interface HideContentDto {
  reason: string;
}

export interface ApplySystemTagDto {
  systemTagId: number;
  reason?: string;
}

export interface ReviewAppealDto {
  status: AppealStatus;
  reviewNotes: string;
}

export interface CreateAppealDto {
  type: AppealType;
  reason: string;
  additionalInfo?: string;
  targetPostId?: number;
  targetCommentId?: number;
}

// User Report Types
export enum UserReportStatus {
  Pending = 0,
  Reviewed = 1,
  Dismissed = 2,
  ActionTaken = 3,
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
  post?: AdminPost;
  comment?: AdminComment;
  systemTags: SystemTag[];
}

export interface CreateUserReportDto {
  postId?: number;
  commentId?: number;
  reason: string;
  systemTagIds: number[];
}

export interface ReviewUserReportDto {
  status: UserReportStatus;
  reviewNotes: string;
}

export interface HideContentFromReportDto {
  reason: string;
}

// Content Management Types
export enum ContentPageType {
  TermsOfService = 0,
  PrivacyPolicy = 1,
  CommunityGuidelines = 2,
  AboutUs = 3,
  Help = 4,
}

export interface ContentPage {
  id: number;
  title: string;
  slug: string;
  type: ContentPageType;
  publishedVersionId?: number;
  publishedVersion?: ContentPageVersion;
  createdAt: string;
  updatedAt: string;
  totalVersions: number;
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

export interface CreateContentPageVersionDto {
  content: string;
  changeNotes?: string;
}

export interface PublishContentVersionDto {
  versionId: number;
}
