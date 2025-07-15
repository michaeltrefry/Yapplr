'use client';

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { userApi, tagApi } from '@/lib/api';
import { Search, Hash, User } from 'lucide-react';
import Link from 'next/link';
import Sidebar from '@/components/Sidebar';
import { formatNumber } from '@/lib/utils';

type SearchTab = 'users' | 'hashtags';

export default function SearchPage() {
  const [query, setQuery] = useState('');
  const [activeTab, setActiveTab] = useState<SearchTab>('users');

  const { data: users, isLoading: usersLoading } = useQuery({
    queryKey: ['search-users', query],
    queryFn: () => userApi.searchUsers(query),
    enabled: query.length > 0 && activeTab === 'users',
  });

  const { data: hashtags, isLoading: hashtagsLoading } = useQuery({
    queryKey: ['search-hashtags', query],
    queryFn: () => tagApi.searchTags(query, 20),
    enabled: query.length > 0 && activeTab === 'hashtags',
  });

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
            <div className="sticky top-0 bg-white/80 backdrop-blur-md border-b border-gray-200 p-4 z-20">
              <h1 className="text-xl font-bold mb-4 text-gray-900">Search</h1>

              {/* Search Input */}
              <div className="relative mb-4">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-5 h-5" />
                <input
                  type="text"
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  placeholder={activeTab === 'users' ? 'Search for users...' : 'Search for hashtags...'}
                  className="w-full pl-10 pr-4 py-3 bg-gray-100 rounded-full focus:outline-none focus:ring-2 focus:ring-blue-500 focus:bg-white transition-colors text-gray-900 placeholder-gray-500"
                />
              </div>

              {/* Tabs */}
              <div className="flex border-b border-gray-200">
                <button
                  onClick={() => setActiveTab('users')}
                  className={`flex-1 py-3 px-4 text-center font-medium transition-colors ${
                    activeTab === 'users'
                      ? 'text-blue-600 border-b-2 border-blue-600'
                      : 'text-gray-500 hover:text-gray-700'
                  }`}
                >
                  <User className="w-4 h-4 inline mr-2" />
                  Users
                </button>
                <button
                  onClick={() => setActiveTab('hashtags')}
                  className={`flex-1 py-3 px-4 text-center font-medium transition-colors ${
                    activeTab === 'hashtags'
                      ? 'text-blue-600 border-b-2 border-blue-600'
                      : 'text-gray-500 hover:text-gray-700'
                  }`}
                >
                  <Hash className="w-4 h-4 inline mr-2" />
                  Hashtags
                </button>
              </div>
            </div>

            {/* Results */}
            <div className="p-4">
              {query.length === 0 && (
                <div className="text-center text-gray-500 mt-8">
                  <Search className="w-12 h-12 mx-auto mb-4 text-gray-300" />
                  <p>
                    {activeTab === 'users'
                      ? 'Search for users by username or bio'
                      : 'Search for hashtags'
                    }
                  </p>
                </div>
              )}

              {query.length > 0 && (usersLoading || hashtagsLoading) && (
                <div className="text-center text-gray-500 mt-8">
                  <p>Searching...</p>
                </div>
              )}

              {/* Users Results */}
              {activeTab === 'users' && query.length > 0 && users && users.length === 0 && !usersLoading && (
                <div className="text-center text-gray-500 mt-8">
                  <p>No users found for &ldquo;{query}&rdquo;</p>
                </div>
              )}

              {activeTab === 'users' && users && users.length > 0 && (
                <div className="space-y-4">
                  {users.map((user) => (
                    <Link
                      key={user.id}
                      href={`/profile/${user.username}`}
                      className="block p-4 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
                    >
                      <div className="flex items-center space-x-3">
                        <div className="w-12 h-12 bg-blue-600 rounded-full flex items-center justify-center">
                          <span className="text-white font-semibold text-lg">
                            {user.username.charAt(0).toUpperCase()}
                          </span>
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className="font-semibold text-gray-900">
                            @{user.username}
                          </p>
                          {user.bio && (
                            <p className="text-gray-700 text-sm mt-1 truncate">
                              {user.bio}
                            </p>
                          )}
                          {user.pronouns && (
                            <p className="text-gray-500 text-xs mt-1">
                              {user.pronouns}
                            </p>
                          )}
                        </div>
                      </div>
                    </Link>
                  ))}
                </div>
              )}

              {/* Hashtags Results */}
              {activeTab === 'hashtags' && query.length > 0 && hashtags && hashtags.length === 0 && !hashtagsLoading && (
                <div className="text-center text-gray-500 mt-8">
                  <p>No hashtags found for &ldquo;{query}&rdquo;</p>
                </div>
              )}

              {activeTab === 'hashtags' && hashtags && hashtags.length > 0 && (
                <div className="space-y-4">
                  {hashtags.map((hashtag) => (
                    <Link
                      key={hashtag.id}
                      href={`/hashtag/${hashtag.name}`}
                      className="block p-4 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
                    >
                      <div className="flex items-center space-x-3">
                        <div className="w-12 h-12 bg-green-600 rounded-full flex items-center justify-center">
                          <Hash className="w-6 h-6 text-white" />
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className="font-semibold text-gray-900">
                            #{hashtag.name}
                          </p>
                          <p className="text-gray-500 text-sm">
                            {formatNumber(hashtag.postCount || 0)} {(hashtag.postCount || 0) === 1 ? 'post' : 'posts'}
                          </p>
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
