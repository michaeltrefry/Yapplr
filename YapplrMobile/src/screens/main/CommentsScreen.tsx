import React, { useState, useEffect, useCallback, useMemo, useRef } from 'react';
import {
  View,
  Text,
  FlatList,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  SafeAreaView,
  Alert,
  ActivityIndicator,
  Image,
  KeyboardAvoidingView,
  Platform,
  Modal,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { RouteProp, useRoute, useNavigation } from '@react-navigation/native';
import { StackNavigationProp } from '@react-navigation/stack';
import { Comment, Post } from '../../types';
import { useAuth } from '../../contexts/AuthContext';
import ReportModal from '../../components/ReportModal';

type RootStackParamList = {
  Comments: {
    post: Post;
  };
};

type CommentsScreenRouteProp = RouteProp<RootStackParamList, 'Comments'>;
type CommentsScreenNavigationProp = StackNavigationProp<RootStackParamList, 'Comments'>;

// CommentItem component with delete functionality
interface CommentItemProps {
  comment: Comment;
  postId: number;
  getImageUrl: (fileName: string) => string;
  onDelete: () => void;
}

function CommentItem({ comment, postId, getImageUrl, onDelete }: CommentItemProps) {
  const { user, api } = useAuth();
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [showReportModal, setShowReportModal] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isLiking, setIsLiking] = useState(false);

  const isOwner = user && user.id === comment.user.id;

  const handleDelete = async () => {
    if (!isOwner) return;

    setIsDeleting(true);
    try {
      await api.posts.deleteComment(comment.id);
      setShowDeleteConfirm(false);
      onDelete();
    } catch (error) {
      console.error('Error deleting comment:', error);
      Alert.alert('Error', 'Failed to delete comment. Please try again.');
    } finally {
      setIsDeleting(false);
    }
  };

  const handleLike = async () => {
    if (!user) return;

    setIsLiking(true);
    try {
      if (comment.isLikedByCurrentUser) {
        await api.posts.unlikeComment(postId, comment.id);
      } else {
        await api.posts.likeComment(postId, comment.id);
      }
      // Note: In a real app, you'd want to update the comment state here
      // For now, we'll rely on the parent component to refresh
      onDelete(); // This triggers a refresh of comments
    } catch (error) {
      console.error('Error liking comment:', error);
      Alert.alert('Failed to like comment. Please try again.');
    } finally {
      setIsLiking(false);
    }
  };

  return (
    <View style={commentStyles.commentItem}>
      <View style={commentStyles.commentHeader}>
        <View style={commentStyles.avatar}>
          {comment.user.profileImageFileName ? (
            <Image
              source={{ uri: getImageUrl(comment.user.profileImageFileName) }}
              style={commentStyles.profileImage}
              onError={() => {
                console.log('Failed to load profile image in comments');
              }}
            />
          ) : (
            <Text style={commentStyles.avatarText}>
              {comment.user.username.charAt(0).toUpperCase()}
            </Text>
          )}
        </View>
        <View style={commentStyles.commentInfo}>
          <Text style={commentStyles.username}>@{comment.user.username}</Text>
          <Text style={commentStyles.timestamp}>
            {new Date(comment.createdAt).toLocaleDateString()}
            {comment.isEdited && <Text style={commentStyles.editedText}> (edited)</Text>}
          </Text>
        </View>
      </View>
      <Text style={commentStyles.commentContent}>{comment.content}</Text>

      {/* Action Bar */}
      <View style={commentStyles.actionBar}>
        <View style={commentStyles.leftActions}>
          {/* Like Button */}
          <TouchableOpacity
            style={commentStyles.actionButton}
            onPress={handleLike}
            disabled={isLiking}
            activeOpacity={0.7}
          >
            <Ionicons
              name={comment.isLikedByCurrentUser ? "heart" : "heart-outline"}
              size={16}
              color={comment.isLikedByCurrentUser ? "#EF4444" : "#6B7280"}
            />
            <Text style={[commentStyles.actionText, comment.isLikedByCurrentUser && { color: '#EF4444' }]}>
              {comment.likeCount}
            </Text>
          </TouchableOpacity>

          {/* Reply Button */}
          <TouchableOpacity
            style={commentStyles.actionButton}
            activeOpacity={0.7}
          >
            <Ionicons name="chatbubble-outline" size={16} color="#6B7280" />
            <Text style={commentStyles.actionText}>Reply</Text>
          </TouchableOpacity>

          {/* Report Button - only for other users' comments */}
          {!isOwner && (
            <TouchableOpacity
              style={commentStyles.actionButton}
              onPress={() => setShowReportModal(true)}
              activeOpacity={0.7}
            >
              <Ionicons name="flag-outline" size={16} color="#6B7280" />
              <Text style={commentStyles.actionText}>Report</Text>
            </TouchableOpacity>
          )}
        </View>

        {/* Owner Actions */}
        {isOwner && (
          <View style={commentStyles.rightActions}>
            <TouchableOpacity
              style={commentStyles.actionButton}
              onPress={() => setShowDeleteConfirm(true)}
              activeOpacity={0.7}
            >
              <Ionicons name="trash-outline" size={16} color="#EF4444" />
            </TouchableOpacity>
          </View>
        )}
      </View>

      {/* Delete Confirmation Modal */}
      <Modal
        visible={showDeleteConfirm}
        transparent={true}
        animationType="fade"
        onRequestClose={() => setShowDeleteConfirm(false)}
      >
        <View style={commentStyles.modalOverlay}>
          <View style={commentStyles.modalContent}>
            <Text style={commentStyles.modalTitle}>Delete Comment</Text>
            <Text style={commentStyles.modalMessage}>
              Are you sure you want to delete this comment? This action cannot be undone.
            </Text>
            <View style={commentStyles.modalButtons}>
              <TouchableOpacity
                style={commentStyles.cancelButton}
                onPress={() => setShowDeleteConfirm(false)}
                disabled={isDeleting}
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
      </Modal>

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

export default function CommentsScreen() {
  const route = useRoute<CommentsScreenRouteProp>();
  const navigation = useNavigation<CommentsScreenNavigationProp>();
  const { post } = route.params;
  const { user, api } = useAuth() || {};

  const [comments, setComments] = useState<Comment[]>([]);
  const [newComment, setNewComment] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [currentCommentCount, setCurrentCommentCount] = useState(post.commentCount);
  const flatListRef = useRef<FlatList>(null);

  // Helper function to generate image URL
  const getImageUrl = useCallback((fileName: string) => {
    if (!fileName) return '';
    return `http://192.168.254.181:5161/api/images/${fileName}`;
  }, []);

  useEffect(() => {
    loadComments();
  }, []);

  const loadComments = useCallback(async () => {
    if (!api) {
      console.error('API not available in loadComments');
      setIsLoading(false);
      return;
    }

    try {
      setIsLoading(true);
      console.log('Loading comments for post:', post.id);
      const commentsData = await api.posts.getComments(post.id);
      console.log('Comments loaded successfully:', commentsData.length, 'comments');
      setComments(commentsData);
      // Update the comment count to match the actual number of comments loaded
      setCurrentCommentCount(commentsData.length);
    } catch (error) {
      console.error('Error loading comments:', error);
      Alert.alert('Error', 'Failed to load comments');
    } finally {
      setIsLoading(false);
    }
  }, [api, post.id]);

  const handleSubmitComment = async () => {
    if (!newComment.trim()) return;
    if (!api) {
      Alert.alert('Error', 'API not available');
      return;
    }

    try {
      setIsSubmitting(true);
      console.log('Submitting comment:', newComment.trim());
      const comment = await api.posts.addComment(post.id, {
        content: newComment.trim(),
      });
      console.log('Comment created successfully:', comment);
      setComments(prev => {
        const newComments = [...prev, comment];
        // Update the local comment count display
        setCurrentCommentCount(newComments.length);

        // Scroll to the bottom to show the new comment
        setTimeout(() => {
          flatListRef.current?.scrollToEnd({ animated: true });
        }, 100);

        return newComments;
      });
      setNewComment('');
    } catch (error) {
      console.error('Error adding comment:', error);
      Alert.alert('Error', 'Failed to add comment');
    } finally {
      setIsSubmitting(false);
    }
  };

  const renderComment = useCallback(({ item }: { item: Comment }) => (
    <CommentItem
      comment={item}
      postId={post.id}
      getImageUrl={getImageUrl}
      onDelete={loadComments}
    />
  ), [getImageUrl, loadComments, post.id]);

  const renderHeader = useMemo(() => (
    <View style={styles.postContainer}>
      <View style={styles.postHeader}>
        <View style={styles.avatar}>
          {post.user.profileImageFileName ? (
            <Image
              source={{ uri: getImageUrl(post.user.profileImageFileName) }}
              style={styles.profileImage}
              onError={() => {
                console.log('Failed to load profile image in post');
              }}
            />
          ) : (
            <Text style={styles.avatarText}>
              {post.user.username.charAt(0).toUpperCase()}
            </Text>
          )}
        </View>
        <View>
          <Text style={styles.username}>@{post.user.username}</Text>
          <Text style={styles.timestamp}>
            {new Date(post.createdAt).toLocaleDateString()}
          </Text>
        </View>
      </View>
      <Text style={styles.postContent}>{post.content}</Text>
      {post.imageUrl && (
        <Image source={{ uri: post.imageUrl }} style={styles.postImage} />
      )}
      <View style={styles.postStats}>
        <Text style={styles.statsText}>
          {currentCommentCount} {currentCommentCount === 1 ? 'comment' : 'comments'}
        </Text>
      </View>
    </View>
  ), [post, currentCommentCount, getImageUrl]);

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity
          style={styles.backButton}
          onPress={() => navigation.goBack()}
        >
          <Ionicons name="arrow-back" size={24} color="#000" />
        </TouchableOpacity>
        <Text style={styles.headerTitle}>Comments</Text>
      </View>

      <KeyboardAvoidingView 
        style={styles.content}
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        keyboardVerticalOffset={Platform.OS === 'ios' ? 90 : 0}
      >
        {isLoading ? (
          <View style={styles.loadingContainer}>
            <ActivityIndicator size="large" color="#3B82F6" />
            <Text style={styles.loadingText}>Loading comments...</Text>
          </View>
        ) : (
          <FlatList
            ref={flatListRef}
            data={comments}
            renderItem={renderComment}
            keyExtractor={(item) => item.id.toString()}
            ListHeaderComponent={renderHeader}
            contentContainerStyle={styles.listContainer}
            showsVerticalScrollIndicator={false}
          />
        )}

        <View style={styles.inputContainer}>
          <TextInput
            style={styles.textInput}
            placeholder="Add a comment..."
            value={newComment}
            onChangeText={setNewComment}
            multiline
            maxLength={256}
          />
          <TouchableOpacity
            style={[
              styles.sendButton,
              (!newComment.trim() || isSubmitting) && styles.sendButtonDisabled,
            ]}
            onPress={handleSubmitComment}
            disabled={!newComment.trim() || isSubmitting}
          >
            {isSubmitting ? (
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
    backgroundColor: '#fff',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  backButton: {
    marginRight: 16,
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#000',
  },
  content: {
    flex: 1,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingText: {
    marginTop: 8,
    color: '#6B7280',
  },
  listContainer: {
    paddingBottom: 16,
  },
  postContainer: {
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
    backgroundColor: '#F9FAFB',
  },
  postHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 8,
  },
  avatar: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: '#E5E7EB',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  profileImage: {
    width: 40,
    height: 40,
    borderRadius: 20,
  },
  avatarText: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#6B7280',
  },
  username: {
    fontSize: 14,
    fontWeight: '600',
    color: '#000',
  },
  timestamp: {
    fontSize: 12,
    color: '#6B7280',
  },
  editedText: {
    fontStyle: 'italic',
  },
  postContent: {
    fontSize: 16,
    lineHeight: 24,
    color: '#000',
    marginBottom: 8,
  },
  postImage: {
    width: '100%',
    height: 200,
    borderRadius: 8,
    marginBottom: 8,
  },
  postStats: {
    marginTop: 8,
  },
  statsText: {
    fontSize: 14,
    color: '#6B7280',
    fontWeight: '500',
  },
  commentItem: {
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#F3F4F6',
  },
  commentHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 8,
  },
  commentInfo: {
    flex: 1,
  },
  commentContent: {
    fontSize: 14,
    lineHeight: 20,
    color: '#000',
    marginLeft: 52,
  },
  inputContainer: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderTopWidth: 1,
    borderTopColor: '#E5E7EB',
    backgroundColor: '#fff',
  },
  textInput: {
    flex: 1,
    borderWidth: 1,
    borderColor: '#D1D5DB',
    borderRadius: 20,
    paddingHorizontal: 16,
    paddingVertical: 8,
    marginRight: 8,
    maxHeight: 100,
    fontSize: 16,
  },
  sendButton: {
    backgroundColor: '#3B82F6',
    borderRadius: 20,
    width: 40,
    height: 40,
    justifyContent: 'center',
    alignItems: 'center',
  },
  sendButtonDisabled: {
    backgroundColor: '#9CA3AF',
  },
});

