import React, { useState, useEffect, useCallback, useRef } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  ActivityIndicator,
  Alert,
  FlatList,
  TextInput,
  KeyboardAvoidingView,
  Platform,
  Image,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { useThemeColors } from '../../hooks/useThemeColors';
import { useAuth } from '../../contexts/AuthContext';
import { Post, TimelineItem, ReactionType, PostType, Comment } from '../../types';
import PostCard from '../../components/PostCard';
import ReportModal from '../../components/ReportModal';
import ReactionPicker from '../../components/ReactionPicker';
import ReactionCountsDisplay from '../../components/ReactionCountsDisplay';

interface SinglePostScreenProps {
  navigation: any;
  route: {
    params: {
      postId: number;
      scrollToComment?: number;
      showComments?: boolean; // New parameter to determine if comments should be shown
    };
  };
}

// CommentItem component
interface CommentItemProps {
  comment: Comment;
  postId: number;
  onDelete: () => void;
  colors: any;
  user: any;
  api: any;
}

function CommentItem({ comment, postId, onDelete, colors, user, api }: CommentItemProps) {
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [showReportModal, setShowReportModal] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isReacting, setIsReacting] = useState(false);

  const handleDelete = async () => {
    if (!api) return;

    try {
      setIsDeleting(true);
      await api.posts.deleteComment(comment.id);
      onDelete();
      setShowDeleteConfirm(false);
    } catch (error) {
      console.error('Error deleting comment:', error);
      Alert.alert('Error', 'Failed to delete comment');
    } finally {
      setIsDeleting(false);
    }
  };

  const handleReact = async (reactionType: ReactionType) => {
    if (!api || isReacting) return;

    try {
      setIsReacting(true);
      await api.posts.reactToComment(postId, comment.id, reactionType);
      // Note: We'd need to reload comments to see updated reactions
    } catch (error) {
      console.error('Error reacting to comment:', error);
      Alert.alert('Error', 'Failed to react to comment');
    } finally {
      setIsReacting(false);
    }
  };

  const formatTimeAgo = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);

    if (diffInSeconds < 60) return `${diffInSeconds}s`;
    if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m`;
    if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h`;
    return `${Math.floor(diffInSeconds / 86400)}d`;
  };

  return (
    <View style={[commentStyles.container, { borderBottomColor: colors.border }]}>
      <View style={[commentStyles.avatar, { backgroundColor: colors.primary }]}>
        {comment.user.profileImageUrl ? (
          <Image
            source={{ uri: comment.user.profileImageUrl }}
            style={commentStyles.profileImage}
          />
        ) : (
          <Text style={[commentStyles.avatarText, { color: colors.primaryText }]}>
            {comment.user.username.charAt(0).toUpperCase()}
          </Text>
        )}
      </View>
      <View style={commentStyles.content}>
        <View style={commentStyles.header}>
          <Text style={[commentStyles.username, { color: colors.text }]}>
            @{comment.user.username}
          </Text>
          <Text style={[commentStyles.timestamp, { color: colors.textSecondary }]}>
            {formatTimeAgo(comment.createdAt)}
          </Text>
          {user && user.id === comment.user.id && (
            <TouchableOpacity
              onPress={() => setShowDeleteConfirm(true)}
              style={commentStyles.deleteButton}
            >
              <Ionicons name="trash-outline" size={16} color={colors.error} />
            </TouchableOpacity>
          )}
        </View>
        <Text style={[commentStyles.text, { color: colors.text }]}>
          {comment.content}
        </Text>

        {/* Reaction Counts */}
        {comment.reactionCounts && comment.reactionCounts.length > 0 && (
          <ReactionCountsDisplay reactionCounts={comment.reactionCounts} />
        )}
      </View>

      {/* Delete Confirmation Modal */}
      {showDeleteConfirm && (
        <View style={commentStyles.deleteModal}>
          <View style={commentStyles.deleteModalContent}>
            <Text style={commentStyles.deleteModalTitle}>Delete Comment</Text>
            <Text style={commentStyles.deleteModalText}>
              Are you sure you want to delete this comment?
            </Text>
            <View style={commentStyles.deleteModalButtons}>
              <TouchableOpacity
                style={commentStyles.cancelButton}
                onPress={() => setShowDeleteConfirm(false)}
              >
                <Text style={commentStyles.cancelButtonText}>Cancel</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={commentStyles.deleteConfirmButton}
                onPress={handleDelete}
                disabled={isDeleting}
              >
                {isDeleting ? (
                  <ActivityIndicator size="small" color="#FFFFFF" />
                ) : (
                  <Text style={commentStyles.deleteButtonText}>Delete</Text>
                )}
              </TouchableOpacity>
            </View>
          </View>
        </View>
      )}

      {/* Report Modal */}
      <ReportModal
        visible={showReportModal}
        onClose={() => setShowReportModal(false)}
        commentId={comment.id}
        contentType="comment"
        contentPreview={comment.content}
      />
    </View>
  );
}

