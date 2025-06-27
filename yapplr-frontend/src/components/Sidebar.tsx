'use client';

import { useAuth } from '@/contexts/AuthContext';
import { useNotifications } from '@/contexts/NotificationContext';
import { Home, User, Search, LogOut, Settings, MessageCircle } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import FollowingList from './FollowingList';

export default function Sidebar() {
  const { user, logout } = useAuth();
  const { unreadMessageCount } = useNotifications();
  const router = useRouter();

  const handleLogout = () => {
    logout();
    router.push('/login');
  };

  return (
    <div className="h-full bg-white dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 p-4">
      <div className="flex flex-col h-full">
        {/* Logo */}
        <div className="mb-8">
          <h1 className="text-2xl font-bold text-blue-600 dark:text-blue-400 hidden lg:block">Yapplr</h1>
          <h1 className="text-2xl font-bold text-blue-600 dark:text-blue-400 lg:hidden">Y</h1>
        </div>

        {/* Navigation */}
        <nav className="flex-1 space-y-2">
          <Link
            href="/"
            className="flex items-center space-x-3 px-3 py-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors text-gray-700 dark:text-gray-200"
          >
            <Home className="w-6 h-6" />
            <span className="text-lg hidden lg:block">Home</span>
          </Link>

          <Link
            href="/search"
            className="flex items-center space-x-3 px-3 py-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors text-gray-700 dark:text-gray-200"
          >
            <Search className="w-6 h-6" />
            <span className="text-lg hidden lg:block">Search</span>
          </Link>

          <Link
            href="/messages"
            className="flex items-center space-x-3 px-3 py-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors relative text-gray-700 dark:text-gray-200"
          >
            <div className="relative">
              <MessageCircle className="w-6 h-6" />
              {unreadMessageCount > 0 && (
                <div className="absolute -top-1 -right-1 bg-red-500 text-white text-xs rounded-full w-5 h-5 flex items-center justify-center">
                  {unreadMessageCount > 9 ? '9+' : unreadMessageCount}
                </div>
              )}
            </div>
            <span className="text-lg hidden lg:block">Messages</span>
          </Link>

          <Link
            href={`/profile/${user?.username}`}
            className="flex items-center space-x-3 px-3 py-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors text-gray-700 dark:text-gray-200"
          >
            <User className="w-6 h-6" />
            <span className="text-lg hidden lg:block">Profile</span>
          </Link>

          <Link
            href="/settings"
            className="flex items-center space-x-3 px-3 py-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors text-gray-700 dark:text-gray-200"
          >
            <Settings className="w-6 h-6" />
            <span className="text-lg hidden lg:block">Settings</span>
          </Link>

          {/* Following Section */}
          <div className="mt-4">
            <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3 px-3 hidden lg:block">
              Following
            </h3>
            <FollowingList />
          </div>
        </nav>

        {/* User Info & Logout */}
        <div className="border-t border-gray-200 dark:border-gray-700 pt-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-3">
              <div className="w-10 h-10 bg-blue-600 dark:bg-blue-500 rounded-full flex items-center justify-center">
                <span className="text-white font-semibold">
                  {user?.username.charAt(0).toUpperCase()}
                </span>
              </div>
              <div className="flex-1 min-w-0 hidden lg:block">
                <p className="text-sm font-medium text-gray-900 dark:text-white truncate">
                  @{user?.username}
                </p>
                <p className="text-xs text-gray-500 dark:text-gray-400 truncate">
                  {user?.email}
                </p>
              </div>
            </div>
            <button
              onClick={handleLogout}
              className="p-2 text-gray-400 dark:text-gray-500 hover:text-gray-600 dark:hover:text-gray-300 transition-colors"
              title="Logout"
            >
              <LogOut className="w-5 h-5" />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
