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
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { RouteProp, useRoute, useNavigation } from '@react-navigation/native';
import { StackNavigationProp } from '@react-navigation/stack';
import { Comment, Post } from '../../types';
import { useAuth } from '../../contexts/AuthContext';

type RootStackParamList = {
  Comments: {
    post: Post;
    onCommentCountUpdate?: (postId: number, newCount: number) => void;
  };
};

type CommentsScreenRouteProp = RouteProp<RootStackParamList, 'Comments'>;
type CommentsScreenNavigationProp = StackNavigationProp<RootStackParamList, 'Comments'>;

export default function CommentsScreen() {
  const route = useRoute<CommentsScreenRouteProp>();
  const navigation = useNavigation<CommentsScreenNavigationProp>();
  const { post, onCommentCountUpdate } = route.params;
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

  const loadComments = async () => {
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
  };

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
        // Update the comment count in the parent screen
        onCommentCountUpdate?.(post.id, newComments.length);
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
    <View style={styles.commentItem}>
      <View style={styles.commentHeader}>
        <View style={styles.avatar}>
          {item.user.profileImageFileName ? (
            <Image
              source={{ uri: getImageUrl(item.user.profileImageFileName) }}
              style={styles.profileImage}
              onError={() => {
                console.log('Failed to load profile image in comments');
              }}
            />
          ) : (
            <Text style={styles.avatarText}>
              {item.user.username.charAt(0).toUpperCase()}
            </Text>
          )}
        </View>
        <View style={styles.commentInfo}>
          <Text style={styles.username}>@{item.user.username}</Text>
          <Text style={styles.timestamp}>
            {new Date(item.createdAt).toLocaleDateString()}
            {item.isEdited && <Text style={styles.editedText}> (edited)</Text>}
          </Text>
        </View>
      </View>
      <Text style={styles.commentContent}>{item.content}</Text>
    </View>
  ), [getImageUrl]);

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
