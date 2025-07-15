import { NavigationContainerRef } from '@react-navigation/native';
import { SignalRNotificationPayload } from './SignalRService';
import { RootStackParamList } from '../navigation/AppNavigator';

// We'll need access to the API client to fetch post data
let apiClient: any = null;

class NotificationNavigationService {
  private navigationRef: NavigationContainerRef<RootStackParamList> | null = null;

  setNavigationRef(ref: NavigationContainerRef<RootStackParamList>) {
    this.navigationRef = ref;
  }

  setApiClient(api: any) {
    apiClient = api;
  }

  /**
   * Navigate to the appropriate screen based on notification data
   */
  navigateFromNotification(notification: SignalRNotificationPayload): boolean {
    console.log('📱🔔 🚀 NEW NAVIGATION SERVICE CALLED 🚀 with:', notification);

    if (!this.navigationRef) {
      console.warn('📱🔔 Navigation ref not set, cannot navigate from notification');
      return false;
    }

    if (!this.navigationRef.isReady()) {
      console.warn('📱🔔 Navigation not ready, cannot navigate from notification');
      return false;
    }

    try {
      const { type, data } = notification;
      console.log('📱🔔 Navigating from notification:', { type, data });

      // Add a small delay to ensure React state has settled
      setTimeout(async () => {
        try {
          // Check both the notification type and the data type for proper routing
          const actualType = type === 'generic' && data?.type ? data.type : type;
          console.log('📱🔔 Actual notification type for navigation:', actualType);

          switch (actualType) {
            case 'message':
              this.navigateToMessage(data);
              break;

            case 'mention':
            case 'reply':
            case 'comment':
            case 'like':
            case 'repost':
              await this.navigateToPost(data);
              break;

            case 'follow':
            case 'follow_request':
              this.navigateToUserProfile(data);
              break;

            case 'test':
            case 'generic':
            case 'VideoProcessingCompleted':
            case 'systemMessage':
              console.log('📱🔔 System notification, no specific navigation');
              break;

            default:
              console.log('📱🔔 No specific navigation for notification type:', actualType);
              break;
          }
        } catch (navError) {
          console.error('📱🔔 Error during delayed navigation:', navError);
        }
      }, 50);

      return true; // Return true immediately since we're doing delayed navigation
    } catch (error) {
      console.error('📱🔔 Error navigating from notification:', error);
      return false;
    }
  }

  private navigateToMessage(data?: Record<string, string | undefined>): void {
    if (!data?.conversationId) {
      console.warn('📱🔔 Missing conversation data for message navigation');
      return;
    }

    const conversationId = parseInt(data.conversationId);

    if (isNaN(conversationId)) {
      console.warn('📱🔔 Invalid conversation ID for message navigation');
      return;
    }

    try {
      // For messages, we can navigate with just the conversation ID
      // The conversation screen will load the other user's info
      if (data.senderUsername) {
        // If we have sender info, use it
        this.navigationRef?.navigate('Conversation', {
          conversationId,
          otherUser: {
            id: parseInt(data.userId || '0') || 0,
            username: data.senderUsername,
          },
        });
        console.log('📱🔔 Navigated to conversation with sender info:', conversationId);
      } else {
        // Navigate to messages list if we don't have enough info for direct conversation
        this.navigationRef?.navigate('Messages');
        console.log('📱🔔 Navigated to messages list (insufficient conversation data)');
      }
    } catch (error) {
      console.error('📱🔔 Error navigating to conversation:', error);
    }
  }

  private async navigateToPost(data?: Record<string, string | undefined>): Promise<void> {
    if (!data?.postId) {
      console.warn('📱🔔 Missing post data for post navigation');
      return;
    }

    if (!apiClient) {
      console.warn('📱🔔 API client not available for post navigation');
      return;
    }

    try {
      const postId = parseInt(data.postId);
      if (isNaN(postId)) {
        console.warn('📱🔔 Invalid post ID for navigation:', data.postId);
        return;
      }

      console.log('📱🔔 Fetching post data for navigation, postId:', postId);

      // Fetch the post data from the timeline or posts endpoint
      // We'll try to get it from the timeline first, then fall back to a direct post fetch
      const timelineResponse = await apiClient.posts.getTimeline(1, 50);
      let post = timelineResponse.find((item: any) => item.post.id === postId)?.post;

      if (!post) {
        console.log('📱🔔 Post not found in timeline, this might be an older post');
        // For now, we'll just navigate to the home screen
        // In the future, we could implement a direct post fetch endpoint
        this.navigationRef?.navigate('MainTabs');
        console.log('📱🔔 Navigated to home screen (post not in current timeline)');
        return;
      }

      // Navigate to the comments screen with the post data
      this.navigationRef?.navigate('Comments', { post });
      console.log('📱🔔 Navigated to post comments:', postId);

    } catch (error) {
      console.error('📱🔔 Error fetching post for navigation:', error);
      // Fall back to navigating to home screen
      this.navigationRef?.navigate('MainTabs');
      console.log('📱🔔 Navigated to home screen (error fetching post)');
    }
  }

  private navigateToUserProfile(data?: Record<string, string | undefined>): void {
    if (!data?.username && !data?.userId) {
      console.warn('📱🔔 Missing user data for profile navigation');
      return;
    }

    try {
      // Prefer username over userId for navigation
      const username = data.username || `user-${data.userId}`;

      this.navigationRef?.navigate('UserProfile', { username });

      console.log('📱🔔 Navigated to user profile:', username);
    } catch (error) {
      console.error('📱🔔 Error navigating to user profile:', error);
    }
  }

  /**
   * Check if navigation is ready
   */
  isReady(): boolean {
    return this.navigationRef?.isReady() ?? false;
  }
}

// Export singleton instance
export default new NotificationNavigationService();
