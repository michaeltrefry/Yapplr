'use client';

import { useAuth } from '@/contexts/AuthContext';
import { useNotifications } from '@/contexts/NotificationContext';
import { Home, Search, LogOut, Settings, MessageCircle, Bell, TestTube, TrendingUp, Shield, Users } from 'lucide-react';
import { UserRole } from '@/types';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import Image from 'next/image';
import FollowingList from './FollowingList';

export default function Sidebar() {
  const { user, logout } = useAuth();
  const { unreadMessageCount, unreadNotificationCount } = useNotifications();
  const router = useRouter();

  const handleLogout = () => {
    logout();
    router.push('/login');
  };

  return (
    <div className="h-full bg-background border-r border-default p-2 lg:p-4">
      <div className="flex flex-col h-full">
        {/* Logo */}
        <div className="mb-6 lg:mb-8">
          <Link href="/" className="flex items-center justify-center lg:justify-start space-x-2">
            <Image
              src="/logo-64.png"
              alt="Yapplr Logo"
              width={48}
              height={48}
              className="w-12 h-12"
            />
            <h1 className="text-4xl font-bold text-blue-600 hidden lg:block pb-1">Yapplr</h1>
          </Link>
        </div>

        {/* Navigation */}
        <nav className="flex-1 flex flex-col space-y-2 min-h-0">
          <div className="space-y-2">
            <Link
              href="/"
              className="flex items-center justify-center lg:justify-start lg:space-x-3 px-1 lg:px-3 py-2 rounded-lg hover:bg-gray-100 text-foreground"
            >
              <Home className="w-6 h-6" />
              <span className="text-lg hidden lg:block">Home</span>
            </Link>

            <Link
              href="/search"
              className="flex items-center justify-center lg:justify-start lg:space-x-3 px-1 lg:px-3 py-2 rounded-lg hover:bg-gray-100 text-foreground"
            >
              <Search className="w-6 h-6" />
              <span className="text-lg hidden lg:block">Search</span>
            </Link>

          <Link
            href="/trending"
            className="flex items-center justify-center lg:justify-start lg:space-x-3 px-1 lg:px-3 py-2 rounded-lg hover:bg-gray-100 text-foreground"
          >
            <TrendingUp className="w-6 h-6" />
            <span className="text-lg hidden lg:block">Trending</span>
          </Link>

          <Link
            href="/groups"
            className="flex items-center justify-center lg:justify-start lg:space-x-3 px-1 lg:px-3 py-2 rounded-lg hover:bg-gray-100 text-foreground"
          >
            <Users className="w-6 h-6" />
            <span className="text-lg hidden lg:block">Groups</span>
          </Link>

          <Link
            href="/notifications"
            className="flex items-center justify-center lg:justify-start lg:space-x-3 px-1 lg:px-3 py-2 rounded-lg hover:bg-gray-100 relative text-foreground"
          >
            <div className="relative">
              <Bell className="w-6 h-6" />
              {unreadNotificationCount > 0 && (
                <div className="absolute -top-1 -right-1 bg-red-500 text-white text-xs rounded-full w-5 h-5 flex items-center justify-center">
                  {unreadNotificationCount > 9 ? '9+' : unreadNotificationCount}
                </div>
              )}
            </div>
            <span className="text-lg hidden lg:block">Notifications</span>
          </Link>

          <Link
            href="/messages"
            className="flex items-center justify-center lg:justify-start lg:space-x-3 px-1 lg:px-3 py-2 rounded-lg hover:bg-gray-100 relative text-foreground"
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
            href="/settings"
            className="flex items-center justify-center lg:justify-start lg:space-x-3 px-1 lg:px-3 py-2 rounded-lg hover:bg-gray-100 text-foreground"
          >
            <Settings className="w-6 h-6" />
            <span className="text-lg hidden lg:block">Settings</span>
          </Link>

          {/* Admin Navigation - Show for Moderators and Admins */}
          {user && (user.role === UserRole.Moderator || user.role === UserRole.Admin) && (
            <Link
              href="/admin"
              className="flex items-center justify-center lg:justify-start lg:space-x-3 px-1 lg:px-3 py-2 rounded-lg hover:bg-blue-100 text-blue-600 border border-blue-200"
            >
              <Shield className="w-6 h-6" />
              <span className="text-lg hidden lg:block font-medium">Admin</span>
            </Link>
          )}



          {/* Development only - Notification Test */}
          {process.env.NODE_ENV === 'development' && (
            <Link
              href="/notification-test"
              className="flex items-center justify-center lg:justify-start lg:space-x-3 px-1 lg:px-3 py-2 rounded-lg hover:bg-gray-100 text-foreground border-t border-default mt-2 pt-2"
            >
              <TestTube className="w-6 h-6" />
              <span className="text-lg hidden lg:block">Notification Test</span>
            </Link>
          )}

          </div>

          {/* Following Section - Flexible container */}
          <div className="flex-1 flex flex-col mt-4 min-h-0">
            <h3 className="text-sm font-semibold text-gray-700 mb-3 px-3 hidden lg:block">
              Following
            </h3>
            <div className="flex-1 min-h-0">
              <FollowingList />
            </div>
          </div>
        </nav>

        {/* User Info & Logout */}
        <div className="border-t border-default pt-4 space-y-2 bg-background">
          <Link
            href={`/profile/${user?.username}`}
            className="flex items-center justify-center lg:justify-start hover:bg-gray-100 rounded-lg p-2 cursor-pointer text-foreground"
            title={`Go to @${user?.username}'s profile`}
          >
            <div className="w-10 h-10 bg-blue-600 rounded-full flex items-center justify-center flex-shrink-0">
              <span className="text-white font-semibold">
                {user?.username.charAt(0).toUpperCase()}
              </span>
            </div>
            <div className="flex-1 min-w-0 hidden lg:block lg:ml-3">
              <p className="text-sm font-medium text-foreground truncate">
                @{user?.username}
              </p>
              <p className="text-xs text-secondary truncate">
                {user?.email}
              </p>
            </div>
          </Link>

          <button
            onClick={handleLogout}
            className="flex items-center justify-center lg:justify-start px-1 lg:px-3 py-2 rounded-lg hover:bg-gray-100 text-foreground w-full"
            title="Logout"
          >
            <LogOut className="w-6 h-6 flex-shrink-0" />
            <span className="text-lg hidden lg:block lg:ml-3">Logout</span>
          </button>
        </div>
      </div>
    </div>
  );
}
