'use client';

import React, { useState, useEffect } from 'react';
import { enhancedTrendingApi } from '@/lib/api';
import { TrendingHashtagDto, CategoryTrendingDto } from '@/types';

interface TrendingDashboardProps {
  className?: string;
  limit?: number;
  timeWindow?: number;
  onHashtagClick?: (hashtag: string) => void;
}

export default function TrendingDashboard({
  className = '',
  limit = 20,
  timeWindow = 24,
  onHashtagClick
}: TrendingDashboardProps) {
  const [trendingHashtags, setTrendingHashtags] = useState<TrendingHashtagDto[]>([]);
  const [categories, setCategories] = useState<CategoryTrendingDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedCategory, setSelectedCategory] = useState<string | null>(null);

  useEffect(() => {
    loadTrendingData();
  }, [limit, timeWindow, selectedCategory]);

  const loadTrendingData = async () => {
    try {
      setLoading(true);
      setError(null);

      if (selectedCategory) {
        const hashtags = await enhancedTrendingApi.getTrendingByCategory(selectedCategory, limit);
        setTrendingHashtags(hashtags);
      } else {
        const hashtags = await enhancedTrendingApi.getVelocityTrendingHashtags(limit, timeWindow);
        setTrendingHashtags(hashtags);
      }
    } catch (err) {
      console.error('Failed to load trending data:', err);
      setError('Failed to load trending data');
    } finally {
      setLoading(false);
    }
  };

  const getVelocityIcon = (velocity: number) => {
    if (velocity > 0.7) return '🔥';
    if (velocity > 0.4) return '📈';
    return '📊';
  };

  const getVelocityLabel = (velocity: number) => {
    if (velocity > 0.7) return 'Hot';
    if (velocity > 0.4) return 'Rising';
    return 'Steady';
  };

  const getVelocityColor = (velocity: number) => {
    if (velocity > 0.7) return 'text-red-500';
    if (velocity > 0.4) return 'text-orange-500';
    return 'text-blue-500';
  };

  if (loading) {
    return (
      <div className={`trending-dashboard ${className}`}>
        <div className="animate-pulse">
          <div className="h-8 bg-gray-200 dark:bg-gray-700 rounded mb-4"></div>
          <div className="space-y-3">
            {[...Array(5)].map((_, i) => (
              <div key={i} className="h-16 bg-gray-200 dark:bg-gray-700 rounded"></div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={`trending-dashboard ${className}`}>
        <div className="text-center py-8">
          <p className="text-red-500 mb-4">{error}</p>
          <button 
            onClick={loadTrendingData}
            className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className={`trending-dashboard bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 ${className}`}>
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
          🔥 Trending Now
        </h2>
        
        {/* Velocity Legend */}
        <div className="flex items-center space-x-4 text-sm">
          <div className="flex items-center space-x-1">
            <span>🔥</span>
            <span className="text-red-500">Hot</span>
          </div>
          <div className="flex items-center space-x-1">
            <span>📈</span>
            <span className="text-orange-500">Rising</span>
          </div>
          <div className="flex items-center space-x-1">
            <span>📊</span>
            <span className="text-blue-500">Steady</span>
          </div>
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
          {['Technology', 'Sports', 'Entertainment', 'News', 'Gaming'].map((category) => (
            <button
              key={category}
              onClick={() => setSelectedCategory(category)}
              className={`px-3 py-1 rounded-full text-sm font-medium transition-colors ${
                selectedCategory === category
                  ? 'bg-blue-500 text-white'
                  : 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-600'
              }`}
            >
              {category}
            </button>
          ))}
        </div>
      </div>

      {/* Trending List */}
      <div className="space-y-4">
        {trendingHashtags.map((hashtag, index) => (
          <div
            key={hashtag.name}
            className="trending-item bg-gray-50 dark:bg-gray-700 rounded-lg p-4 hover:bg-gray-100 dark:hover:bg-gray-600 transition-colors cursor-pointer"
            onClick={() => onHashtagClick?.(hashtag.name)}
          >
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-3">
                <div className="flex items-center justify-center w-8 h-8 bg-gray-200 dark:bg-gray-600 rounded-full text-sm font-bold">
                  {index + 1}
                </div>
                <div>
                  <div className="flex items-center space-x-2">
                    <span className="text-lg font-semibold text-blue-600 dark:text-blue-400">
                      #{hashtag.name}
                    </span>
                    <span className="text-lg">
                      {getVelocityIcon(hashtag.velocity)}
                    </span>
                  </div>
                  {hashtag.category && (
                    <span className="text-xs text-gray-500 dark:text-gray-400">
                      {hashtag.category}
                    </span>
                  )}
                </div>
              </div>

              <div className="text-right">
                <div className="flex items-center space-x-2 mb-1">
                  <span className={`text-sm font-medium ${getVelocityColor(hashtag.velocity)}`}>
                    {getVelocityLabel(hashtag.velocity)}
                  </span>
                  <span className="text-sm text-gray-600 dark:text-gray-400">
                    +{(hashtag.velocity * 100).toFixed(0)}%
                  </span>
                </div>
                <div className="text-xs text-gray-500 dark:text-gray-400">
                  {hashtag.postCount.toLocaleString()} posts
                </div>
              </div>
            </div>

            {/* Velocity Bar */}
            <div className="mt-3">
              <div className="w-full bg-gray-200 dark:bg-gray-600 rounded-full h-2">
                <div 
                  className={`h-2 rounded-full transition-all duration-300 ${
                    hashtag.velocity > 0.7 ? 'bg-red-500' :
                    hashtag.velocity > 0.4 ? 'bg-orange-500' : 'bg-blue-500'
                  }`}
                  style={{ width: `${Math.min(hashtag.velocity * 100, 100)}%` }}
                />
              </div>
            </div>

            {/* Stats */}
            <div className="flex items-center justify-between mt-3 text-xs text-gray-500 dark:text-gray-400">
              <div className="flex items-center space-x-4">
                <span>{hashtag.uniqueUsers} users</span>
                <span>{(hashtag.engagementRate * 100).toFixed(1)}% engagement</span>
                <span>Score: {hashtag.trendingScore.toFixed(1)}</span>
              </div>
              <span className="text-xs text-gray-400">
                {hashtag.category}
              </span>
            </div>
          </div>
        ))}
      </div>

      {trendingHashtags.length === 0 && (
        <div className="text-center py-8 text-gray-500 dark:text-gray-400">
          No trending hashtags found for the selected criteria.
        </div>
      )}
    </div>
  );
}
