import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  RefreshControl,
  ActivityIndicator,
  StyleSheet,
  Dimensions,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useTheme } from '@react-navigation/native';
import { api } from '../../api/client';
import { TrendingHashtagDto } from '../../types';

const { width } = Dimensions.get('window');

interface TrendingScreenProps {
  navigation: any;
}

export default function TrendingScreen({ navigation }: TrendingScreenProps) {
  const { colors } = useTheme();
  const [trendingHashtags, setTrendingHashtags] = useState<TrendingHashtagDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);
  const [timeWindow, setTimeWindow] = useState(24);

  const categories = ['Technology', 'Sports', 'Entertainment', 'Gaming', 'News', 'Music'];

  useEffect(() => {
    loadTrendingData();
  }, [selectedCategory, timeWindow]);

  const loadTrendingData = async () => {
    try {
      setError(null);
      // Using the enhanced trending API
      const hashtags = selectedCategory 
        ? await api.trending.getTrendingByCategory(selectedCategory, 20)
        : await api.trending.getVelocityTrendingHashtags(20, timeWindow);
      setTrendingHashtags(hashtags);
    } catch (err) {
      console.error('Failed to load trending data:', err);
      setError('Failed to load trending data');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  const onRefresh = () => {
    setRefreshing(true);
    loadTrendingData();
  };

  const getVelocityIcon = (velocity: number) => {
    if (velocity > 0.7) return '🔥';
    if (velocity > 0.4) return '📈';
    return '📊';
  };

  const getVelocityColor = (velocity: number) => {
    if (velocity > 0.7) return '#ef4444';
    if (velocity > 0.4) return '#f97316';
    return '#3b82f6';
  };

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
    timeFilters: {
      flexDirection: 'row',
      paddingHorizontal: 16,
      paddingVertical: 12,
      gap: 8,
    },
    timeFilter: {
      paddingHorizontal: 12,
      paddingVertical: 6,
      borderRadius: 16,
      backgroundColor: colors.card,
      borderWidth: 1,
      borderColor: colors.border,
    },
    timeFilterActive: {
      backgroundColor: colors.primary,
      borderColor: colors.primary,
    },
    timeFilterText: {
      fontSize: 12,
      color: colors.text,
      fontWeight: '500',
    },
    timeFilterTextActive: {
      color: '#ffffff',
    },
    categoryFilters: {
      paddingHorizontal: 16,
      paddingBottom: 12,
    },
    categoryScroll: {
      flexDirection: 'row',
      gap: 8,
    },
    categoryFilter: {
      paddingHorizontal: 12,
      paddingVertical: 6,
      borderRadius: 16,
      backgroundColor: colors.card,
      borderWidth: 1,
      borderColor: colors.border,
    },
    categoryFilterActive: {
      backgroundColor: colors.primary,
      borderColor: colors.primary,
    },
    categoryFilterText: {
      fontSize: 12,
      color: colors.text,
      fontWeight: '500',
    },
    categoryFilterTextActive: {
      color: '#ffffff',
    },
    content: {
      flex: 1,
    },
    trendingItem: {
      flexDirection: 'row',
      alignItems: 'center',
      padding: 16,
      marginHorizontal: 16,
      marginVertical: 4,
      backgroundColor: colors.card,
      borderRadius: 12,
      borderWidth: 1,
      borderColor: colors.border,
    },
    rankBadge: {
      width: 32,
      height: 32,
      borderRadius: 16,
      backgroundColor: colors.border,
      alignItems: 'center',
      justifyContent: 'center',
      marginRight: 12,
    },
    rankText: {
      fontSize: 14,
      fontWeight: 'bold',
      color: colors.text,
    },
    hashtagInfo: {
      flex: 1,
    },
    hashtagName: {
      fontSize: 16,
      fontWeight: '600',
      color: colors.primary,
      marginBottom: 4,
    },
    hashtagStats: {
      flexDirection: 'row',
      alignItems: 'center',
      gap: 8,
    },
    statText: {
      fontSize: 12,
      color: colors.text + '80',
    },
    velocityInfo: {
      alignItems: 'flex-end',
    },
    velocityIcon: {
      fontSize: 20,
      marginBottom: 4,
    },
    velocityText: {
      fontSize: 12,
      fontWeight: '600',
    },
    velocityPercentage: {
      fontSize: 10,
      color: colors.text + '80',
    },
    velocityBar: {
      width: 40,
      height: 4,
      backgroundColor: colors.border,
      borderRadius: 2,
      marginTop: 4,
      overflow: 'hidden',
    },
    velocityFill: {
      height: '100%',
      borderRadius: 2,
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
          <Text style={styles.title}>🔥 Trending</Text>
          <Text style={styles.subtitle}>What's hot right now</Text>
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
          <Text style={styles.title}>🔥 Trending</Text>
          <Text style={styles.subtitle}>What's hot right now</Text>
        </View>
        <View style={styles.errorContainer}>
          <Text style={styles.errorText}>{error}</Text>
          <TouchableOpacity style={styles.retryButton} onPress={loadTrendingData}>
            <Text style={styles.retryButtonText}>Try Again</Text>
          </TouchableOpacity>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.title}>🔥 Trending</Text>
        <Text style={styles.subtitle}>What's hot right now</Text>
      </View>

      {/* Time Filters */}
      <View style={styles.timeFilters}>
        {[6, 12, 24, 48].map((hours) => (
          <TouchableOpacity
            key={hours}
            style={[
              styles.timeFilter,
              timeWindow === hours && styles.timeFilterActive,
            ]}
            onPress={() => setTimeWindow(hours)}
          >
            <Text
              style={[
                styles.timeFilterText,
                timeWindow === hours && styles.timeFilterTextActive,
              ]}
            >
              {hours}h
            </Text>
          </TouchableOpacity>
        ))}
      </View>

      {/* Category Filters */}
      <View style={styles.categoryFilters}>
        <ScrollView horizontal showsHorizontalScrollIndicator={false} style={styles.categoryScroll}>
          <TouchableOpacity
            style={[
              styles.categoryFilter,
              selectedCategory === null && styles.categoryFilterActive,
            ]}
            onPress={() => setSelectedCategory(null)}
          >
            <Text
              style={[
                styles.categoryFilterText,
                selectedCategory === null && styles.categoryFilterTextActive,
              ]}
            >
              All
            </Text>
          </TouchableOpacity>
          {categories.map((category) => (
            <TouchableOpacity
              key={category}
              style={[
                styles.categoryFilter,
                selectedCategory === category && styles.categoryFilterActive,
              ]}
              onPress={() => setSelectedCategory(category)}
            >
              <Text
                style={[
                  styles.categoryFilterText,
                  selectedCategory === category && styles.categoryFilterTextActive,
                ]}
              >
                {category}
              </Text>
            </TouchableOpacity>
          ))}
        </ScrollView>
      </View>

      {/* Trending List */}
      <ScrollView
        style={styles.content}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
      >
        {trendingHashtags.length === 0 ? (
          <View style={styles.emptyContainer}>
            <Text style={styles.emptyText}>
              No trending hashtags found for the selected criteria.
            </Text>
          </View>
        ) : (
          trendingHashtags.map((hashtag, index) => (
            <TouchableOpacity
              key={hashtag.name}
              style={styles.trendingItem}
              onPress={() => {
                // Navigate to hashtag details or posts
                navigation.navigate('HashtagPosts', { hashtag: hashtag.name });
              }}
            >
              <View style={styles.rankBadge}>
                <Text style={styles.rankText}>{index + 1}</Text>
              </View>

              <View style={styles.hashtagInfo}>
                <Text style={styles.hashtagName}>#{hashtag.name}</Text>
                <View style={styles.hashtagStats}>
                  <Text style={styles.statText}>
                    {hashtag.postCount.toLocaleString()} posts
                  </Text>
                  <Text style={styles.statText}>•</Text>
                  <Text style={styles.statText}>
                    {hashtag.uniqueUsers} users
                  </Text>
                  <Text style={styles.statText}>•</Text>
                  <Text style={styles.statText}>
                    {(hashtag.engagementRate * 100).toFixed(1)}% engagement
                  </Text>
                </View>
              </View>

              <View style={styles.velocityInfo}>
                <Text style={styles.velocityIcon}>
                  {getVelocityIcon(hashtag.velocity)}
                </Text>
                <Text
                  style={[
                    styles.velocityText,
                    { color: getVelocityColor(hashtag.velocity) },
                  ]}
                >
                  +{(hashtag.velocity * 100).toFixed(0)}%
                </Text>
                <View style={styles.velocityBar}>
                  <View
                    style={[
                      styles.velocityFill,
                      {
                        width: `${Math.min(hashtag.velocity * 100, 100)}%`,
                        backgroundColor: getVelocityColor(hashtag.velocity),
                      },
                    ]}
                  />
                </View>
              </View>
            </TouchableOpacity>
          ))
        )}
      </ScrollView>
    </SafeAreaView>
  );
}
