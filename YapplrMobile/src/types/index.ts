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
  Private = 1,
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

export interface TimelineItem {
  type: 'post' | 'repost';
  createdAt: string;
  post: Post;
  repostedBy?: User;
}

export interface Message {
  id: number;
  content: string;
  createdAt: string;
  updatedAt?: string;
  isEdited: boolean;
  imageFileName?: string;
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
  };
  posts: {
    getTimeline: (page: number, limit: number) => Promise<TimelineItem[]>;
    createPost: (data: CreatePostData) => Promise<Post>;
    likePost: (postId: number) => Promise<void>;
    repostPost: (postId: number) => Promise<void>;
    getUserTimeline: (userId: number, page: number, limit: number) => Promise<TimelineItem[]>;
  };
  users: {
    searchUsers: (query: string) => Promise<User[]>;
    getUserProfile: (username: string) => Promise<UserProfile>;
  };
  messages: {
    getConversations: () => Promise<ConversationListItem[]>;
    canMessage: (userId: number) => Promise<CanMessageResponse>;
    getOrCreateConversation: (userId: number) => Promise<Conversation>;
    getMessages: (conversationId: number, page: number, limit: number) => Promise<Message[]>;
    sendMessageToConversation: (data: SendMessageData) => Promise<Message>;
  };
  images: {
    uploadImage: (uri: string, fileName: string, type: string) => Promise<ImageUploadResponse>;
    deleteImage: (fileName: string) => Promise<void>;
  };
}
