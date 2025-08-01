'use client';

import React, { useState, useEffect } from 'react';
import { exploreApi } from '@/lib/api';
import { ExplorePageDto, ExploreConfigDto } from '@/types';
import UserRecommendations from './UserRecommendations';
import ContentClusters from './ContentClusters';
import TrendingTopics from './TrendingTopics';
import PostCard from '@/components/PostCard';

interface ExplorePageProps {
  className?: string;
}

export default function ExplorePage({ className = '' }: ExplorePageProps) {
  const [exploreData, setExploreData] = useState<ExplorePageDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeSection, setActiveSection] = useState<'overview' | 'users' | 'content' | 'topics'>('overview');

  useEffect(() => {
    loadExploreData();
  }, []);

  const loadExploreData = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const config: Partial<ExploreConfigDto> = {
        trendingPostsLimit: 10,
        trendingHashtagsLimit: 15,
        recommendedUsersLimit: 8,
        timeWindowHours: 24,
        includePersonalizedContent: true,
        includeUserRecommendations: true,
        preferredCategories: ['Technology', 'Sports', 'Entertainment'],
        minSimilarityScore: 0.1
      };

      const data = await exploreApi.getExplorePage(config);
      setExploreData(data);
    } catch (err) {
      console.error('Failed to load explore data:', err);
      setError('Failed to load explore data');
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className={`explore-page ${className}`}>
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

  if (error || !exploreData) {
    return (
      <div className={`explore-page ${className}`}>
        <div className="text-center py-12">
          <div className="text-6xl mb-4">😕</div>
          <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
            Something went wrong
          </h3>
          <p className="text-gray-500 dark:text-gray-400 mb-4">
            {error || 'Unable to load explore content'}
          </p>
          <button 
            onClick={loadExploreData}
            className="px-6 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className={`explore-page ${className}`}>
      {/* Header */}
      <div className="mb-8">
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
          🌟 Explore & Discover
        </h2>
        <p className="text-gray-600 dark:text-gray-400">
          Find new content, users, and topics tailored to your interests
        </p>
      </div>

      {/* Section Navigation */}
      <div className="mb-8">
        <div className="flex space-x-1 bg-gray-100 dark:bg-gray-800 rounded-lg p-1">
          <button
            onClick={() => setActiveSection('overview')}
            className={`px-4 py-2 rounded-md font-medium transition-colors ${
              activeSection === 'overview'
                ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
                : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
            }`}
          >
            Overview
          </button>
          <button
            onClick={() => setActiveSection('users')}
            className={`px-4 py-2 rounded-md font-medium transition-colors ${
              activeSection === 'users'
                ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
                : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
            }`}
          >
            👥 People
          </button>
          <button
            onClick={() => setActiveSection('content')}
            className={`px-4 py-2 rounded-md font-medium transition-colors ${
              activeSection === 'content'
                ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
                : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
            }`}
          >
            📝 Content
          </button>
          <button
            onClick={() => setActiveSection('topics')}
            className={`px-4 py-2 rounded-md font-medium transition-colors ${
              activeSection === 'topics'
                ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
                : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
            }`}
          >
            🏷️ Topics
          </button>
        </div>
      </div>

      {/* Content */}
      {activeSection === 'overview' && (
        <div className="space-y-8">
          {/* Hero Section - Trending Posts */}
          <section className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
            <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
              🔥 Trending Posts
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {exploreData.trendingPosts.slice(0, 6).map((post) => (
                <div key={post.id} className="relative">
                  <PostCard post={post} compact />
                  <div className="absolute top-2 right-2 bg-red-500 text-white px-2 py-1 rounded-full text-xs font-bold">
                    🔥
                  </div>
                </div>
              ))}
            </div>
          </section>

          {/* Quick Sections Grid */}
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <section className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                👥 People You Might Like
              </h3>
              <UserRecommendations 
                users={exploreData.recommendedUsers.slice(0, 3)} 
                compact 
              />
              <button 
                onClick={() => setActiveSection('users')}
                className="mt-4 text-blue-500 hover:text-blue-600 text-sm font-medium"
              >
                View all recommendations →
              </button>
            </section>

            <section className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                🏷️ Trending Topics
              </h3>
              <div className="space-y-2">
                {exploreData.trendingCategories.slice(0, 5).map((category) => (
                  <div key={category.category} className="flex items-center justify-between">
                    <span className="text-gray-900 dark:text-white font-medium">
                      {category.category}
                    </span>
                    <div className="flex items-center space-x-2">
                      <span className="text-sm text-gray-500 dark:text-gray-400">
                        {category.postCount} posts
                      </span>
                      <span className="text-sm text-green-500">
                        +{(category.growthRate * 100).toFixed(0)}%
                      </span>
                    </div>
                  </div>
                ))}
              </div>
              <button 
                onClick={() => setActiveSection('topics')}
                className="mt-4 text-blue-500 hover:text-blue-600 text-sm font-medium"
              >
                Explore topics →
              </button>
            </section>
          </div>

          {/* Metrics */}
          <section className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
              📊 Discovery Insights
            </h3>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div className="text-center">
                <div className="text-2xl font-bold text-blue-500">
                  {exploreData.metrics.totalTrendingPosts}
                </div>
                <div className="text-sm text-gray-500 dark:text-gray-400">
                  Trending Posts
                </div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-green-500">
                  {exploreData.metrics.totalRecommendedUsers}
                </div>
                <div className="text-sm text-gray-500 dark:text-gray-400">
                  User Recommendations
                </div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-purple-500">
                  {(exploreData.metrics.averageEngagementRate * 100).toFixed(1)}%
                </div>
                <div className="text-sm text-gray-500 dark:text-gray-400">
                  Avg Engagement
                </div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-orange-500">
                  {(exploreData.metrics.personalizationScore * 100).toFixed(0)}%
                </div>
                <div className="text-sm text-gray-500 dark:text-gray-400">
                  Personalization
                </div>
              </div>
            </div>
          </section>
        </div>
      )}

      {activeSection === 'users' && (
        <UserRecommendations users={exploreData.recommendedUsers} />
      )}

      {activeSection === 'content' && (
        <ContentClusters />
      )}

      {activeSection === 'topics' && (
        <TrendingTopics />
      )}
    </div>
  );
}