export default function SinglePostScreen({ navigation, route }: SinglePostScreenProps) {
  const colors = useThemeColors();
  const { api, user } = useAuth();
  const { postId, scrollToComment, showComments = false } = route.params;

  const [post, setPost] = useState<Post | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Comments state
  const [comments, setComments] = useState<Comment[]>([]);
  const [newComment, setNewComment] = useState('');
  const [isLoadingComments, setIsLoadingComments] = useState(false);
  const [isSubmittingComment, setIsSubmittingComment] = useState(false);
  const [currentCommentCount, setCurrentCommentCount] = useState(0);

  // UI state
  const [showCommentsSection, setShowCommentsSection] = useState(showComments);
  const flatListRef = useRef<FlatList>(null);

  useEffect(() => {
    loadPost();
  }, [postId]);

  useEffect(() => {
    if (showCommentsSection && post) {
      loadComments();
    }
  }, [showCommentsSection, post]);

  const loadPost = async () => {
    try {
      setLoading(true);
      setError(null);

      const postData = await api.posts.getPost(postId);
      setPost(postData);
      setCurrentCommentCount(postData.commentCount);
    } catch (err) {
      console.error('Error loading post:', err);
      setError(err instanceof Error ? err.message : 'Failed to load post');
    } finally {
      setLoading(false);
    }
  };

  const loadComments = useCallback(async () => {
    if (!api || !post) {
      console.error('API or post not available in loadComments');
      return;
    }

    try {
      setIsLoadingComments(true);
      console.log('Loading comments for post:', post.id);
      const commentsData = await api.posts.getComments(post.id);
      console.log('Comments loaded successfully:', commentsData.length, 'comments');
      setComments(commentsData);
      setCurrentCommentCount(commentsData.length);
    } catch (error) {
      console.error('Error loading comments:', error);
      Alert.alert('Error', 'Failed to load comments');
    } finally {
      setIsLoadingComments(false);
    }
  }, [api, post]);

  const handleReact = async (postId: number, reactionType: ReactionType) => {
    try {
      await api.posts.reactToPost(postId, reactionType);
      // Reload post to get updated reaction counts
      await loadPost();
    } catch (error) {
      console.error('Error reacting to post:', error);
      Alert.alert('Error', 'Failed to react to post');
    }
  };

  const handleRemoveReaction = async (postId: number) => {
    try {
      await api.posts.removePostReaction(postId);
      // Reload post to get updated reaction counts
      await loadPost();
    } catch (error) {
      console.error('Error removing reaction:', error);
      Alert.alert('Error', 'Failed to remove reaction');
    }
  };

  const handleUserPress = (username: string) => {
    navigation.navigate('UserProfile', { username });
  };

  const handleCommentPress = (post: Post) => {
    setShowCommentsSection(true);
  };

  const handleSubmitComment = async () => {
    if (!newComment.trim() || !api || !post) return;

    try {
      setIsSubmittingComment(true);
      console.log('Submitting comment:', newComment.trim());
      const comment = await api.posts.addComment(post.id, {
        content: newComment.trim(),
      });
      console.log('Comment submitted successfully:', comment);

      setComments(prevComments => [...prevComments, comment]);
      setCurrentCommentCount(prev => prev + 1);
      setNewComment('');

      // Scroll to bottom to show new comment
      setTimeout(() => {
        flatListRef.current?.scrollToEnd({ animated: true });
      }, 100);
    } catch (error) {
      console.error('Error submitting comment:', error);
      Alert.alert('Error', 'Failed to submit comment');
    } finally {
      setIsSubmittingComment(false);
    }
  };

  const handleDelete = () => {
    // Navigate back after deletion
    navigation.goBack();
  };

  const handleUnrepost = () => {
    // Navigate back after unrepost
    navigation.goBack();
  };

  const handleCommentDelete = useCallback((commentId: number) => {
    setComments(prevComments => prevComments.filter(c => c.id !== commentId));
    setCurrentCommentCount(prev => prev - 1);
  }, []);

  const renderComment = useCallback(({ item: comment }: { item: Comment }) => (
    <CommentItem
      comment={comment}
      postId={post?.id || 0}
      onDelete={() => handleCommentDelete(comment.id)}
      colors={colors}
      user={user}
      api={api}
    />
  ), [post?.id, handleCommentDelete, colors, user, api]);

  if (loading) {
    return (
      <SafeAreaView style={[styles.container, { backgroundColor: colors.background }]}>
        <View style={styles.header}>
          <TouchableOpacity onPress={() => navigation.goBack()}>
            <Ionicons name="arrow-back" size={24} color={colors.text} />
          </TouchableOpacity>
          <Text style={[styles.headerTitle, { color: colors.text }]}>Post</Text>
          <View style={{ width: 24 }} />
        </View>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={colors.primary} />
          <Text style={[styles.loadingText, { color: colors.text }]}>Loading post...</Text>
        </View>
      </SafeAreaView>
    );
  }

  if (error || !post) {
    return (
      <SafeAreaView style={[styles.container, { backgroundColor: colors.background }]}>
        <View style={styles.header}>
          <TouchableOpacity onPress={() => navigation.goBack()}>
            <Ionicons name="arrow-back" size={24} color={colors.text} />
          </TouchableOpacity>
          <Text style={[styles.headerTitle, { color: colors.text }]}>Post</Text>
          <View style={{ width: 24 }} />
        </View>
        <View style={styles.errorContainer}>
          <Ionicons name="alert-circle-outline" size={64} color={colors.error} />
          <Text style={[styles.errorText, { color: colors.error }]}>
            {error || 'Post not found'}
          </Text>
          <TouchableOpacity
            style={[styles.retryButton, { backgroundColor: colors.primary }]}
            onPress={loadPost}
          >
            <Text style={styles.retryButtonText}>Try Again</Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    );
  }

  // Convert Post to TimelineItem for PostCard compatibility
  const timelineItem: TimelineItem = {
    type: 'post', // Always 'post' for single post view, regardless of postType
    createdAt: post.createdAt,
    post: post,
    repostedBy: undefined, // Not applicable for single post view
  };

  if (!showCommentsSection) {
    // Show just the post
    return (
      <SafeAreaView style={[styles.container, { backgroundColor: colors.background }]}>
        <View style={styles.header}>
          <TouchableOpacity onPress={() => navigation.goBack()}>
            <Ionicons name="arrow-back" size={24} color={colors.text} />
          </TouchableOpacity>
          <Text style={[styles.headerTitle, { color: colors.text }]}>Post</Text>
          <View style={{ width: 24 }} />
        </View>

        <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
          <PostCard
            item={timelineItem}
            onReact={handleReact}
            onRemoveReaction={handleRemoveReaction}
            onUserPress={handleUserPress}
            onCommentPress={handleCommentPress}
            onDelete={handleDelete}
            onUnrepost={handleUnrepost}
          />
        </ScrollView>
      </SafeAreaView>
    );
  }

  // Show post with comments
  return (
    <SafeAreaView style={[styles.container, { backgroundColor: colors.background }]}>
      <View style={styles.header}>
        <TouchableOpacity onPress={() => navigation.goBack()}>
          <Ionicons name="arrow-back" size={24} color={colors.text} />
        </TouchableOpacity>
        <Text style={[styles.headerTitle, { color: colors.text }]}>
          {showCommentsSection ? 'Comments' : 'Post'}
        </Text>
        <View style={{ width: 24 }} />
      </View>

      <KeyboardAvoidingView
        style={styles.content}
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        keyboardVerticalOffset={Platform.OS === 'ios' ? 90 : 0}
      >
        {isLoadingComments ? (
          <View style={styles.loadingContainer}>
            <ActivityIndicator size="large" color={colors.primary} />
            <Text style={[styles.loadingText, { color: colors.text }]}>Loading comments...</Text>
          </View>
        ) : (
          <FlatList
            ref={flatListRef}
            data={comments}
            renderItem={renderComment}
            keyExtractor={(item) => item.id.toString()}
            ListHeaderComponent={() => (
              <PostCard
                item={timelineItem}
                onReact={handleReact}
                onRemoveReaction={handleRemoveReaction}
                onUserPress={handleUserPress}
                onCommentPress={handleCommentPress}
                onDelete={handleDelete}
                onUnrepost={handleUnrepost}
              />
            )}
            contentContainerStyle={styles.listContainer}
            showsVerticalScrollIndicator={false}
          />
        )}

        <View style={[styles.inputContainer, { backgroundColor: colors.background, borderTopColor: colors.border }]}>
          <TextInput
            style={[styles.textInput, { color: colors.text, backgroundColor: colors.inputBackground, borderColor: colors.border }]}
            placeholder="Add a comment..."
            placeholderTextColor={colors.textSecondary}
            value={newComment}
            onChangeText={setNewComment}
            multiline
            maxLength={256}
          />
          <TouchableOpacity
            style={[
              styles.sendButton,
              { backgroundColor: colors.primary },
              (!newComment.trim() || isSubmittingComment) && styles.sendButtonDisabled,
            ]}
            onPress={handleSubmitComment}
            disabled={!newComment.trim() || isSubmittingComment}
          >
            {isSubmittingComment ? (
              <ActivityIndicator size="small" color="#fff" />
            ) : (
              <Ionicons name="send" size={20} color="#fff" />
            )}
          </TouchableOpacity>
        </View>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: '600',
  },
  content: {
    flex: 1,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    gap: 16,
  },
  loadingText: {
    fontSize: 16,
  },
  errorContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    gap: 16,
    paddingHorizontal: 32,
  },
  errorText: {
    fontSize: 16,
    textAlign: 'center',
  },
  retryButton: {
    paddingHorizontal: 24,
    paddingVertical: 12,
    borderRadius: 8,
  },
  retryButtonText: {
    color: '#FFFFFF',
    fontSize: 16,
    fontWeight: '600',
  },
  listContainer: {
    paddingBottom: 20,
  },
  inputContainer: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderTopWidth: 1,
  },
  textInput: {
    flex: 1,
    borderWidth: 1,
    borderRadius: 20,
    paddingHorizontal: 16,
    paddingVertical: 12,
    marginRight: 12,
    maxHeight: 100,
    fontSize: 16,
  },
  sendButton: {
    width: 40,
    height: 40,
    borderRadius: 20,
    justifyContent: 'center',
    alignItems: 'center',
  },
  sendButtonDisabled: {
    opacity: 0.5,
  },
});

