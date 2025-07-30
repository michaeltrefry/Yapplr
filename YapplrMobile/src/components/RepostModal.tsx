import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  Modal,
  StyleSheet,
  SafeAreaView,
  Alert,
  KeyboardAvoidingView,
  Platform,
  Image,
  ActivityIndicator,
  Keyboard,
  ScrollView,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import * as ImagePicker from 'expo-image-picker';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../contexts/AuthContext';
import { useThemeColors } from '../hooks/useThemeColors';
import { CreateRepostData, CreateRepostWithMediaData, PostPrivacy, MediaType, MediaFile, UploadedFile, Post } from '../types';
import GifPicker from './GifPicker';
import type { SelectedGif } from '../lib/tenor';

interface RepostModalProps {
  visible: boolean;
  onClose: () => void;
  repostedPost: Post;
}

export default function RepostModal({ visible, onClose, repostedPost }: RepostModalProps) {
  const { api, user } = useAuth();
  const colors = useThemeColors();
  const queryClient = useQueryClient();

  const styles = createStyles(colors);
  const [content, setContent] = useState('');
  const [privacy, setPrivacy] = useState<PostPrivacy>(PostPrivacy.Public);
  const [selectedFiles, setSelectedFiles] = useState<Array<{ uri: string; fileName: string; type: string; mediaType: MediaType }>>([]);
  const [uploadedFiles, setUploadedFiles] = useState<UploadedFile[]>([]);
  const [isUploadingMedia, setIsUploadingMedia] = useState(false);
  const [keyboardHeight, setKeyboardHeight] = useState(0);

  // GIF state
  const [showGifPicker, setShowGifPicker] = useState(false);
  const [selectedGif, setSelectedGif] = useState<SelectedGif | null>(null);

  useEffect(() => {
    const keyboardDidShowListener = Keyboard.addListener('keyboardDidShow', (e) => {
      setKeyboardHeight(e.endCoordinates.height);
    });
    const keyboardDidHideListener = Keyboard.addListener('keyboardDidHide', () => {
      setKeyboardHeight(0);
    });

    return () => {
      keyboardDidShowListener?.remove();
      keyboardDidHideListener?.remove();
    };
  }, []);

  const createRepostMutation = useMutation({
    mutationFn: async () => {
      if (uploadedFiles.length > 0 || selectedGif) {
        const mediaFiles: MediaFile[] = uploadedFiles.map(file => ({
          fileName: file.fileName,
          mediaType: file.mediaType,
          width: file.width,
          height: file.height,
          fileSizeBytes: file.fileSizeBytes,
          duration: file.duration
        }));

        // Add GIF if selected
        if (selectedGif) {
          mediaFiles.push({
            fileName: `gif_${Date.now()}.gif`,
            mediaType: MediaType.Gif,
            width: selectedGif.width,
            height: selectedGif.height,
            gifUrl: selectedGif.url,
            gifPreviewUrl: selectedGif.previewUrl
          });
        }

        const data: CreateRepostWithMediaData = {
          content: content.trim() || undefined, // Allow empty content for simple reposts
          repostedPostId: repostedPost.id,
          privacy,
          mediaFiles
        };

        return await api.posts.createRepostWithMedia(data);
      } else {
        const data: CreateRepostData = {
          content: content.trim() || undefined, // Allow empty content for simple reposts
          repostedPostId: repostedPost.id,
          privacy
        };

        return await api.posts.createRepost(data);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['userPosts'] });
      queryClient.invalidateQueries({ queryKey: ['userTimeline'] });
      queryClient.invalidateQueries({ queryKey: ['reposts', repostedPost.id] });
      queryClient.invalidateQueries({ queryKey: ['post', repostedPost.id] });

      // Reset form
      setContent('');
      setSelectedFiles([]);
      setUploadedFiles([]);
      setSelectedGif(null);
      setPrivacy(PostPrivacy.Public);

      onClose();
    },
    onError: (error: any) => {
      console.error('Error creating repost:', error);
      Alert.alert('Error', 'Failed to create repost. Please try again.');
    },
  });

  const handleSubmit = () => {
    // Allow submission even with empty content for simple reposts
    createRepostMutation.mutate();
  };

  const pickImage = async () => {
    if (selectedFiles.length >= 10) {
      Alert.alert('Limit Reached', 'You can only upload up to 10 files per repost.');
      return;
    }

    const result = await ImagePicker.launchImageLibraryAsync({
      mediaTypes: ImagePicker.MediaTypeOptions.All,
      allowsMultipleSelection: true,
      quality: 0.8,
      videoQuality: ImagePicker.VideoQuality.Medium,
    });

    if (!result.canceled && result.assets) {
      const newFiles = result.assets.slice(0, 10 - selectedFiles.length).map(asset => ({
        uri: asset.uri,
        fileName: asset.fileName || `media_${Date.now()}`,
        type: asset.type || 'image',
        mediaType: asset.type === 'video' ? MediaType.Video : MediaType.Image
      }));

      setSelectedFiles(prev => [...prev, ...newFiles]);
      uploadMedia(newFiles);
    }
  };

  const uploadMedia = async (files: Array<{ uri: string; fileName: string; type: string; mediaType: MediaType }>) => {
    setIsUploadingMedia(true);
    
    try {
      const formData = new FormData();
      
      files.forEach((file, index) => {
        formData.append('files', {
          uri: file.uri,
          name: file.fileName,
          type: file.type === 'video' ? 'video/mp4' : 'image/jpeg',
        } as any);
      });

      const response = await api.uploads.uploadMultipleFiles(formData);
      
      if (response.uploadedFiles && response.uploadedFiles.length > 0) {
        setUploadedFiles(prev => [...prev, ...response.uploadedFiles]);
      }

      if (response.errors && response.errors.length > 0) {
        console.error('Upload errors:', response.errors);
        Alert.alert('Upload Error', `Some files failed to upload: ${response.errors.map(e => e.errorMessage).join(', ')}`);
      }
    } catch (error) {
      console.error('Upload failed:', error);
      Alert.alert('Upload Failed', 'Failed to upload media. Please try again.');
    } finally {
      setIsUploadingMedia(false);
    }
  };

  const removeFile = (index: number) => {
    setSelectedFiles(prev => prev.filter((_, i) => i !== index));
    setUploadedFiles(prev => prev.filter((_, i) => i !== index));
  };

  const removeGif = () => {
    setSelectedGif(null);
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString() + ' ' + date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  };

  return (
    <Modal
      visible={visible}
      animationType="slide"
      presentationStyle="pageSheet"
      onRequestClose={onClose}
    >
      <SafeAreaView style={[styles.container, { paddingBottom: keyboardHeight }]}>
        <KeyboardAvoidingView 
          style={styles.keyboardAvoid}
          behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        >
          {/* Header */}
          <View style={styles.header}>
            <TouchableOpacity onPress={onClose} style={styles.cancelButton}>
              <Text style={styles.cancelText}>Cancel</Text>
            </TouchableOpacity>
            <Text style={styles.headerTitle}>Repost</Text>
            <TouchableOpacity 
              onPress={handleSubmit}
              disabled={createRepostMutation.isPending || isUploadingMedia}
              style={[styles.postButton, (createRepostMutation.isPending || isUploadingMedia) && styles.postButtonDisabled]}
            >
              {createRepostMutation.isPending ? (
                <ActivityIndicator size="small" color="white" />
              ) : (
                <Text style={styles.postButtonText}>Repost</Text>
              )}
            </TouchableOpacity>
          </View>

          <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
            {/* User input area */}
            <View style={styles.inputSection}>
              <View style={styles.userInfo}>
                <Image source={{ uri: user?.profileImageUrl }} style={styles.avatar} />
                <Text style={styles.username}>{user?.displayName}</Text>
              </View>
              
              <TextInput
                style={styles.textInput}
                placeholder="Add a comment (optional)..."
                placeholderTextColor={colors.textSecondary}
                value={content}
                onChangeText={setContent}
                multiline
                maxLength={1024}
              />
              
              <Text style={styles.characterCount}>{content.length}/1024</Text>
            </View>

            {/* Media preview */}
            {selectedFiles.length > 0 && (
              <View style={styles.mediaPreview}>
                <ScrollView horizontal showsHorizontalScrollIndicator={false}>
                  {selectedFiles.map((file, index) => (
                    <View key={index} style={styles.mediaItem}>
                      <Image source={{ uri: file.uri }} style={styles.mediaImage} />
                      <TouchableOpacity 
                        style={styles.removeMediaButton}
                        onPress={() => removeFile(index)}
                      >
                        <Ionicons name="close-circle" size={24} color="red" />
                      </TouchableOpacity>
                    </View>
                  ))}
                </ScrollView>
              </View>
            )}

            {/* GIF preview */}
            {selectedGif && (
              <View style={styles.gifPreview}>
                <Image source={{ uri: selectedGif.previewUrl }} style={styles.gifImage} />
                <TouchableOpacity style={styles.removeGifButton} onPress={removeGif}>
                  <Ionicons name="close-circle" size={24} color="red" />
                </TouchableOpacity>
              </View>
            )}

            {/* Upload progress */}
            {isUploadingMedia && (
              <View style={styles.uploadProgress}>
                <ActivityIndicator size="small" color={colors.primary} />
                <Text style={styles.uploadText}>Uploading media...</Text>
              </View>
            )}

            {/* Reposted post preview */}
            <View style={styles.quotedPost}>
              <View style={styles.quotedPostHeader}>
                <Image source={{ uri: repostedPost.user.profileImageUrl }} style={styles.quotedAvatar} />
                <View style={styles.quotedUserInfo}>
                  <Text style={styles.quotedDisplayName}>{repostedPost.user.displayName}</Text>
                  <Text style={styles.quotedUsername}>@{repostedPost.user.username}</Text>
                  <Text style={styles.quotedDate}>{formatDate(repostedPost.createdAt)}</Text>
                </View>
              </View>
              
              <Text style={styles.quotedContent}>{repostedPost.content}</Text>
              
              {repostedPost.mediaItems && repostedPost.mediaItems.length > 0 && (
                <View style={styles.quotedMedia}>
                  <Image 
                    source={{ uri: repostedPost.mediaItems[0].imageUrl || repostedPost.mediaItems[0].videoThumbnailUrl }} 
                    style={styles.quotedMediaImage} 
                  />
                  {repostedPost.mediaItems.length > 1 && (
                    <View style={styles.mediaCountBadge}>
                      <Text style={styles.mediaCountText}>+{repostedPost.mediaItems.length - 1}</Text>
                    </View>
                  )}
                </View>
              )}
            </View>
          </ScrollView>

          {/* Bottom toolbar */}
          <View style={styles.toolbar}>
            <TouchableOpacity 
              onPress={pickImage}
              disabled={selectedFiles.length >= 10}
              style={[styles.toolbarButton, selectedFiles.length >= 10 && styles.toolbarButtonDisabled]}
            >
              <Ionicons name="image-outline" size={24} color={selectedFiles.length >= 10 ? colors.textSecondary : colors.primary} />
            </TouchableOpacity>
            
            <TouchableOpacity 
              onPress={() => setShowGifPicker(true)}
              disabled={selectedGif !== null}
              style={[styles.toolbarButton, selectedGif !== null && styles.toolbarButtonDisabled]}
            >
              <Ionicons name="happy-outline" size={24} color={selectedGif !== null ? colors.textSecondary : colors.primary} />
            </TouchableOpacity>
          </View>

          {/* GIF Picker */}
          {showGifPicker && (
            <GifPicker
              onGifSelect={(gif) => {
                setSelectedGif(gif);
                setShowGifPicker(false);
              }}
              onClose={() => setShowGifPicker(false)}
            />
          )}
        </KeyboardAvoidingView>
      </SafeAreaView>
    </Modal>
  );
}

