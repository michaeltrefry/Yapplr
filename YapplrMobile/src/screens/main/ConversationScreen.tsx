import React, { useState, useRef, useEffect } from 'react';
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
import * as ImagePicker from 'expo-image-picker';
import { useAuth } from '../../contexts/AuthContext';
import { RootStackParamList } from '../../navigation/AppNavigator';
import { Message } from '../../types';

type ConversationScreenProps = StackScreenProps<RootStackParamList, 'Conversation'>;

export default function ConversationScreen({ route, navigation }: ConversationScreenProps) {
  const { conversationId, otherUser } = route.params;
  const { api, user: currentUser } = useAuth();
  const [messageText, setMessageText] = useState('');
  const [isSending, setIsSending] = useState(false);
  const [selectedImageUri, setSelectedImageUri] = useState<string | null>(null);
  const [isUploadingImage, setIsUploadingImage] = useState(false);
  const flatListRef = useRef<FlatList>(null);
  const queryClient = useQueryClient();

  // Helper function to generate image URL
  const getImageUrl = (fileName: string) => {
    if (!fileName) return '';
    return `http://192.168.254.181:5161/api/images/${fileName}`;
  };

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
    mutationFn: async ({ content, imageFileName }: { content: string; imageFileName?: string }) => {
      return await api.messages.sendMessageToConversation({
        conversationId,
        content,
        imageFileName,
      });
    },
    onSuccess: async () => {
      // Mark conversation as read when sending a message
      try {
        await api.messages.markConversationAsRead(conversationId);
      } catch (error) {
        console.error('Failed to mark conversation as read after sending:', error);
      }

      // Refresh messages and unread count
      queryClient.invalidateQueries({ queryKey: ['conversationMessages', conversationId] });
      queryClient.invalidateQueries({ queryKey: ['conversations'] });
      queryClient.invalidateQueries({ queryKey: ['unreadMessageCount'] });
      setMessageText('');
      setSelectedImageUri(null);
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
        mediaTypes: ['images'],
        allowsEditing: true,
        aspect: [4, 3],
        quality: 0.8,
      });

      if (!result.canceled && result.assets && result.assets[0]) {
        const asset = result.assets[0];
        setSelectedImageUri(asset.uri);
      }
    } catch (error) {
      console.error('Error picking image:', error);
      Alert.alert('Error', 'Failed to pick image. Please try again.');
    }
  };

  const uploadImageAndSend = async (imageUri: string, fileName: string) => {
    try {
      setIsUploadingImage(true);

      // Upload the image first
      const uploadResult = await api.images.uploadImage(imageUri, fileName, 'image/jpeg');

      // Send message with image
      await sendMessageMutation.mutateAsync({
        content: messageText.trim(),
        imageFileName: uploadResult.fileName,
      });
    } catch (error) {
      console.error('Error uploading image:', error);
      Alert.alert('Error', 'Failed to upload image. Please try again.');
    } finally {
      setIsUploadingImage(false);
    }
  };

  const handleSendMessage = async () => {
    const trimmedMessage = messageText.trim();

    // Check if we have content or an image
    if (!trimmedMessage && !selectedImageUri) return;
    if (isSending || isUploadingImage) return;

    setIsSending(true);
    try {
      if (selectedImageUri) {
        // Upload image and send message
        await uploadImageAndSend(selectedImageUri, 'message-image.jpg');
      } else {
        // Send text-only message
        await sendMessageMutation.mutateAsync({ content: trimmedMessage });
      }
    } finally {
      setIsSending(false);
    }
  };

  const removeSelectedImage = () => {
    setSelectedImageUri(null);
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

        {/* Image preview */}
        {selectedImageUri && (
          <View style={styles.imagePreviewContainer}>
            <Image source={{ uri: selectedImageUri }} style={styles.imagePreview} />
            <TouchableOpacity style={styles.removeImageButton} onPress={removeSelectedImage}>
              <Ionicons name="close-circle" size={24} color="#EF4444" />
            </TouchableOpacity>
          </View>
        )}

        <View style={styles.inputContainer}>
          <TouchableOpacity
            style={styles.imageButton}
            onPress={pickImage}
            disabled={isUploadingImage}
          >
            <Ionicons name="image" size={24} color="#6B7280" />
          </TouchableOpacity>

          <TextInput
            style={styles.messageInput}
            placeholder="Type a message..."
            value={messageText}
            onChangeText={setMessageText}
            multiline
            maxLength={1000}
            returnKeyType="send"
            onSubmitEditing={handleSendMessage}
            blurOnSubmit={false}
          />

          <TouchableOpacity
            style={[
              styles.sendButton,
              (!messageText.trim() && !selectedImageUri || isSending || isUploadingImage) && styles.sendButtonDisabled
            ]}
            onPress={handleSendMessage}
            disabled={(!messageText.trim() && !selectedImageUri) || isSending || isUploadingImage}
          >
            {(isSending || isUploadingImage) ? (
              <ActivityIndicator size="small" color="#fff" />
            ) : (
              <Ionicons
                name="send"
                size={20}
                color={(!messageText.trim() && !selectedImageUri || isSending || isUploadingImage) ? '#9CA3AF' : '#fff'}
              />
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
    backgroundColor: '#3B82F6',
    borderBottomRightRadius: 4,
  },
  otherBubble: {
    backgroundColor: '#F3F4F6',
    borderBottomLeftRadius: 4,
  },
  messageText: {
    fontSize: 16,
    lineHeight: 20,
  },
  ownMessageText: {
    color: '#fff',
  },
  otherMessageText: {
    color: '#1F2937',
  },
  messageTime: {
    fontSize: 12,
    marginTop: 4,
  },
  ownMessageTime: {
    color: 'rgba(255, 255, 255, 0.7)',
  },
  otherMessageTime: {
    color: '#6B7280',
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
  imagePreviewContainer: {
    position: 'relative',
    paddingHorizontal: 16,
    paddingVertical: 8,
    backgroundColor: '#fff',
  },
  imagePreview: {
    width: 100,
    height: 75,
    borderRadius: 8,
  },
  removeImageButton: {
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
    borderTopColor: '#E5E7EB',
    backgroundColor: '#fff',
  },
  imageButton: {
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
    borderColor: '#D1D5DB',
    borderRadius: 20,
    paddingHorizontal: 16,
    paddingVertical: 10,
    marginRight: 8,
    maxHeight: 100,
    fontSize: 16,
  },
  sendButton: {
    backgroundColor: '#3B82F6',
    width: 40,
    height: 40,
    borderRadius: 20,
    justifyContent: 'center',
    alignItems: 'center',
  },
  sendButtonDisabled: {
    backgroundColor: '#E5E7EB',
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
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingTop: 60,
  },
  emptyText: {
    fontSize: 18,
    fontWeight: '600',
    color: '#6B7280',
    marginBottom: 8,
  },
  emptySubtext: {
    fontSize: 14,
    color: '#9CA3AF',
  },
});
