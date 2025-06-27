'use client';

import { useAuth } from '@/contexts/AuthContext';
import { useTheme } from '@/contexts/ThemeContext';
import { useRouter } from 'next/navigation';
import { useEffect } from 'react';
import Sidebar from '@/components/Sidebar';
import Link from 'next/link';
import { Shield, ArrowRight, Moon, Sun } from 'lucide-react';

export default function SettingsPage() {
  const { user, isLoading } = useAuth();
  const { isDarkMode, toggleDarkMode } = useTheme();
  const router = useRouter();

  useEffect(() => {
    if (!isLoading && !user) {
      router.push('/login');
    }
  }, [user, isLoading, router]);

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900">
        <div className="text-lg text-gray-900 dark:text-white">Loading...</div>
      </div>
    );
  }

  if (!user) {
    return null;
  }

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <div className="max-w-6xl mx-auto flex">
        {/* Sidebar */}
        <div className="w-16 lg:w-64 fixed h-full z-10">
          <Sidebar />
        </div>

        {/* Main Content */}
        <div className="flex-1 ml-16 lg:ml-64">
          <div className="max-w-2xl mx-auto lg:border-x border-gray-200 dark:border-gray-700 min-h-screen bg-white dark:bg-gray-800">
            {/* Header */}
            <div className="sticky top-0 bg-white/80 dark:bg-gray-800/80 backdrop-blur-md border-b border-gray-200 dark:border-gray-700 p-4">
              <h1 className="text-xl font-bold text-gray-900 dark:text-white">Settings</h1>
            </div>

            {/* Settings Content */}
            <div className="p-6">
              <div className="space-y-6">
                {/* Appearance Section */}
                <div>
                  <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">Appearance</h2>

                  {/* Dark Mode Toggle */}
                  <div className="flex items-center justify-between p-4 bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg">
                    <div className="flex items-center space-x-3">
                      <div className="p-2 bg-blue-100 dark:bg-blue-900 rounded-lg">
                        {isDarkMode ? (
                          <Moon className="w-5 h-5 text-blue-600 dark:text-blue-400" />
                        ) : (
                          <Sun className="w-5 h-5 text-blue-600 dark:text-blue-400" />
                        )}
                      </div>
                      <div>
                        <h3 className="font-semibold text-gray-900 dark:text-white">Dark Mode</h3>
                        <p className="text-sm text-gray-600 dark:text-gray-300">
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

                {/* Privacy & Safety Section */}
                <div>
                  <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">Privacy & Safety</h2>

                  {/* Blocklist Setting */}
                  <Link
                    href="/settings/blocklist"
                    className="flex items-center justify-between p-4 bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-600 transition-colors"
                  >
                    <div className="flex items-center space-x-3">
                      <div className="p-2 bg-red-100 dark:bg-red-900 rounded-lg">
                        <Shield className="w-5 h-5 text-red-600 dark:text-red-400" />
                      </div>
                      <div>
                        <h3 className="font-semibold text-gray-900 dark:text-white">Blocklist</h3>
                        <p className="text-sm text-gray-600 dark:text-gray-300">
                          Manage users you've blocked
                        </p>
                      </div>
                    </div>
                    <ArrowRight className="w-5 h-5 text-gray-400 dark:text-gray-500" />
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
