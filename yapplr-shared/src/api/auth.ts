import { ApiClient } from './client';
import { AuthResponse, LoginData, RegisterData, User } from '../types';

export class AuthApi {
  constructor(private client: ApiClient) {}

  async login(data: LoginData): Promise<AuthResponse> {
    return this.client.post('/auth/login', data);
  }

  async register(data: RegisterData): Promise<AuthResponse> {
    return this.client.post('/auth/register', data);
  }

  async getCurrentUser(): Promise<User> {
    return this.client.get('/auth/me');
  }

  async refreshToken(): Promise<AuthResponse> {
    return this.client.post('/auth/refresh');
  }
}
