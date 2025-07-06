import { ApiClient } from './client';
import { Tag, Post } from '../types';

export class TagsApi {
  constructor(private client: ApiClient) {}

  async searchTags(query: string, limit = 20): Promise<Tag[]> {
    return this.client.get(`/tags/search/${encodeURIComponent(query)}?limit=${limit}`);
  }

  async getTrendingTags(limit = 10): Promise<Tag[]> {
    return this.client.get(`/tags/trending?limit=${limit}`);
  }

  async getTag(tagName: string): Promise<Tag> {
    return this.client.get(`/tags/${encodeURIComponent(tagName)}`);
  }

  async getPostsByTag(tagName: string, page = 1, pageSize = 25): Promise<Post[]> {
    return this.client.get(`/tags/${encodeURIComponent(tagName)}/posts?page=${page}&pageSize=${pageSize}`);
  }
}
