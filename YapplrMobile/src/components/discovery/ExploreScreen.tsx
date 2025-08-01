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
  Image,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useTheme } from '@react-navigation/native';
import { api } from '../../api/client';
import { ExplorePageDto, UserRecommendationDto, Post } from '../../types';

interface ExploreScreenProps {
  navigation: any;
}

export default function ExploreScreen({ navigation }: ExploreScreenProps) {
  const { colors } = useTheme();
  const [exploreData, setExploreData] = useState<ExplorePageDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [activeSection, setActiveSection] = useState<'overview' | 'users' | 'content'>('overview');

  useEffect(() => {
    loadExploreData();
  }, []);

  const loadExploreData = async () => {
    try {
      setError(null);
      const config = {
        trendingPostsLimit: 10,
        trendingHashtagsLimit: 15,
        recommendedUsersLimit: 8,
        timeWindowHours: 24,
        includePersonalizedContent: true,
        includeUserRecommendations: true,
        minSimilarityScore: 0.1
      };
      const data = await api.explore.getExplorePage(config);
      setExploreData(data);
    } catch (err) {
      console.error('Failed to load explore data:', err);
      setError('Failed to load explore data');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  const onRefresh = () => {
    setRefreshing(true);
    loadExploreData();
  };

  const renderUserRecommendation = ({ item }: { item: UserRecommendationDto }) => (
    <TouchableOpacity
      style={styles.userCard}
      onPress={() => navigation.navigate('Profile', { username: item.user.username })}
    >
      <Image
        source={{ uri: item.user.profilePictureUrl || 'https://via.placeholder.com/60' }}
        style={styles.userAvatar}
      />
      <View style={styles.userInfo}>
        <Text style={[styles.username, { color: colors.text }]} numberOfLines={1}>
          {item.user.username}
        </Text>
        <Text style={[styles.userBio, { color: colors.text + '80' }]} numberOfLines={2}>
          {item.user.bio || 'No bio available'}
        </Text>
        <Text style={[styles.matchScore, { color: colors.primary }]}>
          {(item.similarityScore * 100).toFixed(0)}% match
        </Text>
      </View>
      <TouchableOpacity style={[styles.followButton, { backgroundColor: colors.primary }]}>
        <Text style={styles.followButtonText}>Follow</Text>
      </TouchableOpacity>
    </TouchableOpacity>
  );

  const renderTrendingPost = ({ item }: { item: Post }) => (
    <TouchableOpacity
      style={styles.postCard}
      onPress={() => navigation.navigate('PostDetail', { postId: item.id })}
    >
      <View style={styles.postHeader}>
        <Image
          source={{ uri: item.user.profilePictureUrl || 'https://via.placeholder.com/40' }}
          style={styles.postUserAvatar}
        />
        <View style={styles.postUserInfo}>
          <Text style={[styles.postUsername, { color: colors.text }]}>
            {item.user.username}
          </Text>
          <Text style={[styles.postTime, { color: colors.text + '80' }]}>
            {new Date(item.createdAt).toLocaleDateString()}
          </Text>
        </View>
        <View style={styles.trendingBadge}>
          <Text style={styles.trendingBadgeText}>🔥</Text>
        </View>
      </View>
      <Text style={[styles.postContent, { color: colors.text }]} numberOfLines={3}>
        {item.content}
      </Text>
    </TouchableOpacity>
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
    sectionTabs: {
      flexDirection: 'row',
      paddingHorizontal: 16,
      paddingVertical: 12,
      gap: 8,
    },
    sectionTab: {
      paddingHorizontal: 16,
      paddingVertical: 8,
      borderRadius: 20,
      backgroundColor: colors.card,
      borderWidth: 1,
      borderColor: colors.border,
    },
    sectionTabActive: {
      backgroundColor: colors.primary,
      borderColor: colors.primary,
    },
    sectionTabText: {
      fontSize: 14,
      color: colors.text,
      fontWeight: '500',
    },
    sectionTabTextActive: {
      color: '#ffffff',
    },
    content: {
      flex: 1,
    },
    section: {
      marginBottom: 24,
    },
    sectionHeader: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      alignItems: 'center',
      paddingHorizontal: 16,
      marginBottom: 12,
    },
    sectionTitle: {
      fontSize: 18,
      fontWeight: '600',
      color: colors.text,
    },
    seeAllButton: {
      paddingHorizontal: 12,
      paddingVertical: 6,
      borderRadius: 12,
      backgroundColor: colors.card,
    },
    seeAllText: {
      fontSize: 12,
      color: colors.primary,
      fontWeight: '500',
    },
    userCard: {
      flexDirection: 'row',
      alignItems: 'center',
      padding: 12,
      marginHorizontal: 16,
      marginVertical: 4,
      backgroundColor: colors.card,
      borderRadius: 12,
      borderWidth: 1,
      borderColor: colors.border,
    },
    userAvatar: {
      width: 50,
      height: 50,
      borderRadius: 25,
      marginRight: 12,
    },
    userInfo: {
      flex: 1,
    },
    username: {
      fontSize: 16,
      fontWeight: '600',
      marginBottom: 2,
    },
    userBio: {
      fontSize: 12,
      marginBottom: 4,
    },
    matchScore: {
      fontSize: 11,
      fontWeight: '500',
    },
    followButton: {
      paddingHorizontal: 16,
      paddingVertical: 6,
      borderRadius: 16,
    },
    followButtonText: {
      color: '#ffffff',
      fontSize: 12,
      fontWeight: '600',
    },
    postCard: {
      padding: 16,
      marginHorizontal: 16,
      marginVertical: 4,
      backgroundColor: colors.card,
      borderRadius: 12,
      borderWidth: 1,
      borderColor: colors.border,
    },
    postHeader: {
      flexDirection: 'row',
      alignItems: 'center',
      marginBottom: 12,
    },
    postUserAvatar: {
      width: 40,
      height: 40,
      borderRadius: 20,
      marginRight: 12,
    },
    postUserInfo: {
      flex: 1,
    },
    postUsername: {
      fontSize: 14,
      fontWeight: '600',
    },
    postTime: {
      fontSize: 12,
    },
    trendingBadge: {
      width: 24,
      height: 24,
      borderRadius: 12,
      backgroundColor: '#ef4444',
      alignItems: 'center',
      justifyContent: 'center',
    },
    trendingBadgeText: {
      fontSize: 12,
    },
    postContent: {
      fontSize: 14,
      lineHeight: 20,
    },
    metricsCard: {
      flexDirection: 'row',
      marginHorizontal: 16,
      marginBottom: 16,
      backgroundColor: colors.card,
      borderRadius: 12,
      borderWidth: 1,
      borderColor: colors.border,
    },
    metricItem: {
      flex: 1,
      padding: 16,
      alignItems: 'center',
    },
    metricValue: {
      fontSize: 20,
      fontWeight: 'bold',
      marginBottom: 4,
    },
    metricLabel: {
      fontSize: 12,
      color: colors.text + '80',
      textAlign: 'center',
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
  });

  if (loading) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.title}>🌟 Explore</Text>
          <Text style={styles.subtitle}>Discover new content and people</Text>
        </View>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color={colors.primary} />
        </View>
      </SafeAreaView>
    );
  }

  if (error || !exploreData) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.title}>🌟 Explore</Text>
          <Text style={styles.subtitle}>Discover new content and people</Text>
        </View>
        <View style={styles.errorContainer}>
          <Text style={styles.errorText}>{error || 'Unable to load explore content'}</Text>
          <TouchableOpacity style={styles.retryButton} onPress={loadExploreData}>
            <Text style={styles.retryButtonText}>Try Again</Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>🌟 Explore</Text>
        <Text style={styles.subtitle}>Discover new content and people</Text>
      </View>

      {/* Section Tabs */}
      <View style={styles.sectionTabs}>
        {[
          { key: 'overview', label: 'Overview' },
          { key: 'users', label: '👥 People' },
          { key: 'content', label: '📝 Content' },
        ].map(tab => (
          <TouchableOpacity
            key={tab.key}
            style={[
              styles.sectionTab,
              activeSection === tab.key && styles.sectionTabActive,
            ]}
            onPress={() => setActiveSection(tab.key as any)}
          >
            <Text
              style={[
                styles.sectionTabText,
                activeSection === tab.key && styles.sectionTabTextActive,
              ]}
            >
              {tab.label}
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      <ScrollView
        style={styles.content}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
      >
        {activeSection === 'overview' && (
          <>
            {/* Metrics */}
            <View style={styles.metricsCard}>
              <View style={styles.metricItem}>
                <Text style={[styles.metricValue, { color: '#3b82f6' }]}>
                  {exploreData.metrics.totalTrendingPosts}
                </Text>
                <Text style={styles.metricLabel}>Trending Posts</Text>
              </View>
              <View style={styles.metricItem}>
                <Text style={[styles.metricValue, { color: '#10b981' }]}>
                  {exploreData.metrics.totalRecommendedUsers}
                </Text>
                <Text style={styles.metricLabel}>Recommended Users</Text>
              </View>
              <View style={styles.metricItem}>
                <Text style={[styles.metricValue, { color: '#8b5cf6' }]}>
                  {(exploreData.metrics.averageEngagementRate * 100).toFixed(1)}%
                </Text>
                <Text style={styles.metricLabel}>Avg Engagement</Text>
              </View>
            </View>

            {/* Trending Posts Section */}
            <View style={styles.section}>
              <View style={styles.sectionHeader}>
                <Text style={styles.sectionTitle}>🔥 Trending Posts</Text>
                <TouchableOpacity style={styles.seeAllButton}>
                  <Text style={styles.seeAllText}>See All</Text>
                </TouchableOpacity>
              </View>
              <FlatList
                data={exploreData.trendingPosts.slice(0, 5)}
                renderItem={renderTrendingPost}
                keyExtractor={(item) => item.id.toString()}
                scrollEnabled={false}
              />
            </View>

            {/* Recommended Users Section */}
            <View style={styles.section}>
              <View style={styles.sectionHeader}>
                <Text style={styles.sectionTitle}>👥 People You Might Like</Text>
                <TouchableOpacity style={styles.seeAllButton}>
                  <Text style={styles.seeAllText}>See All</Text>
                </TouchableOpacity>
              </View>
              <FlatList
                data={exploreData.recommendedUsers.slice(0, 3)}
                renderItem={renderUserRecommendation}
                keyExtractor={(item) => item.user.id.toString()}
                scrollEnabled={false}
              />
            </View>
          </>
        )}

        {activeSection === 'users' && (
          <View style={styles.section}>
            <FlatList
              data={exploreData.recommendedUsers}
              renderItem={renderUserRecommendation}
              keyExtractor={(item) => item.user.id.toString()}
              scrollEnabled={false}
            />
          </View>
        )}

        {activeSection === 'content' && (
          <View style={styles.section}>
            <FlatList
              data={exploreData.trendingPosts}
              renderItem={renderTrendingPost}
              keyExtractor={(item) => item.id.toString()}
              scrollEnabled={false}
            />
          </View>
        )}
      </ScrollView>
    </SafeAreaView>
  );
}
