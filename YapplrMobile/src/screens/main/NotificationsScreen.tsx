import React, { useState, useCallback } from 'react';
import {
  View,
  Text,
  FlatList,
  TouchableOpacity,
  StyleSheet,
  SafeAreaView,
  RefreshControl,
  Alert,
  ActivityIndicator,
} from 'react-native';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { StackNavigationProp } from '@react-navigation/stack';
import { useAuth } from '../../contexts/AuthContext';
import { useThemeColors } from '../../hooks/useThemeColors';
import { Notification, NotificationType } from '../../types';
import { RootStackParamList } from '../../navigation/AppNavigator';
import { Ionicons } from '@expo/vector-icons';

type NotificationsScreenNavigationProp = StackNavigationProp<RootStackParamList, 'Notifications'>;

interface NotificationsScreenProps {
  navigation: NotificationsScreenNavigationProp;
}

const getNotificationIcon = (type: NotificationType): string => {
  switch (type) {
    case NotificationType.Mention:
      return 'at-outline';
    case NotificationType.Like:
      return 'heart';
    case NotificationType.Repost:
      return 'repeat';
    case NotificationType.Follow:
      return 'person-add';
    case NotificationType.Comment:
      return 'chatbubble-outline';
    case NotificationType.FollowRequest:
      return 'person-add-outline';
    // Moderation notifications
    case NotificationType.UserSuspended:
      return 'ban';
    case NotificationType.UserBanned:
      return 'ban';
    case NotificationType.UserUnsuspended:
      return 'checkmark-circle';
    case NotificationType.UserUnbanned:
      return 'checkmark-circle';
    case NotificationType.ContentHidden:
      return 'eye-off';
    case NotificationType.ContentDeleted:
      return 'trash';
    case NotificationType.ContentRestored:
      return 'refresh';
    case NotificationType.AppealApproved:
      return 'checkmark-circle';
    case NotificationType.AppealDenied:
      return 'close-circle';
    case NotificationType.SystemMessage:
      return 'information-circle';
    default:
      return 'notifications';
  }
};

const getNotificationIconColor = (type: NotificationType): string => {
  switch (type) {
    case NotificationType.Mention:
      return '#3B82F6';
    case NotificationType.Like:
      return '#EF4444';
    case NotificationType.Repost:
      return '#10B981';
    case NotificationType.Follow:
      return '#8B5CF6';
    case NotificationType.Comment:
      return '#3B82F6';
    case NotificationType.FollowRequest:
      return '#F59E0B';
    // Moderation notifications
    case NotificationType.UserSuspended:
      return '#F59E0B';
    case NotificationType.UserBanned:
      return '#EF4444';
    case NotificationType.UserUnsuspended:
      return '#10B981';
    case NotificationType.UserUnbanned:
      return '#10B981';
    case NotificationType.ContentHidden:
      return '#F59E0B';
    case NotificationType.ContentDeleted:
      return '#EF4444';
    case NotificationType.ContentRestored:
      return '#10B981';
    case NotificationType.AppealApproved:
      return '#10B981';
    case NotificationType.AppealDenied:
      return '#EF4444';
    case NotificationType.SystemMessage:
      return '#3B82F6';
    default:
      return '#6B7280';
  }
};

const formatTimeAgo = (dateString: string): string => {
  const date = new Date(dateString);
  const now = new Date();
  const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);

  if (diffInSeconds < 60) return 'just now';
  if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m`;
  if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h`;
  if (diffInSeconds < 604800) return `${Math.floor(diffInSeconds / 86400)}d`;
  return date.toLocaleDateString();
};

