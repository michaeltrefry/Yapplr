import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  RefreshControl,
  ActivityIndicator,
  StyleSheet,
  FlatList,
  TextInput,
  Switch,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useTheme } from '@react-navigation/native';
import { api } from '../../api/client';
import { TopicDto, TopicFollowDto, TopicRecommendationDto } from '../../types';

interface TopicsScreenProps {
  navigation: any;
}

export default function TopicsScreen({ navigation }: TopicsScreenProps) {
  const { colors } = useTheme();
  const [activeTab, setActiveTab] = useState<'discover' | 'following' | 'feed'>('discover');
  const [featuredTopics, setFeaturedTopics] = useState<TopicDto[]>([]);
  const [followedTopics, setFollowedTopics] = useState<TopicFollowDto[]>([]);
  const [recommendations, setRecommendations] = useState<TopicRecommendationDto[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const categories = ['Technology', 'Sports', 'Entertainment', 'Gaming', 'News', 'Music'];

  useEffect(() => {
    loadTopicsData();
  }, [activeTab]);

  const loadTopicsData = async () => {
    try {
      setError(null);
      
      if (activeTab === 'discover') {
        const [featured, recs] = await Promise.all([
          api.topics.getTopics(undefined, true),
          api.topics.getTopicRecommendations(8)
        ]);
        setFeaturedTopics(featured);
        setRecommendations(recs);
      } else if (activeTab === 'following') {
        const followed = await api.topics.getUserTopics();
        setFollowedTopics(followed);
      }
    } catch (err) {
      console.error('Failed to load topics data:', err);
      setError('Failed to load topics');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  const onRefresh = () => {
    setRefreshing(true);
    loadTopicsData();
  };

  const handleFollowTopic = async (topicName: string) => {
    try {
      await api.topics.followTopic({
        topicName,
        category: 'General',
        relatedHashtags: [],
        interestLevel: 0.7,
        includeInMainFeed: true,
        enableNotifications: false,
        notificationThreshold: 5
      });
      loadTopicsData(); // Refresh data
    } catch (err) {
      console.error('Failed to follow topic:', err);
    }
  };

  const handleUnfollowTopic = async (topicName: string) => {
    try {
      await api.topics.unfollowTopic(topicName);
      loadTopicsData(); // Refresh data
    } catch (err) {
      console.error('Failed to unfollow topic:', err);
    }
  };

  const getTopicIcon = (category: string) => {
    const icons: Record<string, string> = {
      'Technology': '💻', 'Sports': '⚽', 'Entertainment': '🎬', 'Gaming': '🎮',
      'News': '📰', 'Music': '🎵', 'Art': '🎨', 'Food': '🍕', 'Travel': '✈️', 'Fashion': '👗'
    };
    return icons[category] || '🏷️';
  };

  const renderTopicCard = ({ item }: { item: TopicDto }) => (
    <TouchableOpacity
      style={styles.topicCard}
      onPress={() => navigation.navigate('TopicFeed', { topicName: item.name })}
    >
      <View style={styles.topicHeader}>
        <Text style={styles.topicIcon}>{getTopicIcon(item.category)}</Text>
        <View style={styles.topicInfo}>
          <Text style={[styles.topicName, { color: colors.text }]} numberOfLines={1}>
            {item.name}
          </Text>
          <Text style={[styles.topicCategory, { color: colors.text + '80' }]}>
            {item.category}
          </Text>
        </View>
        {item.isFeatured && (
          <Text style={styles.featuredBadge}>⭐</Text>
        )}
      </View>
      
      <Text style={[styles.topicDescription, { color: colors.text + '80' }]} numberOfLines={2}>
        {item.description}
      </Text>
      
      <View style={styles.topicFooter}>
        <Text style={[styles.followerCount, { color: colors.text + '60' }]}>
          {item.followerCount.toLocaleString()} followers
        </Text>
        <TouchableOpacity
          style={[
            styles.followButton,
            item.isFollowedByCurrentUser 
              ? { backgroundColor: colors.border }
              : { backgroundColor: colors.primary }
          ]}
          onPress={() => {
            if (item.isFollowedByCurrentUser) {
              handleUnfollowTopic(item.name);
            } else {
              handleFollowTopic(item.name);
            }
          }}
        >
          <Text
            style={[
              styles.followButtonText,
              { color: item.isFollowedByCurrentUser ? colors.text : '#ffffff' }
            ]}
          >
            {item.isFollowedByCurrentUser ? 'Following' : 'Follow'}
          </Text>
        </TouchableOpacity>
      </View>
    </TouchableOpacity>
  );

  const renderFollowedTopic = ({ item }: { item: TopicFollowDto }) => (
    <View style={styles.followedTopicCard}>
      <View style={styles.topicHeader}>
        <Text style={styles.topicIcon}>{getTopicIcon(item.category)}</Text>
        <View style={styles.topicInfo}>
          <Text style={[styles.topicName, { color: colors.text }]}>
            {item.topicName}
          </Text>
          <Text style={[styles.topicCategory, { color: colors.text + '80' }]}>
            {item.category}
          </Text>
        </View>
        <TouchableOpacity
          onPress={() => handleUnfollowTopic(item.topicName)}
          style={styles.unfollowButton}
        >
          <Text style={styles.unfollowButtonText}>×</Text>
        </TouchableOpacity>
      </View>

      <View style={styles.topicSettings}>
        <View style={styles.settingRow}>
          <Text style={[styles.settingLabel, { color: colors.text }]}>
            Include in main feed
          </Text>
          <Switch
            value={item.includeInMainFeed}
            onValueChange={(value) => {
              // Update topic settings
              api.topics.updateTopicFollow(item.topicName, {
                includeInMainFeed: value
              });
            }}
          />
        </View>
        
        <View style={styles.settingRow}>
          <Text style={[styles.settingLabel, { color: colors.text }]}>
            Notifications
          </Text>
          <Switch
            value={item.enableNotifications}
            onValueChange={(value) => {
              // Update topic settings
              api.topics.updateTopicFollow(item.topicName, {
                enableNotifications: value
              });
            }}
          />
        </View>
      </View>
    </View>
  );

  const styles = StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: colors.background,
    },
    header: {
      padding: 16,
      borderBottomWidth: 1,
      borderBottomColor: colors.border,
    },
    title: {
      fontSize: 24,
      fontWeight: 'bold',
      color: colors.text,
      marginBottom: 8,
    },
    subtitle: {
      fontSize: 14,
      color: colors.text + '80',
    },
    tabs: {
      flexDirection: 'row',
      paddingHorizontal: 16,
      paddingVertical: 12,
      gap: 8,
    },
    tab: {
      paddingHorizontal: 16,
      paddingVertical: 8,
      borderRadius: 20,
      backgroundColor: colors.card,
      borderWidth: 1,
      borderColor: colors.border,
    },
    tabActive: {
      backgroundColor: colors.primary,
      borderColor: colors.primary,
    },
    tabText: {
      fontSize: 14,
      color: colors.text,
      fontWeight: '500',
    },
    tabTextActive: {
      color: '#ffffff',
    },
    searchContainer: {
      paddingHorizontal: 16,
      paddingBottom: 12,
    },
    searchInput: {
      height: 40,
      borderWidth: 1,
      borderColor: colors.border,
      borderRadius: 20,
      paddingHorizontal: 16,
      backgroundColor: colors.card,
      color: colors.text,
    },
    content: {
      flex: 1,
    },
    topicCard: {
      padding: 16,
      marginHorizontal: 16,
      marginVertical: 4,
      backgroundColor: colors.card,
      borderRadius: 12,
      borderWidth: 1,
      borderColor: colors.border,
    },
    followedTopicCard: {
      padding: 16,
      marginHorizontal: 16,
      marginVertical: 4,
      backgroundColor: colors.card,
      borderRadius: 12,
      borderWidth: 1,
      borderColor: colors.border,
    },
    topicHeader: {
      flexDirection: 'row',
      alignItems: 'center',
      marginBottom: 8,
    },
    topicIcon: {
      fontSize: 24,
      marginRight: 12,
    },
    topicInfo: {
      flex: 1,
    },
    topicName: {
      fontSize: 16,
      fontWeight: '600',
      marginBottom: 2,
    },
    topicCategory: {
      fontSize: 12,
    },
    featuredBadge: {
      fontSize: 16,
    },
    topicDescription: {
      fontSize: 14,
      lineHeight: 20,
      marginBottom: 12,
    },
    topicFooter: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
    },
    followerCount: {
      fontSize: 12,
    },
    followButton: {
      paddingHorizontal: 16,
      paddingVertical: 6,
      borderRadius: 16,
    },
    followButtonText: {
      fontSize: 12,
      fontWeight: '600',
    },
    unfollowButton: {
      width: 24,
      height: 24,
      borderRadius: 12,
      backgroundColor: colors.border,
      alignItems: 'center',
      justifyContent: 'center',
    },
    unfollowButtonText: {
      fontSize: 16,
      color: colors.text,
      fontWeight: 'bold',
    },
    topicSettings: {
      marginTop: 12,
      paddingTop: 12,
      borderTopWidth: 1,
      borderTopColor: colors.border,
    },
    settingRow: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      paddingVertical: 8,
    },
    settingLabel: {
      fontSize: 14,
    },
    sectionTitle: {
      fontSize: 18,
      fontWeight: '600',
      color: colors.text,
      paddingHorizontal: 16,
      marginBottom: 12,
      marginTop: 16,
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
      fontSize: 16,
      color: colors.text,
      textAlign: 'center',
      marginBottom: 16,
    },
    retryButton: {
      paddingHorizontal: 24,
      paddingVertical: 12,
      backgroundColor: colors.primary,
      borderRadius: 8,
    },
    retryButtonText: {
      color: '#ffffff',
      fontWeight: '600',
    },
    emptyContainer: {
      flex: 1,
      justifyContent: 'center',
      alignItems: 'center',
      padding: 32,
    },
    emptyText: {
      fontSize: 16,
      color: colors.text + '80',
      textAlign: 'center',
    },
  });

  if (loading) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.title}>🎯 Topics</Text>
          <Text style={styles.subtitle}>Follow topics that interest you</Text>
        </View>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={colors.primary} />
        </View>
      </SafeAreaView>
    );
  }

  if (error) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.title}>🎯 Topics</Text>
          <Text style={styles.subtitle}>Follow topics that interest you</Text>
        </View>
        <View style={styles.errorContainer}>
          <Text style={styles.errorText}>{error}</Text>
          <TouchableOpacity style={styles.retryButton} onPress={loadTopicsData}>
            <Text style={styles.retryButtonText}>Try Again</Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>🎯 Topics</Text>
        <Text style={styles.subtitle}>Follow topics that interest you</Text>
      </View>

      {/* Tabs */}
      <View style={styles.tabs}>
        {[
          { key: 'discover', label: 'Discover' },
          { key: 'following', label: 'Following' },
          { key: 'feed', label: 'Feed' },
        ].map(tab => (
          <TouchableOpacity
            key={tab.key}
            style={[
              styles.tab,
              activeTab === tab.key && styles.tabActive,
            ]}
            onPress={() => setActiveTab(tab.key as any)}
          >
            <Text
              style={[
                styles.tabText,
                activeTab === tab.key && styles.tabTextActive,
              ]}
            >
              {tab.label}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      {/* Search */}
      {activeTab === 'discover' && (
        <View style={styles.searchContainer}>
          <TextInput
            style={styles.searchInput}
            placeholder="Search topics..."
            placeholderTextColor={colors.text + '60'}
            value={searchQuery}
            onChangeText={setSearchQuery}
          />
        </View>
      )}

      <ScrollView
        style={styles.content}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
      >
        {activeTab === 'discover' && (
          <>
            {featuredTopics.length > 0 && (
              <>
                <Text style={styles.sectionTitle}>🌟 Featured Topics</Text>
                <FlatList
                  data={featuredTopics}
                  renderItem={renderTopicCard}
                  keyExtractor={(item) => item.id.toString()}
                  scrollEnabled={false}
                />
              </>
            )}

            {recommendations.length > 0 && (
              <>
                <Text style={styles.sectionTitle}>✨ Recommended for You</Text>
                <FlatList
                  data={recommendations.map(r => r.topic)}
                  renderItem={renderTopicCard}
                  keyExtractor={(item) => item.id.toString()}
                  scrollEnabled={false}
                />
              </>
            )}
          </>
        )}

        {activeTab === 'following' && (
          <>
            {followedTopics.length === 0 ? (
              <View style={styles.emptyContainer}>
                <Text style={styles.emptyText}>
                  You're not following any topics yet. Discover some topics to get started!
                </Text>
              </View>
            ) : (
              <FlatList
                data={followedTopics}
                renderItem={renderFollowedTopic}
                keyExtractor={(item) => item.id.toString()}
                scrollEnabled={false}
              />
            )}
          </>
        )}

        {activeTab === 'feed' && (
          <View style={styles.emptyContainer}>
            <Text style={styles.emptyText}>
              Topic feed coming soon! Follow some topics to see personalized content.
            </Text>
          </View>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}
