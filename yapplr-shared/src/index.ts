// Export all types
export * from './types';

// Export API classes
export { ApiClient } from './api/client';
export { AuthApi } from './api/auth';
export { PostsApi } from './api/posts';
export { UsersApi } from './api/users';
export { MessagesApi } from './api/messages';

// Export a factory function to create all APIs
import { ApiClient, ApiClientConfig } from './api/client';
import { AuthApi } from './api/auth';
import { PostsApi } from './api/posts';
import { UsersApi } from './api/users';
import { MessagesApi } from './api/messages';

export interface YapplrApi {
  auth: AuthApi;
  posts: PostsApi;
  users: UsersApi;
  messages: MessagesApi;
}

export function createYapplrApi(config: ApiClientConfig): YapplrApi {
  const client = new ApiClient(config);
  
  return {
    auth: new AuthApi(client),
    posts: new PostsApi(client),
    users: new UsersApi(client),
    messages: new MessagesApi(client),
  };
}