const commentStyles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
  },
  avatar: {
    width: 32,
    height: 32,
    borderRadius: 16,
    alignItems: 'center',
    justifyContent: 'center',
    overflow: 'hidden',
  },
  profileImage: {
    width: 32,
    height: 32,
    borderRadius: 16,
  },
  avatarText: {
    fontWeight: '600',
    fontSize: 14,
  },
  content: {
    flex: 1,
    marginLeft: 12,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 4,
  },
  username: {
    fontSize: 14,
    fontWeight: '600',
    marginRight: 8,
  },
  timestamp: {
    fontSize: 12,
    flex: 1,
  },
  deleteButton: {
    padding: 4,
  },
  text: {
    fontSize: 14,
    lineHeight: 20,
  },
  deleteModal: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  deleteModalContent: {
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 20,
    margin: 20,
    minWidth: 280,
  },
  deleteModalTitle: {
    fontSize: 18,
    fontWeight: '600',
    marginBottom: 8,
    textAlign: 'center',
  },
  deleteModalText: {
    fontSize: 14,
    color: '#666',
    marginBottom: 20,
    textAlign: 'center',
  },
  deleteModalButtons: {
    flexDirection: 'row',
    justifyContent: 'space-between',
  },
  cancelButton: {
    flex: 1,
    paddingVertical: 12,
    marginRight: 8,
    borderRadius: 8,
    backgroundColor: '#f3f4f6',
    alignItems: 'center',
  },
  cancelButtonText: {
    fontSize: 16,
    color: '#374151',
  },
  deleteConfirmButton: {
    flex: 1,
    paddingVertical: 12,
    marginLeft: 8,
    borderRadius: 8,
    backgroundColor: '#ef4444',
    alignItems: 'center',
  },
  deleteButtonText: {
    fontSize: 16,
    color: 'white',
    fontWeight: '600',
  },
});
