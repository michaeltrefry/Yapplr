import React, { useState } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  SafeAreaView,
  FlatList,
  RefreshControl,
  Alert,
  ActivityIndicator,
} from 'react-native';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
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
  const [isCreatingConversation, setIsCreatingConversation] = useState(false);
  const queryClient = useQueryClient();

  const isOwnProfile = currentUser?.username === username;

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

  // Check if current user can message this user
  const { data: canMessageData } = useQuery({
    queryKey: ['canMessage', profile?.id],
    queryFn: () => api.messages.canMessage(profile!.id),
    enabled: !!profile?.id && !isOwnProfile,
    retry: 1,
  });

  // Follow mutation
  const followMutation = useMutation({
    mutationFn: (userId: number) => api.users.follow(userId),
    onSuccess: (data) => {
      // Update the profile data with new follow status and count
      queryClient.setQueryData(['userProfile', username], (oldData: any) => {
        if (oldData) {
          return {
            ...oldData,
            isFollowedByCurrentUser: data.isFollowing,
            followerCount: data.followerCount,
          };
        }
        return oldData;
      });
    },
    onError: (error) => {
      console.error('Failed to follow user:', error);
      Alert.alert('Error', 'Failed to follow user. Please try again.');
    },
  });

  // Unfollow mutation
  const unfollowMutation = useMutation({
    mutationFn: (userId: number) => api.users.unfollow(userId),
    onSuccess: (data) => {
      // Update the profile data with new follow status and count
      queryClient.setQueryData(['userProfile', username], (oldData: any) => {
        if (oldData) {
          return {
            ...oldData,
            isFollowedByCurrentUser: data.isFollowing,
            followerCount: data.followerCount,
          };
        }
        return oldData;
      });
    },
    onError: (error) => {
      console.error('Failed to unfollow user:', error);
      Alert.alert('Error', 'Failed to unfollow user. Please try again.');
    },
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

  const handleFollowToggle = async () => {
    if (!profile) return;

    if (profile.isFollowedByCurrentUser) {
      unfollowMutation.mutate(profile.id);
    } else {
      followMutation.mutate(profile.id);
    }
  };

  const handleStartConversation = async () => {
    if (!profile) return;

    setIsCreatingConversation(true);
    try {
      const conversation = await api.messages.getOrCreateConversation(profile.id);
      navigation.navigate('Conversation', {
        conversationId: conversation.id,
        otherUser: {
          id: profile.id,
          username: profile.username,
        },
      });
    } catch (error) {
      console.error('Failed to create conversation:', error);
      Alert.alert(
        'Error',
        'Failed to start conversation. Please try again.',
        [{ text: 'OK' }]
      );
    } finally {
      setIsCreatingConversation(false);
    }
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
            
            <View style={styles.usernameContainer}>
              <Text style={styles.username}>@{profile.username}</Text>
              {profile.pronouns && (
                <Text style={styles.pronouns}> ({profile.pronouns})</Text>
              )}
            </View>

            {profile.bio && (
              <Text style={styles.bio}>{profile.bio}</Text>
            )}

            {profile.tagline && (
              <Text style={styles.tagline}>"{profile.tagline}"</Text>
            )}

            {profile.birthday && (
              <Text style={styles.birthday}>🎂 Born {new Date(profile.birthday).toLocaleDateString()}</Text>
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
              <View style={styles.actionButtonsContainer}>
                <TouchableOpacity
                  style={[
                    styles.actionButton,
                    styles.followButton,
                    profile.isFollowedByCurrentUser && styles.followingButton
                  ]}
                  onPress={handleFollowToggle}
                  disabled={followMutation.isPending || unfollowMutation.isPending}
                >
                  {(followMutation.isPending || unfollowMutation.isPending) ? (
                    <ActivityIndicator size="small" color="#fff" />
                  ) : (
                    <Text style={[
                      styles.actionButtonText,
                      styles.followButtonText,
                      profile.isFollowedByCurrentUser && styles.followingButtonText
                    ]}>
                      {profile.isFollowedByCurrentUser ? 'Following' : 'Follow'}
                    </Text>
                  )}
                </TouchableOpacity>

                {canMessageData?.canMessage && (
                  <TouchableOpacity
                    style={[styles.actionButton, styles.messageButton]}
                    onPress={handleStartConversation}
                    disabled={isCreatingConversation}
                  >
                    {isCreatingConversation ? (
                      <ActivityIndicator size="small" color="#fff" />
                    ) : (
                      <>
                        <Ionicons name="chatbubble-outline" size={16} color="#fff" style={styles.messageIcon} />
                        <Text style={[styles.actionButtonText, styles.messageButtonText]}>
                          Message
                        </Text>
                      </>
                    )}
                  </TouchableOpacity>
                )}
              </View>
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
  usernameContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 8,
  },
  username: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#1F2937',
  },
  bio: {
    fontSize: 16,
    color: '#1F2937',
    textAlign: 'center',
    lineHeight: 24,
    marginBottom: 12,
  },
  pronouns: {
    fontSize: 18,
    color: '#6B7280',
    fontWeight: 'normal',
  },
  tagline: {
    fontSize: 14,
    color: '#6B7280',
    textAlign: 'center',
    fontStyle: 'italic',
    marginBottom: 8,
  },
  birthday: {
    fontSize: 14,
    color: '#6B7280',
    textAlign: 'center',
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
  actionButtonsContainer: {
    flexDirection: 'row',
    justifyContent: 'center',
    gap: 12,
    marginBottom: 16,
  },
  actionButton: {
    flex: 1,
    paddingVertical: 12,
    paddingHorizontal: 20,
    borderRadius: 24,
    alignItems: 'center',
    justifyContent: 'center',
    minHeight: 44,
  },
  actionButtonText: {
    fontWeight: '600',
    fontSize: 16,
  },
  messageButton: {
    backgroundColor: '#10B981',
    flexDirection: 'row',
    alignItems: 'center',
  },
  messageButtonText: {
    color: '#fff',
  },
  messageIcon: {
    marginRight: 6,
  },
});
