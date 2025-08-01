'use client';

import React, { useState, useEffect } from 'react';
import { personalizationApi } from '@/lib/api';
import { PersonalizationInsightsDto, InterestInsightDto, ContentTypeInsightDto, EngagementPatternDto, UserSimilarityDto } from '@/types';
import UserAvatar from '@/components/UserAvatar';
import Link from 'next/link';

interface PersonalizationInsightsProps {
  className?: string;
}

export default function PersonalizationInsights({ className = '' }: PersonalizationInsightsProps) {
  const [insights, setInsights] = useState<PersonalizationInsightsDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'interests' | 'content' | 'patterns' | 'similar'>('interests');

  useEffect(() => {
    loadInsights();
  }, []);

  const loadInsights = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await personalizationApi.getInsights();
      setInsights(data);
    } catch (err) {
      console.error('Failed to load personalization insights:', err);
      setError('Failed to load insights');
    } finally {
      setLoading(false);
    }
  };

  const getInterestTrendIcon = (trendDirection: number) => {
    if (trendDirection > 0.3) return '📈';
    if (trendDirection < -0.3) return '📉';
    return '➡️';
  };

  const getInterestTrendColor = (trendDirection: number) => {
    if (trendDirection > 0.3) return 'text-green-500';
    if (trendDirection < -0.3) return 'text-red-500';
    return 'text-gray-500';
  };

  const getContentTypeIcon = (contentType: string) => {
    const icons: Record<string, string> = {
      'text': '📝',
      'image': '🖼️',
      'video': '🎥',
      'link': '🔗',
      'poll': '📊',
      'quote': '💬'
    };
    return icons[contentType.toLowerCase()] || '📄';
  };

  if (loading) {
    return (
      <div className={`personalization-insights ${className}`}>
        <div className="animate-pulse space-y-6">
          <div className="h-8 bg-gray-200 dark:bg-gray-700 rounded"></div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            {[...Array(4)].map((_, i) => (
              <div key={i} className="h-24 bg-gray-200 dark:bg-gray-700 rounded-lg"></div>
            ))}
          </div>
          <div className="h-64 bg-gray-200 dark:bg-gray-700 rounded-lg"></div>
        </div>
      </div>
    );
  }

  if (error || !insights) {
    return (
      <div className={`personalization-insights ${className}`}>
        <div className="text-center py-12">
          <div className="text-6xl mb-4">😕</div>
          <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
            Something went wrong
          </h3>
          <p className="text-gray-500 dark:text-gray-400 mb-4">
            {error || 'Unable to load personalization insights'}
          </p>
          <button 
            onClick={loadInsights}
            className="px-6 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className={`personalization-insights ${className}`}>
      {/* Header */}
      <div className="mb-6">
        <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
          ✨ Your Personalization Profile
        </h3>
        <p className="text-gray-600 dark:text-gray-400">
          Insights into your interests, preferences, and behavior patterns
        </p>
      </div>

      {/* Stats Overview */}
      <div className="mb-8 grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
          <div className="text-2xl font-bold text-blue-500">
            {(insights.stats.overallConfidence * 100).toFixed(0)}%
          </div>
          <div className="text-sm text-gray-500 dark:text-gray-400">
            Profile Confidence
          </div>
        </div>
        
        <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
          <div className="text-2xl font-bold text-green-500">
            {insights.stats.totalInteractions.toLocaleString()}
          </div>
          <div className="text-sm text-gray-500 dark:text-gray-400">
            Total Interactions
          </div>
        </div>
        
        <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
          <div className="text-2xl font-bold text-purple-500">
            {insights.stats.uniqueInterests}
          </div>
          <div className="text-sm text-gray-500 dark:text-gray-400">
            Unique Interests
          </div>
        </div>
        
        <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
          <div className="text-2xl font-bold text-orange-500">
            {(insights.stats.diversityScore * 100).toFixed(0)}%
          </div>
          <div className="text-sm text-gray-500 dark:text-gray-400">
            Diversity Score
          </div>
        </div>
      </div>

      {/* Tab Navigation */}
      <div className="mb-6">
        <div className="flex space-x-1 bg-gray-100 dark:bg-gray-800 rounded-lg p-1">
          <button
            onClick={() => setActiveTab('interests')}
            className={`px-4 py-2 rounded-md font-medium transition-colors ${
              activeTab === 'interests'
                ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
                : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
            }`}
          >
            🎯 Interests
          </button>
          <button
            onClick={() => setActiveTab('content')}
            className={`px-4 py-2 rounded-md font-medium transition-colors ${
              activeTab === 'content'
                ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
                : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
            }`}
          >
            📱 Content Types
          </button>
          <button
            onClick={() => setActiveTab('patterns')}
            className={`px-4 py-2 rounded-md font-medium transition-colors ${
              activeTab === 'patterns'
                ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
                : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
            }`}
          >
            ⏰ Patterns
          </button>
          <button
            onClick={() => setActiveTab('similar')}
            className={`px-4 py-2 rounded-md font-medium transition-colors ${
              activeTab === 'similar'
                ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
                : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
            }`}
          >
            👥 Similar Users
          </button>
        </div>
      </div>

      {/* Tab Content */}
      <div className="bg-white dark:bg-gray-800 rounded-lg p-6 border border-gray-200 dark:border-gray-700">
        {activeTab === 'interests' && (
          <div>
            <h4 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
              Your Top Interests
            </h4>
            <div className="space-y-4">
              {insights.topInterests.map((interest, index) => (
                <div key={interest.interest} className="flex items-center justify-between p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
                  <div className="flex items-center space-x-4">
                    <div className="flex items-center justify-center w-8 h-8 bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 rounded-full text-sm font-bold">
                      {index + 1}
                    </div>
                    <div>
                      <h5 className="font-medium text-gray-900 dark:text-white">
                        {interest.interest}
                      </h5>
                      <div className="flex items-center space-x-2 text-sm text-gray-500 dark:text-gray-400">
                        <span>{interest.postCount} posts</span>
                        <span>•</span>
                        <span>{interest.engagementCount} engagements</span>
                        <span>•</span>
                        <span className="capitalize">{interest.category}</span>
                      </div>
                    </div>
                  </div>
                  
                  <div className="flex items-center space-x-3">
                    <div className="text-right">
                      <div className="text-lg font-semibold text-gray-900 dark:text-white">
                        {(interest.score * 100).toFixed(0)}%
                      </div>
                      <div className={`text-sm flex items-center space-x-1 ${getInterestTrendColor(interest.trendDirection)}`}>
                        <span>{getInterestTrendIcon(interest.trendDirection)}</span>
                        <span>{interest.isGrowing ? 'Growing' : 'Stable'}</span>
                      </div>
                    </div>
                    
                    <div className="w-16 bg-gray-200 dark:bg-gray-600 rounded-full h-2">
                      <div 
                        className="bg-blue-500 h-2 rounded-full transition-all duration-300"
                        style={{ width: `${interest.score * 100}%` }}
                      />
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {activeTab === 'content' && (
          <div>
            <h4 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
              Content Type Preferences
            </h4>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {insights.contentPreferences.map((contentType) => (
                <div key={contentType.contentType} className="p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
                  <div className="flex items-center space-x-3 mb-3">
                    <span className="text-2xl">{getContentTypeIcon(contentType.contentType)}</span>
                    <div>
                      <h5 className="font-medium text-gray-900 dark:text-white capitalize">
                        {contentType.contentType}
                      </h5>
                      <p className="text-sm text-gray-500 dark:text-gray-400">
                        {(contentType.preferenceScore * 100).toFixed(0)}% preference
                      </p>
                    </div>
                  </div>
                  
                  <div className="space-y-2 text-sm">
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Engagement Rate:</span>
                      <span className="font-medium">{(contentType.engagementRate * 100).toFixed(1)}%</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Views:</span>
                      <span className="font-medium">{contentType.viewCount.toLocaleString()}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Interactions:</span>
                      <span className="font-medium">{contentType.interactionCount.toLocaleString()}</span>
                    </div>
                  </div>
                  
                  <div className="mt-3 w-full bg-gray-200 dark:bg-gray-600 rounded-full h-2">
                    <div 
                      className="bg-green-500 h-2 rounded-full transition-all duration-300"
                      style={{ width: `${contentType.preferenceScore * 100}%` }}
                    />
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {activeTab === 'patterns' && (
          <div>
            <h4 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
              Engagement Patterns
            </h4>
            <div className="space-y-4">
              {insights.engagementPatterns.map((pattern) => (
                <div key={pattern.timeOfDay} className="p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
                  <div className="flex items-center justify-between mb-3">
                    <h5 className="font-medium text-gray-900 dark:text-white">
                      {pattern.timeOfDay}
                    </h5>
                    <div className="text-sm text-gray-500 dark:text-gray-400">
                      {pattern.activityCount} activities
                    </div>
                  </div>
                  
                  <div className="mb-3">
                    <div className="flex justify-between text-sm mb-1">
                      <span className="text-gray-600 dark:text-gray-400">Engagement Score</span>
                      <span className="font-medium">{(pattern.engagementScore * 100).toFixed(0)}%</span>
                    </div>
                    <div className="w-full bg-gray-200 dark:bg-gray-600 rounded-full h-2">
                      <div 
                        className="bg-purple-500 h-2 rounded-full transition-all duration-300"
                        style={{ width: `${pattern.engagementScore * 100}%` }}
                      />
                    </div>
                  </div>
                  
                  <div className="flex flex-wrap gap-1">
                    {pattern.preferredContentTypes.map((type) => (
                      <span 
                        key={type}
                        className="px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 rounded-full text-xs"
                      >
                        {type}
                      </span>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {activeTab === 'similar' && (
          <div>
            <h4 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
              Users Similar to You
            </h4>
            <div className="space-y-4">
              {insights.similarUsers.map((similarity) => (
                <div key={similarity.similarUser.id} className="flex items-center space-x-4 p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
                  <Link href={`/profile/${similarity.similarUser.username}`}>
                    <UserAvatar user={similarity.similarUser} size="md" className="cursor-pointer" />
                  </Link>
                  
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between mb-2">
                      <Link 
                        href={`/profile/${similarity.similarUser.username}`}
                        className="font-medium text-gray-900 dark:text-white hover:text-blue-500 truncate"
                      >
                        {similarity.similarUser.username}
                      </Link>
                      <div className="text-sm font-medium text-blue-500">
                        {(similarity.similarityScore * 100).toFixed(0)}% match
                      </div>
                    </div>
                    
                    <p className="text-sm text-gray-600 dark:text-gray-400 mb-2">
                      {similarity.similarityReason}
                    </p>
                    
                    <div className="flex flex-wrap gap-1 mb-2">
                      {similarity.commonInterests.slice(0, 3).map((interest) => (
                        <span 
                          key={interest}
                          className="px-2 py-1 bg-green-100 dark:bg-green-900/30 text-green-600 dark:text-green-400 rounded-full text-xs"
                        >
                          {interest}
                        </span>
                      ))}
                      {similarity.commonInterests.length > 3 && (
                        <span className="text-xs text-gray-500 dark:text-gray-400">
                          +{similarity.commonInterests.length - 3} more
                        </span>
                      )}
                    </div>
                    
                    {similarity.sharedFollows.length > 0 && (
                      <div className="text-xs text-gray-500 dark:text-gray-400">
                        Mutual follows: {similarity.sharedFollows.slice(0, 2).join(', ')}
                        {similarity.sharedFollows.length > 2 && ` +${similarity.sharedFollows.length - 2} more`}
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>

      {/* Recommendation Tips */}
      {insights.recommendationTips.length > 0 && (
        <div className="mt-6 bg-blue-50 dark:bg-blue-900/20 rounded-lg p-4">
          <h4 className="text-lg font-semibold text-blue-900 dark:text-blue-100 mb-3">
            💡 Personalization Tips
          </h4>
          <ul className="space-y-2">
            {insights.recommendationTips.map((tip, index) => (
              <li key={index} className="text-sm text-blue-800 dark:text-blue-200 flex items-start space-x-2">
                <span className="text-blue-500 mt-1">•</span>
                <span>{tip}</span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
