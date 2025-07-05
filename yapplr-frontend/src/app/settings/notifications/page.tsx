'use client';

import React, { useState } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '@/lib/api';
import Sidebar from '@/components/Sidebar';
import { ArrowLeft, Bell, Clock, Shield, BarChart3 } from 'lucide-react';
import Link from 'next/link';

interface NotificationPreferences {
  id: number;
  userId: number;
  preferredMethod: number;
  enableMessageNotifications: boolean;
  enableMentionNotifications: boolean;
  enableReplyNotifications: boolean;
  enableCommentNotifications: boolean;
  enableFollowNotifications: boolean;
  enableLikeNotifications: boolean;
  enableRepostNotifications: boolean;
  enableFollowRequestNotifications: boolean;
  enableQuietHours: boolean;
  quietHoursStart: string;
  quietHoursEnd: string;
  quietHoursTimezone: string;
  enableFrequencyLimits: boolean;
  maxNotificationsPerHour: number;
  maxNotificationsPerDay: number;
  requireDeliveryConfirmation: boolean;
  enableReadReceipts: boolean;
  enableMessageHistory: boolean;
  messageHistoryDays: number;
  enableOfflineReplay: boolean;
}

const deliveryMethods = [
  { value: 0, label: 'Auto (Best Available)', description: 'Use the best available method' },
  { value: 1, label: 'Push Notifications Only', description: 'Firebase push notifications only' },
  { value: 2, label: 'Real-time Only', description: 'SignalR real-time notifications only' },
  { value: 3, label: 'Polling Only', description: 'Check for updates periodically' },
  { value: 4, label: 'Disabled', description: 'No notifications' }
];

