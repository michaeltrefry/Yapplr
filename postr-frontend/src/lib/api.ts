import axios from 'axios';
import type {
  AuthResponse,
  LoginData,
  RegisterData,
  Post,
  CreatePostData,
  Comment,
  CreateCommentData,
  User,
  UserProfile,
  UpdateUserData,
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

  getTimeline: async (page = 1, pageSize = 20): Promise<Post[]> => {
    const response = await api.get(`/posts/timeline?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  getUserPosts: async (userId: number, page = 1, pageSize = 20): Promise<Post[]> => {
    const response = await api.get(`/posts/user/${userId}?page=${page}&pageSize=${pageSize}`);
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

export default api;
