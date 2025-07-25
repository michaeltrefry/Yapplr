import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Alert,
  ActivityIndicator,
  RefreshControl,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { useThemeColors } from '../../hooks/useThemeColors';
import { useAuth } from '../../contexts/AuthContext';
import { Group } from '../../types';

interface GroupDetailScreenProps {
  navigation: any;
  route: {
    params: {
      groupId: number;
    };
  };
}

export default function GroupDetailScreen({ navigation, route }: GroupDetailScreenProps) {
  const colors = useThemeColors();
  const { user, api } = useAuth();
  const { groupId } = route.params;
  const [group, setGroup] = useState<Group | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [activeTab, setActiveTab] = useState<'posts' | 'members'>('posts');

  const loadGroup = async () => {
    try {
      setLoading(true);
      const groupData = await api.groups.getGroup(groupId);
      setGroup(groupData);
    } catch (error) {
      console.error('Failed to load group:', error);
      Alert.alert('Error', 'Failed to load group. Please try again.');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    loadGroup();
  }, [groupId]);

  const handleRefresh = () => {
    setRefreshing(true);
    loadGroup();
  };

  const handleJoinGroup = async () => {
    if (!group) return;

    try {
      await api.groups.joinGroup(group.id);
      setGroup(prev => prev ? {
        ...prev,
        isCurrentUserMember: true,
        memberCount: prev.memberCount + 1,
      } : null);
      Alert.alert('Success', 'You have joined the group!');
    } catch (error) {
      console.error('Failed to join group:', error);
      Alert.alert('Error', 'Failed to join group. Please try again.');
    }
  };

  const handleLeaveGroup = async () => {
    if (!group) return;
    
    Alert.alert(
      'Leave Group',
      'Are you sure you want to leave this group?',
      [
        { text: 'Cancel', style: 'cancel' },
        {
          text: 'Leave',
          style: 'destructive',
          onPress: async () => {
            try {
              await api.groups.leaveGroup(group.id);
              setGroup(prev => prev ? {
                ...prev,
                isCurrentUserMember: false,
                memberCount: prev.memberCount - 1,
              } : null);
              Alert.alert('Success', 'You have left the group.');
            } catch (error) {
              console.error('Failed to leave group:', error);
              Alert.alert('Error', 'Failed to leave group. Please try again.');
            }
          },
        },
      ]
    );
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
    });
  };

  if (loading) {
    return (
      <SafeAreaView style={[styles.container, { backgroundColor: colors.background }]}>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={colors.primary} />
        </View>
      </SafeAreaView>
    );
  }

  if (!group) {
    return (
      <SafeAreaView style={[styles.container, { backgroundColor: colors.background }]}>
        <View style={styles.errorContainer}>
          <Ionicons name="alert-circle-outline" size={64} color={colors.error} />
          <Text style={[styles.errorText, { color: colors.error }]}>
            Group not found
          </Text>
          <TouchableOpacity
            style={[styles.retryButton, { backgroundColor: colors.primary }]}
            onPress={loadGroup}
          >
            <Text style={[styles.retryButtonText, { color: colors.onPrimary }]}>
              Try Again
            </Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    );
  }

  const isOwner = user?.id === group.user.id;

  return (
    <SafeAreaView style={[styles.container, { backgroundColor: colors.background }]}>
      <ScrollView
        refreshControl={
          <RefreshControl
            refreshing={refreshing}
            onRefresh={handleRefresh}
            colors={[colors.primary]}
          />
        }
      >
        {/* Header */}
        <View style={[styles.header, { backgroundColor: colors.surface }]}>
          <TouchableOpacity
            style={styles.backButton}
            onPress={() => navigation.goBack()}
          >
            <Ionicons name="arrow-back" size={24} color={colors.onSurface} />
          </TouchableOpacity>
          <Text style={[styles.headerTitle, { color: colors.onSurface }]} numberOfLines={1}>
            {group.name}
          </Text>
          {isOwner && (
            <TouchableOpacity
              style={styles.editButton}
              onPress={() => navigation.navigate('EditGroup', { group })}
            >
              <Ionicons name="settings-outline" size={24} color={colors.onSurface} />
            </TouchableOpacity>
          )}
        </View>

        {/* Group Info */}
        <View style={[styles.groupInfo, { backgroundColor: colors.surface }]}>
          <View style={[styles.groupAvatar, { backgroundColor: colors.primary }]}>
            <Text style={[styles.groupAvatarText, { color: colors.onPrimary }]}>
              {group.name.charAt(0).toUpperCase()}
            </Text>
          </View>
          
          <Text style={[styles.groupName, { color: colors.onSurface }]}>
            {group.name}
          </Text>
          
          {group.description && (
            <Text style={[styles.groupDescription, { color: colors.onSurfaceVariant }]}>
              {group.description}
            </Text>
          )}

          <View style={styles.groupStats}>
            <View style={styles.statItem}>
              <Text style={[styles.statNumber, { color: colors.onSurface }]}>
                {group.memberCount}
              </Text>
              <Text style={[styles.statLabel, { color: colors.onSurfaceVariant }]}>
                {group.memberCount === 1 ? 'Member' : 'Members'}
              </Text>
            </View>
            <View style={styles.statItem}>
              <Text style={[styles.statNumber, { color: colors.onSurface }]}>
                {group.postCount}
              </Text>
              <Text style={[styles.statLabel, { color: colors.onSurfaceVariant }]}>
                {group.postCount === 1 ? 'Post' : 'Posts'}
              </Text>
            </View>
          </View>

          <Text style={[styles.groupMeta, { color: colors.onSurfaceVariant }]}>
            Created by @{group.user.username} • {formatDate(group.createdAt)}
          </Text>

          {/* Action Button */}
          {user && !isOwner && (
            <TouchableOpacity
              style={[
                styles.actionButton,
                {
                  backgroundColor: group.isCurrentUserMember ? colors.error : colors.primary,
                },
              ]}
              onPress={group.isCurrentUserMember ? handleLeaveGroup : handleJoinGroup}
            >
              <Text style={[styles.actionButtonText, { color: colors.onPrimary }]}>
                {group.isCurrentUserMember ? 'Leave Group' : 'Join Group'}
              </Text>
            </TouchableOpacity>
          )}
        </View>

        {/* Tabs */}
        <View style={[styles.tabContainer, { backgroundColor: colors.surface }]}>
          <TouchableOpacity
            style={[
              styles.tab,
              activeTab === 'posts' && { borderBottomColor: colors.primary },
            ]}
            onPress={() => setActiveTab('posts')}
          >
            <Text
              style={[
                styles.tabText,
                {
                  color: activeTab === 'posts' ? colors.primary : colors.onSurfaceVariant,
                },
              ]}
            >
              Posts ({group.postCount})
            </Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[
              styles.tab,
              activeTab === 'members' && { borderBottomColor: colors.primary },
            ]}
            onPress={() => setActiveTab('members')}
          >
            <Text
              style={[
                styles.tabText,
                {
                  color: activeTab === 'members' ? colors.primary : colors.onSurfaceVariant,
                },
              ]}
            >
              Members ({group.memberCount})
            </Text>
          </TouchableOpacity>
        </View>

        {/* Content */}
        <View style={styles.content}>
          {activeTab === 'posts' ? (
            <View style={styles.postsContainer}>
              <Text style={[styles.placeholderText, { color: colors.onSurfaceVariant }]}>
                Posts will be displayed here
              </Text>
            </View>
          ) : (
            <View style={styles.membersContainer}>
              <Text style={[styles.placeholderText, { color: colors.onSurfaceVariant }]}>
                Members will be displayed here
              </Text>
            </View>
          )}
        </View>
      </ScrollView>

      {/* Floating Action Button for Posts */}
      {user && group.isCurrentUserMember && activeTab === 'posts' && (
        <TouchableOpacity
          style={[styles.fab, { backgroundColor: colors.primary }]}
          onPress={() => navigation.navigate('CreatePost', { groupId: group.id })}
        >
          <Ionicons name="add" size={24} color={colors.onPrimary} />
        </TouchableOpacity>
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
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
    fontWeight: '600',
    marginTop: 16,
    marginBottom: 24,
  },
  retryButton: {
    paddingHorizontal: 24,
    paddingVertical: 12,
    borderRadius: 8,
  },
  retryButtonText: {
    fontSize: 16,
    fontWeight: '600',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(0,0,0,0.1)',
  },
  backButton: {
    marginRight: 16,
  },
  headerTitle: {
    flex: 1,
    fontSize: 18,
    fontWeight: '600',
  },
  editButton: {
    marginLeft: 16,
  },
  groupInfo: {
    padding: 24,
    alignItems: 'center',
  },
  groupAvatar: {
    width: 80,
    height: 80,
    borderRadius: 40,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 16,
  },
  groupAvatarText: {
    fontSize: 32,
    fontWeight: 'bold',
  },
  groupName: {
    fontSize: 24,
    fontWeight: 'bold',
    textAlign: 'center',
    marginBottom: 8,
  },
  groupDescription: {
    fontSize: 16,
    textAlign: 'center',
    marginBottom: 16,
    lineHeight: 22,
  },
  groupStats: {
    flexDirection: 'row',
    marginBottom: 16,
    gap: 32,
  },
  statItem: {
    alignItems: 'center',
  },
  statNumber: {
    fontSize: 20,
    fontWeight: 'bold',
  },
  statLabel: {
    fontSize: 12,
    marginTop: 2,
  },
  groupMeta: {
    fontSize: 12,
    textAlign: 'center',
    marginBottom: 24,
  },
  actionButton: {
    paddingHorizontal: 32,
    paddingVertical: 12,
    borderRadius: 24,
  },
  actionButtonText: {
    fontSize: 16,
    fontWeight: '600',
  },
  tabContainer: {
    flexDirection: 'row',
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(0,0,0,0.1)',
  },
  tab: {
    flex: 1,
    paddingVertical: 16,
    alignItems: 'center',
    borderBottomWidth: 2,
    borderBottomColor: 'transparent',
  },
  tabText: {
    fontSize: 16,
    fontWeight: '500',
  },
  content: {
    flex: 1,
    padding: 16,
  },
  postsContainer: {
    flex: 1,
  },
  membersContainer: {
    flex: 1,
  },
  placeholderText: {
    fontSize: 16,
    textAlign: 'center',
    marginTop: 32,
  },
  fab: {
    position: 'absolute',
    bottom: 24,
    right: 24,
    width: 56,
    height: 56,
    borderRadius: 28,
    justifyContent: 'center',
    alignItems: 'center',
    elevation: 8,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 8,
  },
});