export default function NotificationPreferencesPage() {
  const { user, isLoading } = useAuth();
  const router = useRouter();
  const queryClient = useQueryClient();

  const { data: preferences, isLoading: preferencesLoading } = useQuery({
    queryKey: ['notificationPreferences'],
    queryFn: () => api.get('/notification-preferences').then(res => res.data),
    enabled: !!user,
  });

  const updatePreferencesMutation = useMutation({
    mutationFn: (updates: Partial<NotificationPreferences>) =>
      api.put('/notification-preferences', updates),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notificationPreferences'] });
    },
  });

  if (isLoading || preferencesLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-lg text-gray-900">Loading...</div>
      </div>
    );
  }

  if (!user) {
    router.push('/login');
    return null;
  }

  const handleToggle = (field: keyof NotificationPreferences, value: boolean) => {
    updatePreferencesMutation.mutate({ [field]: value });
  };

  const handleSelectChange = (field: keyof NotificationPreferences, value: number) => {
    updatePreferencesMutation.mutate({ [field]: value });
  };

  const handleTimeChange = (field: keyof NotificationPreferences, value: string) => {
    updatePreferencesMutation.mutate({ [field]: value });
  };

  const handleNumberChange = (field: keyof NotificationPreferences, value: number) => {
    updatePreferencesMutation.mutate({ [field]: value });
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
          <div className="max-w-4xl mx-auto lg:border-x border-gray-200 min-h-screen bg-white">
            {/* Header */}
            <div className="sticky top-0 bg-white/80 backdrop-blur-md border-b border-gray-200 p-4">
              <div className="flex items-center space-x-3">
                <Link href="/settings" className="p-2 hover:bg-gray-100 rounded-full transition-colors">
                  <ArrowLeft className="w-5 h-5 text-gray-600" />
                </Link>
                <h1 className="text-xl font-bold text-gray-900">Notification Preferences</h1>
              </div>
            </div>

            {/* Content */}
            <div className="p-6 space-y-8">
              {/* Delivery Method */}
              <div>
                <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
                  <Bell className="w-5 h-5 mr-2" />
                  Delivery Method
                </h2>
                <div className="space-y-3">
                  {deliveryMethods.map((method) => (
                    <label key={method.value} className="flex items-start space-x-3 cursor-pointer">
                      <input
                        type="radio"
                        name="preferredMethod"
                        value={method.value}
                        checked={preferences?.preferredMethod === method.value}
                        onChange={() => handleSelectChange('preferredMethod', method.value)}
                        className="mt-1 text-blue-600 focus:ring-blue-500"
                      />
                      <div>
                        <div className="font-medium text-gray-900">{method.label}</div>
                        <div className="text-sm text-gray-600">{method.description}</div>
                      </div>
                    </label>
                  ))}
                </div>
              </div>

              {/* Notification Types */}
              <div>
                <h2 className="text-lg font-semibold text-gray-900 mb-4">Notification Types</h2>
                <div className="space-y-4">
                  {[
                    { key: 'enableMessageNotifications', label: 'Messages', description: 'New direct messages' },
                    { key: 'enableMentionNotifications', label: 'Mentions', description: 'When someone mentions you' },
                    { key: 'enableReplyNotifications', label: 'Replies', description: 'Replies to your comments' },
                    { key: 'enableCommentNotifications', label: 'Comments', description: 'Comments on your posts' },
                    { key: 'enableFollowNotifications', label: 'Follows', description: 'New followers' },
                    { key: 'enableLikeNotifications', label: 'Likes', description: 'Likes on your posts' },
                    { key: 'enableRepostNotifications', label: 'Reposts', description: 'Reposts of your content' },
                    { key: 'enableFollowRequestNotifications', label: 'Follow Requests', description: 'New follow requests' },
                  ].map((item) => (
                    <div key={item.key} className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                      <div>
                        <div className="font-medium text-gray-900">{item.label}</div>
                        <div className="text-sm text-gray-600">{item.description}</div>
                      </div>
                      <label className="relative inline-flex items-center cursor-pointer">
                        <input
                          type="checkbox"
                          checked={preferences?.[item.key as keyof NotificationPreferences] as boolean}
                          onChange={(e) => handleToggle(item.key as keyof NotificationPreferences, e.target.checked)}
                          className="sr-only peer"
                        />
                        <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                      </label>
                    </div>
                  ))}
                </div>
              </div>

              {/* Quiet Hours */}
              <div>
                <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
                  <Clock className="w-5 h-5 mr-2" />
                  Quiet Hours
                </h2>
                <div className="space-y-4">
                  <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                    <div>
                      <div className="font-medium text-gray-900">Enable Quiet Hours</div>
                      <div className="text-sm text-gray-600">Pause notifications during specified hours</div>
                    </div>
                    <label className="relative inline-flex items-center cursor-pointer">
                      <input
                        type="checkbox"
                        checked={preferences?.enableQuietHours}
                        onChange={(e) => handleToggle('enableQuietHours', e.target.checked)}
                        className="sr-only peer"
                      />
                      <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                    </label>
                  </div>

                  {preferences?.enableQuietHours && (
                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">Start Time</label>
                        <input
                          type="time"
                          value={preferences.quietHoursStart}
                          onChange={(e) => handleTimeChange('quietHoursStart', e.target.value)}
                          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">End Time</label>
                        <input
                          type="time"
                          value={preferences.quietHoursEnd}
                          onChange={(e) => handleTimeChange('quietHoursEnd', e.target.value)}
                          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        />
                      </div>
                    </div>
                  )}
                </div>
              </div>

              {/* Frequency Limits */}
              <div>
                <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
                  <BarChart3 className="w-5 h-5 mr-2" />
                  Frequency Limits
                </h2>
                <div className="space-y-4">
                  <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                    <div>
                      <div className="font-medium text-gray-900">Enable Frequency Limits</div>
                      <div className="text-sm text-gray-600">Limit the number of notifications per hour/day</div>
                    </div>
                    <label className="relative inline-flex items-center cursor-pointer">
                      <input
                        type="checkbox"
                        checked={preferences?.enableFrequencyLimits}
                        onChange={(e) => handleToggle('enableFrequencyLimits', e.target.checked)}
                        className="sr-only peer"
                      />
                      <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                    </label>
                  </div>

                  {preferences?.enableFrequencyLimits && (
                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">Max per Hour</label>
                        <input
                          type="number"
                          min="1"
                          max="100"
                          value={preferences.maxNotificationsPerHour}
                          onChange={(e) => handleNumberChange('maxNotificationsPerHour', parseInt(e.target.value))}
                          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        />
                      </div>
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">Max per Day</label>
                        <input
                          type="number"
                          min="1"
                          max="1000"
                          value={preferences.maxNotificationsPerDay}
                          onChange={(e) => handleNumberChange('maxNotificationsPerDay', parseInt(e.target.value))}
                          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        />
                      </div>
                    </div>
                  )}
                </div>
              </div>

              {/* Advanced Options */}
              <div>
                <h2 className="text-lg font-semibold text-gray-900 mb-4 flex items-center">
                  <Shield className="w-5 h-5 mr-2" />
                  Advanced Options
                </h2>
                <div className="space-y-4">
                  {[
                    { key: 'requireDeliveryConfirmation', label: 'Delivery Confirmation', description: 'Track when notifications are delivered' },
                    { key: 'enableReadReceipts', label: 'Read Receipts', description: 'Track when notifications are read' },
                    { key: 'enableMessageHistory', label: 'Message History', description: 'Keep history of notifications' },
                    { key: 'enableOfflineReplay', label: 'Offline Replay', description: 'Replay missed notifications when back online' },
                  ].map((item) => (
                    <div key={item.key} className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                      <div>
                        <div className="font-medium text-gray-900">{item.label}</div>
                        <div className="text-sm text-gray-600">{item.description}</div>
                      </div>
                      <label className="relative inline-flex items-center cursor-pointer">
                        <input
                          type="checkbox"
                          checked={preferences?.[item.key as keyof NotificationPreferences] as boolean}
                          onChange={(e) => handleToggle(item.key as keyof NotificationPreferences, e.target.checked)}
                          className="sr-only peer"
                        />
                        <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                      </label>
                    </div>
                  ))}

                  {preferences?.enableMessageHistory && (
                    <div>
                      <label className="block text-sm font-medium text-gray-700 mb-2">History Retention (Days)</label>
                      <input
                        type="number"
                        min="1"
                        max="365"
                        value={preferences.messageHistoryDays}
                        onChange={(e) => handleNumberChange('messageHistoryDays', parseInt(e.target.value))}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                      />
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
