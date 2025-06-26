import { ApiClient } from './client';
import { 
  Post, 
  CreatePostData, 
  UpdatePostData, 
  Comment, 
  CreateCommentData, 
  UpdateCommentData,
  TimelineItem 
} from '../types';

export class PostsApi {
  constructor(private client: ApiClient) {}

  async getTimeline(page: number = 1, pageSize: number = 25): Promise<TimelineItem[]> {
    return this.client.get(`/posts/timeline?page=${page}&pageSize=${pageSize}`);
  }

  async getPost(id: number): Promise<Post> {
    return this.client.get(`/posts/${id}`);
  }

  async createPost(data: CreatePostData): Promise<Post> {
    return this.client.post('/posts', data);
  }

  async updatePost(id: number, data: UpdatePostData): Promise<Post> {
    return this.client.put(`/posts/${id}`, data);
  }

  async deletePost(id: number): Promise<void> {
    return this.client.delete(`/posts/${id}`);
  }

  async likePost(id: number): Promise<{ isLiked: boolean; likeCount: number }> {
    return this.client.post(`/posts/${id}/like`);
  }

  async repostPost(id: number): Promise<{ isReposted: boolean; repostCount: number }> {
    return this.client.post(`/posts/${id}/repost`);
  }

  async getComments(postId: number, page: number = 1, pageSize: number = 25): Promise<Comment[]> {
    return this.client.get(`/posts/${postId}/comments?page=${page}&pageSize=${pageSize}`);
  }

  async createComment(postId: number, data: CreateCommentData): Promise<Comment> {
    return this.client.post(`/posts/${postId}/comments`, data);
  }

  async updateComment(postId: number, commentId: number, data: UpdateCommentData): Promise<Comment> {
    return this.client.put(`/posts/${postId}/comments/${commentId}`, data);
  }

  async deleteComment(postId: number, commentId: number): Promise<void> {
    return this.client.delete(`/posts/${postId}/comments/${commentId}`);
  }

  async getUserPosts(username: string, page: number = 1, pageSize: number = 25): Promise<Post[]> {
    return this.client.get(`/posts/user/${username}?page=${page}&pageSize=${pageSize}`);
  }
}
