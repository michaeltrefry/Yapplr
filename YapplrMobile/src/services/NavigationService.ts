// Simple navigation state tracking service
class NavigationService {
  private currentScreen: string | null = null;
  private currentConversationId: number | null = null;

  setCurrentScreen(screenName: string) {
    this.currentScreen = screenName;
    console.log('📱🧭 Navigation: Current screen set to:', screenName);
  }

  setCurrentConversation(conversationId: number | null) {
    this.currentConversationId = conversationId;
    console.log('📱🧭 Navigation: Current conversation set to:', conversationId);
  }

  getCurrentScreen(): string | null {
    return this.currentScreen;
  }

  getCurrentConversationId(): number | null {
    return this.currentConversationId;
  }

  isInConversation(conversationId: number): boolean {
    return this.currentScreen === 'Conversation' && this.currentConversationId === conversationId;
  }

  clearConversation() {
    this.currentConversationId = null;
    console.log('📱🧭 Navigation: Conversation cleared');
  }
}

// Export singleton instance
export const navigationService = new NavigationService();
