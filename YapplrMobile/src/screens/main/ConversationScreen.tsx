import React, { useState, useRef, useEffect, useCallback } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  SafeAreaView,
  FlatList,
  KeyboardAvoidingView,
  Platform,
  Alert,
  Image,
  ActivityIndicator,
} from 'react-native';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Ionicons } from '@expo/vector-icons';
import { StackScreenProps } from '@react-navigation/stack';
import { useFocusEffect } from '@react-navigation/native';
import * as ImagePicker from 'expo-image-picker';
import { useAuth } from '../../contexts/AuthContext';
import { useNotifications } from '../../contexts/NotificationContext';
import { useThemeColors } from '../../hooks/useThemeColors';
import { RootStackParamList } from '../../navigation/AppNavigator';
import { Message } from '../../types';
import { navigationService } from '../../services/NavigationService';
import TypingIndicator from '../../components/TypingIndicator';

type ConversationScreenProps = StackScreenProps<RootStackParamList, 'Conversation'>;

export default function ConversationScreen({ route, navigation }: ConversationScreenProps) {
  const { conversationId, otherUser } = route.params;
  const { api, user: currentUser } = useAuth();
  const { joinConversation, leaveConversation, signalRService } = useNotifications();
  const colors = useThemeColors();
  const [messageText, setMessageText] = useState('');
  const [isSending, setIsSending] = useState(false);
  const [selectedMediaUri, setSelectedMediaUri] = useState<string | null>(null);
  const [mediaType, setMediaType] = useState<'image' | 'video' | null>(null);
  const [isUploadingMedia, setIsUploadingMedia] = useState(false);
  const [isTyping, setIsTyping] = useState(false);
  const flatListRef = useRef<FlatList>(null);
  const queryClient = useQueryClient();
  const typingTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  const styles = createStyles(colors);

  // Fetch conversation messages
  const { data: messages, isLoading, error, refetch } = useQuery({
    queryKey: ['conversationMessages', conversationId],
    queryFn: async () => {
      const response = await api.messages.getMessages(conversationId, 1, 50);
      return response;
    },
    enabled: !!conversationId,
    retry: 2,
  });

  // Mark conversation as read when entering
  useEffect(() => {
    if (conversationId) {
      const markAsRead = async () => {
        try {
          await api.messages.markConversationAsRead(conversationId);
          console.log('Conversation marked as read:', conversationId);

          // Invalidate caches to update UI
          queryClient.invalidateQueries({ queryKey: ['unreadMessageCount'] });
          queryClient.invalidateQueries({ queryKey: ['conversations'] });
        } catch (error) {
          console.error('Failed to mark conversation as read:', error);
        }
      };

      markAsRead();
    }
  }, [conversationId, queryClient, api]);

  // Send message mutation
  const sendMessageMutation = useMutation({
    mutationFn: async ({ content, imageFileName, videoFileName }: { content: string; imageFileName?: string; videoFileName?: string }) => {
      return await api.messages.sendMessageToConversation({
        conversationId,
        content,
        imageFileName,
        videoFileName,
      });
    },
    onSuccess: async () => {
      // Mark conversation as read when sending a message
      try {
        await api.messages.markConversationAsRead(conversationId);
      } catch (error) {
        console.error('Failed to mark conversation as read after sending:', error);
      }

      // Stop typing indicator when message is sent
      stopTyping();

      // Refresh messages and unread count
      queryClient.invalidateQueries({ queryKey: ['conversationMessages', conversationId] });
      queryClient.invalidateQueries({ queryKey: ['conversations'] });
      queryClient.invalidateQueries({ queryKey: ['unreadMessageCount'] });
      setMessageText('');
      setSelectedMediaUri(null);
      setMediaType(null);
      // Scroll to bottom
      setTimeout(() => {
        flatListRef.current?.scrollToEnd({ animated: true });
      }, 100);
    },
    onError: (error) => {
      console.error('Failed to send message:', error);
      Alert.alert('Error', 'Failed to send message. Please try again.');
    },
  });

  // Typing indicator functions
  const startTyping = useCallback(() => {
    if (!isTyping && signalRService) {
      setIsTyping(true);
      signalRService.startTyping(conversationId);
    }

    // Clear existing timeout
    if (typingTimeoutRef.current) {
      clearTimeout(typingTimeoutRef.current);
    }

    // Set new timeout to stop typing after 3 seconds of inactivity
    typingTimeoutRef.current = setTimeout(() => {
      setIsTyping(false);
      if (signalRService) {
        signalRService.stopTyping(conversationId);
      }
    }, 3000);
  }, [conversationId, isTyping]);

  const stopTyping = useCallback(() => {
    if (isTyping && signalRService) {
      setIsTyping(false);
      signalRService.stopTyping(conversationId);
    }

    if (typingTimeoutRef.current) {
      clearTimeout(typingTimeoutRef.current);
      typingTimeoutRef.current = null;
    }
  }, [conversationId, isTyping]);

  // Cleanup typing timeout on unmount
  useEffect(() => {
    return () => {
      if (typingTimeoutRef.current) {
        clearTimeout(typingTimeoutRef.current);
      }
    };
  }, []);

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

      if (!result.canceled && result.assets && result.assets[0]) {
        const asset = result.assets[0];

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

        setSelectedMediaUri(asset.uri);
        setMediaType(isVideo ? 'video' : 'image');
      }
    } catch (error) {
      console.error('Error picking media:', error);
      Alert.alert('Error', 'Failed to pick media. Please try again.');
    }
  };

  const uploadMediaAndSend = async (mediaUri: string, fileName: string, type: 'image' | 'video') => {
    try {
      setIsUploadingMedia(true);

      let uploadResult;
      let messageData: { content: string; imageFileName?: string; videoFileName?: string } = {
        content: messageText.trim(),
      };

      if (type === 'video') {
        // Determine video MIME type
        let mimeType = 'video/mp4'; // default
        if (fileName.toLowerCase().endsWith('.mov')) {
          mimeType = 'video/quicktime';
        } else if (fileName.toLowerCase().endsWith('.avi')) {
          mimeType = 'video/x-msvideo';
        } else if (fileName.toLowerCase().endsWith('.wmv')) {
          mimeType = 'video/x-ms-wmv';
        }

        uploadResult = await api.videos.uploadVideo(mediaUri, fileName, mimeType);
        messageData.videoFileName = uploadResult.fileName;
      } else {
        uploadResult = await api.images.uploadImage(mediaUri, fileName, 'image/jpeg');
        messageData.imageFileName = uploadResult.fileName;
      }

      // Send message with media
      await sendMessageMutation.mutateAsync(messageData);
    } catch (error) {
      console.error(`Error uploading ${type}:`, error);
      Alert.alert('Error', `Failed to upload ${type}. Please try again.`);
    } finally {
      setIsUploadingMedia(false);
    }
  };

  const handleSendMessage = async () => {
    const trimmedMessage = messageText.trim();

    // Check if we have content or media
    if (!trimmedMessage && !selectedMediaUri) return;
    if (isSending || isUploadingMedia) return;

    setIsSending(true);
    try {
      if (selectedMediaUri && mediaType) {
        // Upload media and send message
        const fileName = mediaType === 'video' ? 'message-video.mp4' : 'message-image.jpg';
        await uploadMediaAndSend(selectedMediaUri, fileName, mediaType);
      } else {
        // Send text-only message
        await sendMessageMutation.mutateAsync({ content: trimmedMessage });
      }
    } finally {
      setIsSending(false);
    }
  };

  const removeSelectedMedia = () => {
    setSelectedMediaUri(null);
    setMediaType(null);
  };

  const renderMessage = ({ item }: { item: Message }) => {
    const isOwnMessage = item.sender.id === currentUser?.id;

    return (
      <View style={[
        styles.messageContainer,
        isOwnMessage ? styles.ownMessage : styles.otherMessage
      ]}>
        <View style={[
          styles.messageBubble,
          isOwnMessage ? styles.ownBubble : styles.otherBubble
        ]}>
          {/* Image display */}
          {item.imageUrl && (
            <Image
              source={{ uri: `http://192.168.254.181:5161${item.imageUrl}` }}
              style={styles.messageImage}
              resizeMode="cover"
            />
          )}

          {/* Text content */}
          {item.content && (
            <Text style={[
              styles.messageText,
              isOwnMessage ? styles.ownMessageText : styles.otherMessageText,
              item.imageUrl && styles.messageTextWithImage
            ]}>
              {item.content}
            </Text>
          )}

          <Text style={[
            styles.messageTime,
            isOwnMessage ? styles.ownMessageTime : styles.otherMessageTime
          ]}>
            {new Date(item.createdAt).toLocaleTimeString([], {
              hour: '2-digit',
              minute: '2-digit'
            })}
          </Text>
        </View>
      </View>
    );
  };

  // Scroll to bottom when messages load
  useEffect(() => {
    if (messages && messages.length > 0) {
      setTimeout(() => {
        flatListRef.current?.scrollToEnd({ animated: false });
      }, 100);
    }
  }, [messages]);

  // Join/leave SignalR conversation when screen is focused/unfocused
  useFocusEffect(
    React.useCallback(() => {
      console.log('📱💬 ConversationScreen focused, joining SignalR conversation:', conversationId);
      navigationService.setCurrentScreen('Conversation');
      navigationService.setCurrentConversation(conversationId);
      joinConversation(conversationId);

      return () => {
        console.log('📱💬 ConversationScreen unfocused, leaving SignalR conversation:', conversationId);
        navigationService.clearConversation();
        leaveConversation(conversationId);
      };
    }, [conversationId, joinConversation, leaveConversation])
  );

  if (isLoading) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.header}>
          <TouchableOpacity onPress={() => navigation.goBack()}>
            <Ionicons name="arrow-back" size={24} color="#1F2937" />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>@{otherUser.username}</Text>
          <View style={{ width: 24 }} />
        </View>
        <View style={styles.loadingContainer}>
          <Text>Loading conversation...</Text>
        </View>
      </SafeAreaView>
    );
  }

  if (error) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.header}>
          <TouchableOpacity onPress={() => navigation.goBack()}>
            <Ionicons name="arrow-back" size={24} color="#1F2937" />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>@{otherUser.username}</Text>
          <View style={{ width: 24 }} />
        </View>
        <View style={styles.errorContainer}>
          <Text style={styles.errorText}>Failed to load conversation</Text>
          <TouchableOpacity style={styles.retryButton} onPress={() => refetch()}>
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
        <Text style={styles.headerTitle}>@{otherUser.username}</Text>
        <View style={{ width: 24 }} />
      </View>

      <KeyboardAvoidingView 
        style={styles.keyboardAvoidingView}
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        keyboardVerticalOffset={Platform.OS === 'ios' ? 90 : 0}
      >
        <FlatList
          ref={flatListRef}
          data={messages || []}
          renderItem={renderMessage}
          keyExtractor={(item) => item.id.toString()}
          contentContainerStyle={styles.messagesContainer}
          showsVerticalScrollIndicator={false}
          ListEmptyComponent={
            <View style={styles.emptyContainer}>
              <Text style={styles.emptyText}>No messages yet</Text>
              <Text style={styles.emptySubtext}>Start the conversation!</Text>
            </View>
          }
        />

        {/* Typing indicator */}
        <TypingIndicator conversationId={conversationId} />

        {/* Media preview */}
        {selectedMediaUri && (
          <View style={styles.mediaPreviewContainer}>
            {mediaType === 'video' ? (
              <View style={styles.videoPreviewPlaceholder}>
                <Ionicons name="play-circle" size={48} color="#6B7280" />
                <Text style={styles.videoPreviewText}>Video selected</Text>
              </View>
            ) : (
              <Image source={{ uri: selectedMediaUri }} style={styles.imagePreview} />
            )}
            <TouchableOpacity style={styles.removeMediaButton} onPress={removeSelectedMedia}>
              <Ionicons name="close-circle" size={24} color="#EF4444" />
            </TouchableOpacity>
          </View>
        )}

        <View style={styles.inputContainer}>
          <TouchableOpacity
            style={styles.mediaButton}
            onPress={pickMedia}
            disabled={isUploadingMedia}
          >
            <Ionicons name="attach" size={24} color="#6B7280" />
          </TouchableOpacity>

          <TextInput
            style={styles.messageInput}
            placeholder="Type a message..."
            value={messageText}
            onChangeText={(text) => {
              setMessageText(text);
              // Handle typing indicators
              if (text.trim().length > 0) {
                startTyping();
              } else {
                stopTyping();
              }
            }}
            multiline
            maxLength={1000}
            returnKeyType="send"
            onSubmitEditing={handleSendMessage}
            blurOnSubmit={false}
          />

          <TouchableOpacity
            style={[
              styles.sendButton,
              (!messageText.trim() && !selectedMediaUri || isSending || isUploadingMedia) && styles.sendButtonDisabled
            ]}
            onPress={handleSendMessage}
            disabled={(!messageText.trim() && !selectedMediaUri) || isSending || isUploadingMedia}
          >
            {(isSending || isUploadingMedia) ? (
              <ActivityIndicator size="small" color="#fff" />
            ) : (
              <Ionicons
                name="send"
                size={20}
                color={(!messageText.trim() && !selectedMediaUri || isSending || isUploadingMedia) ? '#9CA3AF' : '#fff'}
              />
            )}
          </TouchableOpacity>
        </View>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const createStyles = (colors: any) => StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
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
  headerTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: colors.text,
  },
  keyboardAvoidingView: {
    flex: 1,
  },
  messagesContainer: {
    flexGrow: 1,
    paddingHorizontal: 16,
    paddingVertical: 8,
  },
  messageContainer: {
    marginVertical: 4,
  },
  ownMessage: {
    alignItems: 'flex-end',
  },
  otherMessage: {
    alignItems: 'flex-start',
  },
  messageBubble: {
    maxWidth: '80%',
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderRadius: 20,
  },
  ownBubble: {
    backgroundColor: colors.primary,
    borderBottomRightRadius: 4,
  },
  otherBubble: {
    backgroundColor: colors.surface,
    borderBottomLeftRadius: 4,
  },
  messageText: {
    fontSize: 16,
    lineHeight: 20,
  },
  ownMessageText: {
    color: colors.primaryText,
  },
  otherMessageText: {
    color: colors.text,
  },
  messageTime: {
    fontSize: 12,
    marginTop: 4,
  },
  ownMessageTime: {
    color: colors.background === '#FFFFFF' ? 'rgba(255, 255, 255, 0.7)' : 'rgba(255, 255, 255, 0.8)',
  },
  otherMessageTime: {
    color: colors.textSecondary,
  },
  messageImage: {
    width: 200,
    height: 150,
    borderRadius: 12,
    marginBottom: 8,
  },
  messageTextWithImage: {
    marginTop: 0,
  },
  mediaPreviewContainer: {
    position: 'relative',
    paddingHorizontal: 16,
    paddingVertical: 8,
    backgroundColor: colors.background,
  },
  imagePreview: {
    width: 100,
    height: 75,
    borderRadius: 8,
  },
  videoPreviewPlaceholder: {
    width: 100,
    height: 75,
    borderRadius: 8,
    backgroundColor: colors.surface,
    justifyContent: 'center',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: colors.border,
  },
  videoPreviewText: {
    marginTop: 4,
    fontSize: 12,
    color: colors.textSecondary,
  },
  removeMediaButton: {
    position: 'absolute',
    top: 4,
    right: 12,
  },
  inputContainer: {
    flexDirection: 'row',
    alignItems: 'flex-end',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderTopWidth: 1,
    borderTopColor: colors.border,
    backgroundColor: colors.background,
  },
  mediaButton: {
    width: 40,
    height: 40,
    borderRadius: 20,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 8,
  },
  messageInput: {
    flex: 1,
    borderWidth: 1,
    borderColor: colors.inputBorder,
    borderRadius: 20,
    paddingHorizontal: 16,
    paddingVertical: 10,
    marginRight: 8,
    maxHeight: 100,
    fontSize: 16,
    backgroundColor: colors.input,
    color: colors.text,
  },
  sendButton: {
    backgroundColor: colors.primary,
    width: 40,
    height: 40,
    borderRadius: 20,
    justifyContent: 'center',
    alignItems: 'center',
  },
  sendButtonDisabled: {
    backgroundColor: colors.border,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
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
    color: colors.textSecondary,
    marginBottom: 16,
  },
  retryButton: {
    backgroundColor: colors.primary,
    paddingHorizontal: 24,
    paddingVertical: 12,
    borderRadius: 8,
  },
  retryText: {
    color: colors.primaryText,
    fontWeight: '600',
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingTop: 60,
  },
  emptyText: {
    fontSize: 18,
    fontWeight: '600',
    color: colors.textSecondary,
    marginBottom: 8,
  },
  emptySubtext: {
    fontSize: 14,
    color: colors.textMuted,
  },
});
