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
  Linking,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import * as ImagePicker from 'expo-image-picker';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../contexts/AuthContext';
import { useThemeColors } from '../hooks/useThemeColors';
import { CreatePostData, CreatePostWithMediaData, PostPrivacy, MediaType, MediaFile, UploadedFile } from '../types';
import GifPicker from './GifPicker';
import type { SelectedGif } from '../lib/tenor';

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

  // GIF state
  const [showGifPicker, setShowGifPicker] = useState(false);
  const [selectedGif, setSelectedGif] = useState<SelectedGif | null>(null);

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
      setSelectedFiles([]); // Clear selected files after successful upload
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
    setSelectedGif(null);
    setPrivacy(PostPrivacy.Public);
    setIsUploadingMedia(false);
    // Legacy cleanup
    setSelectedMedia(null);
    setMediaType(null);
    setUploadedFileName(null);
  };

  const pickMedia = async () => {
    console.log('=== pickMedia function started ===');
    console.log('=== ImagePicker available?', typeof ImagePicker);
    console.log('=== ImagePicker.MediaTypeOptions available?', typeof ImagePicker.MediaTypeOptions);
    console.log('=== ImagePicker.requestMediaLibraryPermissionsAsync available?', typeof ImagePicker.requestMediaLibraryPermissionsAsync);

    try {
      console.log('=== Step 1: Inside try block ===');

      console.log('=== Step 2: About to check permissions ===');
      let permissionResult;

      try {
        // Try to get current permissions first
        console.log('=== Step 2a: Getting current permissions ===');
        const currentPermissions = await ImagePicker.getMediaLibraryPermissionsAsync();
        console.log('=== Step 2b: Current permissions:', currentPermissions);

        if (currentPermissions.status !== 'granted') {
          console.log('=== Step 2c: Requesting new permissions ===');
          permissionResult = await ImagePicker.requestMediaLibraryPermissionsAsync();
          console.log('=== Step 3: Permission request completed ===');
          console.log('Permission result:', permissionResult);
        } else {
          console.log('=== Step 3: Permissions already granted ===');
          permissionResult = currentPermissions;
        }
      } catch (permError) {
        console.error('=== CRASH: Permission request failed ===');
        console.error('Permission error:', permError);
        console.error('Permission error type:', typeof permError);
        console.error('Permission error message:', permError instanceof Error ? permError.message : 'No message');
        Alert.alert('Permission Error', `Permission request failed: ${permError instanceof Error ? permError.message : 'Unknown error'}`);
        return;
      }

      if (permissionResult.status !== 'granted') {
        console.log('=== Permission denied ===');
        Alert.alert('Permission Required', 'Permission to access camera roll is required!');
        return;
      }

      console.log('=== Step 4: About to launch image picker ===');

      console.log('=== Step 5: Creating picker options ===');
      const pickerOptions = {
        mediaTypes: ImagePicker.MediaTypeOptions.Images,
        allowsEditing: false,
        quality: 1,
      };
      console.log('=== Step 6: Picker options created ===', pickerOptions);

      let result;
      try {
        console.log('=== Step 7: About to call launchImageLibraryAsync ===');
        result = await ImagePicker.launchImageLibraryAsync(pickerOptions);
        console.log('=== Step 8: Image picker completed successfully ===');
      } catch (pickerError) {
        console.error('=== CRASH: Image picker failed ===');
        console.error('Picker error:', pickerError);
        console.error('Picker error details:', JSON.stringify(pickerError, null, 2));
        Alert.alert('Image Picker Error', `The image picker crashed: ${pickerError instanceof Error ? pickerError.message : 'Unknown error'}`);
        return;
      }

      console.log('Result:', JSON.stringify(result, null, 2));

      if (!result.canceled && result.assets && result.assets.length > 0) {
        console.log('Selected assets:', result.assets.length);
        Alert.alert('Success!', `Selected ${result.assets.length} file(s). Check console for details.`);
        // TODO: Add back file processing logic once we confirm picker works

        // Temporarily comment out complex processing to test if picker works
        /*
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
            // Video size limit: 1GB (matching backend)
            const maxVideoSize = 1024 * 1024 * 1024; // 1GB in bytes
            if (asset.fileSize && asset.fileSize > maxVideoSize) {
              errors.push(`${fileName}: Video too large (max 1GB)`);
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
        */
      }
    } catch (error) {
      console.error('=== CRASH: Unexpected error in pickMedia ===');
      console.error('Error picking media:', error);
      console.error('Error details:', JSON.stringify(error, null, 2));
      console.error('Error stack:', error instanceof Error ? error.stack : 'No stack trace');
      Alert.alert('Unexpected Error', `Failed to pick media: ${error instanceof Error ? error.message : 'Unknown error'}. Please try again.`);
    }

    console.log('=== pickMedia function completed ===');
  };

  const pickMediaWorking = async () => {
    try {
      console.log('=== Starting proper image picker with permissions ===');
      console.log('=== Device info - Platform:', Platform.OS);
      console.log('=== Device info - Is simulator:', __DEV__);

      // Check if we're at the limit
      const totalFiles = selectedFiles.length + uploadedFiles.length;
      if (totalFiles >= 10) {
        Alert.alert('Limit Reached', 'You can only select up to 10 files per post.');
        return;
      }

      // Check current permissions first
      console.log('=== Checking current permissions ===');
      const currentPermissions = await ImagePicker.getMediaLibraryPermissionsAsync();
      console.log('=== Current permissions:', currentPermissions);

      // Now that we have proper native permissions, request them properly
      console.log('=== Requesting media library permissions ===');
      const { status } = await ImagePicker.requestMediaLibraryPermissionsAsync();
      console.log('=== Permission status:', status);

      if (status !== 'granted') {
        console.log('=== Permission denied, showing alert ===');
        Alert.alert(
          'Permission Required',
          'Permission to access camera roll is required! Please go to Settings > Privacy & Security > Photos and enable access for this app.',
          [
            { text: 'Cancel', style: 'cancel' },
            { text: 'Open Settings', onPress: () => {
              // On iOS, this will open the app's settings page
              if (Platform.OS === 'ios') {
                Linking.openURL('app-settings:');
              }
            }}
          ]
        );
        return;
      }

      console.log('=== Launching image picker with proper permissions ===');

      const result = await ImagePicker.launchImageLibraryAsync({
        mediaTypes: ImagePicker.MediaTypeOptions.All, // Support both images and videos
        allowsEditing: false,
        quality: 0.8,
        allowsMultipleSelection: false, // Start with single selection for stability
      });

      console.log('=== Image picker completed ===');

      if (!result.canceled && result.assets && result.assets.length > 0) {
        const asset = result.assets[0];
        console.log('Processing selected asset:', asset);

        // Determine media type and create file info
        let mediaType: MediaType;
        let mimeType: string;
        let fileName: string;

        if (asset.type === 'image') {
          mediaType = MediaType.Image;
          const extension = asset.uri.split('.').pop()?.toLowerCase() || 'jpg';
          mimeType = asset.mimeType || `image/${extension === 'jpg' ? 'jpeg' : extension}`;
          fileName = asset.fileName || `image_${Date.now()}.${extension}`;
        } else if (asset.type === 'video') {
          mediaType = MediaType.Video;
          const extension = asset.uri.split('.').pop()?.toLowerCase() || 'mp4';
          mimeType = asset.mimeType || `video/${extension}`;
          fileName = asset.fileName || `video_${Date.now()}.${extension}`;
        } else {
          Alert.alert('Error', 'Unsupported file type');
          return;
        }

        // Basic file size validation
        const maxSize = mediaType === MediaType.Video ? 1024 * 1024 * 1024 : 5 * 1024 * 1024; // 1GB for video, 5MB for image
        if (asset.fileSize && asset.fileSize > maxSize) {
          Alert.alert('Error', `File too large. Maximum size is ${mediaType === MediaType.Video ? '1GB' : '5MB'}.`);
          return;
        }

        console.log('Valid file:', { uri: asset.uri, fileName, type: mimeType, mediaType });

        // Add to selected files
        const fileInfo = {
          uri: asset.uri,
          fileName,
          type: mimeType,
          mediaType,
        };

        setSelectedFiles(prev => [...prev, fileInfo]);

        // Upload the file
        multipleUploadMutation.mutate([{
          uri: asset.uri,
          fileName: fileName,
          type: mimeType,
        }]);

        console.log('=== File added and upload started ===');
      }
    } catch (error) {
      console.error('=== Error in pickMediaWorking ===');
      console.error('Error:', error);
      Alert.alert('Error', `Failed to pick media: ${error instanceof Error ? error.message : 'Unknown error'}`);
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
    // Allow submission if either content exists or media files are uploaded or GIF is selected
    const hasContent = content.trim().length > 0;
    const hasMedia = uploadedFiles.length > 0 || selectedMedia || uploadedFileName;
    const hasGif = selectedGif !== null;

    if (!hasContent && !hasMedia && !hasGif) {
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

    // Handle GIF submission
    if (selectedGif) {
      const mediaFiles: MediaFile[] = [{
        fileName: selectedGif.id, // Use GIF ID as filename
        mediaType: MediaType.Gif,
        width: selectedGif.width,
        height: selectedGif.height,
        gifUrl: selectedGif.url,
        gifPreviewUrl: selectedGif.previewUrl,
      }];

      const postData: CreatePostWithMediaData = {
        content: content.trim() || undefined,
        privacy,
        mediaFiles,
      };

      createPostWithMediaMutation.mutate(postData);
    }
    // Use new multiple media API if we have uploaded files
    else if (uploadedFiles.length > 0) {
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

  const handleGifSelect = (gif: SelectedGif) => {
    // Clear other media when selecting a GIF
    setSelectedFiles([]);
    setUploadedFiles([]);
    setSelectedMedia(null);
    setSelectedGif(gif);
    setShowGifPicker(false);
  };

  const removeGif = () => {
    setSelectedGif(null);
  };

  const handleClose = () => {
    if ((content.trim() || selectedMedia || selectedGif) && !createPostMutation.isPending) {
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
    <>
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
                onPress={() => {
                  console.log('=== PAPERCLIP BUTTON PRESSED ===');
                  // Use the working direct picker approach
                  pickMediaWorking();
                }}
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

              <TouchableOpacity
                style={styles.mediaButton}
                onPress={() => setShowGifPicker(true)}
                disabled={isUploadingMedia || selectedGif !== null || uploadedFiles.length > 0 || selectedFiles.length > 0}
              >
                <Ionicons
                  name="happy-outline"
                  size={20}
                  color={isUploadingMedia || selectedGif !== null || uploadedFiles.length > 0 || selectedFiles.length > 0 ? "#9CA3AF" : "#6B7280"}
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

            {/* GIF Preview */}
            {selectedGif && (
              <View style={styles.mediaContainer}>
                <Image source={{ uri: selectedGif.previewUrl }} style={styles.gifPreview} />
                <TouchableOpacity style={styles.removeMediaButton} onPress={removeGif}>
                  <Ionicons name="close" size={20} color="#fff" />
                </TouchableOpacity>
                <View style={styles.gifBadge}>
                  <Text style={styles.gifBadgeText}>GIF</Text>
                </View>
              </View>
            )}
          </ScrollView>
        </KeyboardAvoidingView>
      </SafeAreaView>
    </Modal>

    {/* GIF Picker Modal */}
    <GifPicker
      visible={showGifPicker}
      onClose={() => setShowGifPicker(false)}
      onSelectGif={handleGifSelect}
    />
    </>
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
  gifPreview: {
    width: '100%',
    height: 200,
    borderRadius: 8,
    resizeMode: 'contain',
  },
  gifBadge: {
    position: 'absolute',
    bottom: 8,
    left: 8,
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 4,
  },
  gifBadgeText: {
    color: '#fff',
    fontSize: 12,
    fontWeight: '600',
  },
});