// Styles for CommentItem component
const commentStyles = StyleSheet.create({
  commentItem: {
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#F3F4F6',
  },
  commentHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: 8,
  },
  avatar: {
    width: 32,
    height: 32,
    borderRadius: 16,
    backgroundColor: '#E5E7EB',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  profileImage: {
    width: 32,
    height: 32,
    borderRadius: 16,
  },
  avatarText: {
    fontSize: 14,
    fontWeight: 'bold',
    color: '#6B7280',
  },
  commentInfo: {
    flex: 1,
  },
  username: {
    fontSize: 14,
    fontWeight: '600',
    color: '#000',
  },
  timestamp: {
    fontSize: 12,
    color: '#6B7280',
  },
  editedText: {
    fontStyle: 'italic',
  },
  commentContent: {
    fontSize: 14,
    lineHeight: 20,
    color: '#000',
    marginLeft: 44,
  },
  actionButtons: {
    flexDirection: 'row',
    gap: 8,
  },
  actionButton: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 8,
    borderRadius: 16,
    backgroundColor: '#F9FAFB',
    gap: 4,
  },
  actionBar: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginLeft: 44,
    marginTop: 8,
    paddingTop: 8,
  },
  leftActions: {
    flexDirection: 'row',
    gap: 12,
  },
  rightActions: {
    flexDirection: 'row',
    gap: 8,
  },
  actionText: {
    fontSize: 12,
    color: '#6B7280',
    fontWeight: '500',
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20,
  },
  modalContent: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    padding: 24,
    width: '100%',
    maxWidth: 320,
  },
  modalTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#000',
    marginBottom: 12,
    textAlign: 'center',
  },
  modalMessage: {
    fontSize: 14,
    color: '#6B7280',
    marginBottom: 24,
    textAlign: 'center',
    lineHeight: 20,
  },
  modalButtons: {
    flexDirection: 'row',
    gap: 12,
  },
  cancelButton: {
    flex: 1,
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#D1D5DB',
    backgroundColor: '#F9FAFB',
  },
  cancelButtonText: {
    color: '#000',
    fontSize: 16,
    fontWeight: '500',
    textAlign: 'center',
  },
  deleteConfirmButton: {
    flex: 1,
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderRadius: 8,
    backgroundColor: '#EF4444',
    justifyContent: 'center',
    alignItems: 'center',
  },
  deleteButtonText: {
    color: '#FFFFFF',
    fontSize: 16,
    fontWeight: '500',
    textAlign: 'center',
  },
});
