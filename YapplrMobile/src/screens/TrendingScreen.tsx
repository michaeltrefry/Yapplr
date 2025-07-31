import React, { useState } from 'react';
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  RefreshControl,
  ActivityIndicator,
  StyleSheet,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { Ionicons } from '@expo/vector-icons';
import { useNavigation } from '@react-navigation/native';
import { useAuth } from '../contexts/AuthContext';
import { Tag, Post } from '../types';
import PostCard from '../components/PostCard';

type TrendingPeriod = 'now' | 'today' | 'week';
type TrendingTab = 'hashtags' | 'posts';

const TrendingScreen = () => {
  const navigation = useNavigation();
  const { api } = useAuth();
  const [activePeriod, setActivePeriod] = useState<TrendingPeriod>('now');
  const [activeTab, setActiveTab] = useState<TrendingTab>('hashtags');

  const { data: trendingTags, isLoading: tagsLoading, error: tagsError, refetch: refetchTags } = useQuery({
    queryKey: ['trending-tags', activePeriod],
    queryFn: () => api.tags.getTrendingTags(20),
    refetchInterval: activePeriod === 'now' ? 30000 : 60000,
    enabled: activeTab === 'hashtags',
  });

  const { data: trendingPosts, isLoading: postsLoading, error: postsError, refetch: refetchPosts } = useQuery({
    queryKey: ['trending-posts', activePeriod],
    queryFn: () => {
      switch (activePeriod) {
        case 'now':
          return api.trending.getTrendingPostsNow(20);
        case 'today':
          return api.trending.getTrendingPostsToday(20);
        case 'week':
          return api.trending.getTrendingPostsWeek(20);
        default:
          return api.trending.getTrendingPostsToday(20);
      }
    },
    refetchInterval: activePeriod === 'now' ? 30000 : 60000,
    enabled: activeTab === 'posts',
  });

  const handleRefresh = () => {
    if (activeTab === 'hashtags') {
      refetchTags();
    } else {
      refetchPosts();
    }
  };

  const isLoading = activeTab === 'hashtags' ? tagsLoading : postsLoading;
  const error = activeTab === 'hashtags' ? tagsError : postsError;

  const navigateToHashtag = (tagName: string) => {
    navigation.navigate('HashtagFeed', { hashtag: tagName });
  };

  const formatNumber = (num: number): string => {
    if (num < 1000) return num.toString();
    if (num < 1000000) return `${(num / 1000).toFixed(1)}K`;
    return `${(num / 1000000).toFixed(1)}M`;
  };

  const getGradientColors = (index: number): string[] => {
    const gradients = [
      ['#FF6B35', '#F7931E'], // Orange to yellow
      ['#FFD23F', '#FF6B35'], // Yellow to orange
      ['#06FFA5', '#4ECDC4'], // Green to teal
      ['#4ECDC4', '#44A08D'], // Teal to green
      ['#A8E6CF', '#7FCDCD'], // Light green to light teal
    ];
    return gradients[index % gradients.length];
  };

  if (error) {
    return (
      <View style={styles.container}>
        <View style={styles.header}>
          <TouchableOpacity
            style={styles.backButton}
            onPress={() => navigation.goBack()}
          >
            <Ionicons name="arrow-back" size={24} color="#000" />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>Trending</Text>
        </View>
        <View style={styles.errorContainer}>
          <Ionicons name="trending-up" size={64} color="#ccc" />
          <Text style={styles.errorTitle}>Unable to load trending hashtags</Text>
          <Text style={styles.errorText}>
            There was an error loading the trending hashtags. Please try again later.
          </Text>
          <TouchableOpacity style={styles.retryButton} onPress={handleRefresh}>
            <Text style={styles.retryButtonText}>Try Again</Text>
          </TouchableOpacity>
        </View>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <TouchableOpacity
          style={styles.backButton}
          onPress={() => navigation.goBack()}
        >
          <Ionicons name="arrow-back" size={24} color="#000" />
        </TouchableOpacity>
        <View style={styles.headerContent}>
          <Text style={styles.headerTitle}>
            Trending {activeTab === 'hashtags' ? 'Hashtags' : 'Posts'}
          </Text>
          <Text style={styles.headerSubtitle}>
            {activeTab === 'hashtags' && (
              <>
                {activePeriod === 'now' && 'Popular hashtags right now'}
                {activePeriod === 'today' && 'Popular hashtags today'}
                {activePeriod === 'week' && 'Popular hashtags this week'}
              </>
            )}
            {activeTab === 'posts' && (
              <>
                {activePeriod === 'now' && 'Popular posts right now'}
                {activePeriod === 'today' && 'Popular posts today'}
                {activePeriod === 'week' && 'Popular posts this week'}
              </>
            )}
          </Text>
        </View>
        <View style={styles.liveIndicator}>
          <Ionicons name="time" size={12} color="#666" />
          <Text style={styles.liveText}>
            {activePeriod === 'now' ? 'Live' : 'Updated'}
          </Text>
        </View>
      </View>

      {/* Content Type Tabs */}
      <View style={styles.contentTabContainer}>
        <TouchableOpacity
          style={[styles.contentTab, activeTab === 'hashtags' && styles.activeContentTab]}
          onPress={() => setActiveTab('hashtags')}
        >
          <Ionicons name="pricetag" size={16} color={activeTab === 'hashtags' ? '#FF6B35' : '#666'} />
          <Text style={[styles.contentTabText, activeTab === 'hashtags' && styles.activeContentTabText]}>
            Hashtags
          </Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.contentTab, activeTab === 'posts' && styles.activeContentTab]}
          onPress={() => setActiveTab('posts')}
        >
          <Ionicons name="chatbubble" size={16} color={activeTab === 'posts' ? '#FF6B35' : '#666'} />
          <Text style={[styles.contentTabText, activeTab === 'posts' && styles.activeContentTabText]}>
            Posts
          </Text>
        </TouchableOpacity>
      </View>

      {/* Time Period Tabs */}
      <View style={styles.tabContainer}>
        <TouchableOpacity
          style={[styles.tab, activePeriod === 'now' && styles.activeTab]}
          onPress={() => setActivePeriod('now')}
        >
          <Ionicons name="flash" size={16} color={activePeriod === 'now' ? '#FF6B35' : '#666'} />
          <Text style={[styles.tabText, activePeriod === 'now' && styles.activeTabText]}>
            Right Now
          </Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.tab, activePeriod === 'today' && styles.activeTab]}
          onPress={() => setActivePeriod('today')}
        >
          <Ionicons name="time" size={16} color={activePeriod === 'today' ? '#FF6B35' : '#666'} />
          <Text style={[styles.tabText, activePeriod === 'today' && styles.activeTabText]}>
            Today
          </Text>
        </TouchableOpacity>
        <TouchableOpacity
          style={[styles.tab, activePeriod === 'week' && styles.activeTab]}
          onPress={() => setActivePeriod('week')}
        >
          <Ionicons name="calendar" size={16} color={activePeriod === 'week' ? '#FF6B35' : '#666'} />
          <Text style={[styles.tabText, activePeriod === 'week' && styles.activeTabText]}>
            This Week
          </Text>
        </TouchableOpacity>
      </View>

      {/* Content */}
      <ScrollView
        style={styles.content}
        refreshControl={
          <RefreshControl refreshing={isLoading} onRefresh={handleRefresh} />
        }
      >
        {isLoading ? (
          <View style={styles.loadingContainer}>
            {Array.from({ length: 10 }).map((_, i) => (
              <View key={i} style={styles.loadingItem}>
                <View style={styles.loadingRank} />
                <View style={styles.loadingIcon} />
                <View style={styles.loadingContent}>
                  <View style={styles.loadingTitle} />
                  <View style={styles.loadingSubtitle} />
                </View>
              </View>
            ))}
          </View>
        ) : activeTab === 'hashtags' && trendingTags && trendingTags.length > 0 ? (
          <View style={styles.trendingList}>
            {trendingTags.map((tag: Tag, index: number) => (
              <TouchableOpacity
                key={tag.id}
                style={styles.trendingItem}
                onPress={() => navigateToHashtag(tag.name)}
              >
                {/* Rank */}
                <View style={styles.rankContainer}>
                  <Text style={[styles.rank, index < 3 && styles.topRank]}>
                    #{index + 1}
                  </Text>
                </View>

                {/* Hashtag Icon */}
                <View style={[styles.hashtagIcon, { backgroundColor: getGradientColors(index)[0] }]}>
                  <Ionicons name="pricetag" size={20} color="#fff" />
                </View>

                {/* Hashtag Info */}
                <View style={styles.hashtagInfo}>
                  <Text style={styles.hashtagName}>#{tag.name}</Text>
                  <View style={styles.hashtagMeta}>
                    <Ionicons name="people" size={12} color="#666" />
                    <Text style={styles.postCount}>
                      {formatNumber(tag.postCount || 0)} {(tag.postCount || 0) === 1 ? 'post' : 'posts'}
                    </Text>
                    {activePeriod === 'now' && (
                      <Text style={styles.trendingBadge}>TRENDING</Text>
                    )}
                  </View>
                  {index < 3 && (
                    <Text style={styles.rankDescription}>
                      {index === 0 && '🔥 Most popular hashtag'}
                      {index === 1 && '⭐ Second most popular'}
                      {index === 2 && '🚀 Third most popular'}
                    </Text>
                  )}
                </View>

                {/* Trending Indicator */}
                {index < 5 && (
                  <View style={styles.trendingIndicator}>
                    <Ionicons
                      name="trending-up"
                      size={16}
                      color={index < 3 ? '#FF6B35' : '#4CAF50'}
                    />
                    <Text style={[styles.hotText, { color: index < 3 ? '#FF6B35' : '#4CAF50' }]}>
                      {index < 3 ? 'Hot' : 'Rising'}
                    </Text>
                  </View>
                )}
              </TouchableOpacity>
            ))}
          </View>
        ) : activeTab === 'posts' && trendingPosts && trendingPosts.length > 0 ? (
          <View style={styles.trendingList}>
            {trendingPosts.map((post: Post, index: number) => (
              <View key={post.id} style={styles.postContainer}>
                {/* Trending Rank Badge */}
                <View style={styles.postRankBadge}>
                  <Text style={[styles.postRank, index < 3 && styles.topPostRank]}>
                    {index + 1}
                  </Text>
                </View>

                {/* Post Card */}
                <View style={styles.postCardWrapper}>
                  <PostCard
                    post={post}
                    onPress={() => {
                      // Navigate to post detail if needed
                    }}
                  />
                </View>
              </View>
            ))}
          </View>
        ) : (
          <View style={styles.emptyContainer}>
            <Ionicons name="trending-up" size={64} color="#ccc" />
            <Text style={styles.emptyTitle}>
              {activeTab === 'hashtags' ? 'No trending hashtags yet' : 'No trending posts yet'}
            </Text>
            <Text style={styles.emptyText}>
              {activeTab === 'hashtags'
                ? 'Start using hashtags in your posts to see them trend!'
                : 'Create engaging content to see it trend!'
              }
            </Text>
          </View>
        )}

        {/* Footer */}
        {((activeTab === 'hashtags' && trendingTags && trendingTags.length > 0) ||
          (activeTab === 'posts' && trendingPosts && trendingPosts.length > 0)) && (
          <View style={styles.footer}>
            <Text style={styles.footerText}>
              {activePeriod === 'now' && 'Rankings update every 30 seconds based on recent activity'}
              {activePeriod === 'today' && 'Rankings based on activity from the last 24 hours'}
              {activePeriod === 'week' && 'Rankings based on activity from the last 7 days'}
            </Text>
            <Text style={styles.footerTime}>
              Last updated: {new Date().toLocaleTimeString()}
            </Text>
          </View>
        )}
      </ScrollView>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f8f9fa',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    backgroundColor: '#fff',
    borderBottomWidth: 1,
    borderBottomColor: '#e1e8ed',
  },
  backButton: {
    padding: 8,
    marginRight: 8,
  },
  headerContent: {
    flex: 1,
  },
  headerTitle: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#000',
  },
  headerSubtitle: {
    fontSize: 14,
    color: '#666',
    marginTop: 2,
  },
  liveIndicator: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  liveText: {
    fontSize: 12,
    color: '#666',
    marginLeft: 4,
  },
  tabContainer: {
    flexDirection: 'row',
    backgroundColor: '#fff',
    borderBottomWidth: 1,
    borderBottomColor: '#e1e8ed',
  },
  tab: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 12,
    borderBottomWidth: 2,
    borderBottomColor: 'transparent',
  },
  activeTab: {
    borderBottomColor: '#FF6B35',
  },
  tabText: {
    fontSize: 14,
    fontWeight: '500',
    color: '#666',
    marginLeft: 6,
  },
  activeTabText: {
    color: '#FF6B35',
  },
  contentTabContainer: {
    flexDirection: 'row',
    backgroundColor: '#f0f0f0',
    marginHorizontal: 16,
    marginVertical: 8,
    borderRadius: 8,
    padding: 4,
  },
  contentTab: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 8,
    paddingHorizontal: 12,
    borderRadius: 6,
  },
  activeContentTab: {
    backgroundColor: '#fff',
    shadowColor: '#000',
    shadowOffset: {
      width: 0,
      height: 1,
    },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
  },
  contentTabText: {
    fontSize: 14,
    fontWeight: '500',
    color: '#666',
    marginLeft: 6,
  },
  activeContentTabText: {
    color: '#FF6B35',
  },
  content: {
    flex: 1,
  },
  loadingContainer: {
    padding: 16,
  },
  loadingItem: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
  },
  loadingRank: {
    width: 24,
    height: 16,
    backgroundColor: '#e1e8ed',
    borderRadius: 4,
    marginRight: 12,
  },
  loadingIcon: {
    width: 48,
    height: 48,
    backgroundColor: '#e1e8ed',
    borderRadius: 24,
    marginRight: 12,
  },
  loadingContent: {
    flex: 1,
  },
  loadingTitle: {
    height: 16,
    backgroundColor: '#e1e8ed',
    borderRadius: 4,
    marginBottom: 8,
    width: '60%',
  },
  loadingSubtitle: {
    height: 12,
    backgroundColor: '#e1e8ed',
    borderRadius: 4,
    width: '40%',
  },
  trendingList: {
    backgroundColor: '#fff',
  },
  trendingItem: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#f0f0f0',
  },
  rankContainer: {
    width: 32,
    alignItems: 'center',
    marginRight: 12,
  },
  rank: {
    fontSize: 14,
    fontWeight: 'bold',
    color: '#666',
  },
  topRank: {
    color: '#FF6B35',
  },
  hashtagIcon: {
    width: 48,
    height: 48,
    borderRadius: 24,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 12,
  },
  hashtagInfo: {
    flex: 1,
  },
  hashtagName: {
    fontSize: 18,
    fontWeight: '600',
    color: '#000',
    marginBottom: 4,
  },
  hashtagMeta: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  postCount: {
    fontSize: 14,
    color: '#666',
    marginLeft: 4,
    marginRight: 8,
  },
  trendingBadge: {
    fontSize: 10,
    fontWeight: 'bold',
    color: '#FF6B35',
    backgroundColor: '#FFF3E0',
    paddingHorizontal: 6,
    paddingVertical: 2,
    borderRadius: 8,
  },
  rankDescription: {
    fontSize: 12,
    color: '#999',
    marginTop: 2,
  },
  trendingIndicator: {
    alignItems: 'center',
  },
  hotText: {
    fontSize: 10,
    fontWeight: '600',
    marginTop: 2,
  },
  emptyContainer: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 64,
    paddingHorizontal: 32,
  },
  emptyTitle: {
    fontSize: 20,
    fontWeight: '600',
    color: '#000',
    marginTop: 16,
    marginBottom: 8,
  },
  emptyText: {
    fontSize: 16,
    color: '#666',
    textAlign: 'center',
    lineHeight: 24,
  },
  errorContainer: {
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 64,
    paddingHorizontal: 32,
  },
  errorTitle: {
    fontSize: 20,
    fontWeight: '600',
    color: '#000',
    marginTop: 16,
    marginBottom: 8,
  },
  errorText: {
    fontSize: 16,
    color: '#666',
    textAlign: 'center',
    lineHeight: 24,
    marginBottom: 24,
  },
  retryButton: {
    backgroundColor: '#1DA1F2',
    paddingHorizontal: 24,
    paddingVertical: 12,
    borderRadius: 24,
  },
  retryButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  footer: {
    padding: 16,
    backgroundColor: '#fff',
    borderTopWidth: 1,
    borderTopColor: '#e1e8ed',
  },
  footerText: {
    fontSize: 12,
    color: '#666',
    textAlign: 'center',
    marginBottom: 4,
  },
  footerTime: {
    fontSize: 10,
    color: '#999',
    textAlign: 'center',
  },
  postContainer: {
    position: 'relative',
    marginBottom: 8,
  },
  postRankBadge: {
    position: 'absolute',
    top: 12,
    left: 8,
    zIndex: 10,
    backgroundColor: '#FF6B35',
    borderRadius: 12,
    width: 24,
    height: 24,
    alignItems: 'center',
    justifyContent: 'center',
    shadowColor: '#000',
    shadowOffset: {
      width: 0,
      height: 2,
    },
    shadowOpacity: 0.1,
    shadowRadius: 3,
    elevation: 3,
  },
  postRank: {
    fontSize: 12,
    fontWeight: 'bold',
    color: '#fff',
  },
  topPostRank: {
    color: '#fff',
  },
  postCardWrapper: {
    marginLeft: 16,
    backgroundColor: '#fff',
    borderRadius: 8,
    shadowColor: '#000',
    shadowOffset: {
      width: 0,
      height: 1,
    },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
  },
});

export default TrendingScreen;
