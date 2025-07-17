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
import { CreatePostData, CreatePostWithMediaData, PostPrivacy, MediaType, MediaFile, UploadedFile } from '../types';

interface CreatePostModalProps {
  visible: boolean;
  onClose: () => void;
}

export default function CreatePostModal({ visible, onClose }: CreatePostModalProps) {
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

  // Legacy state for backward compatibility
  const [selectedMedia, setSelectedMedia] = useState<string | null>(null);
  const [mediaType, setMediaType] = useState<'image' | 'video' | null>(null);
  const [uploadedFileName, setUploadedFileName] = useState<string | null>(null);

  useEffect(() => {
    const keyboardDidShowListener = Keyboard.addListener('keyboardDidShow', (e) => {
      setKeyboardHeight(e.endCoordinates.height);
    });
    const keyboardDidHideListener = Keyboard.addListener('keyboardDidHide', () => {
      setKeyboardHeight(0);
    });

    return () => {
      keyboardDidShowListener.remove();
      keyboardDidHideListener.remove();
    };
  }, []);

  const createPostMutation = useMutation({
    mutationFn: (data: CreatePostData) => api.posts.createPost(data),
    onSuccess: () => {
      // Refresh the timeline
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      resetForm();
      onClose();
      Alert.alert('Success', 'Post created successfully!');
    },
    onError: (error) => {
      console.error('Create post error:', error);
      Alert.alert('Error', 'Failed to create post. Please try again.');
    },
  });

  const createPostWithMediaMutation = useMutation({
    mutationFn: (data: CreatePostWithMediaData) => api.posts.createPostWithMedia(data),
    onSuccess: () => {
      // Refresh the timeline
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      resetForm();
      onClose();
      Alert.alert('Success', 'Post created successfully!');
    },
    onError: (error) => {
      console.error('Create post with media error:', error);
      Alert.alert('Error', 'Failed to create post. Please try again.');
    },
  });

  const multipleUploadMutation = useMutation({
    mutationFn: (files: Array<{ uri: string; fileName: string; type: string }>) => api.uploads.uploadMultipleFiles(files),
    onMutate: () => {
      setIsUploadingMedia(true);
    },
    onSuccess: (data) => {
      setUploadedFiles(data.uploadedFiles);
      setIsUploadingMedia(false);
      if (data.errors.length > 0) {
        const errorMessages = data.errors.map(e => `${e.originalFileName}: ${e.errorMessage}`).join('\n');
        Alert.alert('Upload Errors', `Some files failed to upload:\n${errorMessages}`);
      }
    },
    onError: (error) => {
      setIsUploadingMedia(false);
      console.error('Multiple upload error:', error);
      Alert.alert('Error', 'Failed to upload files. Please try again.');
    },
  });

  const resetForm = () => {
    setContent('');
    setSelectedFiles([]);
    setUploadedFiles([]);
    setPrivacy(PostPrivacy.Public);
    setIsUploadingMedia(false);
    // Legacy cleanup
    setSelectedMedia(null);
    setMediaType(null);
    setUploadedFileName(null);
  };

  const pickMedia = async () => {
    try {
      // Check if we're at the limit
      const totalFiles = selectedFiles.length + uploadedFiles.length;
      if (totalFiles >= 10) {
        Alert.alert('Limit Reached', 'You can only select up to 10 files per post.');
        return;
      }

      // Request permission
      const permissionResult = await ImagePicker.requestMediaLibraryPermissionsAsync();

      if (permissionResult.granted === false) {
        Alert.alert('Permission Required', 'Permission to access camera roll is required!');
        return;
      }

      // Calculate remaining slots
      const remainingSlots = 10 - totalFiles;

      // Launch media picker supporting both images and videos
      const result = await ImagePicker.launchImageLibraryAsync({
        mediaTypes: ['images', 'videos'], // Support both images and videos
        allowsEditing: false, // Disable editing for multiple selection
        quality: 0.8, // Good quality for images
        allowsMultipleSelection: true,
        selectionLimit: Math.min(remainingSlots, 10),
        exif: false,
        videoMaxDuration: 60, // Limit videos to 60 seconds
      });

      if (!result.canceled && result.assets && result.assets.length > 0) {
        console.log('Selected assets:', result.assets.length);

        const validFiles: Array<{ uri: string; fileName: string; type: string; mediaType: MediaType }> = [];
        const errors: string[] = [];

        for (const asset of result.assets) {
          console.log('Processing asset:', JSON.stringify(asset, null, 2));

          // Determine if this is an image or video
          const isVideo = asset.type === 'video' ||
                         (asset.mimeType && asset.mimeType.startsWith('video/')) ||
                         (asset.fileName && /\.(mp4|mov|avi|wmv|flv|webm|mkv)$/i.test(asset.fileName));

          const fileName = asset.fileName || (isVideo ? `video_${Date.now()}.mp4` : `image_${Date.now()}.jpg`);

          // Validate file size based on type
          if (isVideo) {
            // Video size limit: 100MB (matching backend)
            const maxVideoSize = 100 * 1024 * 1024; // 100MB in bytes
            if (asset.fileSize && asset.fileSize > maxVideoSize) {
              errors.push(`${fileName}: Video too large (max 100MB)`);
              continue;
            }

            // Validate video file type
            const supportedVideoTypes = ['mp4', 'mov', 'avi', 'wmv', 'flv', 'webm', 'mkv'];
            const fileExtension = fileName.toLowerCase().split('.').pop();

            if (!fileExtension || !supportedVideoTypes.includes(fileExtension)) {
              errors.push(`${fileName}: Unsupported video format`);
              continue;
            }
          } else {
            // Image size limit: 5MB (matching existing logic)
            const maxImageSize = 5 * 1024 * 1024; // 5MB in bytes
            if (asset.fileSize && asset.fileSize > maxImageSize) {
              errors.push(`${fileName}: Image too large (max 5MB)`);
              continue;
            }

            // Validate image file type
            const supportedImageTypes = ['jpg', 'jpeg', 'png', 'gif', 'heic', 'webp'];
            const fileExtension = fileName.toLowerCase().split('.').pop();

            if (!fileExtension || !supportedImageTypes.includes(fileExtension)) {
              errors.push(`${fileName}: Unsupported image format`);
              continue;
            }
          }

          // Determine MIME type
          let mimeType = asset.mimeType || asset.type;
          if (isVideo) {
            if (!mimeType || !mimeType.startsWith('video/')) {
              // Fallback based on file extension
              if (fileName.toLowerCase().endsWith('.mov')) {
                mimeType = 'video/quicktime';
              } else if (fileName.toLowerCase().endsWith('.avi')) {
                mimeType = 'video/x-msvideo';
              } else if (fileName.toLowerCase().endsWith('.wmv')) {
                mimeType = 'video/x-ms-wmv';
              } else if (fileName.toLowerCase().endsWith('.flv')) {
                mimeType = 'video/x-flv';
              } else if (fileName.toLowerCase().endsWith('.webm')) {
                mimeType = 'video/webm';
              } else if (fileName.toLowerCase().endsWith('.mkv')) {
                mimeType = 'video/x-matroska';
              } else {
                mimeType = 'video/mp4';
              }
            }
          } else {
            if (!mimeType || !mimeType.startsWith('image/')) {
              // Fallback based on file extension
              if (fileName.toLowerCase().endsWith('.png')) {
                mimeType = 'image/png';
              } else if (fileName.toLowerCase().endsWith('.gif')) {
                mimeType = 'image/gif';
              } else if (fileName.toLowerCase().endsWith('.heic')) {
                mimeType = 'image/heic';
              } else if (fileName.toLowerCase().endsWith('.webp')) {
                mimeType = 'image/webp';
              } else {
                mimeType = 'image/jpeg';
              }
            }
          }

          validFiles.push({
            uri: asset.uri,
            fileName,
            type: mimeType,
            mediaType: isVideo ? MediaType.Video : MediaType.Image,
          });
        }

        if (errors.length > 0) {
          Alert.alert('File Validation Errors', errors.join('\n'));
        }

        if (validFiles.length > 0) {
          // Add to selected files and upload immediately
          setSelectedFiles(prev => [...prev, ...validFiles]);

          // Upload files
          const uploadFiles = validFiles.map(file => ({
            uri: file.uri,
            fileName: file.fileName,
            type: file.type,
          }));

          multipleUploadMutation.mutate(uploadFiles);
        }
      }
    } catch (error) {
      console.error('Error picking media:', error);
      Alert.alert('Error', 'Failed to pick media. Please try again.');
    }
  };

  const uploadImage = async (uri: string, fileName: string, type: string) => {
    try {
      setIsUploadingMedia(true);
      const response = await api.images.uploadImage(uri, fileName, type);
      setUploadedFileName(response.fileName);
    } catch (error) {
      console.error('Error uploading image:', error);
      Alert.alert('Error', 'Failed to upload image. Please try again.');
      setSelectedMedia(null);
      setMediaType(null);
    } finally {
      setIsUploadingMedia(false);
    }
  };

  const uploadVideo = async (uri: string, fileName: string, type: string) => {
    try {
      setIsUploadingMedia(true);
      const response = await api.videos.uploadVideo(uri, fileName, type);
      setUploadedFileName(response.fileName);
    } catch (error) {
      console.error('Error uploading video:', error);
      Alert.alert('Error', 'Failed to upload video. Please try again.');
      setSelectedMedia(null);
      setMediaType(null);
    } finally {
      setIsUploadingMedia(false);
    }
  };

  const removeMedia = () => {
    setSelectedMedia(null);
    setMediaType(null);
    setUploadedFileName(null);
    setIsUploadingMedia(false);
  };

  const removeFile = (index: number, isUploaded: boolean = false) => {
    if (isUploaded) {
      setUploadedFiles(prev => prev.filter((_, i) => i !== index));
    } else {
      setSelectedFiles(prev => prev.filter((_, i) => i !== index));
    }
  };

  const removeAllMedia = () => {
    setSelectedFiles([]);
    setUploadedFiles([]);
    // Legacy cleanup
    setSelectedMedia(null);
    setMediaType(null);
    setUploadedFileName(null);
    setIsUploadingMedia(false);
  };

  const handleSubmit = () => {
    // Allow submission if either content exists or media files are uploaded
    const hasContent = content.trim().length > 0;
    const hasMedia = uploadedFiles.length > 0 || selectedMedia || uploadedFileName;

    if (!hasContent && !hasMedia) {
      Alert.alert('Error', 'Please enter some content or add media for your post.');
      return;
    }

    if (content.length > 256) {
      Alert.alert('Error', 'Post content must be 256 characters or less.');
      return;
    }

    if (isUploadingMedia) {
      Alert.alert('Please wait', 'Media is still uploading...');
      return;
    }

    // Use new multiple media API if we have uploaded files
    if (uploadedFiles.length > 0) {
      const mediaFiles: MediaFile[] = uploadedFiles.map(file => ({
        fileName: file.fileName,
        mediaType: file.mediaType,
        width: file.width,
        height: file.height,
        fileSizeBytes: file.fileSizeBytes,
        duration: file.duration,
      }));

      const postData: CreatePostWithMediaData = {
        content: content.trim() || undefined,
        privacy,
        mediaFiles,
      };

      createPostWithMediaMutation.mutate(postData);
    } else {
      // Use legacy API for backward compatibility
      const postData: CreatePostData = {
        content: content.trim(),
        privacy,
      };

      // Add the appropriate file name based on media type
      if (uploadedFileName) {
        if (mediaType === 'video') {
          postData.videoFileName = uploadedFileName;
        } else {
          postData.imageFileName = uploadedFileName;
        }
      }

      createPostMutation.mutate(postData);
    }
  };

  const handleClose = () => {
    if ((content.trim() || selectedMedia) && !createPostMutation.isPending) {
      Alert.alert(
        'Discard Post?',
        'You have unsaved changes. Are you sure you want to discard this post?',
        [
          { text: 'Cancel', style: 'cancel' },
          {
            text: 'Discard',
            style: 'destructive',
            onPress: () => {
              resetForm();
              onClose();
            }
          },
        ]
      );
    } else {
      resetForm();
      onClose();
    }
  };

  const cyclePrivacy = () => {
    switch (privacy) {
      case PostPrivacy.Public:
        setPrivacy(PostPrivacy.Followers);
        break;
      case PostPrivacy.Followers:
        setPrivacy(PostPrivacy.Private);
        break;
      case PostPrivacy.Private:
        setPrivacy(PostPrivacy.Public);
        break;
      default:
        setPrivacy(PostPrivacy.Public);
    }
  };

  const getPrivacyIcon = () => {
    switch (privacy) {
      case PostPrivacy.Public:
        return "globe-outline";
      case PostPrivacy.Followers:
        return "people-outline";
      case PostPrivacy.Private:
        return "lock-closed-outline";
      default:
        return "globe-outline";
    }
  };

  const getPrivacyText = () => {
    switch (privacy) {
      case PostPrivacy.Public:
        return "Public";
      case PostPrivacy.Followers:
        return "Followers";
      case PostPrivacy.Private:
        return "Private";
      default:
        return "Public";
    }
  };

  const remainingChars = 256 - content.length;
  const isOverLimit = remainingChars < 0;
  const hasContent = content.trim().length > 0;
  const hasMedia = uploadedFiles.length > 0 || selectedMedia || uploadedFileName;
  const canSubmit = (hasContent || hasMedia) && !isOverLimit && !createPostMutation.isPending && !createPostWithMediaMutation.isPending && !isUploadingMedia;

  return (
    <Modal
      visible={visible}
      animationType="slide"
      presentationStyle="pageSheet"
      onRequestClose={handleClose}
    >
      <SafeAreaView style={styles.container}>
        <KeyboardAvoidingView
          style={styles.keyboardAvoid}
          behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
          keyboardVerticalOffset={Platform.OS === 'ios' ? 0 : 20}
        >
          {/* Header */}
          <View style={styles.header}>
            <TouchableOpacity onPress={handleClose} style={styles.headerButton}>
              <Text style={styles.cancelText}>Cancel</Text>
            </TouchableOpacity>
            
            <Text style={styles.headerTitle}>New Post</Text>
            
            <TouchableOpacity 
              onPress={handleSubmit} 
              style={[styles.headerButton, styles.postButton, !canSubmit && styles.postButtonDisabled]}
              disabled={!canSubmit}
            >
              {createPostMutation.isPending ? (
                <Text style={[styles.postText, styles.postTextDisabled]}>Posting...</Text>
              ) : (
                <Text style={[styles.postText, !canSubmit && styles.postTextDisabled]}>Post</Text>
              )}
            </TouchableOpacity>
          </View>

          {/* User Info */}
          <View style={styles.userInfo}>
            <View style={styles.avatar}>
              <Text style={styles.avatarText}>{user?.username.charAt(0).toUpperCase()}</Text>
            </View>
            <Text style={styles.username}>@{user?.username}</Text>
          </View>

          {/* Controls - moved to top */}
          <View style={styles.topControls}>
            <View style={styles.controlsLeft}>
              <TouchableOpacity
                style={styles.privacyButton}
                onPress={cyclePrivacy}
              >
                <Ionicons
                  name={getPrivacyIcon()}
                  size={16}
                  color="#6B7280"
                />
                <Text style={styles.privacyText}>
                  {getPrivacyText()}
                </Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={styles.mediaButton}
                onPress={pickMedia}
                disabled={isUploadingMedia || (uploadedFiles.length + selectedFiles.length) >= 10}
              >
                <Ionicons
                  name="attach-outline"
                  size={20}
                  color={isUploadingMedia || (uploadedFiles.length + selectedFiles.length) >= 10 ? "#9CA3AF" : "#6B7280"}
                />
                {(uploadedFiles.length + selectedFiles.length) > 0 && (
                  <Text style={styles.fileCount}>
                    {uploadedFiles.length + selectedFiles.length}/10
                  </Text>
                )}
              </TouchableOpacity>
            </View>

            <Text style={[styles.charCount, isOverLimit && styles.charCountOver]}>
              {remainingChars}
            </Text>
          </View>

          {/* Scrollable Content */}
          <ScrollView
            style={styles.scrollContainer}
            contentContainerStyle={styles.contentContainer}
            keyboardShouldPersistTaps="handled"
            showsVerticalScrollIndicator={false}
          >
            <TextInput
              style={styles.contentInput}
              placeholder="What's happening?"
              placeholderTextColor={colors.textMuted}
              value={content}
              onChangeText={setContent}
              multiline
              maxLength={300} // Allow a bit over to show error
              autoFocus
              textAlignVertical="top"
            />

            {/* Multiple Media Preview */}
            {(uploadedFiles.length > 0 || selectedFiles.length > 0) && (
              <View style={styles.mediaContainer}>
                <View style={styles.mediaHeader}>
                  <Text style={styles.mediaCount}>
                    {uploadedFiles.length + selectedFiles.length} file(s) selected (max 10)
                  </Text>
                  <TouchableOpacity onPress={removeAllMedia}>
                    <Text style={styles.removeAllText}>Remove all</Text>
                  </TouchableOpacity>
                </View>

                <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.mediaScroll}>
                  {/* Uploaded Files */}
                  {uploadedFiles.map((file, index) => (
                    <View key={`uploaded-${index}`} style={styles.mediaItem}>
                      {file.mediaType === MediaType.Image ? (
                        <Image source={{ uri: file.fileUrl }} style={styles.mediaThumbnail} />
                      ) : (
                        <View style={[styles.mediaThumbnail, styles.videoThumbnail]}>
                          <Ionicons name="play-circle" size={32} color="#6B7280" />
                        </View>
                      )}
                      <TouchableOpacity
                        style={styles.removeItemButton}
                        onPress={() => removeFile(index, true)}
                      >
                        <Ionicons name="close" size={16} color="#fff" />
                      </TouchableOpacity>
                      <View style={styles.uploadedBadge}>
                        <Ionicons name="checkmark" size={12} color="#fff" />
                      </View>
                    </View>
                  ))}

                  {/* Files being uploaded */}
                  {selectedFiles.map((file, index) => (
                    <View key={`selected-${index}`} style={styles.mediaItem}>
                      {file.mediaType === MediaType.Image ? (
                        <Image source={{ uri: file.uri }} style={styles.mediaThumbnail} />
                      ) : (
                        <View style={[styles.mediaThumbnail, styles.videoThumbnail]}>
                          <Ionicons name="play-circle" size={32} color="#6B7280" />
                        </View>
                      )}
                      <TouchableOpacity
                        style={styles.removeItemButton}
                        onPress={() => removeFile(index, false)}
                      >
                        <Ionicons name="close" size={16} color="#fff" />
                      </TouchableOpacity>
                      {isUploadingMedia && (
                        <View style={styles.uploadingOverlay}>
                          <ActivityIndicator size="small" color="#3B82F6" />
                        </View>
                      )}
                    </View>
                  ))}
                </ScrollView>
              </View>
            )}

            {/* Legacy Media Preview (for backward compatibility) */}
            {selectedMedia && !uploadedFiles.length && !selectedFiles.length && (
              <View style={styles.mediaContainer}>
                {mediaType === 'video' ? (
                  <View style={styles.videoPreview}>
                    <Ionicons name="play-circle" size={64} color="#6B7280" />
                    <Text style={styles.videoText}>Video selected</Text>
                  </View>
                ) : (
                  <Image source={{ uri: selectedMedia }} style={styles.imagePreview} />
                )}
                {isUploadingMedia && (
                  <View style={styles.mediaOverlay}>
                    <ActivityIndicator size="large" color="#3B82F6" />
                    <Text style={styles.uploadingText}>
                      {mediaType === 'video' ? 'Uploading video...' : 'Uploading image...'}
                    </Text>
                  </View>
                )}
                <TouchableOpacity style={styles.removeMediaButton} onPress={removeAllMedia}>
                  <Ionicons name="close" size={20} color="#fff" />
                </TouchableOpacity>
              </View>
            )}
          </ScrollView>
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
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  headerButton: {
    paddingHorizontal: 8,
    paddingVertical: 4,
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: colors.text,
  },
  cancelText: {
    fontSize: 16,
    color: colors.textSecondary,
  },
  postButton: {
    backgroundColor: colors.primary,
    borderRadius: 20,
    paddingHorizontal: 16,
    paddingVertical: 8,
  },
  postButtonDisabled: {
    backgroundColor: colors.border,
  },
  postText: {
    color: colors.primaryText,
    fontWeight: '600',
    fontSize: 16,
  },
  postTextDisabled: {
    color: colors.textMuted,
  },
  userInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  avatar: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: colors.primary,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 12,
  },
  avatarText: {
    color: colors.primaryText,
    fontWeight: '600',
    fontSize: 16,
  },
  username: {
    fontSize: 16,
    fontWeight: '500',
    color: colors.text,
  },
  topControls: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  controlsLeft: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  scrollContainer: {
    flex: 1,
  },
  contentContainer: {
    flexGrow: 1,
    paddingHorizontal: 16,
    paddingBottom: 20,
  },
  contentInput: {
    fontSize: 18,
    color: colors.text,
    lineHeight: 24,
    minHeight: 120,
    maxHeight: 200, // Prevent input from taking too much space
    textAlignVertical: 'top',
  },

  privacyButton: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 16,
    backgroundColor: '#F3F4F6',
    alignSelf: 'flex-start',
  },
  privacyText: {
    marginLeft: 4,
    fontSize: 14,
    color: colors.textSecondary,
    fontWeight: '500',
  },
  charCount: {
    fontSize: 14,
    color: colors.textSecondary,
    fontWeight: '500',
  },
  charCountOver: {
    color: colors.error,
  },
  mediaButton: {
    marginLeft: 12,
    padding: 8,
    borderRadius: 20,
    backgroundColor: colors.surface,
  },
  mediaContainer: {
    marginTop: 16,
    borderRadius: 12,
    overflow: 'hidden',
    position: 'relative',
  },
  imagePreview: {
    width: '100%',
    height: 200,
    resizeMode: 'cover',
  },
  videoPreview: {
    width: '100%',
    height: 200,
    backgroundColor: colors.surface,
    justifyContent: 'center',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 12,
  },
  videoText: {
    marginTop: 8,
    fontSize: 16,
    color: colors.textSecondary,
    fontWeight: '500',
  },
  mediaOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  uploadingText: {
    color: '#fff',
    marginTop: 8,
    fontSize: 16,
    fontWeight: '500',
  },
  removeMediaButton: {
    position: 'absolute',
    top: 8,
    right: 8,
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
    borderRadius: 16,
    width: 32,
    height: 32,
    justifyContent: 'center',
    alignItems: 'center',
  },
  fileCount: {
    position: 'absolute',
    top: -4,
    right: -4,
    backgroundColor: '#3B82F6',
    borderRadius: 8,
    paddingHorizontal: 4,
    paddingVertical: 1,
    fontSize: 10,
    color: '#fff',
    fontWeight: 'bold',
    minWidth: 16,
    textAlign: 'center',
  },
  mediaHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  mediaCount: {
    fontSize: 14,
    color: colors.textSecondary,
    fontWeight: '500',
  },
  removeAllText: {
    fontSize: 14,
    color: '#EF4444',
    fontWeight: '500',
  },
  mediaScroll: {
    flexDirection: 'row',
  },
  mediaItem: {
    marginRight: 8,
    position: 'relative',
  },
  mediaThumbnail: {
    width: 80,
    height: 80,
    borderRadius: 8,
    backgroundColor: '#F3F4F6',
  },
  videoThumbnail: {
    justifyContent: 'center',
    alignItems: 'center',
  },
  removeItemButton: {
    position: 'absolute',
    top: -4,
    right: -4,
    backgroundColor: '#EF4444',
    borderRadius: 12,
    width: 24,
    height: 24,
    justifyContent: 'center',
    alignItems: 'center',
  },
  uploadedBadge: {
    position: 'absolute',
    bottom: -4,
    right: -4,
    backgroundColor: '#10B981',
    borderRadius: 12,
    width: 24,
    height: 24,
    justifyContent: 'center',
    alignItems: 'center',
  },
  uploadingOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
  },
});
