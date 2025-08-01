'use client';

import React, { useState, useEffect } from 'react';
import { topicApi } from '@/lib/api';
import { TopicDto, TopicRecommendationDto, TopicSearchResultDto } from '@/types';

interface TopicDiscoveryProps {
  className?: string;
  onTopicSelect?: (topic: TopicDto) => void;
}

export default function TopicDiscovery({ 
  className = '', 
  onTopicSelect 
}: TopicDiscoveryProps) {
  const [featuredTopics, setFeaturedTopics] = useState<TopicDto[]>([]);
  const [recommendations, setRecommendations] = useState<TopicRecommendationDto[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState<TopicSearchResultDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [searchLoading, setSearchLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);

  const categories = ['Technology', 'Sports', 'Entertainment', 'Gaming', 'News', 'Music', 'Art', 'Food', 'Travel', 'Fashion'];

  useEffect(() => {
    loadInitialData();
  }, [selectedCategory]);

  useEffect(() => {
    if (searchQuery.length > 2) {
      const debounceTimer = setTimeout(() => {
        searchTopics();
      }, 300);
      return () => clearTimeout(debounceTimer);
    } else {
      setSearchResults(null);
    }
  }, [searchQuery]);

  const loadInitialData = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const [featured, recs] = await Promise.all([
        topicApi.getTopics(selectedCategory || undefined, true),
        topicApi.getTopicRecommendations(8)
      ]);
      
      setFeaturedTopics(featured);
      setRecommendations(recs);
    } catch (err) {
      console.error('Failed to load topic data:', err);
      setError('Failed to load topics');
    } finally {
      setLoading(false);
    }
  };

  const searchTopics = async () => {
    try {
      setSearchLoading(true);
      const results = await topicApi.searchTopics(searchQuery, 20);
      setSearchResults(results);
    } catch (err) {
      console.error('Failed to search topics:', err);
    } finally {
      setSearchLoading(false);
    }
  };

  const handleTopicClick = (topic: TopicDto) => {
    onTopicSelect?.(topic);
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
      'Fashion': '👗'
    };
    return icons[category] || '🏷️';
  };

  if (loading) {
    return (
      <div className={`topic-discovery ${className}`}>
        <div className="animate-pulse space-y-6">
          <div className="h-8 bg-gray-200 dark:bg-gray-700 rounded"></div>
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
            {[...Array(8)].map((_, i) => (
              <div key={i} className="h-32 bg-gray-200 dark:bg-gray-700 rounded-lg"></div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={`topic-discovery ${className}`}>
        <div className="text-center py-12">
          <div className="text-6xl mb-4">😕</div>
          <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
            Something went wrong
          </h3>
          <p className="text-gray-500 dark:text-gray-400 mb-4">{error}</p>
          <button 
            onClick={loadInitialData}
            className="px-6 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className={`topic-discovery ${className}`}>
      {/* Header */}
      <div className="mb-6">
        <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
          🎯 Discover Topics
        </h3>
        <p className="text-gray-600 dark:text-gray-400">
          Find and follow topics that interest you
        </p>
      </div>

      {/* Search */}
      <div className="mb-6">
        <div className="relative">
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Search topics..."
            className="w-full px-4 py-3 pl-10 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent bg-white dark:bg-gray-800 text-gray-900 dark:text-white"
          />
          <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
            <svg className="w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
          </div>
          {searchLoading && (
            <div className="absolute inset-y-0 right-0 pr-3 flex items-center">
              <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-500"></div>
            </div>
          )}
        </div>
      </div>

      {/* Category Filter */}
      <div className="mb-6">
        <div className="flex flex-wrap gap-2">
          <button
            onClick={() => setSelectedCategory(null)}
            className={`px-3 py-1 rounded-full text-sm font-medium transition-colors ${
              selectedCategory === null
                ? 'bg-blue-500 text-white'
                : 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-600'
            }`}
          >
            All
          </button>
          {categories.map((category) => (
            <button
              key={category}
              onClick={() => setSelectedCategory(category)}
              className={`px-3 py-1 rounded-full text-sm font-medium transition-colors flex items-center space-x-1 ${
                selectedCategory === category
                  ? 'bg-blue-500 text-white'
                  : 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-600'
              }`}
            >
              <span>{getTopicIcon(category)}</span>
              <span>{category}</span>
            </button>
          ))}
        </div>
      </div>

      {/* Search Results */}
      {searchResults && (
        <div className="mb-8">
          <h4 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
            Search Results ({searchResults.totalResults})
          </h4>
          
          {searchResults.exactMatches.length > 0 && (
            <div className="mb-6">
              <h5 className="text-md font-medium text-gray-700 dark:text-gray-300 mb-3">
                Exact Matches
              </h5>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {searchResults.exactMatches.map((topic) => (
                  <TopicCard 
                    key={topic.id} 
                    topic={topic} 
                    onClick={() => handleTopicClick(topic)} 
                  />
                ))}
              </div>
            </div>
          )}

          {searchResults.partialMatches.length > 0 && (
            <div className="mb-6">
              <h5 className="text-md font-medium text-gray-700 dark:text-gray-300 mb-3">
                Related Topics
              </h5>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {searchResults.partialMatches.map((topic) => (
                  <TopicCard 
                    key={topic.id} 
                    topic={topic} 
                    onClick={() => handleTopicClick(topic)} 
                  />
                ))}
              </div>
            </div>
          )}

          {searchResults.suggestedHashtags.length > 0 && (
            <div className="mb-6">
              <h5 className="text-md font-medium text-gray-700 dark:text-gray-300 mb-3">
                Related Hashtags
              </h5>
              <div className="flex flex-wrap gap-2">
                {searchResults.suggestedHashtags.map((hashtag) => (
                  <span 
                    key={hashtag}
                    className="px-3 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 rounded-full text-sm cursor-pointer hover:bg-blue-200 dark:hover:bg-blue-900/50"
                  >
                    #{hashtag}
                  </span>
                ))}
              </div>
            </div>
          )}
        </div>
      )}

      {/* Featured Topics */}
      {!searchResults && (
        <>
          <div className="mb-8">
            <h4 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
              🌟 Featured Topics
            </h4>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
              {featuredTopics.map((topic) => (
                <TopicCard 
                  key={topic.id} 
                  topic={topic} 
                  featured 
                  onClick={() => handleTopicClick(topic)} 
                />
              ))}
            </div>
          </div>

          {/* Recommendations */}
          {recommendations.length > 0 && (
            <div>
              <h4 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                ✨ Recommended for You
              </h4>
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {recommendations.map((rec) => (
                  <RecommendationCard 
                    key={rec.topic.id} 
                    recommendation={rec} 
                    onClick={() => handleTopicClick(rec.topic)} 
                  />
                ))}
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}

// Topic Card Component
interface TopicCardProps {
  topic: TopicDto;
  featured?: boolean;
  onClick: () => void;
}

function TopicCard({ topic, featured = false, onClick }: TopicCardProps) {
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
      'Fashion': '👗'
    };
    return icons[category] || '🏷️';
  };

  return (
    <div 
      onClick={onClick}
      className={`bg-white dark:bg-gray-800 rounded-lg p-4 border cursor-pointer transition-all hover:shadow-md ${
        featured 
          ? 'border-blue-200 dark:border-blue-800 bg-gradient-to-br from-blue-50 to-white dark:from-blue-900/20 dark:to-gray-800' 
          : 'border-gray-200 dark:border-gray-700 hover:border-blue-300 dark:hover:border-blue-600'
      }`}
    >
      <div className="flex items-center space-x-3 mb-3">
        <span className="text-2xl">{getTopicIcon(topic.category)}</span>
        <div className="flex-1 min-w-0">
          <h5 className="font-semibold text-gray-900 dark:text-white truncate">
            {topic.name}
          </h5>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            {topic.category}
          </p>
        </div>
        {featured && (
          <span className="text-yellow-500">⭐</span>
        )}
      </div>
      
      <p className="text-sm text-gray-600 dark:text-gray-400 mb-3 line-clamp-2">
        {topic.description}
      </p>
      
      <div className="flex items-center justify-between text-xs text-gray-500 dark:text-gray-400">
        <span>{topic.followerCount.toLocaleString()} followers</span>
        {topic.isFollowedByCurrentUser && (
          <span className="px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 rounded-full">
            Following
          </span>
        )}
      </div>
    </div>
  );
}

// Recommendation Card Component
interface RecommendationCardProps {
  recommendation: TopicRecommendationDto;
  onClick: () => void;
}

function RecommendationCard({ recommendation, onClick }: RecommendationCardProps) {
  return (
    <div 
      onClick={onClick}
      className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700 cursor-pointer transition-all hover:shadow-md hover:border-blue-300 dark:hover:border-blue-600"
    >
      <TopicCard topic={recommendation.topic} onClick={() => {}} />
      
      <div className="mt-3 pt-3 border-t border-gray-200 dark:border-gray-700">
        <p className="text-sm text-gray-600 dark:text-gray-400 mb-2">
          {recommendation.recommendationReason}
        </p>
        
        {recommendation.matchingInterests.length > 0 && (
          <div className="flex flex-wrap gap-1">
            {recommendation.matchingInterests.slice(0, 3).map((interest) => (
              <span 
                key={interest}
                className="px-2 py-1 bg-green-100 dark:bg-green-900/30 text-green-600 dark:text-green-400 rounded-full text-xs"
              >
                {interest}
              </span>
            ))}
          </div>
        )}
        
        <div className="mt-2 text-xs text-gray-500 dark:text-gray-400">
          Match: {(recommendation.recommendationScore * 100).toFixed(0)}%
        </div>
      </div>
    </div>
  );
}
