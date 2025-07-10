import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  Animated,
  Dimensions,
  SafeAreaView,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useThemeColors } from '../hooks/useThemeColors';
import { SignalRNotificationPayload } from '../services/SignalRService';

interface NotificationBannerProps {
  notification: SignalRNotificationPayload | null;
  onPress?: () => void;
  onDismiss?: () => void;
  duration?: number; // Auto-dismiss duration in ms
}

const { width: screenWidth } = Dimensions.get('window');

export default function NotificationBanner({
  notification,
  onPress,
  onDismiss,
  duration = 4000,
}: NotificationBannerProps) {
  const colors = useThemeColors();
  const [slideAnim] = useState(new Animated.Value(-100));
  const [isVisible, setIsVisible] = useState(false);

  const styles = createStyles(colors);

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
        return 'chatbubble';
      case 'mention':
        return 'at';
      case 'comment':
        return 'chatbubble-outline';
      case 'like':
        return 'heart';
      case 'follow':
        return 'person-add';
      case 'repost':
        return 'repeat';
      default:
        return 'notifications';
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
      <Animated.View
        style={[
          styles.banner,
          {
            transform: [{ translateY: slideAnim }],
          },
        ]}
      >
        <TouchableOpacity
          style={styles.content}
          onPress={handlePress}
          activeOpacity={0.8}
        >
          <View style={styles.iconContainer}>
            <Ionicons
              name={getNotificationIcon(notification.type) as any}
              size={24}
              color={getNotificationColor(notification.type)}
            />
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
            <Ionicons name="close" size={20} color={colors.textMuted} />
          </TouchableOpacity>
        </TouchableOpacity>
      </Animated.View>
    </SafeAreaView>
  );
}

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
  });
