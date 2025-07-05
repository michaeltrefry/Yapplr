'use client';

import { useAuth } from '@/contexts/AuthContext';
import { useNotifications } from '@/contexts/NotificationContext';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api, { notificationApi, followRequestsApi } from '@/lib/api';
import { useState, useEffect } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { ArrowLeft, Bell, Heart, MessageCircle, Repeat, UserPlus, AtSign, Check, X } from 'lucide-react';
import Sidebar from '@/components/Sidebar';
import NotificationPermissionButton from '@/components/NotificationPermissionButton';
import type { Notification, NotificationType } from '@/types';

const getNotificationIcon = (type: NotificationType) => {
  switch (type) {
    case 1: // Mention
      return <AtSign className="w-5 h-5 text-blue-500" />;
    case 2: // Like
      return <Heart className="w-5 h-5 text-red-500" />;
    case 3: // Repost
      return <Repeat className="w-5 h-5 text-green-500" />;
    case 4: // Follow
      return <UserPlus className="w-5 h-5 text-purple-500" />;
    case 5: // Comment
      return <MessageCircle className="w-5 h-5 text-blue-500" />;
    case 6: // FollowRequest
      return <UserPlus className="w-5 h-5 text-orange-500" />;
    default:
      return <Bell className="w-5 h-5 text-gray-500" />;
  }
};

const formatTimeAgo = (dateString: string) => {
  const date = new Date(dateString);
  const now = new Date();
  const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);

  if (diffInSeconds < 60) return 'just now';
  if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m`;
  if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h`;
  if (diffInSeconds < 604800) return `${Math.floor(diffInSeconds / 86400)}d`;
  return date.toLocaleDateString();
};

