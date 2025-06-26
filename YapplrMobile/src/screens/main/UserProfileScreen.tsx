import React from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  SafeAreaView,
  FlatList,
  RefreshControl,
  Alert,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { Ionicons } from '@expo/vector-icons';
import { StackScreenProps } from '@react-navigation/stack';
import { useAuth } from '../../contexts/AuthContext';
import { TimelineItem } from '../../types';
import PostCard from '../../components/PostCard';
import { RootStackParamList } from '../../navigation/AppNavigator';

type UserProfileScreenProps = StackScreenProps<RootStackParamList, 'UserProfile'>;

export default function UserProfileScreen({ route, navigation }: UserProfileScreenProps) {
  const { username } = route.params;
  const { api, user: currentUser } = useAuth();

  const {
    data: profile,
    isLoading: profileLoading,
    error: profileError,
    refetch: refetchProfile,
  } = useQuery({
    queryKey: ['userProfile', username],
    queryFn: () => api.users.getUserProfile(username),
    retry: 2,
  });

  const {
    data: userTimeline,
    isLoading: timelineLoading,
    error: timelineError,
    refetch: refetchTimeline,
  } = useQuery({
    queryKey: ['userTimeline', profile?.id],
    queryFn: () => api.posts.getUserTimeline(profile!.id, 1, 25),
    enabled: !!profile?.id,
    retry: 2,
  });

  const handleLikePost = async (postId: number) => {
    try {
      await api.posts.likePost(postId);
      refetchTimeline();
    } catch (error) {
      Alert.alert('Error', 'Failed to like post');
    }
  };

  const handleRepost = async (postId: number) => {
    try {
      await api.posts.repostPost(postId);
      refetchTimeline();
    } catch (error) {
      Alert.alert('Error', 'Failed to repost');
    }
  };

  const onRefresh = async () => {
    await Promise.all([refetchProfile(), refetchTimeline()]);
  };

  const handleUserPress = (username: string) => {
    navigation.navigate('UserProfile', { username });
  };

  const renderTimelineItem = ({ item }: { item: TimelineItem }) => (
    <PostCard
      item={item}
      onLike={handleLikePost}
      onRepost={handleRepost}
      onUserPress={handleUserPress}
    />
  );

  if (profileLoading) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.header}>
          <TouchableOpacity onPress={() => navigation.goBack()}>
            <Ionicons name="arrow-back" size={24} color="#1F2937" />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>Profile</Text>
          <View style={{ width: 24 }} />
        </View>
        <View style={styles.loadingContainer}>
          <Text>Loading profile...</Text>
        </View>
      </SafeAreaView>
    );
  }

  if (profileError || !profile) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.header}>
          <TouchableOpacity onPress={() => navigation.goBack()}>
            <Ionicons name="arrow-back" size={24} color="#1F2937" />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>Profile</Text>
          <View style={{ width: 24 }} />
        </View>
        <View style={styles.errorContainer}>
          <Text style={styles.errorText}>User not found</Text>
          <TouchableOpacity style={styles.retryButton} onPress={() => refetchProfile()}>
            <Text style={styles.retryText}>Try Again</Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    );
  }

  const isOwnProfile = currentUser?.username === username;

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity onPress={() => navigation.goBack()}>
          <Ionicons name="arrow-back" size={24} color="#1F2937" />
        </TouchableOpacity>
        <Text style={styles.headerTitle}>@{profile.username}</Text>
        <View style={{ width: 24 }} />
      </View>

      <FlatList
        data={userTimeline || []}
        renderItem={renderTimelineItem}
        keyExtractor={(item) => `${item.type}-${item.post.id}-${item.createdAt}`}
        refreshControl={
          <RefreshControl refreshing={timelineLoading} onRefresh={onRefresh} />
        }
        ListHeaderComponent={
          <View style={styles.profileSection}>
            <View style={styles.avatar}>
              <Text style={styles.avatarText}>
                {profile.username.charAt(0).toUpperCase()}
              </Text>
            </View>
            
            <Text style={styles.username}>@{profile.username}</Text>
            
            {profile.bio && (
              <Text style={styles.bio}>{profile.bio}</Text>
            )}

            <View style={styles.statsContainer}>
              <View style={styles.statItem}>
                <Text style={styles.statNumber}>{profile.postCount}</Text>
                <Text style={styles.statLabel}>Posts</Text>
              </View>
              <View style={styles.statItem}>
                <Text style={styles.statNumber}>{profile.followingCount}</Text>
                <Text style={styles.statLabel}>Following</Text>
              </View>
              <View style={styles.statItem}>
                <Text style={styles.statNumber}>{profile.followerCount}</Text>
                <Text style={styles.statLabel}>Followers</Text>
              </View>
            </View>

            {!isOwnProfile && (
              <TouchableOpacity 
                style={[
                  styles.followButton,
                  profile.isFollowedByCurrentUser && styles.followingButton
                ]}
              >
                <Text style={[
                  styles.followButtonText,
                  profile.isFollowedByCurrentUser && styles.followingButtonText
                ]}>
                  {profile.isFollowedByCurrentUser ? 'Following' : 'Follow'}
                </Text>
              </TouchableOpacity>
            )}

            <View style={styles.divider} />
          </View>
        }
        ListEmptyComponent={
          timelineLoading ? (
            <View style={styles.loadingContainer}>
              <Text>Loading posts...</Text>
            </View>
          ) : (
            <View style={styles.emptyContainer}>
              <Text style={styles.emptyText}>
                {isOwnProfile ? "You haven't posted anything yet" : "No posts yet"}
              </Text>
            </View>
          )
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
    fontSize: 18,
    fontWeight: 'bold',
    color: '#1F2937',
  },
  profileSection: {
    alignItems: 'center',
    paddingVertical: 24,
    paddingHorizontal: 16,
  },
  avatar: {
    width: 80,
    height: 80,
    borderRadius: 40,
    backgroundColor: '#3B82F6',
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 16,
  },
  avatarText: {
    color: '#fff',
    fontWeight: 'bold',
    fontSize: 32,
  },
  username: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#1F2937',
    marginBottom: 8,
  },
  bio: {
    fontSize: 16,
    color: '#1F2937',
    textAlign: 'center',
    lineHeight: 24,
    marginBottom: 16,
  },
  statsContainer: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    width: '100%',
    paddingVertical: 16,
    marginBottom: 16,
  },
  statItem: {
    alignItems: 'center',
  },
  statNumber: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#1F2937',
  },
  statLabel: {
    fontSize: 14,
    color: '#6B7280',
    marginTop: 4,
  },
  followButton: {
    backgroundColor: '#3B82F6',
    paddingHorizontal: 32,
    paddingVertical: 12,
    borderRadius: 24,
    marginBottom: 16,
  },
  followingButton: {
    backgroundColor: '#E5E7EB',
    borderWidth: 1,
    borderColor: '#D1D5DB',
  },
  followButtonText: {
    color: '#fff',
    fontWeight: '600',
    fontSize: 16,
  },
  followingButtonText: {
    color: '#374151',
  },
  divider: {
    height: 1,
    backgroundColor: '#E5E7EB',
    width: '100%',
  },
  loadingContainer: {
    padding: 32,
    alignItems: 'center',
  },
  errorContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 32,
  },
  errorText: {
    fontSize: 18,
    color: '#6B7280',
    marginBottom: 16,
  },
  retryButton: {
    backgroundColor: '#3B82F6',
    paddingHorizontal: 24,
    paddingVertical: 12,
    borderRadius: 8,
  },
  retryText: {
    color: '#fff',
    fontWeight: '600',
  },
  emptyContainer: {
    padding: 32,
    alignItems: 'center',
  },
  emptyText: {
    fontSize: 16,
    color: '#6B7280',
    textAlign: 'center',
  },
  listContainer: {
    flexGrow: 1,
  },
});
