'use client';

import React, { useState, useEffect } from 'react';
import { topicApi } from '@/lib/api';
import { PersonalizedTopicFeedDto, TopicFeedConfigDto, Post } from '@/types';
import PostCard from '@/components/PostCard';

interface PersonalizedTopicFeedProps {
  className?: string;
}

export default function PersonalizedTopicFeed({ className = '' }: PersonalizedTopicFeedProps) {
  const [feedData, setFeedData] = useState<PersonalizedTopicFeedDto | null>(null);
  const [mixedFeed, setMixedFeed] = useState<Post[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [viewMode, setViewMode] = useState<'topics' | 'mixed'>('mixed');
  const [config, setConfig] = useState<Partial<TopicFeedConfigDto>>({
    postsPerTopic: 5,
    maxTopics: 8,
    timeWindowHours: 24,
    sortBy: 'personalized'
  });

  useEffect(() => {
    loadFeedData();
  }, [config]);

  const loadFeedData = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const [personalizedFeed, mixed] = await Promise.all([
        topicApi.getPersonalizedTopicFeed(config),
        topicApi.getMixedTopicFeed(config)
      ]);
      
      setFeedData(personalizedFeed);
      setMixedFeed(mixed);
    } catch (err) {
      console.error('Failed to load topic feed:', err);
      setError('Failed to load personalized feed');
    } finally {
      setLoading(false);
    }
  };

  const updateConfig = (updates: Partial<TopicFeedConfigDto>) => {
    setConfig(prev => ({ ...prev, ...updates }));
  };

  const getTopicIcon = (category: string) => {
    const icons: Record<string, string> = {
      'Technology': '💻', 'Sports': '⚽', 'Entertainment': '🎬', 'Gaming': '🎮',
      'News': '📰', 'Music': '🎵', 'Art': '🎨', 'Food': '🍕', 'Travel': '✈️', 'Fashion': '👗'
    };
    return icons[category] || '🏷️';
  };

  if (loading) {
    return (
      <div className={`personalized-topic-feed ${className}`}>
        <div className="animate-pulse space-y-6">
          <div className="h-8 bg-gray-200 dark:bg-gray-700 rounded"></div>
          <div className="space-y-4">
            {[...Array(5)].map((_, i) => (
              <div key={i} className="h-32 bg-gray-200 dark:bg-gray-700 rounded-lg"></div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  if (error || !feedData) {
    return (
      <div className={`personalized-topic-feed ${className}`}>
        <div className="text-center py-12">
          <div className="text-6xl mb-4">😕</div>
          <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
            Something went wrong
          </h3>
          <p className="text-gray-500 dark:text-gray-400 mb-4">
            {error || 'Unable to load personalized feed'}
          </p>
          <button 
            onClick={loadFeedData}
            className="px-6 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className={`personalized-topic-feed ${className}`}>
      {/* Header */}
      <div className="mb-6">
        <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
          ✨ Your Personalized Feed
        </h3>
        <p className="text-gray-600 dark:text-gray-400">
          Content from your followed topics, tailored to your interests
        </p>
      </div>

      {/* Controls */}
      <div className="mb-6 space-y-4">
        {/* View Mode Toggle */}
        <div className="flex space-x-1 bg-gray-100 dark:bg-gray-800 rounded-lg p-1">
          <button
            onClick={() => setViewMode('mixed')}
            className={`px-4 py-2 rounded-md font-medium transition-colors ${
              viewMode === 'mixed'
                ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
                : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
            }`}
          >
            🌊 Mixed Feed
          </button>
          <button
            onClick={() => setViewMode('topics')}
            className={`px-4 py-2 rounded-md font-medium transition-colors ${
              viewMode === 'topics'
                ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
                : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
            }`}
          >
            🎯 By Topics
          </button>
        </div>

        {/* Feed Controls */}
        <div className="flex flex-wrap gap-4 items-center">
          <div className="flex items-center space-x-2">
            <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
              Sort by:
            </label>
            <select
              value={config.sortBy}
              onChange={(e) => updateConfig({ sortBy: e.target.value })}
              className="px-3 py-1 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 text-gray-900 dark:text-white text-sm"
            >
              <option value="personalized">Personalized</option>
              <option value="trending">Trending</option>
              <option value="recent">Recent</option>
              <option value="engagement">Engagement</option>
            </select>
          </div>

          <div className="flex items-center space-x-2">
            <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
              Time window:
            </label>
            <select
              value={config.timeWindowHours}
              onChange={(e) => updateConfig({ timeWindowHours: parseInt(e.target.value) })}
              className="px-3 py-1 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 text-gray-900 dark:text-white text-sm"
            >
              <option value={6}>6 hours</option>
              <option value={12}>12 hours</option>
              <option value={24}>24 hours</option>
              <option value={48}>48 hours</option>
              <option value={168}>1 week</option>
            </select>
          </div>

          {viewMode === 'topics' && (
            <div className="flex items-center space-x-2">
              <label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                Posts per topic:
              </label>
              <select
                value={config.postsPerTopic}
                onChange={(e) => updateConfig({ postsPerTopic: parseInt(e.target.value) })}
                className="px-3 py-1 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 text-gray-900 dark:text-white text-sm"
              >
                <option value={3}>3</option>
                <option value={5}>5</option>
                <option value={10}>10</option>
                <option value={15}>15</option>
              </select>
            </div>
          )}
        </div>
      </div>

      {/* Feed Metrics */}
      <div className="mb-6 bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-center">
          <div>
            <div className="text-2xl font-bold text-blue-500">
              {feedData.metrics.totalTopicsFollowed}
            </div>
            <div className="text-sm text-gray-500 dark:text-gray-400">
              Topics Followed
            </div>
          </div>
          <div>
            <div className="text-2xl font-bold text-green-500">
              {feedData.metrics.totalPosts}
            </div>
            <div className="text-sm text-gray-500 dark:text-gray-400">
              Total Posts
            </div>
          </div>
          <div>
            <div className="text-2xl font-bold text-purple-500">
              {(feedData.metrics.personalizationScore * 100).toFixed(0)}%
            </div>
            <div className="text-sm text-gray-500 dark:text-gray-400">
              Personalization
            </div>
          </div>
          <div>
            <div className="text-2xl font-bold text-orange-500">
              {feedData.metrics.activeTopics.length}
            </div>
            <div className="text-sm text-gray-500 dark:text-gray-400">
              Active Topics
            </div>
          </div>
        </div>
      </div>

      {/* Feed Content */}
      {viewMode === 'mixed' ? (
        <div className="space-y-4">
          <h4 className="text-lg font-semibold text-gray-900 dark:text-white">
            🌊 Mixed Topic Feed
          </h4>
          {mixedFeed.length === 0 ? (
            <div className="text-center py-8">
              <div className="text-4xl mb-4">📭</div>
              <p className="text-gray-500 dark:text-gray-400">
                No posts available. Try following more topics or adjusting your time window.
              </p>
            </div>
          ) : (
            <div className="space-y-4">
              {mixedFeed.map((post) => (
                <PostCard key={post.id} post={post} />
              ))}
            </div>
          )}
        </div>
      ) : (
        <div className="space-y-8">
          <h4 className="text-lg font-semibold text-gray-900 dark:text-white">
            🎯 Feed by Topics
          </h4>
          {feedData.topicFeeds.length === 0 ? (
            <div className="text-center py-8">
              <div className="text-4xl mb-4">🎯</div>
              <p className="text-gray-500 dark:text-gray-400">
                No topic feeds available. Follow some topics to get started!
              </p>
            </div>
          ) : (
            feedData.topicFeeds.map((topicFeed) => (
              <div 
                key={topicFeed.topicName}
                className="bg-white dark:bg-gray-800 rounded-lg p-6 border border-gray-200 dark:border-gray-700"
              >
                {/* Topic Header */}
                <div className="flex items-center justify-between mb-4">
                  <div className="flex items-center space-x-3">
                    <span className="text-2xl">{getTopicIcon(topicFeed.category)}</span>
                    <div>
                      <h5 className="text-lg font-semibold text-gray-900 dark:text-white">
                        {topicFeed.topicName}
                      </h5>
                      <p className="text-sm text-gray-500 dark:text-gray-400">
                        {topicFeed.category} • {topicFeed.posts.length} posts
                      </p>
                    </div>
                  </div>
                  
                  <div className="text-right text-sm text-gray-500 dark:text-gray-400">
                    <div>Score: {topicFeed.metrics.trendingScore.toFixed(1)}</div>
                    <div>Growth: +{(topicFeed.metrics.growthRate * 100).toFixed(0)}%</div>
                  </div>
                </div>

                {/* Topic Posts */}
                <div className="space-y-4">
                  {topicFeed.posts.map((post) => (
                    <PostCard key={post.id} post={post} compact />
                  ))}
                </div>

                {/* Topic Footer */}
                <div className="mt-4 pt-4 border-t border-gray-200 dark:border-gray-700">
                  <div className="flex items-center justify-between text-sm text-gray-500 dark:text-gray-400">
                    <div className="flex items-center space-x-4">
                      <span>{topicFeed.metrics.uniqueContributors} contributors</span>
                      <span>{(topicFeed.metrics.avgEngagementRate * 100).toFixed(1)}% engagement</span>
                    </div>
                    <button className="text-blue-500 hover:text-blue-600 font-medium">
                      View all posts →
                    </button>
                  </div>
                </div>
              </div>
            ))
          )}
        </div>
      )}

      {/* Load More */}
      <div className="mt-8 text-center">
        <button 
          onClick={loadFeedData}
          className="px-6 py-2 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600 transition-colors"
        >
          Load More Content
        </button>
      </div>
    </div>
  );
}
