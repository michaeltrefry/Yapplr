import { Vibration, Platform } from 'react-native';
import { SignalRNotificationPayload } from './SignalRService';

// Safely import Haptics with fallback
let Haptics: any = null;
try {
  Haptics = require('expo-haptics');
} catch (error) {
  console.warn('📱🔔 expo-haptics not available, using fallback');
}

// Safely import Audio with fallback
let Audio: any = null;
try {
  Audio = require('expo-av');
} catch (error) {
  console.warn('📱🔔 expo-av not available, using fallback');
}

export interface FeedbackPattern {
  haptic?: 'light' | 'medium' | 'heavy' | 'success' | 'warning' | 'error';
  vibration?: number[]; // Pattern for Android vibration
  sound?: boolean;
}

class NotificationFeedbackService {
  private isEnabled = true;
  private soundEnabled = true;
  private hapticEnabled = true;

  /**
   * Enable or disable all feedback
   */
  setEnabled(enabled: boolean) {
    this.isEnabled = enabled;
  }

  /**
   * Enable or disable sound feedback
   */
  setSoundEnabled(enabled: boolean) {
    this.soundEnabled = enabled;
  }

  /**
   * Enable or disable haptic feedback
   */
  setHapticEnabled(enabled: boolean) {
    this.hapticEnabled = enabled;
  }

  /**
   * Provide feedback for a notification
   */
  async provideFeedback(notification: SignalRNotificationPayload): Promise<void> {
    if (!this.isEnabled) {
      return;
    }

    const pattern = this.getFeedbackPattern(notification.type);
    console.log('📱🔔 Providing feedback for notification:', notification.type, pattern);

    // Execute feedback in parallel
    const feedbackPromises: Promise<void>[] = [];

    if (this.hapticEnabled && pattern.haptic) {
      feedbackPromises.push(this.triggerHaptic(pattern.haptic));
    }

    if (this.hapticEnabled && pattern.vibration) {
      feedbackPromises.push(this.triggerVibration(pattern.vibration));
    }

    if (this.soundEnabled && pattern.sound) {
      feedbackPromises.push(this.playNotificationSound(notification.type));
    }

    try {
      await Promise.all(feedbackPromises);
    } catch (error) {
      console.warn('📱🔔 Error providing notification feedback:', error);
    }
  }

  /**
   * Get feedback pattern for notification type
   */
  private getFeedbackPattern(type: string): FeedbackPattern {
    switch (type) {
      case 'message':
        return {
          haptic: 'medium',
          vibration: [0, 100, 50, 100], // Double tap pattern
          sound: true,
        };
      
      case 'mention':
        return {
          haptic: 'heavy',
          vibration: [0, 200], // Single strong vibration
          sound: true,
        };
      
      case 'like':
        return {
          haptic: 'light',
          vibration: [0, 50], // Light tap
          sound: false, // Likes are less urgent
        };
      
      case 'comment':
        return {
          haptic: 'medium',
          vibration: [0, 100],
          sound: true,
        };
      
      case 'follow':
      case 'follow_request':
        return {
          haptic: 'success',
          vibration: [0, 100, 100, 100], // Triple tap
          sound: true,
        };
      
      case 'repost':
        return {
          haptic: 'light',
          vibration: [0, 75],
          sound: false,
        };
      
      default:
        return {
          haptic: 'light',
          vibration: [0, 100],
          sound: false,
        };
    }
  }

  /**
   * Trigger haptic feedback using Expo Haptics
   */
  private async triggerHaptic(type: string): Promise<void> {
    if (!Haptics) {
      console.log('📱🔔 Haptics not available, skipping haptic feedback');
      return;
    }

    try {
      switch (type) {
        case 'light':
          await Haptics.impactAsync(Haptics.ImpactFeedbackStyle?.Light || 0);
          break;
        case 'medium':
          await Haptics.impactAsync(Haptics.ImpactFeedbackStyle?.Medium || 1);
          break;
        case 'heavy':
          await Haptics.impactAsync(Haptics.ImpactFeedbackStyle?.Heavy || 2);
          break;
        case 'success':
          await Haptics.notificationAsync(Haptics.NotificationFeedbackType?.Success || 0);
          break;
        case 'warning':
          await Haptics.notificationAsync(Haptics.NotificationFeedbackType?.Warning || 1);
          break;
        case 'error':
          await Haptics.notificationAsync(Haptics.NotificationFeedbackType?.Error || 2);
          break;
        default:
          await Haptics.impactAsync(Haptics.ImpactFeedbackStyle?.Light || 0);
      }
      console.log('📱🔔 Haptic feedback triggered:', type);
    } catch (error) {
      console.warn('📱🔔 Error triggering haptic feedback:', error);
    }
  }

  /**
   * Trigger vibration using React Native Vibration
   */
  private async triggerVibration(pattern: number[]): Promise<void> {
    try {
      if (Platform.OS === 'android') {
        Vibration.vibrate(pattern);
      } else {
        // iOS doesn't support patterns, use simple vibration
        Vibration.vibrate();
      }
      console.log('📱🔔 Vibration triggered:', pattern);
    } catch (error) {
      console.warn('📱🔔 Error triggering vibration:', error);
    }
  }

  /**
   * Play notification sound
   */
  private async playNotificationSound(type: string): Promise<void> {
    // For now, we'll use the system notification sound
    // In the future, we could load custom sounds for different notification types
    console.log('📱🔔 Playing notification sound for type:', type);
    
    // The sound will be handled by the system notification if the app is in background
    // For foreground notifications, we could implement custom sounds here
    // TODO: Implement custom sound loading and playback using expo-av
  }

  /**
   * Provide feedback for swipe actions
   */
  async provideSwipeFeedback(action: 'markAsRead' | 'reply'): Promise<void> {
    if (!this.isEnabled || !this.hapticEnabled) {
      return;
    }

    try {
      switch (action) {
        case 'markAsRead':
          await this.triggerHaptic('success');
          break;
        case 'reply':
          await this.triggerHaptic('medium');
          break;
      }
    } catch (error) {
      console.warn('📱🔔 Error providing swipe feedback:', error);
    }
  }
}

// Export singleton instance
export default new NotificationFeedbackService();
