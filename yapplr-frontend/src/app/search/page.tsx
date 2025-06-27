'use client';

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { userApi } from '@/lib/api';
import { Search } from 'lucide-react';
import Link from 'next/link';
import Sidebar from '@/components/Sidebar';

export default function SearchPage() {
  const [query, setQuery] = useState('');
  
  const { data: users, isLoading } = useQuery({
    queryKey: ['search', query],
    queryFn: () => userApi.searchUsers(query),
    enabled: query.length > 0,
  });

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
              <h1 className="text-xl font-bold mb-4 text-gray-900 dark:text-white">Search</h1>
              
              {/* Search Input */}
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 dark:text-gray-500 w-5 h-5" />
                <input
                  type="text"
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  placeholder="Search for users..."
                  className="w-full pl-10 pr-4 py-3 bg-gray-100 dark:bg-gray-700 rounded-full focus:outline-none focus:ring-2 focus:ring-blue-500 focus:bg-white dark:focus:bg-gray-600 transition-colors text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400"
                />
              </div>
            </div>

            {/* Results */}
            <div className="p-4">
              {query.length === 0 && (
                <div className="text-center text-gray-500 dark:text-gray-400 mt-8">
                  <Search className="w-12 h-12 mx-auto mb-4 text-gray-300 dark:text-gray-600" />
                  <p>Search for users by username or bio</p>
                </div>
              )}

              {query.length > 0 && isLoading && (
                <div className="text-center text-gray-500 dark:text-gray-400 mt-8">
                  <p>Searching...</p>
                </div>
              )}

              {query.length > 0 && users && users.length === 0 && (
                <div className="text-center text-gray-500 dark:text-gray-400 mt-8">
                  <p>No users found for &ldquo;{query}&rdquo;</p>
                </div>
              )}

              {users && users.length > 0 && (
                <div className="space-y-4">
                  {users.map((user) => (
                    <Link
                      key={user.id}
                      href={`/profile/${user.username}`}
                      className="block p-4 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
                    >
                      <div className="flex items-center space-x-3">
                        <div className="w-12 h-12 bg-blue-600 dark:bg-blue-500 rounded-full flex items-center justify-center">
                          <span className="text-white font-semibold text-lg">
                            {user.username.charAt(0).toUpperCase()}
                          </span>
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className="font-semibold text-gray-900 dark:text-white">
                            {user.username}
                          </p>
                          <p className="text-gray-500 dark:text-gray-400 text-sm">
                            @{user.username}
                          </p>
                          {user.bio && (
                            <p className="text-gray-700 dark:text-gray-300 text-sm mt-1 truncate">
                              {user.bio}
                            </p>
                          )}
                          {user.pronouns && (
                            <p className="text-gray-500 dark:text-gray-400 text-xs mt-1">
                              {user.pronouns}
                            </p>
                          )}
                        </div>
                      </div>
                    </Link>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
