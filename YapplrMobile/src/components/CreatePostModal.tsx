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
import { CreatePostData, PostPrivacy } from '../types';

interface CreatePostModalProps {
  visible: boolean;
  onClose: () => void;
}

export default function CreatePostModal({ visible, onClose }: CreatePostModalProps) {
  const { api, user } = useAuth();
  const queryClient = useQueryClient();
  const [content, setContent] = useState('');
  const [privacy, setPrivacy] = useState<PostPrivacy>(PostPrivacy.Public);
  const [selectedImage, setSelectedImage] = useState<string | null>(null);
  const [uploadedFileName, setUploadedFileName] = useState<string | null>(null);
  const [isUploadingImage, setIsUploadingImage] = useState(false);
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
    setSelectedImage(null);
    setUploadedFileName(null);
    setIsUploadingImage(false);
  };

  const pickImage = async () => {
    try {
      // Request permission
      const permissionResult = await ImagePicker.requestMediaLibraryPermissionsAsync();

      if (permissionResult.granted === false) {
        Alert.alert('Permission Required', 'Permission to access camera roll is required!');
        return;
      }

      // Launch image picker
      const result = await ImagePicker.launchImageLibraryAsync({
        mediaTypes: ImagePicker.MediaTypeOptions.Images,
        allowsEditing: true,
        aspect: [4, 3],
        quality: 0.8,
      });

      if (!result.canceled && result.assets[0]) {
        const asset = result.assets[0];
        setSelectedImage(asset.uri);

        // Upload image immediately
        await uploadImage(asset.uri, asset.fileName || 'image.jpg', asset.type || 'image/jpeg');
      }
    } catch (error) {
      console.error('Error picking image:', error);
      Alert.alert('Error', 'Failed to pick image. Please try again.');
    }
  };

  const uploadImage = async (uri: string, fileName: string, type: string) => {
    try {
      setIsUploadingImage(true);
      const response = await api.images.uploadImage(uri, fileName, type);
      setUploadedFileName(response.fileName);
    } catch (error) {
      console.error('Error uploading image:', error);
      Alert.alert('Error', 'Failed to upload image. Please try again.');
      setSelectedImage(null);
    } finally {
      setIsUploadingImage(false);
    }
  };

  const removeImage = () => {
    setSelectedImage(null);
    setUploadedFileName(null);
    setIsUploadingImage(false);
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

    if (isUploadingImage) {
      Alert.alert('Please wait', 'Image is still uploading...');
      return;
    }

    createPostMutation.mutate({
      content: content.trim(),
      privacy,
      imageFileName: uploadedFileName || undefined,
    });
  };

  const handleClose = () => {
    if ((content.trim() || selectedImage) && !createPostMutation.isPending) {
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

  const remainingChars = 256 - content.length;
  const isOverLimit = remainingChars < 0;
  const canSubmit = content.trim().length > 0 && !isOverLimit && !createPostMutation.isPending && !isUploadingImage;

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
              placeholderTextColor="#9CA3AF"
              value={content}
              onChangeText={setContent}
              multiline
              maxLength={300} // Allow a bit over to show error
              autoFocus
              textAlignVertical="top"
            />

            {/* Image Preview */}
            {selectedImage && (
              <View style={styles.imageContainer}>
                <Image source={{ uri: selectedImage }} style={styles.imagePreview} />
                {isUploadingImage && (
                  <View style={styles.imageOverlay}>
                    <ActivityIndicator size="large" color="#3B82F6" />
                    <Text style={styles.uploadingText}>Uploading...</Text>
                  </View>
                )}
                <TouchableOpacity style={styles.removeImageButton} onPress={removeImage}>
                  <Ionicons name="close" size={20} color="#fff" />
                </TouchableOpacity>
              </View>
            )}
          </ScrollView>

          {/* Footer with controls - outside scroll view */}
          <View style={[styles.footer, { marginBottom: keyboardHeight > 0 ? 10 : 0 }]}>
            <View style={styles.footerLeft}>
              <TouchableOpacity
                style={styles.privacyButton}
                onPress={() => setPrivacy(privacy === PostPrivacy.Public ? PostPrivacy.Private : PostPrivacy.Public)}
              >
                <Ionicons
                  name={privacy === PostPrivacy.Public ? "globe-outline" : "lock-closed-outline"}
                  size={16}
                  color="#6B7280"
                />
                <Text style={styles.privacyText}>
                  {privacy === PostPrivacy.Public ? "Public" : "Private"}
                </Text>
              </TouchableOpacity>

              <TouchableOpacity
                style={styles.imageButton}
                onPress={pickImage}
                disabled={isUploadingImage}
              >
                <Ionicons
                  name="image-outline"
                  size={20}
                  color={isUploadingImage ? "#9CA3AF" : "#6B7280"}
                />
              </TouchableOpacity>
            </View>

            <Text style={[styles.charCount, isOverLimit && styles.charCountOver]}>
              {remainingChars}
            </Text>
          </View>
        </KeyboardAvoidingView>
      </SafeAreaView>
    </Modal>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#fff',
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
    borderBottomColor: '#E5E7EB',
  },
  headerButton: {
    paddingHorizontal: 8,
    paddingVertical: 4,
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#111827',
  },
  cancelText: {
    fontSize: 16,
    color: '#6B7280',
  },
  postButton: {
    backgroundColor: '#3B82F6',
    borderRadius: 20,
    paddingHorizontal: 16,
    paddingVertical: 8,
  },
  postButtonDisabled: {
    backgroundColor: '#D1D5DB',
  },
  postText: {
    color: '#fff',
    fontWeight: '600',
    fontSize: 16,
  },
  postTextDisabled: {
    color: '#9CA3AF',
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
    backgroundColor: '#3B82F6',
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 12,
  },
  avatarText: {
    color: '#fff',
    fontWeight: '600',
    fontSize: 16,
  },
  username: {
    fontSize: 16,
    fontWeight: '500',
    color: '#374151',
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
    color: '#111827',
    lineHeight: 24,
    minHeight: 120,
    maxHeight: 200, // Prevent input from taking too much space
    textAlignVertical: 'top',
  },
  footer: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderTopWidth: 1,
    borderTopColor: '#E5E7EB',
    backgroundColor: '#fff', // Ensure footer has background
    // Remove marginTop since it's now outside scroll view
  },
  footerLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
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
    color: '#6B7280',
    fontWeight: '500',
  },
  charCount: {
    fontSize: 14,
    color: '#6B7280',
    fontWeight: '500',
  },
  charCountOver: {
    color: '#EF4444',
  },
  imageButton: {
    marginLeft: 12,
    padding: 8,
    borderRadius: 20,
    backgroundColor: '#F3F4F6',
  },
  imageContainer: {
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
  imageOverlay: {
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
  removeImageButton: {
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
