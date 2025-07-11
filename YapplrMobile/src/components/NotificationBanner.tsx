import React, { useState, useEffect, useRef } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  Animated,
  Dimensions,
  SafeAreaView,
} from 'react-native';
import { PanGestureHandler, State } from 'react-native-gesture-handler';
import { useThemeColors } from '../hooks/useThemeColors';
import { SignalRNotificationPayload } from '../services/SignalRService';

interface NotificationBannerProps {
  notification: SignalRNotificationPayload | null;
  onPress?: () => void;
  onDismiss?: () => void;
  onMarkAsRead?: () => void;
  onReply?: () => void;
  duration?: number; // Auto-dismiss duration in ms
}

const { width: screenWidth } = Dimensions.get('window');

function NotificationBanner({
  notification,
  onPress,
  onDismiss,
  onMarkAsRead,
  onReply,
  duration = 4000,
}: NotificationBannerProps) {
  const colors = useThemeColors();

  const [slideAnim] = useState(new Animated.Value(-100));
  const [isVisible, setIsVisible] = useState(false);
  const [swipeAnim] = useState(new Animated.Value(0));
  const [showActions, setShowActions] = useState(false);
  const gestureRef = useRef(null);

  const styles = createStyles(colors);

  // Handle swipe gestures
  const onGestureEvent = Animated.event(
    [{ nativeEvent: { translationX: swipeAnim } }],
    { useNativeDriver: true }
  );

  const onHandlerStateChange = (event: any) => {
    if (event.nativeEvent.state === State.END) {
      const { translationX, velocityX } = event.nativeEvent;

      // Determine swipe direction and distance
      const swipeThreshold = 50;
      const velocityThreshold = 500;

      if (translationX > swipeThreshold || velocityX > velocityThreshold) {
        // Swipe right - Mark as Read
        if (onMarkAsRead) {
          onMarkAsRead();
          handleDismiss();
        } else {
          resetSwipe();
        }
      } else if (translationX < -swipeThreshold || velocityX < -velocityThreshold) {
        // Swipe left - Reply (for message notifications)
        if (onReply && notification?.type === 'message') {
          onReply();
          handleDismiss();
        } else {
          resetSwipe();
        }
      } else {
        resetSwipe();
      }
    }
  };

  const resetSwipe = () => {
    Animated.spring(swipeAnim, {
      toValue: 0,
      useNativeDriver: true,
    }).start();
  };

  // Show/hide animation
  useEffect(() => {
    if (notification) {
      setIsVisible(true);
      // Slide in
      Animated.timing(slideAnim, {
        toValue: 0,
        duration: 300,
        useNativeDriver: true,
      }).start();

      // Auto-dismiss after duration
      const timer = setTimeout(() => {
        handleDismiss();
      }, duration);

      return () => clearTimeout(timer);
    }
  }, [notification, duration]);

  const handleDismiss = () => {
    // Slide out
    Animated.timing(slideAnim, {
      toValue: -100,
      duration: 300,
      useNativeDriver: true,
    }).start(() => {
      setIsVisible(false);
      onDismiss?.();
    });
  };

  const handlePress = () => {
    handleDismiss();
    onPress?.();
  };

  const getNotificationIcon = (type: string) => {
    switch (type) {
      case 'message':
        return '💬';
      case 'mention':
        return '@';
      case 'comment':
        return '💭';
      case 'like':
        return '❤️';
      case 'follow':
        return '👤';
      case 'repost':
        return '🔄';
      default:
        return '🔔';
    }
  };

  const getNotificationColor = (type: string) => {
    switch (type) {
      case 'message':
        return colors.primary;
      case 'mention':
        return '#FF6B35';
      case 'comment':
        return '#4ECDC4';
      case 'like':
        return '#E74C3C';
      case 'follow':
        return '#9B59B6';
      case 'repost':
        return '#2ECC71';
      default:
        return colors.primary;
    }
  };

  if (!notification || !isVisible) {
    return null;
  }

  return (
    <SafeAreaView style={styles.container} pointerEvents="box-none">
      <PanGestureHandler
        ref={gestureRef}
        onGestureEvent={onGestureEvent}
        onHandlerStateChange={onHandlerStateChange}
        activeOffsetX={[-10, 10]}
      >
        <Animated.View
          style={[
            styles.banner,
            {
              transform: [
                { translateY: slideAnim },
                { translateX: swipeAnim },
              ],
            },
          ]}
        >
          <TouchableOpacity
            style={styles.content}
            onPress={handlePress}
            activeOpacity={0.8}
          >
            <View style={styles.iconContainer}>
              <Text style={[styles.iconText, { color: getNotificationColor(notification.type) }]}>
                {getNotificationIcon(notification.type)}
              </Text>
            </View>
            <View style={styles.textContainer}>
              <Text style={styles.title} numberOfLines={1}>
                {notification.title}
              </Text>
              <Text style={styles.body} numberOfLines={2}>
                {notification.body}
              </Text>
            </View>
            <TouchableOpacity
              style={styles.dismissButton}
              onPress={handleDismiss}
              hitSlop={{ top: 10, bottom: 10, left: 10, right: 10 }}
            >
              <Text style={[styles.closeIcon, { color: colors.textMuted }]}>✕</Text>
            </TouchableOpacity>
          </TouchableOpacity>
        </Animated.View>
      </PanGestureHandler>
    </SafeAreaView>
  );
}

export default NotificationBanner;

const createStyles = (colors: any) =>
  StyleSheet.create({
    container: {
      position: 'absolute',
      top: 0,
      left: 0,
      right: 0,
      zIndex: 1000,
      pointerEvents: 'box-none',
    },
    banner: {
      marginHorizontal: 16,
      marginTop: 8,
      backgroundColor: colors.card,
      borderRadius: 12,
      shadowColor: '#000',
      shadowOffset: {
        width: 0,
        height: 2,
      },
      shadowOpacity: 0.25,
      shadowRadius: 3.84,
      elevation: 5,
      borderWidth: 1,
      borderColor: colors.border,
    },
    content: {
      flexDirection: 'row',
      alignItems: 'center',
      padding: 16,
    },
    iconContainer: {
      marginRight: 12,
      width: 40,
      height: 40,
      borderRadius: 20,
      backgroundColor: colors.background,
      justifyContent: 'center',
      alignItems: 'center',
    },
    textContainer: {
      flex: 1,
      marginRight: 8,
    },
    title: {
      fontSize: 16,
      fontWeight: '600',
      color: colors.text,
      marginBottom: 2,
    },
    body: {
      fontSize: 14,
      color: colors.textMuted,
      lineHeight: 18,
    },
    dismissButton: {
      padding: 4,
    },
    iconText: {
      fontSize: 20,
      fontWeight: '600',
    },
    closeIcon: {
      fontSize: 16,
      fontWeight: 'bold',
    },
  });
