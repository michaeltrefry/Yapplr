// Local types for mobile app - copied from yapplr-shared

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
  privacy?: PostPrivacy;
}

export enum PostPrivacy {
  Public = 0,
  Followers = 1,
  Private = 2,
}

export interface Post {
  id: number;
  content: string;
  createdAt: string;
  updatedAt?: string;
  isEdited: boolean;
  imageUrl?: string;
  user: User;
  likeCount: number;
  commentCount: number;
  repostCount: number;
  isLikedByCurrentUser: boolean;
  isRepostedByCurrentUser: boolean;
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

// API Client interface
export interface YapplrApi {
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
  preferences: {
    get: () => Promise<{ darkMode: boolean }>;
    update: (preferences: { darkMode?: boolean }) => Promise<{ darkMode: boolean }>;
  };
}