const createStyles = (colors: any) => StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  keyboardAvoid: {
    flex: 1,
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
  cancelButton: {
    paddingVertical: 8,
    paddingHorizontal: 12,
  },
  cancelText: {
    color: colors.primary,
    fontSize: 16,
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: colors.text,
  },
  postButton: {
    backgroundColor: colors.primary,
    paddingVertical: 8,
    paddingHorizontal: 16,
    borderRadius: 20,
    minWidth: 70,
    alignItems: 'center',
  },
  postButtonDisabled: {
    opacity: 0.5,
  },
  postButtonText: {
    color: 'white',
    fontSize: 16,
    fontWeight: '600',
  },
  content: {
    flex: 1,
    paddingHorizontal: 16,
  },
  inputSection: {
    paddingVertical: 16,
  },
  userInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 12,
  },
  avatar: {
    width: 40,
    height: 40,
    borderRadius: 20,
    marginRight: 12,
  },
  username: {
    fontSize: 16,
    fontWeight: '600',
    color: colors.text,
  },
  textInput: {
    fontSize: 16,
    color: colors.text,
    minHeight: 80,
    textAlignVertical: 'top',
    paddingVertical: 8,
  },
  characterCount: {
    fontSize: 12,
    color: colors.textSecondary,
    textAlign: 'right',
    marginTop: 4,
  },
  mediaPreview: {
    marginVertical: 12,
  },
  mediaItem: {
    position: 'relative',
    marginRight: 8,
  },
  mediaImage: {
    width: 80,
    height: 80,
    borderRadius: 8,
  },
  removeMediaButton: {
    position: 'absolute',
    top: -8,
    right: -8,
    backgroundColor: 'white',
    borderRadius: 12,
  },
  gifPreview: {
    position: 'relative',
    marginVertical: 12,
    alignSelf: 'flex-start',
  },
  gifImage: {
    width: 200,
    height: 150,
    borderRadius: 8,
  },
  removeGifButton: {
    position: 'absolute',
    top: -8,
    right: -8,
    backgroundColor: 'white',
    borderRadius: 12,
  },
  uploadProgress: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
  },
  uploadText: {
    marginLeft: 8,
    color: colors.textSecondary,
  },
  quotedPost: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 12,
    padding: 12,
    marginVertical: 16,
    backgroundColor: colors.cardBackground,
  },
  quotedPostHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 8,
  },
  quotedAvatar: {
    width: 32,
    height: 32,
    borderRadius: 16,
    marginRight: 8,
  },
  quotedUserInfo: {
    flex: 1,
  },
  quotedDisplayName: {
    fontSize: 14,
    fontWeight: '600',
    color: colors.text,
  },
  quotedUsername: {
    fontSize: 12,
    color: colors.textSecondary,
  },
  quotedDate: {
    fontSize: 12,
    color: colors.textSecondary,
  },
  quotedContent: {
    fontSize: 14,
    color: colors.text,
    lineHeight: 20,
  },
  quotedMedia: {
    position: 'relative',
    marginTop: 8,
  },
  quotedMediaImage: {
    width: '100%',
    height: 150,
    borderRadius: 8,
  },
  mediaCountBadge: {
    position: 'absolute',
    top: 8,
    right: 8,
    backgroundColor: 'rgba(0, 0, 0, 0.7)',
    borderRadius: 12,
    paddingHorizontal: 8,
    paddingVertical: 4,
  },
  mediaCountText: {
    color: 'white',
    fontSize: 12,
    fontWeight: '600',
  },
  toolbar: {
    flexDirection: 'row',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  toolbarButton: {
    padding: 8,
    marginRight: 16,
  },
  toolbarButtonDisabled: {
    opacity: 0.5,
  },
});
