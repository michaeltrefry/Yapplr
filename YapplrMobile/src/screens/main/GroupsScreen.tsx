import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  FlatList,
  TouchableOpacity,
  TextInput,
  RefreshControl,
  Alert,
  ActivityIndicator,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { useThemeColors } from '../../hooks/useThemeColors';
import { useAuth } from '../../contexts/AuthContext';
import { GroupList, PaginatedResult } from '../../types';
import { useApi } from '../../contexts/ApiContext';

interface GroupsScreenProps {
  navigation: any;
}

export default function GroupsScreen({ navigation }: GroupsScreenProps) {
  const colors = useThemeColors();
  const { user } = useAuth();
  const api = useApi();
  const [groups, setGroups] = useState<GroupList[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [activeTab, setActiveTab] = useState<'all' | 'my-groups'>('all');
  const [hasMore, setHasMore] = useState(false);
  const [page, setPage] = useState(1);

  const loadGroups = async (pageNum: number = 1, reset: boolean = false) => {
    try {
      if (pageNum === 1) {
        setLoading(true);
      } else {
        setLoadingMore(true);
      }

      let result: PaginatedResult<GroupList>;
      
      if (searchQuery) {
        result = await api.groups.searchGroups(searchQuery, pageNum, 20);
      } else if (activeTab === 'my-groups') {
        result = await api.groups.getMyGroups(pageNum, 20);
      } else {
        result = await api.groups.getGroups(pageNum, 20);
      }

      if (reset || pageNum === 1) {
        setGroups(result.items || []);
      } else {
        setGroups(prev => [...prev, ...(result.items || [])]);
      }

      setHasMore(result.hasNextPage || false);
      setPage(pageNum);
    } catch (error) {
      console.error('Failed to load groups:', error);
      Alert.alert('Error', 'Failed to load groups. Please try again.');
    } finally {
      setLoading(false);
      setRefreshing(false);
      setLoadingMore(false);
    }
  };

  useEffect(() => {
    loadGroups(1, true);
  }, [activeTab, searchQuery]);

  const handleRefresh = () => {
    setRefreshing(true);
    loadGroups(1, true);
  };

  const handleLoadMore = () => {
    if (hasMore && !loadingMore && !loading) {
      loadGroups(page + 1);
    }
  };

  const handleJoinGroup = async (groupId: number) => {
    try {
      await api.groups.joinGroup(groupId);
      // Update the group in the list
      setGroups(prev => 
        prev.map(group => 
          group.id === groupId 
            ? { ...group, isCurrentUserMember: true, memberCount: group.memberCount + 1 }
            : group
        )
      );
    } catch (error) {
      console.error('Failed to join group:', error);
      Alert.alert('Error', 'Failed to join group. Please try again.');
    }
  };

  const handleLeaveGroup = async (groupId: number) => {
    try {
      await api.groups.leaveGroup(groupId);
      // Update the group in the list
      setGroups(prev => 
        prev.map(group => 
          group.id === groupId 
            ? { ...group, isCurrentUserMember: false, memberCount: group.memberCount - 1 }
            : group
        )
      );
    } catch (error) {
      console.error('Failed to leave group:', error);
      Alert.alert('Error', 'Failed to leave group. Please try again.');
    }
  };

  const renderGroupItem = ({ item }: { item: GroupList }) => (
    <TouchableOpacity
      style={[styles.groupCard, { backgroundColor: colors.surface }]}
      onPress={() => navigation.navigate('GroupDetail', { groupId: item.id })}
    >
      <View style={styles.groupHeader}>
        <View style={[styles.groupAvatar, { backgroundColor: colors.primary }]}>
          <Text style={[styles.groupAvatarText, { color: colors.onPrimary }]}>
            {item.name.charAt(0).toUpperCase()}
          </Text>
        </View>
        <View style={styles.groupInfo}>
          <Text style={[styles.groupName, { color: colors.onSurface }]} numberOfLines={1}>
            {item.name}
          </Text>
          <Text style={[styles.groupStats, { color: colors.onSurfaceVariant }]}>
            {item.memberCount} members • {item.postCount} posts
          </Text>
        </View>
        {user && (
          <TouchableOpacity
            style={[
              styles.joinButton,
              {
                backgroundColor: item.isCurrentUserMember ? colors.error : colors.primary,
              },
            ]}
            onPress={() => 
              item.isCurrentUserMember 
                ? handleLeaveGroup(item.id)
                : handleJoinGroup(item.id)
            }
          >
            <Text style={[styles.joinButtonText, { color: colors.onPrimary }]}>
              {item.isCurrentUserMember ? 'Leave' : 'Join'}
            </Text>
          </TouchableOpacity>
        )}
      </View>
      {item.description && (
        <Text style={[styles.groupDescription, { color: colors.onSurfaceVariant }]} numberOfLines={2}>
          {item.description}
        </Text>
      )}
      <Text style={[styles.groupCreator, { color: colors.onSurfaceVariant }]}>
        Created by @{item.creatorUsername}
      </Text>
    </TouchableOpacity>
  );

  const renderEmptyState = () => (
    <View style={styles.emptyState}>
      <Ionicons name="people-outline" size={64} color={colors.onSurfaceVariant} />
      <Text style={[styles.emptyStateText, { color: colors.onSurfaceVariant }]}>
        {searchQuery ? `No groups found for "${searchQuery}"` : 
         activeTab === 'my-groups' ? "You haven't joined any groups yet" : 
         "No groups found"}
      </Text>
    </View>
  );

  return (
    <SafeAreaView style={[styles.container, { backgroundColor: colors.background }]}>
      {/* Header */}
      <View style={[styles.header, { backgroundColor: colors.surface }]}>
        <Text style={[styles.title, { color: colors.onSurface }]}>Groups</Text>
        {user && (
          <TouchableOpacity
            style={[styles.createButton, { backgroundColor: colors.primary }]}
            onPress={() => navigation.navigate('CreateGroup')}
          >
            <Ionicons name="add" size={24} color={colors.onPrimary} />
          </TouchableOpacity>
        )}
      </View>

      {/* Search Bar */}
      <View style={[styles.searchContainer, { backgroundColor: colors.surface }]}>
        <Ionicons name="search" size={20} color={colors.onSurfaceVariant} />
        <TextInput
          style={[styles.searchInput, { color: colors.onSurface }]}
          placeholder="Search groups..."
          placeholderTextColor={colors.onSurfaceVariant}
          value={searchQuery}
          onChangeText={setSearchQuery}
        />
      </View>

      {/* Tabs */}
      <View style={[styles.tabContainer, { backgroundColor: colors.surface }]}>
        <TouchableOpacity
          style={[
            styles.tab,
            activeTab === 'all' && { backgroundColor: colors.primaryContainer },
          ]}
          onPress={() => setActiveTab('all')}
        >
          <Text
            style={[
              styles.tabText,
              {
                color: activeTab === 'all' ? colors.onPrimaryContainer : colors.onSurfaceVariant,
              },
            ]}
          >
            All Groups
          </Text>
        </TouchableOpacity>
        {user && (
          <TouchableOpacity
            style={[
              styles.tab,
              activeTab === 'my-groups' && { backgroundColor: colors.primaryContainer },
            ]}
            onPress={() => setActiveTab('my-groups')}
          >
            <Text
              style={[
                styles.tabText,
                {
                  color: activeTab === 'my-groups' ? colors.onPrimaryContainer : colors.onSurfaceVariant,
                },
              ]}
            >
              My Groups
            </Text>
          </TouchableOpacity>
        )}
      </View>

      {/* Groups List */}
      {loading && groups.length === 0 ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={colors.primary} />
        </View>
      ) : (
        <FlatList
          data={groups}
          renderItem={renderGroupItem}
          keyExtractor={(item) => item.id.toString()}
          contentContainerStyle={styles.listContainer}
          refreshControl={
            <RefreshControl
              refreshing={refreshing}
              onRefresh={handleRefresh}
              colors={[colors.primary]}
            />
          }
          onEndReached={handleLoadMore}
          onEndReachedThreshold={0.1}
          ListEmptyComponent={renderEmptyState}
          ListFooterComponent={
            loadingMore ? (
              <View style={styles.loadingMore}>
                <ActivityIndicator size="small" color={colors.primary} />
              </View>
            ) : null
          }
        />
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(0,0,0,0.1)',
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
  },
  createButton: {
    width: 40,
    height: 40,
    borderRadius: 20,
    justifyContent: 'center',
    alignItems: 'center',
  },
  searchContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    margin: 16,
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderRadius: 8,
    gap: 12,
  },
  searchInput: {
    flex: 1,
    fontSize: 16,
  },
  tabContainer: {
    flexDirection: 'row',
    marginHorizontal: 16,
    marginBottom: 16,
    borderRadius: 8,
    padding: 4,
  },
  tab: {
    flex: 1,
    paddingVertical: 8,
    paddingHorizontal: 16,
    borderRadius: 6,
    alignItems: 'center',
  },
  tabText: {
    fontSize: 14,
    fontWeight: '500',
  },
  listContainer: {
    padding: 16,
  },
  groupCard: {
    padding: 16,
    borderRadius: 12,
    marginBottom: 12,
    elevation: 2,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 2,
  },
  groupHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 8,
  },
  groupAvatar: {
    width: 48,
    height: 48,
    borderRadius: 24,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  groupAvatarText: {
    fontSize: 20,
    fontWeight: 'bold',
  },
  groupInfo: {
    flex: 1,
  },
  groupName: {
    fontSize: 16,
    fontWeight: '600',
    marginBottom: 2,
  },
  groupStats: {
    fontSize: 12,
  },
  joinButton: {
    paddingHorizontal: 16,
    paddingVertical: 6,
    borderRadius: 16,
  },
  joinButtonText: {
    fontSize: 12,
    fontWeight: '600',
  },
  groupDescription: {
    fontSize: 14,
    marginBottom: 8,
    lineHeight: 20,
  },
  groupCreator: {
    fontSize: 12,
  },
  emptyState: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingVertical: 64,
  },
  emptyStateText: {
    fontSize: 16,
    textAlign: 'center',
    marginTop: 16,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingMore: {
    paddingVertical: 16,
    alignItems: 'center',
  },
});
