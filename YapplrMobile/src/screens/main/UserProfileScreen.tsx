import React, { useState, useMemo, useEffect } from 'react';
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
  Image,
} from 'react-native';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Ionicons } from '@expo/vector-icons';
import { StackScreenProps } from '@react-navigation/stack';
import { useAuth } from '../../contexts/AuthContext';
import { TimelineItem, Post, ReactionType } from '../../types';
import PostCard from '../../components/PostCard';
import { RootStackParamList } from '../../navigation/AppNavigator';

type UserProfileScreenProps = StackScreenProps<RootStackParamList, 'UserProfile'>;

export default function UserProfileScreen({ route, navigation }: UserProfileScreenProps) {
  const { username } = route.params;
  const { api, user: currentUser } = useAuth();
  const [isCreatingConversation, setIsCreatingConversation] = useState(false);
  const [commentCountUpdates, setCommentCountUpdates] = useState<Record<number, number>>({});
  const [showBlockConfirm, setShowBlockConfirm] = useState(false);
  const queryClient = useQueryClient();

  const isOwnProfile = currentUser?.username === username;

  // Helper function to generate image URL
  const getImageUrl = (fileName: string) => {
    if (!fileName) return '';
    return `http://192.168.254.181:5161/api/images/${fileName}`;
  };

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

  // Check block status
  const { data: blockStatus } = useQuery({
    queryKey: ['blockStatus', profile?.id],
    queryFn: () => api.users.getBlockStatus(profile!.id),
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

  // Block mutation
  const blockMutation = useMutation({
    mutationFn: (userId: number) => api.users.blockUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['blockStatus', profile?.id] });
      queryClient.invalidateQueries({ queryKey: ['userProfile', username] });
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['userTimeline', profile?.id] });
      queryClient.invalidateQueries({ queryKey: ['following'] });
      setShowBlockConfirm(false);
      Alert.alert('User Blocked', 'You have successfully blocked this user.');
    },
    onError: (error) => {
      console.error('Failed to block user:', error);
      Alert.alert('Error', 'Failed to block user. Please try again.');
    },
  });

  // Unblock mutation
  const unblockMutation = useMutation({
    mutationFn: (userId: number) => api.users.unblockUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['blockStatus', profile?.id] });
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['userTimeline', profile?.id] });
      Alert.alert('User Unblocked', 'You have successfully unblocked this user.');
    },
    onError: (error) => {
      console.error('Failed to unblock user:', error);
      Alert.alert('Error', 'Failed to unblock user. Please try again.');
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

  const handleReact = async (postId: number, reactionType: ReactionType) => {
    try {
      await api.posts.reactToPost(postId, reactionType);
      refetchTimeline();
    } catch (error) {
      Alert.alert('Error', 'Failed to react to post');
    }
  };

  const handleRemoveReaction = async (postId: number) => {
    try {
      await api.posts.removePostReaction(postId);
      refetchTimeline();
    } catch (error) {
      Alert.alert('Error', 'Failed to remove reaction');
    }
  };

  const onRefresh = async () => {
    await Promise.all([refetchProfile(), refetchTimeline()]);
  };

  // Listen for focus events to refresh timeline when returning from other screens
  useEffect(() => {
    const unsubscribe = navigation.addListener('focus', () => {
      refetchTimeline();
    });

    return unsubscribe;
  }, [navigation, refetchTimeline]);

  const handleFollowToggle = async () => {
    if (!profile) return;

    if (profile.isFollowedByCurrentUser) {
      unfollowMutation.mutate(profile.id);
    } else {
      followMutation.mutate(profile.id);
    }
  };

  const handleBlockToggle = () => {
    if (!profile) return;

    if (blockStatus?.isBlocked) {
      unblockMutation.mutate(profile.id);
    } else {
      setShowBlockConfirm(true);
    }
  };

  const confirmBlock = () => {
    if (!profile) return;
    blockMutation.mutate(profile.id);
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

  const handleCommentCountUpdate = (postId: number, newCount: number) => {
    console.log(`Updating comment count for post ${postId} to ${newCount}`);
    setCommentCountUpdates(prev => ({
      ...prev,
      [postId]: newCount
    }));
    console.log(`Updated comment count for post ${postId} to ${newCount}`);
  };

  const handleCommentPress = (post: Post) => {
    navigation.navigate('Comments', { post });
  };

  // Merge timeline data with local comment count updates
  const timelineWithUpdates = useMemo(() => {
    if (!userTimeline) return [];

    return userTimeline.map(item => {
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
  }, [userTimeline, commentCountUpdates]);

  const handleDelete = () => {
    refetchTimeline(); // Refresh timeline after deletion
  };

  const handleUnrepost = () => {
    refetchTimeline(); // Refresh timeline after unrepost
  };

  const renderTimelineItem = ({ item }: { item: TimelineItem }) => (
    <PostCard
      item={item}
      onLike={handleLikePost}
      onReact={handleReact}
      onRemoveReaction={handleRemoveReaction}
      onRepost={handleRepost}
      onUserPress={handleUserPress}
      onCommentPress={handleCommentPress}
      onCommentCountUpdate={handleCommentCountUpdate}
      onDelete={handleDelete}
      onUnrepost={handleUnrepost}
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
        data={timelineWithUpdates}
        renderItem={renderTimelineItem}
        keyExtractor={(item) => `${item.type}-${item.post.id}-${item.createdAt}`}
        refreshControl={
          <RefreshControl refreshing={timelineLoading} onRefresh={onRefresh} />
        }
        ListHeaderComponent={
          <View style={styles.profileSection}>
            <View style={styles.userHeader}>
              <View style={styles.avatar}>
                {profile.profileImageFileName ? (
                  <Image
                    source={{ uri: getImageUrl(profile.profileImageFileName) }}
                    style={styles.profileImage}
                    onError={() => {
                      // Fallback to initials if image fails to load
                      console.log('Failed to load profile image');
                    }}
                  />
                ) : (
                  <Text style={styles.avatarText}>
                    {profile.username.charAt(0).toUpperCase()}
                  </Text>
                )}
              </View>

              <View style={styles.userInfo}>
                <View style={styles.usernameContainer}>
                  <Text style={styles.username}>@{profile.username}</Text>
                  {profile.pronouns && (
                    <Text style={styles.pronouns}> ({profile.pronouns})</Text>
                  )}
                </View>
                {profile.tagline && (
                  <Text style={styles.tagline}>"{profile.tagline}"</Text>
                )}
              </View>
            </View>

            {profile.birthday && (
              <Text style={styles.birthday}>🎂 Born {new Date(profile.birthday).toLocaleDateString()}</Text>
            )}

            {profile.bio && (
              <Text style={styles.bio}>{profile.bio}</Text>
            )}

            <View style={styles.statsContainer}>
              <View style={styles.statItem}>
                <Text style={styles.statNumber}>{profile.postCount}</Text>
                <Text style={styles.statLabel}>Posts</Text>
              </View>
              <TouchableOpacity
                style={styles.statItem}
                onPress={() => navigation.navigate('FollowingList', {
                  userId: profile.id,
                  username: profile.username
                })}
                activeOpacity={0.7}
              >
                <Text style={styles.statNumber}>{profile.followingCount}</Text>
                <Text style={styles.statLabel}>Following</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={styles.statItem}
                onPress={() => navigation.navigate('FollowersList', {
                  userId: profile.id,
                  username: profile.username
                })}
                activeOpacity={0.7}
              >
                <Text style={styles.statNumber}>{profile.followerCount}</Text>
                <Text style={styles.statLabel}>Followers</Text>
              </TouchableOpacity>
            </View>

            {!isOwnProfile && (
              <View style={styles.actionButtonsContainer}>
                <TouchableOpacity
                  style={[
                    styles.actionButton,
                    styles.followButton,
                    profile.isFollowedByCurrentUser && styles.followingButton,
                    profile.hasPendingFollowRequest && styles.pendingRequestButton
                  ]}
                  onPress={handleFollowToggle}
                  disabled={followMutation.isPending || unfollowMutation.isPending || profile.hasPendingFollowRequest}
                >
                  {(followMutation.isPending || unfollowMutation.isPending) ? (
                    <ActivityIndicator size="small" color="#fff" />
                  ) : (
                    <Text style={[
                      styles.actionButtonText,
                      styles.followButtonText,
                      profile.isFollowedByCurrentUser && styles.followingButtonText,
                      profile.hasPendingFollowRequest && styles.pendingRequestButtonText
                    ]}>
                      {profile.isFollowedByCurrentUser
                        ? 'Following'
                        : profile.hasPendingFollowRequest
                        ? 'Request Pending'
                        : profile.requiresFollowApproval
                        ? 'Request to Follow'
                        : 'Follow'
                      }
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

                <TouchableOpacity
                  style={[styles.actionButton, styles.blockButton]}
                  onPress={handleBlockToggle}
                  disabled={blockMutation.isPending || unblockMutation.isPending}
                >
                  {(blockMutation.isPending || unblockMutation.isPending) ? (
                    <ActivityIndicator size="small" color="#fff" />
                  ) : (
                    <>
                      <Ionicons
                        name={blockStatus?.isBlocked ? "person-add-outline" : "person-remove-outline"}
                        size={16}
                        color="#fff"
                        style={styles.blockIcon}
                      />
                      <Text style={[styles.actionButtonText, styles.blockButtonText]}>
                        {blockStatus?.isBlocked ? 'Unblock' : 'Block'}
                      </Text>
                    </>
                  )}
                </TouchableOpacity>
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

      {/* Block Confirmation Modal */}
      {showBlockConfirm && (
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <Text style={styles.modalTitle}>Block User</Text>
            <Text style={styles.modalText}>
              Are you sure you want to block @{profile?.username}? They will no longer be able to see your posts or send you messages, and you will automatically unfollow them.
            </Text>
            <View style={styles.modalButtons}>
              <TouchableOpacity
                style={[styles.modalButton, styles.cancelButton]}
                onPress={() => setShowBlockConfirm(false)}
              >
                <Text style={styles.cancelButtonText}>Cancel</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={[styles.modalButton, styles.confirmButton]}
                onPress={confirmBlock}
                disabled={blockMutation.isPending}
              >
                {blockMutation.isPending ? (
                  <ActivityIndicator size="small" color="#fff" />
                ) : (
                  <Text style={styles.confirmButtonText}>Block</Text>
                )}
              </TouchableOpacity>
            </View>
          </View>
        </View>
      )}
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
    paddingVertical: 24,
    paddingHorizontal: 16,
  },
  userHeader: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    marginBottom: 16,
  },
  avatar: {
    width: 80,
    height: 80,
    borderRadius: 40,
    backgroundColor: '#3B82F6',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 16,
    overflow: 'hidden',
  },
  userInfo: {
    flex: 1,
    justifyContent: 'flex-start',
  },
  profileImage: {
    width: 80,
    height: 80,
    borderRadius: 40,
  },
  avatarText: {
    color: '#fff',
    fontWeight: 'bold',
    fontSize: 32,
  },
  usernameContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 4,
  },
  username: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#1F2937',
  },
  bio: {
    fontSize: 16,
    color: '#1F2937',
    lineHeight: 24,
    marginBottom: 12,
  },
  pronouns: {
    fontSize: 16,
    color: '#6B7280',
    fontWeight: 'normal',
  },
  tagline: {
    fontSize: 14,
    color: '#6B7280',
    fontStyle: 'italic',
    marginTop: 4,
    marginBottom: 8,
  },
  birthday: {
    fontSize: 14,
    color: '#6B7280',
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
    fontSize: 14,
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
    fontSize: 14,
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
  blockButton: {
    backgroundColor: '#EF4444',
    flexDirection: 'row',
    alignItems: 'center',
  },
  blockButtonText: {
    color: '#fff',
  },
  blockIcon: {
    marginRight: 6,
  },
  modalOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'center',
    alignItems: 'center',
    zIndex: 1000,
  },
  modalContent: {
    backgroundColor: '#fff',
    margin: 20,
    padding: 20,
    borderRadius: 12,
    maxWidth: 400,
    width: '90%',
  },
  modalTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#1F2937',
    marginBottom: 12,
    textAlign: 'center',
  },
  modalText: {
    fontSize: 16,
    color: '#6B7280',
    lineHeight: 24,
    marginBottom: 20,
    textAlign: 'center',
  },
  modalButtons: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    gap: 12,
  },
  modalButton: {
    flex: 1,
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderRadius: 8,
    alignItems: 'center',
  },
  cancelButton: {
    backgroundColor: '#F3F4F6',
    borderWidth: 1,
    borderColor: '#D1D5DB',
  },
  cancelButtonText: {
    color: '#6B7280',
    fontWeight: '600',
  },
  confirmButton: {
    backgroundColor: '#EF4444',
  },
  confirmButtonText: {
    color: '#fff',
    fontWeight: '600',
  },
  pendingRequestButton: {
    backgroundColor: '#F97316', // Orange color for pending requests
  },
  pendingRequestButtonText: {
    color: '#fff',
  },
});
