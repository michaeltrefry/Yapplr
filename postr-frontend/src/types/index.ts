export enum PostPrivacy {
  Public = 0,
  Followers = 1,
  Private = 2,
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

export interface Post {
  id: number;
  content: string;
  imageUrl?: string;
  privacy: PostPrivacy;
  createdAt: string;
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
  user: User;
}

export interface FollowResponse {
  isFollowing: boolean;
  followerCount: number;
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

export interface CreateCommentData {
  content: string;
}

export interface UpdateUserData {
  bio?: string;
  birthday?: string;
  pronouns?: string;
  tagline?: string;
}
