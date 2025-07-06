'use client';

import { useEffect, useState } from 'react';
import { adminApi } from '@/lib/api';
import { ModerationStats, ContentQueue } from '@/types';
import {
  Users,
  FileText,
  MessageSquare,
  AlertTriangle,
  Shield,
  Eye,
  Ban,
  Clock,
  TrendingUp,
  Activity,
} from 'lucide-react';
import Link from 'next/link';

export default function AdminDashboard() {
  const [stats, setStats] = useState<ModerationStats | null>(null);
  const [queue, setQueue] = useState<ContentQueue | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [statsData, queueData] = await Promise.all([
          adminApi.getStats(),
          adminApi.getContentQueue(),
        ]);
        setStats(statsData);
        setQueue(queueData);
      } catch (error) {
        console.error('Failed to fetch admin data:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  const statCards = [
    {
      title: 'Total Users',
      value: stats?.totalUsers || 0,
      icon: Users,
      color: 'bg-blue-500',
      href: '/admin/users',
    },
    {
      title: 'Active Users',
      value: stats?.activeUsers || 0,
      icon: Activity,
      color: 'bg-green-500',
      href: '/admin/users?status=0',
    },
    {
      title: 'Suspended Users',
      value: stats?.suspendedUsers || 0,
      icon: Clock,
      color: 'bg-yellow-500',
      href: '/admin/users?status=1',
    },
    {
      title: 'Banned Users',
      value: (stats?.bannedUsers || 0) + (stats?.shadowBannedUsers || 0),
      icon: Ban,
      color: 'bg-red-500',
      href: '/admin/users?status=2',
    },
    {
      title: 'Total Posts',
      value: stats?.totalPosts || 0,
      icon: FileText,
      color: 'bg-purple-500',
      href: '/admin/posts',
    },
    {
      title: 'Hidden Posts',
      value: stats?.hiddenPosts || 0,
      icon: Eye,
      color: 'bg-orange-500',
      href: '/admin/posts?isHidden=true',
    },
    {
      title: 'Total Comments',
      value: stats?.totalComments || 0,
      icon: MessageSquare,
      color: 'bg-indigo-500',
      href: '/admin/comments',
    },
    {
      title: 'Pending Appeals',
      value: stats?.pendingAppeals || 0,
      icon: Shield,
      color: 'bg-pink-500',
      href: '/admin/appeals?status=0',
    },
  ];

  const activityCards = [
    {
      title: 'Today\'s Actions',
      value: stats?.todayActions || 0,
      icon: TrendingUp,
      color: 'text-green-600',
    },
    {
      title: 'This Week',
      value: stats?.weekActions || 0,
      icon: TrendingUp,
      color: 'text-blue-600',
    },
    {
      title: 'This Month',
      value: stats?.monthActions || 0,
      icon: TrendingUp,
      color: 'text-purple-600',
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Admin Dashboard</h1>
        <p className="text-gray-600">Overview of platform moderation and activity</p>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {statCards.map((card) => {
          const Icon = card.icon;
          return (
            <Link
              key={card.title}
              href={card.href}
              className="bg-white rounded-lg shadow p-6 hover:shadow-md transition-shadow"
            >
              <div className="flex items-center">
                <div className={`${card.color} p-3 rounded-lg`}>
                  <Icon className="h-6 w-6 text-white" />
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600">{card.title}</p>
                  <p className="text-2xl font-bold text-gray-900">{card.value.toLocaleString()}</p>
                </div>
              </div>
            </Link>
          );
        })}
      </div>

      {/* Activity Overview */}
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Moderation Activity</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {activityCards.map((card) => {
            const Icon = card.icon;
            return (
              <div key={card.title} className="text-center">
                <div className="flex items-center justify-center mb-2">
                  <Icon className={`h-8 w-8 ${card.color}`} />
                </div>
                <p className="text-3xl font-bold text-gray-900">{card.value}</p>
                <p className="text-sm text-gray-600">{card.title}</p>
              </div>
            );
          })}
        </div>
      </div>

      {/* Content Queue Overview */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Flagged Content */}
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-gray-900">Flagged Content</h2>
            <Link
              href="/admin/queue"
              className="text-blue-600 hover:text-blue-800 text-sm font-medium"
            >
              View All
            </Link>
          </div>
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <span className="text-gray-600">Flagged Posts</span>
              <span className="font-semibold">{queue?.flaggedPosts.length || 0}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-gray-600">Flagged Comments</span>
              <span className="font-semibold">{queue?.flaggedComments.length || 0}</span>
            </div>
            <div className="flex items-center justify-between border-t pt-3">
              <span className="font-medium text-gray-900">Total Flagged</span>
              <span className="font-bold text-red-600">{queue?.totalFlaggedContent || 0}</span>
            </div>
          </div>
        </div>

        {/* Recent Appeals */}
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-gray-900">Recent Appeals</h2>
            <Link
              href="/admin/appeals"
              className="text-blue-600 hover:text-blue-800 text-sm font-medium"
            >
              View All
            </Link>
          </div>
          <div className="space-y-3">
            {queue?.pendingAppeals.slice(0, 3).map((appeal) => (
              <div key={appeal.id} className="flex items-center justify-between">
                <div>
                  <p className="font-medium text-gray-900">@{appeal.username}</p>
                  <p className="text-sm text-gray-600">{appeal.type}</p>
                </div>
                <span className="text-xs bg-yellow-100 text-yellow-800 px-2 py-1 rounded-full">
                  {appeal.status}
                </span>
              </div>
            )) || (
              <p className="text-gray-500 text-center py-4">No pending appeals</p>
            )}
          </div>
        </div>
      </div>

      {/* Quick Actions */}
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Quick Actions</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <Link
            href="/admin/queue"
            className="flex items-center p-4 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
          >
            <AlertTriangle className="h-8 w-8 text-red-500 mr-3" />
            <div>
              <p className="font-medium text-gray-900">Review Queue</p>
              <p className="text-sm text-gray-600">Review flagged content</p>
            </div>
          </Link>
          <Link
            href="/admin/users"
            className="flex items-center p-4 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
          >
            <Users className="h-8 w-8 text-blue-500 mr-3" />
            <div>
              <p className="font-medium text-gray-900">Manage Users</p>
              <p className="text-sm text-gray-600">User moderation tools</p>
            </div>
          </Link>
          <Link
            href="/admin/audit-logs"
            className="flex items-center p-4 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
          >
            <Shield className="h-8 w-8 text-green-500 mr-3" />
            <div>
              <p className="font-medium text-gray-900">Audit Logs</p>
              <p className="text-sm text-gray-600">View admin actions</p>
            </div>
          </Link>
        </div>
      </div>
    </div>
  );
}
