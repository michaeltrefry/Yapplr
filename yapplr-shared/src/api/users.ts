import { ApiClient } from './client';
import { 
  User, 
  UserProfile, 
  UserWithOnlineStatus,
  UpdateUserData, 
  FollowResponse,
  BlockResponse,
  BlockStatusResponse
} from '../types';

export class UsersApi {
  constructor(private client: ApiClient) {}

  async getUserProfile(username: string): Promise<UserProfile> {
    return this.client.get(`/users/${username}`);
  }

  async updateProfile(data: UpdateUserData): Promise<User> {
    return this.client.put('/users/me', data);
  }

  async followUser(userId: number): Promise<FollowResponse> {
    return this.client.post(`/users/${userId}/follow`);
  }

  async unfollowUser(userId: number): Promise<FollowResponse> {
    return this.client.delete(`/users/${userId}/follow`);
  }

  async getFollowing(): Promise<User[]> {
    return this.client.get('/users/me/following');
  }

  async getFollowingWithOnlineStatus(): Promise<UserWithOnlineStatus[]> {
    return this.client.get('/users/me/following/online-status');
  }

  async searchUsers(query: string): Promise<User[]> {
    return this.client.get(`/users/search?q=${encodeURIComponent(query)}`);
  }

  async blockUser(userId: number): Promise<BlockResponse> {
    return this.client.post(`/users/${userId}/block`);
  }

  async unblockUser(userId: number): Promise<BlockResponse> {
    return this.client.delete(`/users/${userId}/block`);
  }

  async getBlockStatus(userId: number): Promise<BlockStatusResponse> {
    return this.client.get(`/users/${userId}/block-status`);
  }

  async getBlockedUsers(): Promise<User[]> {
    return this.client.get('/users/me/blocked');
  }

  async uploadProfileImage(imageFile: File | any): Promise<{ profileImageFileName: string }> {
    const formData = new FormData();
    formData.append('image', imageFile);
    return this.client.postFormData('/users/me/profile-image', formData);
  }
}
