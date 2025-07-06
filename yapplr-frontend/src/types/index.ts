export enum PostPrivacy {
  Public = 0,
  Followers = 1,
  Private = 2,
}

export interface Tag {
  id: number;
  name: string;
  postCount: number;
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
  privacy: PostPrivacy;
  createdAt: string;
  updatedAt: string;
  user: User;
  likeCount: number;
  commentCount: number;
  repostCount: number;
  tags: Tag[];
  isLikedByCurrentUser: boolean;
  isRepostedByCurrentUser: boolean;
  isEdited: boolean;
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
