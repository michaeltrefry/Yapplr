'use client';

import { useAuth } from '@/contexts/AuthContext';
import { Home, User, Search, LogOut } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import FollowingList from './FollowingList';

export default function Sidebar() {
  const { user, logout } = useAuth();
  const router = useRouter();

  const handleLogout = () => {
    logout();
    router.push('/login');
  };

  return (
    <div className="h-full bg-white border-r border-gray-200 p-4">
      <div className="flex flex-col h-full">
        {/* Logo */}
        <div className="mb-8">
          <h1 className="text-2xl font-bold text-blue-600 hidden lg:block">Yapplr</h1>
          <h1 className="text-2xl font-bold text-blue-600 lg:hidden">Y</h1>
        </div>

        {/* Navigation */}
        <nav className="flex-1 space-y-2">
          <Link
            href="/"
            className="flex items-center space-x-3 px-3 py-2 rounded-lg hover:bg-gray-100 transition-colors"
          >
            <Home className="w-6 h-6" />
            <span className="text-lg hidden lg:block">Home</span>
          </Link>

          <Link
            href="/search"
            className="flex items-center space-x-3 px-3 py-2 rounded-lg hover:bg-gray-100 transition-colors"
          >
            <Search className="w-6 h-6" />
            <span className="text-lg hidden lg:block">Search</span>
          </Link>

          <Link
            href={`/profile/${user?.username}`}
            className="flex items-center space-x-3 px-3 py-2 rounded-lg hover:bg-gray-100 transition-colors"
          >
            <User className="w-6 h-6" />
            <span className="text-lg hidden lg:block">Profile</span>
          </Link>

          {/* Following Section */}
          <div className="mt-4">
            <h3 className="text-sm font-semibold text-gray-700 mb-3 px-3 hidden lg:block">
              Following
            </h3>
            <FollowingList />
          </div>
        </nav>

        {/* User Info & Logout */}
        <div className="border-t border-gray-200 pt-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-3">
              <div className="w-10 h-10 bg-blue-600 rounded-full flex items-center justify-center">
                <span className="text-white font-semibold">
                  {user?.username.charAt(0).toUpperCase()}
                </span>
              </div>
              <div className="flex-1 min-w-0 hidden lg:block">
                <p className="text-sm font-medium text-gray-900 truncate">
                  @{user?.username}
                </p>
                <p className="text-xs text-gray-500 truncate">
                  {user?.email}
                </p>
              </div>
            </div>
            <button
              onClick={handleLogout}
              className="p-2 text-gray-400 hover:text-gray-600 transition-colors"
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
