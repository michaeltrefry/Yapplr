import React, { useState, useEffect, useMemo } from 'react';
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  RefreshControl,
  TouchableOpacity,
  SafeAreaView,
  Alert,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { Ionicons } from '@expo/vector-icons';
import { StackNavigationProp } from '@react-navigation/stack';
import { useAuth } from '../../contexts/AuthContext';
import { useThemeColors } from '../../hooks/useThemeColors';
import { TimelineItem, Post } from '../../types';

import PostCard from '../../components/PostCard';
import { NotificationStatusIndicator } from '../../components/NotificationStatus';
import { RootStackParamList } from '../../navigation/AppNavigator';

type HomeScreenNavigationProp = StackNavigationProp<RootStackParamList, 'MainTabs'>;

export default function HomeScreen({ navigation }: { navigation: HomeScreenNavigationProp }) {
  const { api, user } = useAuth();
  const colors = useThemeColors();
  const [refreshing, setRefreshing] = useState(false);

  const [commentCountUpdates, setCommentCountUpdates] = useState<Record<number, number>>({});

  const styles = createStyles(colors);

  const {
    data: timeline,
    isLoading,
    refetch,
    error,
  } = useQuery({
    queryKey: ['timeline'],
    queryFn: async () => {
      const result = await api.posts.getTimeline(1, 25);
      console.log('Timeline loaded:', result.length, 'items');
      // Log comment counts for debugging
      result.forEach(item => {
        console.log(`Post ${item.post.id} has ${item.post.commentCount} comments`);
      });
      const postsWithImages = result.filter(item => item.post.imageUrl);
      console.log('Posts with images:', postsWithImages.length);
      postsWithImages.forEach(item => {
        console.log('Post with image:', {
          id: item.post.id,
          content: item.post.content.substring(0, 50) + '...',
          imageUrl: item.post.imageUrl
        });
      });
      return result;
    },
    enabled: !!user,
    retry: 2,
  });

  const { data: notificationUnreadCount } = useQuery({
    queryKey: ['notificationUnreadCount'],
    queryFn: () => api.notifications.getUnreadCount(),
    enabled: !!user,
    refetchInterval: 30000, // Refresh every 30 seconds
  });

  const onRefresh = async () => {
    setRefreshing(true);
    await refetch();
    setRefreshing(false);
  };

  const handleLikePost = async (postId: number) => {
    try {
      await api.posts.likePost(postId);
      refetch(); // Refresh timeline to show updated like count
    } catch (error) {
      Alert.alert('Error', 'Failed to like post');
    }
  };

  const handleRepost = async (postId: number) => {
    try {
      await api.posts.repostPost(postId);
      refetch(); // Refresh timeline to show updated repost count
    } catch (error) {
      Alert.alert('Error', 'Failed to repost');
    }
  };

  const handleUserPress = (username: string) => {
    navigation.navigate('UserProfile', { username });
  };

  const handleCommentCountUpdate = (postId: number, newCount: number) => {
    console.log(`Updating comment count for post ${postId} to ${newCount}`);
    setCommentCountUpdates(prev => ({
      ...prev,
      [postId]: newCount
    }));
    console.log(`Updated comment count for post ${postId} to ${newCount}`);
  };

  const handleCommentPress = (post: Post) => {
    navigation.navigate('Comments', {
      post,
      onCommentCountUpdate: handleCommentCountUpdate
    });
  };

  // Merge timeline data with local comment count updates
  const timelineWithUpdates = useMemo(() => {
    if (!timeline) return [];

    return timeline.map(item => {
      const updatedCount = commentCountUpdates[item.post.id];
      if (updatedCount !== undefined) {
        return {
          ...item,
          post: {
            ...item.post,
            commentCount: updatedCount
          }
        };
      }
      return item;
    });
  }, [timeline, commentCountUpdates]);

  const handleDelete = () => {
    refetch(); // Refresh timeline after deletion
  };

  const handleUnrepost = () => {
    refetch(); // Refresh timeline after unrepost
  };

  const renderTimelineItem = ({ item }: { item: TimelineItem }) => {
    // Debug: Log posts with images
    if (item.post.imageUrl) {
      console.log('Rendering post with image:', {
        postId: item.post.id,
        imageUrl: item.post.imageUrl
      });
    }

    return (
      <PostCard
        item={item}
        onLike={handleLikePost}
        onRepost={handleRepost}
        onUserPress={handleUserPress}
        onCommentPress={handleCommentPress}
        onCommentCountUpdate={handleCommentCountUpdate}
        onDelete={handleDelete}
        onUnrepost={handleUnrepost}
      />
    );
  };

  if (isLoading) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.loadingContainer}>
          <Text>Loading timeline...</Text>
        </View>
      </SafeAreaView>
    );
  }

  if (error) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.loadingContainer}>
          <Text style={styles.errorText}>Failed to load timeline</Text>
          <TouchableOpacity onPress={() => refetch()} style={styles.retryButton}>
            <Text style={styles.retryText}>Retry</Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <View style={styles.headerLeft} />
        <Text style={styles.headerTitle}>Yapplr</Text>
        <View style={styles.headerRight}>
          <TouchableOpacity
            style={styles.notificationButton}
            onPress={() => navigation.navigate('Notifications')}
          >
            <Ionicons name="notifications-outline" size={24} color={colors.text} />
            {notificationUnreadCount && notificationUnreadCount.unreadCount > 0 && (
              <View style={styles.notificationBadge}>
                <Text style={styles.notificationBadgeText}>
                  {notificationUnreadCount.unreadCount > 99 ? '99+' : notificationUnreadCount.unreadCount}
                </Text>
              </View>
            )}
          </TouchableOpacity>
        </View>
      </View>

      <FlatList
        data={timelineWithUpdates}
        renderItem={renderTimelineItem}
        keyExtractor={(item) => `${item.type}-${item.post.id}-${item.createdAt}`}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
        contentContainerStyle={styles.listContainer}
        showsVerticalScrollIndicator={false}
      />

      <NotificationStatusIndicator />
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
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  headerLeft: {
    width: 40,
  },
  headerTitle: {
    fontSize: 24,
    fontWeight: 'bold',
    color: colors.primary,
    textAlign: 'center',
    flex: 1,
  },
  headerRight: {
    width: 40,
    alignItems: 'flex-end',
  },
  notificationButton: {
    padding: 8,
    position: 'relative',
  },
  notificationBadge: {
    position: 'absolute',
    top: 4,
    right: 4,
    backgroundColor: colors.error,
    borderRadius: 10,
    minWidth: 20,
    height: 20,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 4,
  },
  notificationBadgeText: {
    color: colors.background,
    fontSize: 12,
    fontWeight: 'bold',
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  listContainer: {
    paddingBottom: 20,
  },
  errorText: {
    fontSize: 16,
    color: colors.error,
    marginBottom: 16,
  },
  retryButton: {
    backgroundColor: colors.primary,
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 8,
  },
  retryText: {
    color: colors.primaryText,
    fontWeight: '600',
  },
});
