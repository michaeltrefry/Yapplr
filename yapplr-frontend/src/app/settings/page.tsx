'use client';

import { useAuth } from '@/contexts/AuthContext';
import { useTheme } from '@/contexts/ThemeContext';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import Sidebar from '@/components/Sidebar';
import Link from 'next/link';
import { Shield, ArrowRight, Moon, Sun, UserCheck, Bell, Bug } from 'lucide-react';
import { preferencesApi } from '@/lib/api';
import { NotificationStatus } from '@/components/NotificationStatus';

export default function SettingsPage() {
  const { user, isLoading } = useAuth();
  const { isDarkMode, toggleDarkMode } = useTheme();
  const router = useRouter();
  const queryClient = useQueryClient();

  // Fetch user preferences
  const { data: preferences, isLoading: preferencesLoading } = useQuery({
    queryKey: ['preferences'],
    queryFn: preferencesApi.get,
    enabled: !!user,
  });

  // Update preferences mutation
  const updatePreferencesMutation = useMutation({
    mutationFn: preferencesApi.update,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['preferences'] });
    },
  });

  const handleFollowApprovalToggle = () => {
    if (preferences) {
      updatePreferencesMutation.mutate({
        requireFollowApproval: !preferences.requireFollowApproval,
      });
    }
  };

  useEffect(() => {
    if (!isLoading && !user) {
      router.push('/login');
    }
  }, [user, isLoading, router]);

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-lg text-gray-900">Loading...</div>
      </div>
    );
  }

  if (!user) {
    return null;
  }

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
              <h1 className="text-xl font-bold text-gray-900">Settings</h1>
            </div>

            {/* Settings Content */}
            <div className="p-6">
              <div className="space-y-6">
                {/* Appearance Section */}
                <div>
                  <h2 className="text-lg font-semibold text-gray-900 mb-4">Appearance</h2>

                  {/* Dark Mode Toggle */}
                  <div className="flex items-center justify-between p-4 bg-white border border-gray-200 rounded-lg">
                    <div className="flex items-center space-x-3">
                      <div className="p-2 bg-blue-100 rounded-lg">
                        {isDarkMode ? (
                          <Moon className="w-5 h-5 text-blue-600" />
                        ) : (
                          <Sun className="w-5 h-5 text-blue-600" />
                        )}
                      </div>
                      <div>
                        <h3 className="font-semibold text-gray-900">Dark Mode</h3>
                        <p className="text-sm text-gray-600">
                          Switch between light and dark themes
                        </p>
                      </div>
                    </div>
                    <button
                      onClick={toggleDarkMode}
                      className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 ${
                        isDarkMode ? 'bg-blue-600' : 'bg-gray-200'
                      }`}
                    >
                      <span
                        className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                          isDarkMode ? 'translate-x-6' : 'translate-x-1'
                        }`}
                      />
                    </button>
                  </div>
                </div>

                {/* Notifications Section */}
                <div>
                  <h2 className="text-lg font-semibold text-gray-900 mb-4">Notifications</h2>

                  {/* Notification Status */}
                  <div className="p-4 bg-white border border-gray-200 rounded-lg mb-4">
                    <div className="flex items-center space-x-3 mb-3">
                      <div className="p-2 bg-blue-100 rounded-lg">
                        <Bell className="w-5 h-5 text-blue-600" />
                      </div>
                      <div>
                        <h3 className="font-semibold text-gray-900">Notification Status</h3>
                        <p className="text-sm text-gray-600">
                          Current notification delivery method
                        </p>
                      </div>
                    </div>
                    <NotificationStatus showDetails={true} />
                  </div>

                  {/* Notification Preferences Link */}
                  <Link
                    href="/settings/notifications"
                    className="flex items-center justify-between p-4 bg-white border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
                  >
                    <div className="flex items-center space-x-3">
                      <div className="p-2 bg-purple-100 rounded-lg">
                        <Bell className="w-5 h-5 text-purple-600" />
                      </div>
                      <div>
                        <h3 className="font-semibold text-gray-900">Notification Preferences</h3>
                        <p className="text-sm text-gray-600">
                          Customize how you receive notifications
                        </p>
                      </div>
                    </div>
                    <ArrowRight className="w-5 h-5 text-gray-400" />
                  </Link>

                  {/* Debug Link */}
                  <Link
                    href="/debug/notifications"
                    className="flex items-center justify-between p-4 bg-white border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
                  >
                    <div className="flex items-center space-x-3">
                      <div className="p-2 bg-orange-100 rounded-lg">
                        <Bug className="w-5 h-5 text-orange-600" />
                      </div>
                      <div>
                        <h3 className="font-semibold text-gray-900">Debug Center</h3>
                        <p className="text-sm text-gray-600">
                          Test notification fallback scenarios
                        </p>
                      </div>
                    </div>
                    <ArrowRight className="w-5 h-5 text-gray-400" />
                  </Link>
                </div>

                {/* Privacy & Safety Section */}
                <div>
                  <h2 className="text-lg font-semibold text-gray-900 mb-4">Privacy & Safety</h2>

                  {/* Follow Approval Setting */}
                  <div className="flex items-center justify-between p-4 bg-white border border-gray-200 rounded-lg mb-4">
                    <div className="flex items-center space-x-3">
                      <div className="p-2 bg-purple-100 rounded-lg">
                        <UserCheck className="w-5 h-5 text-purple-600" />
                      </div>
                      <div>
                        <h3 className="font-semibold text-gray-900">Require Follow Approval</h3>
                        <p className="text-sm text-gray-600">
                          Require approval before users can follow you
                        </p>
                      </div>
                    </div>
                    <button
                      onClick={handleFollowApprovalToggle}
                      disabled={updatePreferencesMutation.isPending}
                      className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-purple-500 focus:ring-offset-2 disabled:opacity-50 ${
                        preferences?.requireFollowApproval ? 'bg-purple-600' : 'bg-gray-200'
                      }`}
                    >
                      <span
                        className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                          preferences?.requireFollowApproval ? 'translate-x-6' : 'translate-x-1'
                        }`}
                      />
                    </button>
                  </div>

                  {/* Blocklist Setting */}
                  <Link
                    href="/settings/blocklist"
                    className="flex items-center justify-between p-4 bg-white border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
                  >
                    <div className="flex items-center space-x-3">
                      <div className="p-2 bg-red-100 rounded-lg">
                        <Shield className="w-5 h-5 text-red-600" />
                      </div>
                      <div>
                        <h3 className="font-semibold text-gray-900">Blocklist</h3>
                        <p className="text-sm text-gray-600">
                          Manage users you&apos;ve blocked
                        </p>
                      </div>
                    </div>
                    <ArrowRight className="w-5 h-5 text-gray-400" />
                  </Link>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
