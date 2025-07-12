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
import { CreatePostData, PostPrivacy } from '../types';

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
  const [selectedMedia, setSelectedMedia] = useState<string | null>(null);
  const [mediaType, setMediaType] = useState<'image' | 'video' | null>(null);
  const [uploadedFileName, setUploadedFileName] = useState<string | null>(null);
  const [isUploadingMedia, setIsUploadingMedia] = useState(false);
  const [keyboardHeight, setKeyboardHeight] = useState(0);

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

  const resetForm = () => {
    setContent('');
    setSelectedMedia(null);
    setMediaType(null);
    setUploadedFileName(null);
    setIsUploadingMedia(false);
  };

  const pickMedia = async () => {
    try {
      // Request permission
      const permissionResult = await ImagePicker.requestMediaLibraryPermissionsAsync();

      if (permissionResult.granted === false) {
        Alert.alert('Permission Required', 'Permission to access camera roll is required!');
        return;
      }

      // Launch media picker supporting both images and videos
      const result = await ImagePicker.launchImageLibraryAsync({
        mediaTypes: ['images', 'videos'], // Support both images and videos
        allowsEditing: true,
        aspect: [4, 3],
        quality: 0.5, // Reduced quality for images
        allowsMultipleSelection: false,
        selectionLimit: 1,
        exif: false,
        videoMaxDuration: 60, // Limit videos to 60 seconds
      });

      if (!result.canceled && result.assets[0]) {
        const asset = result.assets[0];
        console.log('Full asset details:', JSON.stringify(asset, null, 2));

        // Determine if this is an image or video
        const isVideo = asset.type === 'video' ||
                       (asset.mimeType && asset.mimeType.startsWith('video/')) ||
                       (asset.fileName && /\.(mp4|mov|avi|wmv|flv|webm|mkv)$/i.test(asset.fileName));

        // Validate file size based on type
        if (isVideo) {
          // Video size limit: 100MB (matching backend)
          const maxVideoSize = 100 * 1024 * 1024; // 100MB in bytes
          if (asset.fileSize && asset.fileSize > maxVideoSize) {
            Alert.alert('File Too Large', 'Video files must be less than 100MB. Please select a smaller video or compress it.');
            return;
          }
        } else {
          // Image size limit: 5MB (matching existing logic)
          const maxImageSize = 5 * 1024 * 1024; // 5MB in bytes
          if (asset.fileSize && asset.fileSize > maxImageSize) {
            Alert.alert('File Too Large', 'Image files must be less than 5MB. Please select a smaller image.');
            return;
          }
        }

        setSelectedMedia(asset.uri);
        setMediaType(isVideo ? 'video' : 'image');

        // Upload media immediately
        const fileName = asset.fileName || (isVideo ? 'video.mp4' : 'image.jpg');
        let mimeType = asset.mimeType || asset.type;

        console.log('Original mimeType:', mimeType, 'fileName:', fileName, 'isVideo:', isVideo);

        if (isVideo) {
          // Validate video file type
          const supportedVideoTypes = ['mp4', 'mov', 'avi', 'wmv', 'flv', 'webm', 'mkv'];
          const fileExtension = fileName.toLowerCase().split('.').pop();

          if (!fileExtension || !supportedVideoTypes.includes(fileExtension)) {
            Alert.alert('Unsupported Video Format', 'Please select a video in one of these formats: MP4, MOV, AVI, WMV, FLV, WebM, MKV');
            return;
          }

          // Handle video MIME types
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
          await uploadVideo(asset.uri, fileName, mimeType);
        } else {
          // Validate image file type
          const supportedImageTypes = ['jpg', 'jpeg', 'png', 'gif', 'heic', 'webp'];
          const fileExtension = fileName.toLowerCase().split('.').pop();

          if (!fileExtension || !supportedImageTypes.includes(fileExtension)) {
            Alert.alert('Unsupported Image Format', 'Please select an image in one of these formats: JPG, PNG, GIF, HEIC, WebP');
            return;
          }

          // Handle image MIME types
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
          await uploadImage(asset.uri, fileName, mimeType);
        }

        console.log('Final mimeType:', mimeType);
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

  const handleSubmit = () => {
    if (!content.trim()) {
      Alert.alert('Error', 'Please enter some content for your post.');
      return;
    }

    if (content.length > 256) {
      Alert.alert('Error', 'Post content must be 256 characters or less.');
      return;
    }

    if (isUploadingMedia) {
      Alert.alert('Please wait', `${mediaType === 'video' ? 'Video' : 'Media'} is still uploading...`);
      return;
    }

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
  const canSubmit = content.trim().length > 0 && !isOverLimit && !createPostMutation.isPending && !isUploadingMedia;

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
                disabled={isUploadingMedia}
              >
                <Ionicons
                  name="attach-outline"
                  size={20}
                  color={isUploadingMedia ? "#9CA3AF" : "#6B7280"}
                />
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

            {/* Media Preview */}
            {selectedMedia && (
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
                <TouchableOpacity style={styles.removeMediaButton} onPress={removeMedia}>
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
});
