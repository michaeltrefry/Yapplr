'use client';

import { useEffect, useState, useCallback } from 'react';
import { adminApi } from '@/lib/api';
import {
  UserGrowthStats,
  ContentStats,
  ModerationTrends,
  SystemHealth,
  TopModerators,
  ContentTrends,
  UserEngagementStats,
} from '@/types';
import {
  Users,
  FileText,
  Shield,
  Server,
  Hash,
  Heart,
} from 'lucide-react';

export default function AnalyticsPage() {
  const [loading, setLoading] = useState(true);
  const [timeRange, setTimeRange] = useState(30);
  const [userGrowth, setUserGrowth] = useState<UserGrowthStats | null>(null);
  const [contentStats, setContentStats] = useState<ContentStats | null>(null);
  const [moderationTrends, setModerationTrends] = useState<ModerationTrends | null>(null);
  const [systemHealth, setSystemHealth] = useState<SystemHealth | null>(null);

  const [userEngagement, setUserEngagement] = useState<UserEngagementStats | null>(null);

  useEffect(() => {
    fetchAnalytics();
  }, [fetchAnalytics]);

  const fetchAnalytics = useCallback(async () => {
    try {
      setLoading(true);
      const [
        userGrowthData,
        contentStatsData,
        moderationTrendsData,
        systemHealthData,
        topModeratorsData,
        contentTrendsData,
        userEngagementData,
      ] = await Promise.all([
        adminApi.getUserGrowthStats(timeRange),
        adminApi.getContentStats(timeRange),
        adminApi.getModerationTrends(timeRange),
        adminApi.getSystemHealth(),
        adminApi.getTopModerators(timeRange),
        adminApi.getContentTrends(timeRange),
        adminApi.getUserEngagementStats(timeRange),
      ]);

      setUserGrowth(userGrowthData);
      setContentStats(contentStatsData);
      setModerationTrends(moderationTrendsData);
      setSystemHealth(systemHealthData);
      setUserEngagement(userEngagementData);
    } catch (error) {
      console.error('Failed to fetch analytics:', error);
    } finally {
      setLoading(false);
    }
  }, [timeRange]);

  const formatNumber = (num: number) => {
    if (num >= 1000000) return (num / 1000000).toFixed(1) + 'M';
    if (num >= 1000) return (num / 1000).toFixed(1) + 'K';
    return num.toString();
  };

  const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Analytics Dashboard</h1>
          <p className="text-gray-600">Comprehensive platform analytics and insights</p>
        </div>
        <div className="flex items-center space-x-3">
          <Filter className="h-5 w-5 text-gray-400" />
          <select
            value={timeRange}
            onChange={(e) => setTimeRange(parseInt(e.target.value))}
            className="border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value={7}>Last 7 days</option>
            <option value={30}>Last 30 days</option>
            <option value={90}>Last 90 days</option>
            <option value={365}>Last year</option>
          </select>
        </div>
      </div>

      {/* System Health Overview */}
      {systemHealth && (
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center mb-4">
            <Server className="h-6 w-6 text-green-600 mr-2" />
            <h2 className="text-lg font-semibold text-gray-900">System Health</h2>
          </div>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="text-center">
              <div className="text-2xl font-bold text-green-600">{systemHealth.uptimePercentage}%</div>
              <div className="text-sm text-gray-500">Uptime</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-blue-600">{formatNumber(systemHealth.activeUsers24h)}</div>
              <div className="text-sm text-gray-500">Active Users (24h)</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-orange-600">{systemHealth.averageResponseTime}ms</div>
              <div className="text-sm text-gray-500">Avg Response Time</div>
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-purple-600">{formatBytes(systemHealth.memoryUsage)}</div>
              <div className="text-sm text-gray-500">Memory Usage</div>
            </div>
          </div>
        </div>
      )}

      {/* Key Metrics Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {userGrowth && (
          <div className="bg-white rounded-lg shadow p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">New Users</p>
                <p className="text-2xl font-bold text-gray-900">{formatNumber(userGrowth.totalNewUsers)}</p>
                <p className={`text-sm ${userGrowth.growthRate >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                  {userGrowth.growthRate >= 0 ? '+' : ''}{userGrowth.growthRate}% vs previous period
                </p>
              </div>
              <Users className="h-8 w-8 text-blue-500" />
            </div>
          </div>
        )}

        {contentStats && (
          <div className="bg-white rounded-lg shadow p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Total Posts</p>
                <p className="text-2xl font-bold text-gray-900">{formatNumber(contentStats.totalPosts)}</p>
                <p className="text-sm text-gray-500">{contentStats.averagePostsPerDay}/day avg</p>
              </div>
              <FileText className="h-8 w-8 text-green-500" />
            </div>
          </div>
        )}

        {moderationTrends && (
          <div className="bg-white rounded-lg shadow p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Mod Actions</p>
                <p className="text-2xl font-bold text-gray-900">{formatNumber(moderationTrends.totalActions)}</p>
                <p className="text-sm text-gray-500">Peak: {moderationTrends.peakDayActions}</p>
              </div>
              <Shield className="h-8 w-8 text-orange-500" />
            </div>
          </div>
        )}

        {userEngagement && (
          <div className="bg-white rounded-lg shadow p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Engagement Rate</p>
                <p className="text-2xl font-bold text-gray-900">{userEngagement.retentionRate}%</p>
                <p className="text-sm text-gray-500">{formatNumber(userEngagement.totalSessions)} sessions</p>
              </div>
              <Heart className="h-8 w-8 text-red-500" />
            </div>
          </div>
        )}
      </div>

      {/* Charts Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* User Growth Data */}
        {userGrowth && (
          <div className="bg-white rounded-lg shadow p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">User Growth Trend</h3>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
              <div className="text-center">
                <div className="text-2xl font-bold text-blue-600">{userGrowth.totalNewUsers}</div>
                <div className="text-sm text-gray-600">New Users</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-green-600">{userGrowth.totalActiveUsers}</div>
                <div className="text-sm text-gray-600">Active Users</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-purple-600">{userGrowth.growthRate}%</div>
                <div className="text-sm text-gray-600">Growth Rate</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-orange-600">{userGrowth.peakDayNewUsers}</div>
                <div className="text-sm text-gray-600">Peak Day</div>
              </div>
            </div>
            <div className="text-sm text-gray-500">
              Recent daily registrations: {userGrowth.dailyStats.slice(-7).map(d => d.count).join(', ')}
            </div>
          </div>
        )}

        {/* Content Creation Stats */}
        {contentStats && (
          <div className="bg-white rounded-lg shadow p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">Content Creation</h3>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
              <div className="text-center">
                <div className="text-2xl font-bold text-green-600">{contentStats.totalPosts}</div>
                <div className="text-sm text-gray-600">Total Posts</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-blue-600">{contentStats.totalComments}</div>
                <div className="text-sm text-gray-600">Total Comments</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-purple-600">{contentStats.averagePostsPerDay}</div>
                <div className="text-sm text-gray-600">Posts/Day</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-orange-600">{contentStats.averageCommentsPerDay}</div>
                <div className="text-sm text-gray-600">Comments/Day</div>
              </div>
            </div>
            <div className="text-sm text-gray-500">
              Recent daily posts: {contentStats.dailyPosts.slice(-7).map(d => d.count).join(', ')}
            </div>
          </div>
        )}

        {/* Moderation Actions Breakdown */}
        {moderationTrends && (
          <div className="bg-white rounded-lg shadow p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">Moderation Actions</h3>
            <div className="grid grid-cols-2 md:grid-cols-3 gap-4 mb-4">
              <div className="text-center">
                <div className="text-2xl font-bold text-red-600">{moderationTrends.totalActions}</div>
                <div className="text-sm text-gray-600">Total Actions</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-yellow-600">{moderationTrends.peakDayActions}</div>
                <div className="text-sm text-gray-600">Peak Day Actions</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-green-600">{moderationTrends.actionsGrowthRate}%</div>
                <div className="text-sm text-gray-600">Growth Rate</div>
              </div>
            </div>
            <div className="space-y-2">
              {moderationTrends.actionBreakdown.map((action, index) => (
                <div key={index} className="flex justify-between items-center">
                  <span className="text-sm text-gray-600">{action.actionType}</span>
                  <span className="text-sm font-medium">{action.count} ({action.percentage}%)</span>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* User Engagement Breakdown */}
        {userEngagement && (
          <div className="bg-white rounded-lg shadow p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">Engagement Breakdown</h3>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
              <div className="text-center">
                <div className="text-2xl font-bold text-blue-600">{userEngagement.totalSessions}</div>
                <div className="text-sm text-gray-600">Total Sessions</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-green-600">{userEngagement.averageSessionDuration}</div>
                <div className="text-sm text-gray-600">Avg Duration (min)</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-purple-600">{userEngagement.retentionRate}%</div>
                <div className="text-sm text-gray-600">Retention Rate</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-orange-600">{userEngagement.engagementBreakdown.length}</div>
                <div className="text-sm text-gray-600">Engagement Types</div>
              </div>
            </div>
            <div className="space-y-2">
              {userEngagement.engagementBreakdown.map((engagement, index) => (
                <div key={index} className="flex justify-between items-center">
                  <span className="text-sm text-gray-600">{engagement.type}</span>
                  <span className="text-sm font-medium">{engagement.count}</span>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