export default function NotificationsPage() {
  const { user, isLoading: authLoading } = useAuth();
  const { refreshNotificationCount } = useNotifications();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [page, setPage] = useState(1);
  const pageSize = 25;
  const [processedRequests, setProcessedRequests] = useState<Record<number, 'approved' | 'denied'>>({});

  const { data: notificationData, isLoading, error } = useQuery({
    queryKey: ['notifications', page],
    queryFn: () => notificationApi.getNotifications(page, pageSize),
    enabled: !!user,
  });

  const markAsReadMutation = useMutation({
    mutationFn: notificationApi.markAsRead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      refreshNotificationCount();
    },
  });

  const markAllAsReadMutation = useMutation({
    mutationFn: notificationApi.markAllAsRead,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      refreshNotificationCount();
    },
  });

  const approveFollowRequestMutation = useMutation({
    mutationFn: followRequestsApi.approveByUserId,
    onSuccess: () => {
      // Refresh notifications and counts
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['followRequests'] });
      // Update sidebar following list immediately when approving follow requests
      queryClient.invalidateQueries({ queryKey: ['followingWithOnlineStatus'] });
      refreshNotificationCount();
    },
  });

  const denyFollowRequestMutation = useMutation({
    mutationFn: followRequestsApi.denyByUserId,
    onSuccess: () => {
      // Refresh notifications and counts
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
      queryClient.invalidateQueries({ queryKey: ['followRequests'] });
      refreshNotificationCount();
    },
  });

  // Test notification mutation
  const testNotificationMutation = useMutation({
    mutationFn: () => api.post('/notifications/test'),
    onSuccess: () => {
      console.log('Test notification sent successfully');
    },
    onError: (error) => {
      console.error('Failed to send test notification:', error);
    },
  });

  useEffect(() => {
    if (!authLoading && !user) {
      router.push('/login');
    }
  }, [user, authLoading, router]);

  if (authLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-lg text-gray-900">Loading...</div>
      </div>
    );
  }

  if (!user) {
    return null;
  }

  const handleNotificationClick = async (notification: Notification) => {
    if (!notification.isRead) {
      await markAsReadMutation.mutateAsync(notification.id);
    }

    // Navigate to the relevant content based on notification type
    if (notification.type === 1 && notification.mention) { // Mention
      if (notification.mention.commentId && notification.post) {
        // Mention in comment - go to post and scroll to comment
        router.push(`/yap/${notification.post.id}?scrollToComment=${notification.mention.commentId}`);
      } else if (notification.mention.postId) {
        // Mention in post - go to post
        router.push(`/yap/${notification.mention.postId}`);
      }
    } else if (notification.post) {
      // Other post-related notifications (likes, reposts, comments)
      if (notification.comment) {
        // Comment notification - go to post and scroll to comment
        router.push(`/yap/${notification.post.id}?scrollToComment=${notification.comment.id}`);
      } else {
        // Post notification - go to post
        router.push(`/yap/${notification.post.id}`);
      }
    } else if (notification.actorUser) {
      // Follow notifications - go to user profile
      router.push(`/profile/${notification.actorUser.username}`);
    }
  };

  const handleMarkAllAsRead = () => {
    markAllAsReadMutation.mutate();
  };

  const handleApproveFollowRequest = (notification: Notification, event: React.MouseEvent) => {
    event.stopPropagation();
    if (notification.actorUser) {
      // Immediately update local state to show "Approved"
      setProcessedRequests(prev => ({
        ...prev,
        [notification.id]: 'approved'
      }));
      approveFollowRequestMutation.mutate(notification.actorUser.id);
    }
  };

  const handleDenyFollowRequest = (notification: Notification, event: React.MouseEvent) => {
    event.stopPropagation();
    if (notification.actorUser) {
      // Immediately update local state to show "Declined"
      setProcessedRequests(prev => ({
        ...prev,
        [notification.id]: 'denied'
      }));
      denyFollowRequestMutation.mutate(notification.actorUser.id);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-6xl mx-auto flex">
        {/* Sidebar */}
        <div className="w-16 lg:w-64 fixed h-full z-10">
          <Sidebar />
        </div>

        {/* Main Content */}
        <div className="flex-1 ml-16 lg:ml-64">
          <div className="max-w-2xl mx-auto lg:border-x border-gray-200 min-h-screen bg-white">
            {/* Header */}
            <div className="sticky top-0 bg-white/80 backdrop-blur-md border-b border-gray-200 p-4">
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-4">
                  <button
                    onClick={() => router.back()}
                    className="flex items-center space-x-2 text-gray-600 hover:text-gray-900 transition-colors"
                  >
                    <ArrowLeft className="w-5 h-5" />
                  </button>
                  <h1 className="text-xl font-bold text-gray-900">Notifications</h1>
                </div>
                <div className="flex items-center space-x-3">
                  <NotificationPermissionButton />
                  <button
                    onClick={() => testNotificationMutation.mutate()}
                    disabled={testNotificationMutation.isPending}
                    className="text-sm text-green-600 hover:text-green-800 transition-colors disabled:opacity-50"
                  >
                    Test
                  </button>
                  {notificationData && notificationData.unreadCount > 0 && (
                    <button
                      onClick={handleMarkAllAsRead}
                      disabled={markAllAsReadMutation.isPending}
                      className="text-sm text-blue-600 hover:text-blue-800 transition-colors disabled:opacity-50"
                    >
                      Mark all as read
                    </button>
                  )}
                </div>
              </div>
            </div>

            {/* Notifications List */}
            <div className="divide-y divide-gray-200">
              {isLoading ? (
                <div className="p-8 text-center text-gray-500">
                  Loading notifications...
                </div>
              ) : error ? (
                <div className="p-8 text-center text-red-500">
                  Failed to load notifications
                </div>
              ) : !notificationData?.notifications.length ? (
                <div className="p-8 text-center text-gray-500">
                  <Bell className="w-12 h-12 mx-auto mb-4 text-gray-300" />
                  <p>No notifications yet</p>
                  <p className="text-sm mt-2">When someone mentions you, likes your posts, or follows you, you'll see it here.</p>
                </div>
              ) : (
                notificationData.notifications.map((notification) => (
                  <div
                    key={notification.id}
                    onClick={() => handleNotificationClick(notification)}
                    className={`p-4 hover:bg-gray-50 cursor-pointer transition-colors ${
                      !notification.isRead ? 'bg-blue-50 border-l-4 border-blue-500' : ''
                    }`}
                  >
                    <div className="flex items-start space-x-3">
                      <div className="flex-shrink-0 mt-1">
                        {getNotificationIcon(notification.type)}
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center justify-between">
                          <p className="text-sm text-gray-900">
                            {notification.message}
                          </p>
                          <span className="text-xs text-gray-500 flex-shrink-0 ml-2">
                            {formatTimeAgo(notification.createdAt)}
                          </span>
                        </div>
                        {notification.post && (
                          <div className="mt-2 p-2 bg-gray-100 rounded text-sm text-gray-700">
                            {notification.post.content}
                          </div>
                        )}
                        {notification.comment && (
                          <div className="mt-2 p-2 bg-gray-100 rounded text-sm text-gray-700">
                            {notification.comment.content}
                          </div>
                        )}
                        {notification.type === 6 && notification.actorUser && ( // FollowRequest
                          <div className="mt-3">
                            {notification.status || processedRequests[notification.id] ? (
                              <div className={`flex items-center space-x-1 px-3 py-1 text-sm rounded-full ${
                                (notification.status === 'approved' || processedRequests[notification.id] === 'approved')
                                  ? 'bg-green-100 text-green-800'
                                  : 'bg-gray-100 text-gray-800'
                              }`}>
                                {(notification.status === 'approved' || processedRequests[notification.id] === 'approved') ? (
                                  <>
                                    <Check className="w-4 h-4" />
                                    <span>Accepted</span>
                                  </>
                                ) : (
                                  <>
                                    <X className="w-4 h-4" />
                                    <span>Declined</span>
                                  </>
                                )}
                              </div>
                            ) : (
                              <div className="flex space-x-2">
                                <button
                                  onClick={(e) => handleApproveFollowRequest(notification, e)}
                                  disabled={approveFollowRequestMutation.isPending || denyFollowRequestMutation.isPending}
                                  className="flex items-center space-x-1 px-3 py-1 bg-green-500 text-white text-sm rounded-full hover:bg-green-600 transition-colors disabled:opacity-50"
                                >
                                  <Check className="w-4 h-4" />
                                  <span>Accept</span>
                                </button>
                                <button
                                  onClick={(e) => handleDenyFollowRequest(notification, e)}
                                  disabled={approveFollowRequestMutation.isPending || denyFollowRequestMutation.isPending}
                                  className="flex items-center space-x-1 px-3 py-1 bg-red-500 text-white text-sm rounded-full hover:bg-red-600 transition-colors disabled:opacity-50"
                                >
                                  <X className="w-4 h-4" />
                                  <span>Decline</span>
                                </button>
                              </div>
                            )}
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                ))
              )}
            </div>

            {/* Load More */}
            {notificationData && notificationData.hasMore && (
              <div className="p-4 text-center">
                <button
                  onClick={() => setPage(prev => prev + 1)}
                  className="text-blue-600 hover:text-blue-800 transition-colors"
                >
                  Load more notifications
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
