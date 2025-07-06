'use client';

import { useEffect, useState } from 'react';
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
  BarChart,
  Bar,
  LineChart,
  Line,
  PieChart,
  Pie,
  Cell,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
} from 'recharts';
import {
  TrendingUp,
  Users,
  FileText,
  Shield,
  Activity,
  Server,
  Hash,
  Heart,
  Calendar,
  Filter,
} from 'lucide-react';

export default function AnalyticsPage() {
  const [loading, setLoading] = useState(true);
  const [timeRange, setTimeRange] = useState(30);
  const [userGrowth, setUserGrowth] = useState<UserGrowthStats | null>(null);
  const [contentStats, setContentStats] = useState<ContentStats | null>(null);
  const [moderationTrends, setModerationTrends] = useState<ModerationTrends | null>(null);
  const [systemHealth, setSystemHealth] = useState<SystemHealth | null>(null);
  const [topModerators, setTopModerators] = useState<TopModerators | null>(null);
  const [contentTrends, setContentTrends] = useState<ContentTrends | null>(null);
  const [userEngagement, setUserEngagement] = useState<UserEngagementStats | null>(null);

  useEffect(() => {
    fetchAnalytics();
  }, [timeRange]);

  const fetchAnalytics = async () => {
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
      setTopModerators(topModeratorsData);
      setContentTrends(contentTrendsData);
      setUserEngagement(userEngagementData);
    } catch (error) {
      console.error('Failed to fetch analytics:', error);
    } finally {
      setLoading(false);
    }
  };

  const COLORS = ['#3B82F6', '#EF4444', '#10B981', '#F59E0B', '#8B5CF6', '#EC4899'];

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
        {/* User Growth Chart */}
        {userGrowth && (
          <div className="bg-white rounded-lg shadow p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">User Growth Trend</h3>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={userGrowth.dailyStats}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="label" />
                <YAxis />
                <Tooltip />
                <Line type="monotone" dataKey="count" stroke="#3B82F6" strokeWidth={2} />
              </LineChart>
            </ResponsiveContainer>
          </div>
        )}

        {/* Content Creation Chart */}
        {contentStats && (
          <div className="bg-white rounded-lg shadow p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">Content Creation</h3>
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={contentStats.dailyPosts}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="label" />
                <YAxis />
                <Tooltip />
                <Bar dataKey="count" fill="#10B981" />
              </BarChart>
            </ResponsiveContainer>
          </div>
        )}

        {/* Moderation Actions Breakdown */}
        {moderationTrends && (
          <div className="bg-white rounded-lg shadow p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">Moderation Actions</h3>
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={moderationTrends.actionBreakdown}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={({ actionType, percentage }) => `${actionType}: ${percentage}%`}
                  outerRadius={80}
                  fill="#8884d8"
                  dataKey="count"
                >
                  {moderationTrends.actionBreakdown.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          </div>
        )}

        {/* User Engagement Breakdown */}
        {userEngagement && (
          <div className="bg-white rounded-lg shadow p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">Engagement Breakdown</h3>
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={userEngagement.engagementBreakdown}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="type" />
                <YAxis />
                <Tooltip />
                <Bar dataKey="count" fill="#8B5CF6" />
              </BarChart>
            </ResponsiveContainer>
          </div>
        )}
      </div>
    </div>
  );
}
