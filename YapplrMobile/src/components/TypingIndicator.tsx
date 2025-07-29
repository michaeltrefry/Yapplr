import React, { useEffect, useState, useRef } from 'react';
import { View, Text, StyleSheet, Animated } from 'react-native';
import { useThemeColors } from '../hooks/useThemeColors';
import { useNotifications } from '../contexts/NotificationContext';

interface TypingIndicatorProps {
  conversationId: number;
}

interface TypingUser {
  userId: number;
  username: string;
  timestamp: Date;
}

export default function TypingIndicator({ conversationId }: TypingIndicatorProps) {
  const [typingUsers, setTypingUsers] = useState<TypingUser[]>([]);
  const colors = useThemeColors();
  const notificationContext = useNotifications();
  const { signalRService } = notificationContext;
  const animatedValues = useRef([
    new Animated.Value(0),
    new Animated.Value(0),
    new Animated.Value(0),
  ]).current;

  const styles = createStyles(colors);



  useEffect(() => {
    // Check if SignalR service is available
    if (!signalRService) {
      return;
    }

    const handleTypingEvent = (action: 'started' | 'stopped', data: any) => {
      if (data.conversationId !== conversationId) {
        return;
      }

      setTypingUsers(prev => {
        if (action === 'started') {
          // Add user to typing list if not already there
          const existingUser = prev.find(u => u.userId === data.userId);
          if (!existingUser) {
            const newUsers = [...prev, {
              userId: data.userId,
              username: data.username,
              timestamp: new Date(data.timestamp)
            }];
            return newUsers;
          }
          return prev;
        } else {
          // Remove user from typing list
          const newUsers = prev.filter(u => u.userId !== data.userId);
          return newUsers;
        }
      });
    };

    // Add typing listener
    signalRService.addTypingListener(handleTypingEvent);

    // Cleanup function
    return () => {
      if (signalRService) {
        signalRService.removeTypingListener(handleTypingEvent);
      }
    };
  }, [conversationId]);

  // Auto-cleanup typing indicators after 5 seconds of inactivity
  useEffect(() => {
    const interval = setInterval(() => {
      const now = new Date();
      setTypingUsers(prev => 
        prev.filter(user => {
          const timeDiff = now.getTime() - user.timestamp.getTime();
          return timeDiff < 5000; // Remove if older than 5 seconds
        })
      );
    }, 1000);

    return () => clearInterval(interval);
  }, []);

  // Animate dots when typing users change
  useEffect(() => {
    if (typingUsers.length > 0) {
      // Start animation
      const animations = animatedValues.map((value, index) =>
        Animated.loop(
          Animated.sequence([
            Animated.timing(value, {
              toValue: 1,
              duration: 600,
              delay: index * 200,
              useNativeDriver: true,
            }),
            Animated.timing(value, {
              toValue: 0,
              duration: 600,
              useNativeDriver: true,
            }),
          ])
        )
      );

      animations.forEach(animation => animation.start());

      return () => {
        animations.forEach(animation => animation.stop());
        animatedValues.forEach(value => value.setValue(0));
      };
    } else {
      // Stop animations
      animatedValues.forEach(value => value.setValue(0));
    }
  }, [typingUsers.length, animatedValues]);

  // Don't render if no users are typing
  if (typingUsers.length === 0) {
    return null;
  }

  const getTypingText = () => {
    if (typingUsers.length === 1) {
      return `@${typingUsers[0].username} is typing`;
    } else if (typingUsers.length === 2) {
      return `@${typingUsers[0].username} and @${typingUsers[1].username} are typing`;
    } else {
      return `@${typingUsers[0].username} and ${typingUsers.length - 1} others are typing`;
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.text}>{getTypingText()}</Text>
      <View style={styles.dotsContainer}>
        {animatedValues.map((animatedValue, index) => (
          <Animated.View
            key={index}
            style={[
              styles.dot,
              {
                opacity: animatedValue,
                transform: [
                  {
                    scale: animatedValue.interpolate({
                      inputRange: [0, 1],
                      outputRange: [0.8, 1.2],
                    }),
                  },
                ],
              },
            ]}
          />
        ))}
      </View>
    </View>
  );
}

const createStyles = (colors: any) => StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 8,
    backgroundColor: colors.background,
  },
  text: {
    fontSize: 14,
    color: colors.textSecondary,
    fontStyle: 'italic',
    marginRight: 8,
  },
  dotsContainer: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  dot: {
    width: 4,
    height: 4,
    borderRadius: 2,
    backgroundColor: colors.textSecondary,
    marginHorizontal: 1,
  },
});