export default function NotificationsScreen({ navigation }: NotificationsScreenProps) {
  const { api, user } = useAuth();
  const colors = useThemeColors();
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const [refreshing, setRefreshing] = useState(false);
  const pageSize = 25;

  const styles = createStyles(colors);

  const { data: notificationData, isLoading, error, refetch } = useQuery({
    queryKey: ['notifications', page],
    queryFn: () => api.notifications.getNotifications(page, pageSize),
    enabled: !!user,
  });

  const markAsReadMutation = useMutation({
    mutationFn: api.notifications.markAsRead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['notificationUnreadCount'] });
    },
  });

  const markAllAsReadMutation = useMutation({
    mutationFn: api.notifications.markAllAsRead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['notificationUnreadCount'] });
    },
  });

  const handleRefresh = useCallback(async () => {
    setRefreshing(true);
    try {
      await refetch();
    } finally {
      setRefreshing(false);
    }
  }, [refetch]);

  const handleNotificationPress = async (notification: Notification) => {
    if (!notification.isRead) {
      await markAsReadMutation.mutateAsync(notification.id);
    }

    // Navigate to the relevant content based on notification type
    if (notification.type === NotificationType.Mention && notification.mention) {
      if (notification.mention.commentId && notification.post) {
        // Mention in comment - go to post comments
        navigation.navigate('Comments', { post: notification.post });
      } else if (notification.mention.postId && notification.post) {
        // Mention in post - go to post comments
        navigation.navigate('Comments', { post: notification.post });
      }
    } else if (notification.post) {
      // Other post-related notifications (likes, reposts, comments)
      navigation.navigate('Comments', { post: notification.post });
    } else if (notification.actorUser) {
      // Follow notifications - go to user profile
      navigation.navigate('UserProfile', { username: notification.actorUser.username });
    } else if (notification.type >= 100 && notification.type <= 109) {
      // Moderation notifications - navigate to the moderated content if available
      if (notification.post) {
        navigation.navigate('Comments', { post: notification.post });
      }
      // For other moderation notifications, just mark as read (already done above)
    }
  };

  const handleMarkAllAsRead = () => {
    if (notificationData && notificationData.unreadCount > 0) {
      Alert.alert(
        'Mark All as Read',
        `Mark all ${notificationData.unreadCount} notifications as read?`,
        [
          { text: 'Cancel', style: 'cancel' },
          { text: 'Mark All', onPress: () => markAllAsReadMutation.mutate() },
        ]
      );
    }
  };

  const renderNotificationItem = ({ item }: { item: Notification }) => {
    const isModerationNotification = item.type >= 100 && item.type <= 109;
    const isNegativeModeration = [100, 101, 104, 105, 108].includes(item.type);
    const isPositiveModeration = [102, 103, 106, 107].includes(item.type);

    return (
      <TouchableOpacity
        style={[
          styles.notificationItem,
          !item.isRead && styles.unreadNotification,
          isModerationNotification && !item.isRead && (
            isNegativeModeration ? styles.negativeModeration :
            isPositiveModeration ? styles.positiveModeration :
            styles.neutralModeration
          )
        ]}
        onPress={() => handleNotificationPress(item)}
        activeOpacity={0.7}
      >
        <View style={styles.notificationContent}>
          <View style={styles.iconContainer}>
            <Ionicons
              name={getNotificationIcon(item.type) as any}
              size={20}
              color={getNotificationIconColor(item.type)}
            />
          </View>
          <View style={styles.textContainer}>
            <Text style={styles.notificationMessage}>{item.message}</Text>
            <Text style={styles.notificationTime}>{formatTimeAgo(item.createdAt)}</Text>
            {item.post && (
              <View style={styles.contentPreview}>
                <Text style={styles.contentPreviewText} numberOfLines={2}>
                  {item.post.content}
                </Text>
              </View>
            )}
            {item.comment && (
              <View style={styles.contentPreview}>
                <Text style={styles.contentPreviewText} numberOfLines={2}>
                  {item.comment.content}
                </Text>
              </View>
            )}
          </View>
        </View>
      </TouchableOpacity>
    );
  };

  if (isLoading && !notificationData) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.header}>
          <TouchableOpacity
            style={styles.backButton}
            onPress={() => navigation.goBack()}
          >
            <Ionicons name="arrow-back" size={24} color={colors.text} />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>Notifications</Text>
          <View style={styles.headerRight} />
        </View>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={colors.primary} />
          <Text style={styles.loadingText}>Loading notifications...</Text>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity
          style={styles.backButton}
          onPress={() => navigation.goBack()}
        >
          <Ionicons name="arrow-back" size={24} color={colors.text} />
        </TouchableOpacity>
        <Text style={styles.headerTitle}>Notifications</Text>
        <View style={styles.headerRight}>
          {notificationData && notificationData.unreadCount > 0 && (
            <TouchableOpacity
              style={styles.markAllButton}
              onPress={handleMarkAllAsRead}
              disabled={markAllAsReadMutation.isPending}
            >
              <Text style={styles.markAllButtonText}>Mark All</Text>
            </TouchableOpacity>
          )}
        </View>
      </View>

      {error ? (
        <View style={styles.errorContainer}>
          <Ionicons name="alert-circle" size={48} color={colors.error} />
          <Text style={styles.errorText}>Failed to load notifications</Text>
          <TouchableOpacity style={styles.retryButton} onPress={() => refetch()}>
            <Text style={styles.retryButtonText}>Retry</Text>
          </TouchableOpacity>
        </View>
      ) : !notificationData?.notifications.length ? (
        <View style={styles.emptyContainer}>
          <Ionicons name="notifications-outline" size={64} color={colors.textMuted} />
          <Text style={styles.emptyTitle}>No notifications yet</Text>
          <Text style={styles.emptySubtext}>
            When someone mentions you, likes your posts, or follows you, you'll see it here.
          </Text>
        </View>
      ) : (
        <FlatList
          data={notificationData.notifications}
          renderItem={renderNotificationItem}
          keyExtractor={(item) => item.id.toString()}
          contentContainerStyle={styles.listContainer}
          showsVerticalScrollIndicator={false}
          refreshControl={
            <RefreshControl refreshing={refreshing} onRefresh={handleRefresh} />
          }
        />
      )}
    </SafeAreaView>
  );
}

