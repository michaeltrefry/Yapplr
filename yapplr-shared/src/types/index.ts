export enum PostPrivacy {
  Public = 0,
  Followers = 1,
  Private = 2,
}

export enum PostType {
  Post = 0,
  Comment = 1
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

export enum GroupMemberRole {
  Member = 0,
  Moderator = 1,
  Admin = 2,
}

export interface Group {
  id: number;
  name: string;
  description: string;
  imageFileName?: string;
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
  imageFileName?: string;
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
  imageFileName?: string;
}

export interface UpdateGroup {
  name: string;
  description: string;
  imageFileName?: string;
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

export interface Post {
  id: number;
  content: string;
  imageUrl?: string;
  privacy: PostPrivacy;
  createdAt: string;
  updatedAt: string;
  user: User;
  group?: Group;
  likeCount: number;
  commentCount: number;
  repostCount: number;
  linkPreviews: LinkPreview[];
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
  privacy?: PostPrivacy;
  groupId?: number;
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
