'use client';

import React, { useState, useEffect } from 'react';
import { exploreApi } from '@/lib/api';
import { TrendingTopicDto } from '@/types';
import PostCard from '@/components/PostCard';
import UserAvatar from '@/components/UserAvatar';
import Link from 'next/link';

interface TrendingTopicsProps {
  className?: string;
  timeWindow?: number;
  limit?: number;
}

export default function TrendingTopics({ 
  className = '', 
  timeWindow = 24, 
  limit = 10 
}: TrendingTopicsProps) {
  const [trendingTopics, setTrendingTopics] = useState<TrendingTopicDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedTopic, setSelectedTopic] = useState<TrendingTopicDto | null>(null);
  const [timeFilter, setTimeFilter] = useState(timeWindow);

  useEffect(() => {
    loadTrendingTopics();
  }, [timeFilter, limit]);

  const loadTrendingTopics = async () => {
    try {
      setLoading(true);
      setError(null);
      const topics = await exploreApi.getTrendingTopics(timeFilter, limit);
      setTrendingTopics(topics);
    } catch (err) {
      console.error('Failed to load trending topics:', err);
      setError('Failed to load trending topics');
    } finally {
      setLoading(false);
    }
  };

  const getTopicIcon = (category: string) => {
    const icons: Record<string, string> = {
      'Technology': '💻',
      'Sports': '⚽',
      'Entertainment': '🎬',
      'Gaming': '🎮',
      'News': '📰',
      'Music': '🎵',
      'Art': '🎨',
      'Food': '🍕',
      'Travel': '✈️',
      'Fashion': '👗',
      'Health': '🏥',
      'Science': '🔬'
    };
    return icons[category] || '🏷️';
  };

  const getGrowthColor = (growthRate: number) => {
    if (growthRate > 0.5) return 'text-green-500';
    if (growthRate > 0.2) return 'text-blue-500';
    if (growthRate > 0) return 'text-yellow-500';
    return 'text-red-500';
  };

  if (loading) {
    return (
      <div className={`trending-topics ${className}`}>
        <div className="animate-pulse space-y-6">
          <div className="h-8 bg-gray-200 dark:bg-gray-700 rounded"></div>
          <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-6">
            {[...Array(6)].map((_, i) => (
              <div key={i} className="h-64 bg-gray-200 dark:bg-gray-700 rounded-lg"></div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={`trending-topics ${className}`}>
        <div className="text-center py-12">
          <div className="text-6xl mb-4">😕</div>
          <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
            Something went wrong
          </h3>
          <p className="text-gray-500 dark:text-gray-400 mb-4">{error}</p>
          <button 
            onClick={loadTrendingTopics}
            className="px-6 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className={`trending-topics ${className}`}>
      {/* Header */}
      <div className="mb-6">
        <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
          🏷️ Trending Topics
        </h3>
        <p className="text-gray-600 dark:text-gray-400">
          Discover what's trending across different categories
        </p>
      </div>

      {/* Time Filter */}
      <div className="mb-6">
        <div className="flex space-x-2">
          {[6, 12, 24, 48].map((hours) => (
            <button
              key={hours}
              onClick={() => setTimeFilter(hours)}
              className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
                timeFilter === hours
                  ? 'bg-blue-500 text-white'
                  : 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-600'
              }`}
            >
              {hours}h
            </button>
          ))}
        </div>
      </div>

      {/* Topics Grid */}
      {trendingTopics.length === 0 ? (
        <div className="text-center py-8">
          <div className="text-4xl mb-4">🏷️</div>
          <p className="text-gray-500 dark:text-gray-400">
            No trending topics found for the selected time period
          </p>
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-6">
          {trendingTopics.map((topic) => (
            <div 
              key={topic.topic}
              className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow cursor-pointer"
              onClick={() => setSelectedTopic(topic)}
            >
              {/* Topic Header */}
              <div className="flex items-center justify-between mb-4">
                <div className="flex items-center space-x-3">
                  <span className="text-2xl">
                    {getTopicIcon(topic.category)}
                  </span>
                  <div>
                    <h4 className="text-lg font-semibold text-gray-900 dark:text-white">
                      {topic.topic}
                    </h4>
                    <span className="text-sm text-gray-500 dark:text-gray-400">
                      {topic.category}
                    </span>
                  </div>
                </div>
                <div className="text-right">
                  <div className="text-lg font-bold text-gray-900 dark:text-white">
                    {topic.topicScore.toFixed(1)}
                  </div>
                  <div className={`text-sm font-medium ${getGrowthColor(topic.growthRate)}`}>
                    {topic.growthRate > 0 ? '+' : ''}{(topic.growthRate * 100).toFixed(0)}%
                  </div>
                </div>
              </div>

              {/* Related Hashtags */}
              <div className="mb-4">
                <div className="flex flex-wrap gap-1">
                  {topic.relatedHashtags.slice(0, 3).map((hashtag) => (
                    <span 
                      key={hashtag.name}
                      className="px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 rounded-full text-xs"
                    >
                      #{hashtag.name}
                    </span>
                  ))}
                  {topic.relatedHashtags.length > 3 && (
                    <span className="text-xs text-gray-500 dark:text-gray-400">
                      +{topic.relatedHashtags.length - 3} more
                    </span>
                  )}
                </div>
              </div>

              {/* Top Contributors */}
              <div className="mb-4">
                <h5 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  Top Contributors
                </h5>
                <div className="flex -space-x-2">
                  {topic.topContributors.slice(0, 4).map((user) => (
                    <UserAvatar 
                      key={user.id}
                      user={user} 
                      size="sm" 
                      className="border-2 border-white dark:border-gray-800"
                    />
                  ))}
                  {topic.topContributors.length > 4 && (
                    <div className="w-8 h-8 bg-gray-200 dark:bg-gray-600 rounded-full flex items-center justify-center text-xs font-medium text-gray-600 dark:text-gray-400 border-2 border-white dark:border-gray-800">
                      +{topic.topContributors.length - 4}
                    </div>
                  )}
                </div>
              </div>

              {/* Stats */}
              <div className="flex items-center justify-between text-sm text-gray-500 dark:text-gray-400">
                <span>{topic.trendingPosts.length} trending posts</span>
                <span>Score: {topic.topicScore.toFixed(1)}</span>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Topic Detail Modal */}
      {selectedTopic && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white dark:bg-gray-800 rounded-lg max-w-6xl w-full max-h-[90vh] overflow-y-auto">
            <div className="p-6">
              {/* Modal Header */}
              <div className="flex items-center justify-between mb-6">
                <div className="flex items-center space-x-3">
                  <span className="text-3xl">
                    {getTopicIcon(selectedTopic.category)}
                  </span>
                  <div>
                    <h3 className="text-2xl font-bold text-gray-900 dark:text-white">
                      {selectedTopic.topic}
                    </h3>
                    <p className="text-gray-600 dark:text-gray-400">
                      {selectedTopic.category} • Score: {selectedTopic.topicScore.toFixed(1)}
                    </p>
                  </div>
                </div>
                <button
                  onClick={() => setSelectedTopic(null)}
                  className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                >
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>

              {/* Related Hashtags */}
              <div className="mb-6">
                <h4 className="text-lg font-semibold text-gray-900 dark:text-white mb-3">
                  Related Hashtags
                </h4>
                <div className="flex flex-wrap gap-2">
                  {selectedTopic.relatedHashtags.map((hashtag) => (
                    <span 
                      key={hashtag.name}
                      className="px-3 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 rounded-full text-sm"
                    >
                      #{hashtag.name}
                    </span>
                  ))}
                </div>
              </div>

              {/* Top Contributors */}
              <div className="mb-6">
                <h4 className="text-lg font-semibold text-gray-900 dark:text-white mb-3">
                  Top Contributors
                </h4>
                <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-4">
                  {selectedTopic.topContributors.map((user) => (
                    <Link 
                      key={user.id}
                      href={`/profile/${user.username}`}
                      className="flex flex-col items-center space-y-2 p-3 bg-gray-50 dark:bg-gray-700 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-600 transition-colors"
                    >
                      <UserAvatar user={user} size="md" />
                      <span className="text-sm font-medium text-gray-900 dark:text-white text-center">
                        {user.username}
                      </span>
                    </Link>
                  ))}
                </div>
              </div>

              {/* Trending Posts */}
              <div>
                <h4 className="text-lg font-semibold text-gray-900 dark:text-white mb-3">
                  Trending Posts
                </h4>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                  {selectedTopic.trendingPosts.map((post) => (
                    <PostCard key={post.id} post={post} compact />
                  ))}
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