const createStyles = (colors: any) => StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  backButton: {
    padding: 8,
    marginLeft: -8,
  },
  headerTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: colors.text,
    flex: 1,
    textAlign: 'center',
  },
  headerRight: {
    width: 80,
    alignItems: 'flex-end',
  },
  markAllButton: {
    paddingHorizontal: 12,
    paddingVertical: 6,
    backgroundColor: colors.primary,
    borderRadius: 16,
  },
  markAllButtonText: {
    color: colors.background,
    fontSize: 12,
    fontWeight: '600',
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingText: {
    marginTop: 12,
    color: colors.textMuted,
    fontSize: 16,
  },
  errorContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 32,
  },
  errorText: {
    marginTop: 16,
    color: colors.error,
    fontSize: 16,
    textAlign: 'center',
  },
  retryButton: {
    marginTop: 16,
    paddingHorizontal: 24,
    paddingVertical: 12,
    backgroundColor: colors.primary,
    borderRadius: 8,
  },
  retryButtonText: {
    color: colors.background,
    fontSize: 16,
    fontWeight: '600',
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 32,
  },
  emptyTitle: {
    marginTop: 16,
    fontSize: 18,
    fontWeight: '600',
    color: colors.text,
    textAlign: 'center',
  },
  emptySubtext: {
    marginTop: 8,
    fontSize: 14,
    color: colors.textMuted,
    textAlign: 'center',
    lineHeight: 20,
  },
  listContainer: {
    paddingBottom: 20,
  },
  notificationItem: {
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  unreadNotification: {
    backgroundColor: colors.surface,
  },
  negativeModeration: {
    backgroundColor: '#FEF2F2',
    borderLeftWidth: 4,
    borderLeftColor: '#EF4444',
  },
  positiveModeration: {
    backgroundColor: '#F0FDF4',
    borderLeftWidth: 4,
    borderLeftColor: '#10B981',
  },
  neutralModeration: {
    backgroundColor: '#EFF6FF',
    borderLeftWidth: 4,
    borderLeftColor: '#3B82F6',
  },
  notificationContent: {
    flexDirection: 'row',
    alignItems: 'flex-start',
  },
  iconContainer: {
    marginRight: 12,
    marginTop: 2,
  },
  textContainer: {
    flex: 1,
  },
  notificationMessage: {
    fontSize: 14,
    color: colors.text,
    lineHeight: 20,
  },
  notificationTime: {
    fontSize: 12,
    color: colors.textMuted,
    marginTop: 4,
  },
  contentPreview: {
    marginTop: 8,
    padding: 8,
    backgroundColor: colors.surface,
    borderRadius: 6,
  },
  contentPreviewText: {
    fontSize: 12,
    color: colors.textMuted,
    lineHeight: 16,
  },
});
