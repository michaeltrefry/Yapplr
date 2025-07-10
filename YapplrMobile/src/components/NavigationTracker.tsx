import { useEffect } from 'react';
import { useNavigationState } from '@react-navigation/native';

// Navigation context for smart notification handling
interface NavigationContext {
  currentScreen?: string;
  currentRoute?: any;
  conversationId?: number;
  postId?: number;
  otherUserId?: number;
}

/**
 * NavigationTracker component that tracks the current navigation state
 * and updates the global navigation context for smart notification handling.
 * 
 * This component should be placed inside the NavigationContainer.
 */
export default function NavigationTracker() {
  // Get current navigation state
  const navigationState = useNavigationState(state => state);
  
  // Update navigation context when navigation state changes
  useEffect(() => {
    if (!navigationState) return;
    
    // Extract current screen and route info
    const getCurrentRoute = (state: any): any => {
      if (!state.routes || state.routes.length === 0) return null;
      const route = state.routes[state.index];
      if (route.state) {
        return getCurrentRoute(route.state);
      }
      return route;
    };
    
    const currentRoute = getCurrentRoute(navigationState);
    if (currentRoute) {
      const context: NavigationContext = {
        currentScreen: currentRoute.name,
        currentRoute: currentRoute,
        conversationId: currentRoute.params?.conversationId,
        postId: currentRoute.params?.post?.id,
        otherUserId: currentRoute.params?.otherUser?.id,
      };
      
      // Update global navigation context
      if ((global as any).setNavigationContext) {
        (global as any).setNavigationContext(context);
      }
    }
  }, [navigationState]);

  // This component doesn't render anything
  return null;
}
