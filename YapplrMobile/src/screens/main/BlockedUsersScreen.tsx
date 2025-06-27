import React, { useState } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  SafeAreaView,
  FlatList,
  RefreshControl,
  Alert,
  ActivityIndicator,
  Image,
} from 'react-native';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Ionicons } from '@expo/vector-icons';
import { useNavigation } from '@react-navigation/native';
import { StackNavigationProp } from '@react-navigation/stack';
import { useAuth } from '../../contexts/AuthContext';
import { User } from '../../types';
import { RootStackParamList } from '../../navigation/AppNavigator';

type BlockedUsersScreenNavigationProp = StackNavigationProp<RootStackParamList, 'BlockedUsers'>;

export default function BlockedUsersScreen() {
  const navigation = useNavigation<BlockedUsersScreenNavigationProp>();
  const { api } = useAuth();
  const queryClient = useQueryClient();
  const [unblockingUserId, setUnblockingUserId] = useState<number | null>(null);

  // Helper function to generate image URL
  const getImageUrl = (fileName: string) => {
    if (!fileName) return '';
    return `http://192.168.254.181:5161/api/images/${fileName}`;
  };

  const {
    data: blockedUsers,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ['blockedUsers'],
    queryFn: () => api.users.getBlockedUsers(),
    retry: 2,
  });

  const unblockMutation = useMutation({
    mutationFn: (userId: number) => api.users.unblockUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['blockedUsers'] });
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['following'] });
      setUnblockingUserId(null);
      Alert.alert('Success', 'User has been unblocked.');
    },
    onError: (error) => {
      console.error('Failed to unblock user:', error);
      setUnblockingUserId(null);
      Alert.alert('Error', 'Failed to unblock user. Please try again.');
    },
  });

  const handleUnblock = (user: User) => {
    Alert.alert(
      'Unblock User',
      `Are you sure you want to unblock @${user.username}?`,
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Unblock',
          style: 'destructive',
          onPress: () => {
            setUnblockingUserId(user.id);
            unblockMutation.mutate(user.id);
          },
        },
      ]
    );
  };

  const handleRefresh = () => {
    refetch();
  };

  const renderBlockedUser = ({ item }: { item: User }) => (
    <View style={styles.userItem}>
      <View style={styles.userInfo}>
        <View style={styles.avatar}>
          {item.profileImageFileName ? (
            <Image
              source={{ uri: getImageUrl(item.profileImageFileName) }}
              style={styles.profileImage}
              onError={() => {
                console.log('Failed to load profile image for user:', item.username);
              }}
            />
          ) : (
            <Text style={styles.avatarText}>
              {item.username.charAt(0).toUpperCase()}
            </Text>
          )}
        </View>
        <View style={styles.userDetails}>
          <Text style={styles.username}>@{item.username}</Text>
          {item.pronouns && (
            <Text style={styles.pronouns}>({item.pronouns})</Text>
          )}
          {item.bio && (
            <Text style={styles.bio} numberOfLines={2}>
              {item.bio}
            </Text>
          )}
        </View>
      </View>
      <TouchableOpacity
        style={[styles.unblockButton, unblockingUserId === item.id && styles.unblockButtonDisabled]}
        onPress={() => handleUnblock(item)}
        disabled={unblockingUserId === item.id}
      >
        {unblockingUserId === item.id ? (
          <ActivityIndicator size="small" color="#fff" />
        ) : (
          <Text style={styles.unblockButtonText}>Unblock</Text>
        )}
      </TouchableOpacity>
    </View>
  );

  if (isLoading) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.header}>
          <TouchableOpacity onPress={() => navigation.goBack()}>
            <Ionicons name="arrow-back" size={24} color="#1F2937" />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>Blocked Users</Text>
          <View style={{ width: 24 }} />
        </View>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color="#3B82F6" />
          <Text style={styles.loadingText}>Loading blocked users...</Text>
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
          <Text style={styles.headerTitle}>Blocked Users</Text>
          <View style={{ width: 24 }} />
        </View>
        <View style={styles.errorContainer}>
          <Text style={styles.errorText}>Failed to load blocked users</Text>
          <TouchableOpacity style={styles.retryButton} onPress={handleRefresh}>
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
        <Text style={styles.headerTitle}>Blocked Users</Text>
        <View style={{ width: 24 }} />
      </View>

      <View style={styles.content}>
        <View style={styles.infoSection}>
          <Text style={styles.infoText}>
            Users you've blocked won't be able to see your posts, send you messages, or follow you.
          </Text>
        </View>

        <FlatList
          data={blockedUsers}
          renderItem={renderBlockedUser}
          keyExtractor={(item) => item.id.toString()}
          refreshControl={
            <RefreshControl refreshing={isLoading} onRefresh={handleRefresh} />
          }
          ListEmptyComponent={
            <View style={styles.emptyContainer}>
              <Ionicons name="person-remove-outline" size={48} color="#9CA3AF" />
              <Text style={styles.emptyTitle}>No blocked users</Text>
              <Text style={styles.emptyText}>
                Users you block will appear here. You can unblock them at any time.
              </Text>
            </View>
          }
          contentContainerStyle={blockedUsers?.length === 0 ? styles.emptyListContainer : undefined}
          showsVerticalScrollIndicator={false}
        />
      </View>
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
  content: {
    flex: 1,
  },
  infoSection: {
    padding: 16,
    backgroundColor: '#F9FAFB',
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  infoText: {
    fontSize: 14,
    color: '#6B7280',
    lineHeight: 20,
    textAlign: 'center',
  },
  userItem: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#F3F4F6',
  },
  userInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  avatar: {
    width: 48,
    height: 48,
    borderRadius: 24,
    backgroundColor: '#E5E7EB',
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  profileImage: {
    width: 48,
    height: 48,
    borderRadius: 24,
  },
  avatarText: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#6B7280',
  },
  userDetails: {
    flex: 1,
  },
  username: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1F2937',
  },
  pronouns: {
    fontSize: 14,
    color: '#6B7280',
    marginTop: 2,
  },
  bio: {
    fontSize: 14,
    color: '#6B7280',
    marginTop: 4,
    lineHeight: 18,
  },
  unblockButton: {
    backgroundColor: '#10B981',
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 20,
    minWidth: 80,
    alignItems: 'center',
  },
  unblockButtonDisabled: {
    backgroundColor: '#9CA3AF',
  },
  unblockButtonText: {
    color: '#fff',
    fontSize: 14,
    fontWeight: '600',
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20,
  },
  loadingText: {
    marginTop: 12,
    fontSize: 16,
    color: '#6B7280',
  },
  errorContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20,
  },
  errorText: {
    fontSize: 16,
    color: '#EF4444',
    marginBottom: 16,
    textAlign: 'center',
  },
  retryButton: {
    backgroundColor: '#3B82F6',
    paddingHorizontal: 20,
    paddingVertical: 10,
    borderRadius: 8,
  },
  retryText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  emptyContainer: {
    alignItems: 'center',
    padding: 40,
  },
  emptyListContainer: {
    flex: 1,
    justifyContent: 'center',
  },
  emptyTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#1F2937',
    marginTop: 16,
    marginBottom: 8,
  },
  emptyText: {
    fontSize: 14,
    color: '#6B7280',
    textAlign: 'center',
    lineHeight: 20,
    maxWidth: 280,
  },
});
