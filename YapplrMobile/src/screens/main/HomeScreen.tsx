import React, { useState } from 'react';
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  RefreshControl,
  TouchableOpacity,
  SafeAreaView,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { Ionicons } from '@expo/vector-icons';
import { useAuth } from '../../contexts/AuthContext';
import { TimelineItem } from 'yapplr-shared';

export default function HomeScreen() {
  const { api, user } = useAuth();
  const [refreshing, setRefreshing] = useState(false);

  const {
    data: timeline,
    isLoading,
    refetch,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
  } = useQuery({
    queryKey: ['timeline'],
    queryFn: () => api.posts.getTimeline(1, 25),
    enabled: !!user,
  });

  const onRefresh = async () => {
    setRefreshing(true);
    await refetch();
    setRefreshing(false);
  };

  const renderTimelineItem = ({ item }: { item: TimelineItem }) => (
    <View style={styles.postCard}>
      <View style={styles.postHeader}>
        <View style={styles.userInfo}>
          <View style={styles.avatar}>
            <Text style={styles.avatarText}>
              {item.post.user.username.charAt(0).toUpperCase()}
            </Text>
          </View>
          <View>
            <Text style={styles.username}>@{item.post.user.username}</Text>
            <Text style={styles.timestamp}>
              {new Date(item.post.createdAt).toLocaleDateString()}
            </Text>
          </View>
        </View>
        {item.type === 'repost' && item.repostedBy && (
          <View style={styles.repostBadge}>
            <Ionicons name="repeat" size={14} color="#10B981" />
            <Text style={styles.repostText}>
              Reposted by @{item.repostedBy.username}
            </Text>
          </View>
        )}
      </View>

      <Text style={styles.postContent}>{item.post.content}</Text>

      <View style={styles.postActions}>
        <TouchableOpacity style={styles.actionButton}>
          <Ionicons 
            name={item.post.isLikedByCurrentUser ? "heart" : "heart-outline"} 
            size={20} 
            color={item.post.isLikedByCurrentUser ? "#EF4444" : "#6B7280"} 
          />
          <Text style={styles.actionText}>{item.post.likeCount}</Text>
        </TouchableOpacity>

        <TouchableOpacity style={styles.actionButton}>
          <Ionicons name="chatbubble-outline" size={20} color="#6B7280" />
          <Text style={styles.actionText}>{item.post.commentCount}</Text>
        </TouchableOpacity>

        <TouchableOpacity style={styles.actionButton}>
          <Ionicons 
            name={item.post.isRepostedByCurrentUser ? "repeat" : "repeat-outline"} 
            size={20} 
            color={item.post.isRepostedByCurrentUser ? "#10B981" : "#6B7280"} 
          />
          <Text style={styles.actionText}>{item.post.repostCount}</Text>
        </TouchableOpacity>
      </View>
    </View>
  );

  if (isLoading) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.loadingContainer}>
          <Text>Loading timeline...</Text>
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
  postCard: {
    backgroundColor: '#fff',
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  postHeader: {
    marginBottom: 8,
  },
  userInfo: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  avatar: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: '#3B82F6',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  avatarText: {
    color: '#fff',
    fontWeight: 'bold',
    fontSize: 16,
  },
  username: {
    fontWeight: '600',
    fontSize: 16,
    color: '#1F2937',
  },
  timestamp: {
    fontSize: 12,
    color: '#6B7280',
  },
  repostBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: 4,
  },
  repostText: {
    fontSize: 12,
    color: '#10B981',
    marginLeft: 4,
  },
  postContent: {
    fontSize: 16,
    lineHeight: 24,
    color: '#1F2937',
    marginBottom: 12,
  },
  postActions: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    paddingTop: 8,
  },
  actionButton: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
  },
  actionText: {
    fontSize: 14,
    color: '#6B7280',
  },
});
