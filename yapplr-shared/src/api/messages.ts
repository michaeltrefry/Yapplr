import { ApiClient } from './client';
import { 
  Message, 
  Conversation, 
  ConversationListItem,
  CreateMessageData, 
  SendMessageData,
  CanMessageResponse,
  UnreadCountResponse
} from '../types';

export class MessagesApi {
  constructor(private client: ApiClient) {}

  async getConversations(page: number = 1, pageSize: number = 25): Promise<ConversationListItem[]> {
    return this.client.get(`/messages/conversations?page=${page}&pageSize=${pageSize}`);
  }

  async getConversation(conversationId: number): Promise<Conversation> {
    return this.client.get(`/messages/conversations/${conversationId}`);
  }

  async getMessages(conversationId: number, page: number = 1, pageSize: number = 25): Promise<Message[]> {
    return this.client.get(`/messages/conversations/${conversationId}/messages?page=${page}&pageSize=${pageSize}`);
  }

  async sendMessage(data: CreateMessageData): Promise<Message> {
    return this.client.post('/messages', data);
  }

  async sendMessageToConversation(data: SendMessageData): Promise<Message> {
    return this.client.post('/messages/send', data);
  }

  async markConversationAsRead(conversationId: number): Promise<void> {
    return this.client.post(`/messages/conversations/${conversationId}/read`);
  }

  async canMessage(userId: number): Promise<CanMessageResponse> {
    return this.client.get(`/messages/can-message/${userId}`);
  }

  async getOrCreateConversation(userId: number): Promise<Conversation> {
    return this.client.post(`/messages/conversations/with/${userId}`);
  }

  async getUnreadCount(): Promise<UnreadCountResponse> {
    return this.client.get('/messages/unread-count');
  }
}
