import React, { useState } from 'react';
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  SafeAreaView,
  TouchableOpacity,
  RefreshControl,
  ActivityIndicator,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { Ionicons } from '@expo/vector-icons';
import { StackNavigationProp } from '@react-navigation/stack';
import { RouteProp } from '@react-navigation/native';
import { useAuth } from '../../contexts/AuthContext';
import { useThemeColors } from '../../hooks/useThemeColors';
import { Post, TimelineItem } from '../../types';
import PostCard from '../../components/PostCard';
import { RootStackParamList } from '../../navigation/AppNavigator';

type HashtagFeedScreenNavigationProp = StackNavigationProp<RootStackParamList, 'HashtagFeed'>;
type HashtagFeedScreenRouteProp = RouteProp<RootStackParamList, 'HashtagFeed'>;

interface Props {
  navigation: HashtagFeedScreenNavigationProp;
  route: HashtagFeedScreenRouteProp;
}

export default function HashtagFeedScreen({ navigation, route }: Props) {
  const { hashtag } = route.params;
  const { api } = useAuth();
  const colors = useThemeColors();
  const [refreshing, setRefreshing] = useState(false);

  const styles = createStyles(colors);

  const { data: posts, isLoading, error, refetch } = useQuery({
    queryKey: ['hashtag-posts', hashtag],
    queryFn: () => api.tags.getPostsByTag(hashtag),
    retry: 1,
  });

  const { data: hashtagInfo } = useQuery({
    queryKey: ['hashtag-info', hashtag],
    queryFn: () => api.tags.getTagByName(hashtag),
    retry: 1,
  });

  const handleRefresh = async () => {
    setRefreshing(true);
    await refetch();
    setRefreshing(false);
  };

  const renderPost = ({ item }: { item: Post }) => {
    // Convert Post to TimelineItem format that PostCard expects
    const timelineItem: TimelineItem = {
      type: 'post',
      createdAt: item.createdAt,
      post: item,
      repostedBy: undefined
    };

    return (
      <PostCard
        item={timelineItem}
        onReact={(postId: number, reactionType: any) => {
          // Handle reaction logic if needed
        }}
        onRemoveReaction={(postId: number) => {
          // Handle remove reaction logic if needed
        }}
        onUserPress={(username: string) => {
          navigation.navigate('UserProfile', { username });
        }}
        onCommentPress={(post: Post) => {
          navigation.navigate('SinglePost', {
            postId: post.id,
            showComments: true
          });
        }}
        onHashtagPress={(hashtag: string) => {
          navigation.navigate('HashtagFeed', { hashtag });
        }}
      />
    );
  };

  const renderHeader = () => (
    <View style={styles.headerInfo}>
      <View style={styles.hashtagHeader}>
        <View style={styles.hashtagIcon}>
          <Ionicons name="pricetag" size={24} color="#fff" />
        </View>
        <View style={styles.hashtagDetails}>
          <Text style={styles.hashtagName}>#{hashtag}</Text>
          {hashtagInfo && (
            <Text style={styles.hashtagStats}>
              {hashtagInfo.postCount || 0} {(hashtagInfo.postCount || 0) === 1 ? 'post' : 'posts'}
            </Text>
          )}
        </View>
      </View>
      
      {posts && posts.length > 0 && (
        <View style={styles.feedHeader}>
          <Text style={styles.feedTitle}>Recent Posts</Text>
          <Text style={styles.feedSubtitle}>
            {posts.length} {posts.length === 1 ? 'post' : 'posts'} found
          </Text>
        </View>
      )}
    </View>
  );

  const renderEmptyState = () => (
    <View style={styles.emptyContainer}>
      <Ionicons name="pricetag-outline" size={64} color="#ccc" />
      <Text style={styles.emptyTitle}>No posts found</Text>
      <Text style={styles.emptyText}>
        Be the first to post with #{hashtag}!
      </Text>
      <TouchableOpacity
        style={styles.createPostButton}
        onPress={() => navigation.navigate('CreatePost')}
      >
        <Text style={styles.createPostText}>Create Post</Text>
      </TouchableOpacity>
    </View>
  );

  if (error) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.header}>
          <TouchableOpacity
            style={styles.backButton}
            onPress={() => navigation.goBack()}
          >
            <Ionicons name="arrow-back" size={24} color={colors.text} />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>#{hashtag}</Text>
          <View style={styles.headerRight} />
        </View>
        <View style={styles.errorContainer}>
          <Ionicons name="alert-circle" size={64} color="#ccc" />
          <Text style={styles.errorTitle}>Unable to load posts</Text>
          <Text style={styles.errorText}>
            There was an error loading posts for #{hashtag}.
          </Text>
          <TouchableOpacity style={styles.retryButton} onPress={handleRefresh}>
            <Text style={styles.retryButtonText}>Try Again</Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <TouchableOpacity
          style={styles.backButton}
          onPress={() => navigation.goBack()}
        >
          <Ionicons name="arrow-back" size={24} color={colors.text} />
        </TouchableOpacity>
        <Text style={styles.headerTitle}>#{hashtag}</Text>
        <View style={styles.headerRight} />
      </View>

      {/* Content */}
      {isLoading ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={colors.primary} />
          <Text style={styles.loadingText}>Loading posts...</Text>
        </View>
      ) : (
        <FlatList
          data={posts}
          renderItem={renderPost}
          keyExtractor={(item) => item.id.toString()}
          ListHeaderComponent={renderHeader}
          ListEmptyComponent={renderEmptyState}
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={handleRefresh}
              colors={[colors.primary]}
            />
          }
          contentContainerStyle={posts?.length === 0 ? styles.emptyListContainer : undefined}
          showsVerticalScrollIndicator={false}
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
    paddingHorizontal: 16,
    paddingVertical: 12,
    backgroundColor: colors.card,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  backButton: {
    padding: 8,
    marginRight: 8,
  },
  headerTitle: {
    flex: 1,
    fontSize: 18,
    fontWeight: 'bold',
    color: colors.text,
    textAlign: 'center',
  },
  headerRight: {
    width: 40,
  },
  headerInfo: {
    backgroundColor: colors.card,
    paddingHorizontal: 16,
    paddingVertical: 20,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  hashtagHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 16,
  },
  hashtagIcon: {
    width: 48,
    height: 48,
    borderRadius: 24,
    backgroundColor: '#FF6B35',
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 12,
  },
  hashtagDetails: {
    flex: 1,
  },
  hashtagName: {
    fontSize: 24,
    fontWeight: 'bold',
    color: colors.text,
    marginBottom: 4,
  },
  hashtagStats: {
    fontSize: 14,
    color: colors.textSecondary,
  },
  feedHeader: {
    borderTopWidth: 1,
    borderTopColor: colors.border,
    paddingTop: 16,
  },
  feedTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: colors.text,
    marginBottom: 4,
  },
  feedSubtitle: {
    fontSize: 14,
    color: colors.textSecondary,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 32,
  },
  loadingText: {
    marginTop: 16,
    fontSize: 16,
    color: colors.textSecondary,
  },
  emptyListContainer: {
    flexGrow: 1,
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 32,
  },
  emptyTitle: {
    fontSize: 20,
    fontWeight: '600',
    color: colors.text,
    marginTop: 16,
    marginBottom: 8,
  },
  emptyText: {
    fontSize: 16,
    color: colors.textSecondary,
    textAlign: 'center',
    marginBottom: 24,
  },
  createPostButton: {
    backgroundColor: colors.primary,
    paddingHorizontal: 24,
    paddingVertical: 12,
    borderRadius: 24,
  },
  createPostText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  errorContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 32,
  },
  errorTitle: {
    fontSize: 20,
    fontWeight: '600',
    color: colors.text,
    marginTop: 16,
    marginBottom: 8,
  },
  errorText: {
    fontSize: 16,
    color: colors.textSecondary,
    textAlign: 'center',
    marginBottom: 24,
  },
  retryButton: {
    backgroundColor: colors.primary,
    paddingHorizontal: 24,
    paddingVertical: 12,
    borderRadius: 24,
  },
  retryButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
});
