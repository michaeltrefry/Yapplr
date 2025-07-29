import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  FlatList,
  TouchableOpacity,
  StyleSheet,
  SafeAreaView,
  Image,
} from 'react-native';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { StackNavigationProp } from '@react-navigation/stack';
import { useFocusEffect } from '@react-navigation/native';
import { useAuth } from '../../contexts/AuthContext';
import { useThemeColors } from '../../hooks/useThemeColors';
import { ConversationListItem } from '../../types';
import { RootStackParamList } from '../../navigation/AppNavigator';

type MessagesScreenNavigationProp = StackNavigationProp<RootStackParamList, 'MainTabs'>;

export default function MessagesScreen({ navigation }: { navigation: MessagesScreenNavigationProp }) {
  const { api, user } = useAuth();
  const colors = useThemeColors();
  const queryClient = useQueryClient();

  const styles = createStyles(colors);

  const { data: conversations, isLoading } = useQuery({
    queryKey: ['conversations'],
    queryFn: () => api.messages.getConversations(),
    enabled: !!user,
    retry: 2,
  });

  // Refresh unread count when screen is focused
  useFocusEffect(
    React.useCallback(() => {
      queryClient.invalidateQueries({ queryKey: ['unreadMessageCount'] });
    }, [queryClient])
  );

  const handleConversationPress = (item: ConversationListItem) => {
    navigation.navigate('Conversation', {
      conversationId: item.id,
      otherUser: {
        id: item.otherParticipant.id,
        username: item.otherParticipant.username,
      },
    });
  };

  const renderConversationItem = ({ item }: { item: ConversationListItem }) => {
    const hasUnreadMessages = item.unreadCount > 0;

    return (
      <TouchableOpacity
        style={[
          styles.conversationItem,
          hasUnreadMessages && styles.conversationItemUnread
        ]}
        onPress={() => handleConversationPress(item)}
        activeOpacity={0.7}
      >
        <View style={styles.avatar}>
          {item.otherParticipant.profileImageUrl ? (
            <Image
              source={{ uri: item.otherParticipant.profileImageUrl }}
              style={styles.profileImage}
              onError={() => {
                console.log('Failed to load profile image in conversation list');
              }}
            />
          ) : (
            <Text style={styles.avatarText}>
              {item.otherParticipant.username.charAt(0).toUpperCase()}
            </Text>
          )}
        </View>

        <View style={styles.conversationInfo}>
          <View style={styles.conversationHeader}>
            <Text style={[
              styles.username,
              hasUnreadMessages && styles.usernameUnread
            ]}>
              @{item.otherParticipant.username}
            </Text>
            {hasUnreadMessages && (
              <View style={styles.unreadBadge}>
                <Text style={styles.unreadText}>
                  {item.unreadCount > 99 ? '99+' : item.unreadCount}
                </Text>
              </View>
            )}
          </View>

          {item.lastMessage && (
            <Text
              style={[
                styles.lastMessage,
                hasUnreadMessages && styles.lastMessageUnread
              ]}
              numberOfLines={1}
            >
              {item.lastMessage.content || (item.lastMessage.imageUrl ? '📷 Image' : 'Message')}
            </Text>
          )}

          <Text style={styles.timestamp}>
            {item.lastMessage
              ? new Date(item.lastMessage.createdAt).toLocaleDateString()
              : new Date(item.createdAt).toLocaleDateString()
            }
          </Text>
        </View>
      </TouchableOpacity>
    );
  };

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.headerTitle}>Messages</Text>
      </View>

      {isLoading ? (
        <View style={styles.loadingContainer}>
          <Text>Loading conversations...</Text>
        </View>
      ) : (
        <FlatList
          data={conversations || []}
          renderItem={renderConversationItem}
          keyExtractor={(item) => item.id.toString()}
          contentContainerStyle={styles.listContainer}
          showsVerticalScrollIndicator={false}
          ListEmptyComponent={
            <View style={styles.emptyContainer}>
              <Text style={styles.emptyText}>No conversations yet</Text>
              <Text style={styles.emptySubtext}>
                Start a conversation by searching for users
              </Text>
            </View>
          }
        />
      )}
    </SafeAreaView>
  );
}

const createStyles = (colors: any) => StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  header: {
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  headerTitle: {
    fontSize: 24,
    fontWeight: 'bold',
    color: colors.text,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  listContainer: {
    paddingBottom: 20,
  },
  conversationItem: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  conversationItemUnread: {
    backgroundColor: colors.surface,
  },
  avatar: {
    width: 50,
    height: 50,
    borderRadius: 25,
    backgroundColor: colors.primary,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
    overflow: 'hidden',
  },
  profileImage: {
    width: 50,
    height: 50,
    borderRadius: 25,
  },
  avatarText: {
    color: colors.primaryText,
    fontWeight: 'bold',
    fontSize: 18,
  },
  conversationInfo: {
    flex: 1,
  },
  conversationHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 4,
  },
  username: {
    fontWeight: '600',
    fontSize: 16,
    color: colors.text,
  },
  usernameUnread: {
    fontWeight: 'bold',
    color: colors.text,
  },
  unreadBadge: {
    backgroundColor: colors.error,
    borderRadius: 10,
    paddingHorizontal: 6,
    paddingVertical: 2,
    minWidth: 20,
    alignItems: 'center',
  },
  unreadText: {
    color: colors.primaryText,
    fontSize: 12,
    fontWeight: 'bold',
  },
  lastMessage: {
    fontSize: 14,
    color: colors.textSecondary,
    marginBottom: 2,
  },
  lastMessageUnread: {
    fontWeight: '600',
    color: colors.text,
  },
  timestamp: {
    fontSize: 12,
    color: colors.textMuted,
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
    color: colors.text,
    marginBottom: 8,
  },
  emptySubtext: {
    fontSize: 14,
    color: colors.textSecondary,
    textAlign: 'center',
  },
});
