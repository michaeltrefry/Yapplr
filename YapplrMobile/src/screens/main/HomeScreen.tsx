import React, { useState, useEffect } from 'react';
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
import { TimelineItem } from '../../types';
import CreatePostModal from '../../components/CreatePostModal';
import PostCard from '../../components/PostCard';
import { RootStackParamList } from '../../navigation/AppNavigator';

type HomeScreenNavigationProp = StackNavigationProp<RootStackParamList, 'MainTabs'>;

export default function HomeScreen({ navigation }: { navigation: HomeScreenNavigationProp }) {
  const { api, user } = useAuth();
  const [refreshing, setRefreshing] = useState(false);
  const [showCreatePost, setShowCreatePost] = useState(false);

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
        <Text style={styles.headerTitle}>Yapplr</Text>
        <TouchableOpacity>
          <Ionicons name="add-circle-outline" size={24} color="#3B82F6" />
        </TouchableOpacity>
      </View>

      <FlatList
        data={timeline || []}
        renderItem={renderTimelineItem}
        keyExtractor={(item) => `${item.type}-${item.post.id}-${item.createdAt}`}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
        contentContainerStyle={styles.listContainer}
        showsVerticalScrollIndicator={false}
      />

      {/* Floating Action Button */}
      <TouchableOpacity
        style={styles.fab}
        onPress={() => setShowCreatePost(true)}
        activeOpacity={0.8}
      >
        <Ionicons name="add" size={24} color="#fff" />
      </TouchableOpacity>

      {/* Create Post Modal */}
      <CreatePostModal
        visible={showCreatePost}
        onClose={() => setShowCreatePost(false)}
      />
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#fff',
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  headerTitle: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#1F2937',
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
    color: '#EF4444',
    marginBottom: 16,
  },
  retryButton: {
    backgroundColor: '#3B82F6',
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 8,
  },
  retryText: {
    color: '#fff',
    fontWeight: '600',
  },
  fab: {
    position: 'absolute',
    bottom: 20,
    right: 20,
    width: 56,
    height: 56,
    borderRadius: 28,
    backgroundColor: '#3B82F6',
    alignItems: 'center',
    justifyContent: 'center',
    elevation: 8,
    shadowColor: '#000',
    shadowOffset: {
      width: 0,
      height: 4,
    },
    shadowOpacity: 0.3,
    shadowRadius: 4.65,
  },
});
