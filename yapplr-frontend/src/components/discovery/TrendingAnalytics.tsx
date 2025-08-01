'use client';

import React, { useState, useEffect } from 'react';
import { enhancedTrendingApi, topicApi } from '@/lib/api';
import { TagAnalyticsDto, CreateTopicFollowDto } from '@/types';
import { useRouter } from 'next/navigation';
import { Bell, BellOff } from 'lucide-react';

interface TrendingAnalyticsProps {
  hashtagName: string;
  className?: string;
  onClose?: () => void;
}

// Use the actual TagAnalyticsDto from the API
type HashtagAnalytics = TagAnalyticsDto;

export default function TrendingAnalytics({
  hashtagName,
  className = '',
  onClose
}: TrendingAnalyticsProps) {
  const router = useRouter();
  const [analytics, setAnalytics] = useState<HashtagAnalytics | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isAlertEnabled, setIsAlertEnabled] = useState(false);
  const [alertLoading, setAlertLoading] = useState(false);

  useEffect(() => {
    loadAnalytics();
    checkAlertStatus();
  }, [hashtagName]);

  const loadAnalytics = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await enhancedTrendingApi.getHashtagAnalytics(hashtagName);
      setAnalytics(data);
    } catch (err) {
      console.error('Failed to load hashtag analytics:', err);
      setError('Failed to load analytics data');
    } finally {
      setLoading(false);
    }
  };

  const checkAlertStatus = async () => {
    try {
      // Check if user is following this hashtag as a topic
      const status = await topicApi.isFollowingTopic(hashtagName);
      setIsAlertEnabled(status.isFollowing);
    } catch (err) {
      // If error (like 401), user is probably not logged in, so no alerts
      setIsAlertEnabled(false);
    }
  };

  const getWeeklyGrowth = () => {
    if (!analytics) return 0;
    const monthlyPosts = analytics.postsThisMonth;
    const weeklyPosts = analytics.postsThisWeek;
    const remainingWeeks = 3; // Approximate weeks remaining in month
    const projectedMonthly = weeklyPosts * 4;
    return monthlyPosts > 0 ? ((projectedMonthly - monthlyPosts) / monthlyPosts) * 100 : 0;
  };

  const getGrowthPercentage = () => {
    if (!analytics) return 0;
    return getWeeklyGrowth();
  };

  const getGrowthColor = (growth: number) => {
    if (growth > 50) return 'text-red-500';
    if (growth > 20) return 'text-orange-500';
    if (growth > 0) return 'text-green-500';
    return 'text-blue-500';
  };

  const getVelocityBgColor = (velocity: number) => {
    if (velocity > 0.7) return 'bg-red-100 dark:bg-red-900/20';
    if (velocity > 0.4) return 'bg-orange-100 dark:bg-orange-900/20';
    return 'bg-blue-100 dark:bg-blue-900/20';
  };

  const handleViewPosts = () => {
    router.push(`/hashtag/${encodeURIComponent(hashtagName)}`);
  };

  const handleToggleAlert = async () => {
    setAlertLoading(true);
    try {
      if (isAlertEnabled) {
        // Unfollow the hashtag topic to disable alerts
        await topicApi.unfollowTopic(hashtagName);
        setIsAlertEnabled(false);
        console.log(`Alert disabled for #${hashtagName}`);
      } else {
        // Follow the hashtag as a topic to enable alerts
        await topicApi.followTopic({
          topicName: hashtagName,
          topicDescription: `Get notified about trending posts with #${hashtagName}`,
          category: 'Hashtag Alert',
          relatedHashtags: [hashtagName],
          interestLevel: 0.8,
          includeInMainFeed: false, // Don't clutter main feed
          enableNotifications: true, // Enable notifications for this topic
        });
        setIsAlertEnabled(true);
        console.log(`Alert enabled for #${hashtagName}`);
      }
    } catch (error) {
      console.error('Failed to toggle alert:', error);
      // Show error message to user - could integrate with toast system
      alert('Failed to update alert settings. Please try again.');
    } finally {
      setAlertLoading(false);
    }
  };

  if (loading) {
    return (
      <div className={`trending-analytics bg-white dark:bg-gray-800 rounded-lg shadow-lg p-6 ${className}`}>
        <div className="animate-pulse">
          <div className="h-6 bg-gray-200 dark:bg-gray-700 rounded mb-4"></div>
          <div className="grid grid-cols-2 gap-4 mb-6">
            {[...Array(4)].map((_, i) => (
              <div key={i} className="h-20 bg-gray-200 dark:bg-gray-700 rounded"></div>
            ))}
          </div>
          <div className="h-40 bg-gray-200 dark:bg-gray-700 rounded"></div>
        </div>
      </div>
    );
  }

  if (error || !analytics) {
    return (
      <div className={`trending-analytics bg-white dark:bg-gray-800 rounded-lg shadow-lg p-6 ${className}`}>
        <div className="text-center py-8">
          <p className="text-red-500 mb-4">{error || 'No data available'}</p>
          <button 
            onClick={loadAnalytics}
            className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className={`trending-analytics bg-white dark:bg-gray-800 rounded-lg shadow-lg ${className}`}>
      {/* Header */}
      <div className="flex items-center justify-between p-6 border-b border-gray-200 dark:border-gray-700">
        <div>
          <h3 className="text-xl font-bold text-gray-900 dark:text-white">
            #{analytics.name} Analytics
          </h3>
          <p className="text-sm text-gray-500 dark:text-gray-400">
            Real-time trending analysis
          </p>
        </div>
        {onClose && (
          <button
            onClick={onClose}
            className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
          >
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        )}
      </div>

      <div className="p-6">
        {/* Key Metrics */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
          <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
            <div className="text-2xl font-bold text-gray-900 dark:text-white">
              {analytics.totalPosts.toLocaleString()}
            </div>
            <div className="text-sm text-gray-500 dark:text-gray-400">Total Posts</div>
            <div className={`text-xs mt-1 ${getGrowthPercentage() >= 0 ? 'text-green-500' : 'text-red-500'}`}>
              {getGrowthPercentage() >= 0 ? '+' : ''}{getGrowthPercentage().toFixed(1)}% growth
            </div>
          </div>

          <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
            <div className="text-2xl font-bold text-gray-900 dark:text-white">
              {analytics.postsThisWeek.toLocaleString()}
            </div>
            <div className="text-sm text-gray-500 dark:text-gray-400">This Week</div>
            <div className="text-xs text-gray-400 mt-1">
              {analytics.uniqueUsers} users
            </div>
          </div>

          <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
            <div className={`text-2xl font-bold ${getGrowthColor(getWeeklyGrowth())}`}>
              {analytics.postsThisMonth.toLocaleString()}
            </div>
            <div className="text-sm text-gray-500 dark:text-gray-400">This Month</div>
            <div className={`text-xs px-2 py-1 rounded-full mt-1 inline-block ${getWeeklyGrowth() > 0 ? 'bg-green-100 text-green-600' : 'bg-gray-100 text-gray-600'}`}>
              {getWeeklyGrowth() > 20 ? 'Hot' : getWeeklyGrowth() > 0 ? 'Growing' : 'Stable'}
            </div>
          </div>

          <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
            <div className="text-2xl font-bold text-gray-900 dark:text-white">
              {analytics.uniqueUsers.toLocaleString()}
            </div>
            <div className="text-sm text-gray-500 dark:text-gray-400">Unique Users</div>
            <div className="text-xs text-gray-400 mt-1">
              All time
            </div>
          </div>
        </div>

        {/* Usage Timeline */}
        <div className="mb-6">
          <h4 className="text-lg font-semibold text-gray-900 dark:text-white mb-3">
            Usage Timeline
          </h4>
          <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
            <div className="space-y-3">
              <div className="flex justify-between items-center">
                <span className="text-sm text-gray-600 dark:text-gray-400">First Used:</span>
                <span className="text-sm font-medium text-gray-900 dark:text-white">
                  {new Date(analytics.firstUsed).toLocaleDateString()}
                </span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm text-gray-600 dark:text-gray-400">Last Used:</span>
                <span className="text-sm font-medium text-gray-900 dark:text-white">
                  {new Date(analytics.lastUsed).toLocaleDateString()}
                </span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm text-gray-600 dark:text-gray-400">Days Active:</span>
                <span className="text-sm font-medium text-gray-900 dark:text-white">
                  {Math.ceil((new Date(analytics.lastUsed).getTime() - new Date(analytics.firstUsed).getTime()) / (1000 * 60 * 60 * 24))} days
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Summary Stats */}
        <div className="mb-6">
          <h4 className="text-lg font-semibold text-gray-900 dark:text-white mb-3">
            Usage Summary
          </h4>
          <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <div className="text-sm text-gray-600 dark:text-gray-400">Weekly Average</div>
                <div className="text-lg font-semibold text-gray-900 dark:text-white">
                  {Math.round(analytics.postsThisWeek / 7)} posts/day
                </div>
              </div>
              <div>
                <div className="text-sm text-gray-600 dark:text-gray-400">Monthly Average</div>
                <div className="text-lg font-semibold text-gray-900 dark:text-white">
                  {Math.round(analytics.postsThisMonth / 30)} posts/day
                </div>
              </div>
            </div>
          </div>
        </div>



        {/* Action Buttons */}
        <div className="flex space-x-3">
          <button
            onClick={handleViewPosts}
            className="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors flex items-center space-x-2"
          >
            <span>View Posts</span>
          </button>
          <button
            onClick={handleToggleAlert}
            disabled={alertLoading}
            className={`px-4 py-2 rounded-lg transition-colors flex items-center space-x-2 ${
              isAlertEnabled
                ? 'bg-green-500 text-white hover:bg-green-600'
                : 'bg-gray-200 dark:bg-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-500'
            } ${alertLoading ? 'opacity-50 cursor-not-allowed' : ''}`}
          >
            {alertLoading ? (
              <div className="w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin" />
            ) : isAlertEnabled ? (
              <BellOff className="w-4 h-4" />
            ) : (
              <Bell className="w-4 h-4" />
            )}
            <span>{isAlertEnabled ? 'Disable Alert' : 'Set Alert'}</span>
          </button>
        </div>
      </div>
    </div>
  );
}
