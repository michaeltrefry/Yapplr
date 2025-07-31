'use client';

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useRouter } from 'next/navigation';
import { tagApi, trendingApi } from '@/lib/api';
import { TrendingUp, Hash, ArrowLeft, Clock, Users, Calendar, Zap, MessageSquare } from 'lucide-react';
import Link from 'next/link';
import Sidebar from '@/components/Sidebar';
import { formatNumber } from '@/lib/utils';

type TrendingPeriod = 'now' | 'today' | 'week';
type TrendingTab = 'hashtags' | 'posts';

export default function TrendingPage() {
  const router = useRouter();
  const [activePeriod, setActivePeriod] = useState<TrendingPeriod>('now');
  const [activeTab, setActiveTab] = useState<TrendingTab>('hashtags'); // Default to hashtags since posts endpoint has issues

  // Get trending hashtags based on selected period
  const { data: trendingTags, isLoading: trendingLoading, error } = useQuery({
    queryKey: ['trending-tags', activePeriod],
    queryFn: () => {
      switch (activePeriod) {
        case 'now':
          return tagApi.getTrendingTags(20);
        case 'today':
          // For now, use the same endpoint but could be enhanced with time-based filtering
          return tagApi.getTrendingTags(20);
        case 'week':
          // For now, use the same endpoint but could be enhanced with time-based filtering
          return tagApi.getTrendingTags(20);
        default:
          return tagApi.getTrendingTags(20);
      }
    },
    refetchInterval: activePeriod === 'now' ? 30000 : 60000, // More frequent refresh for "now"
    enabled: activeTab === 'hashtags',
  });

  // Get trending posts based on selected period
  const { data: trendingPosts, isLoading: postsLoading, error: postsError } = useQuery({
    queryKey: ['trending-posts-overview', activePeriod],
    queryFn: () => {
      switch (activePeriod) {
        case 'now':
          return trendingApi.getTrendingPostsNow(10);
        case 'today':
          return trendingApi.getTrendingPostsToday(10);
        case 'week':
          return trendingApi.getTrendingPostsWeek(10);
        default:
          return trendingApi.getTrendingPostsToday(10);
      }
    },
    refetchInterval: activePeriod === 'now' ? 30000 : 60000,
    enabled: activeTab === 'posts',
  });

  const currentError = activeTab === 'hashtags' ? error : postsError;
  const isLoading = activeTab === 'hashtags' ? trendingLoading : postsLoading;

  if (currentError) {
    return (
      <div className="min-h-screen bg-gray-50">
        <div className="max-w-6xl mx-auto flex">
          {/* Sidebar */}
          <div className="w-16 lg:w-64 fixed h-full z-10">
            <Sidebar />
          </div>

          {/* Main Content */}
          <div className="flex-1 ml-16 lg:ml-64">
            <div className="max-w-2xl mx-auto">
              {/* Header */}
              <div className="bg-white border-b border-gray-200 px-4 py-3 sticky top-0 z-20">
                <div className="flex items-center space-x-3">
                  <button
                    onClick={() => router.back()}
                    className="p-2 hover:bg-gray-100 rounded-full"
                  >
                    <ArrowLeft className="w-5 h-5" />
                  </button>
                  <div>
                    <h1 className="text-xl font-bold text-gray-900">Trending</h1>
                    <p className="text-sm text-gray-500">Error loading trending hashtags</p>
                  </div>
                </div>
              </div>

              {/* Error Message */}
              <div className="p-8 text-center">
                <TrendingUp className="w-16 h-16 text-gray-300 mx-auto mb-4" />
                <h2 className="text-xl font-semibold text-gray-900 mb-2">Unable to load trending hashtags</h2>
                <p className="text-gray-600">
                  There was an error loading the trending hashtags. Please try again later.
                </p>
                <Link
                  href="/search"
                  className="inline-block mt-4 bg-blue-500 text-white px-6 py-2 rounded-full hover:bg-blue-600 transition-colors"
                >
                  Search hashtags instead
                </Link>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
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
          <div className="max-w-2xl mx-auto">
            {/* Header */}
            <div className="bg-white border-b border-gray-200 px-4 py-3 sticky top-0 z-20">
              <div className="flex items-center space-x-3 mb-4">
                <button
                  onClick={() => router.back()}
                  className="p-2 hover:bg-gray-100 rounded-full"
                >
                  <ArrowLeft className="w-5 h-5" />
                </button>
                <div className="flex-1">
                  <h1 className="text-xl font-bold text-gray-900 flex items-center">
                    <TrendingUp className="w-6 h-6 mr-2 text-orange-500" />
                    Trending {activeTab === 'hashtags' ? 'Hashtags' : 'Posts'}
                  </h1>
                  <p className="text-sm text-gray-500">
                    {activeTab === 'hashtags' && (
                      <>
                        {activePeriod === 'now' && 'Popular hashtags right now'}
                        {activePeriod === 'today' && 'Popular hashtags today'}
                        {activePeriod === 'week' && 'Popular hashtags this week'}
                      </>
                    )}
                    {activeTab === 'posts' && (
                      <>
                        {activePeriod === 'now' && 'Hottest posts right now'}
                        {activePeriod === 'today' && 'Today\'s most engaging posts'}
                        {activePeriod === 'week' && 'This week\'s viral content'}
                      </>
                    )}
                  </p>
                </div>
                <div className="text-xs text-gray-400 flex items-center">
                  <Clock className="w-3 h-3 mr-1" />
                  {activePeriod === 'now' ? 'Live' : 'Updated'}
                </div>
              </div>

              {/* Content Type Tabs */}
              <div className="flex space-x-1 bg-gray-100 rounded-lg p-1 mb-4">
                <button
                  onClick={() => setActiveTab('hashtags')}
                  className={`flex items-center space-x-2 px-4 py-2 rounded-md text-sm font-medium transition-colors flex-1 justify-center ${
                    activeTab === 'hashtags'
                      ? 'bg-white text-gray-900 shadow-sm'
                      : 'text-gray-600 hover:text-gray-900'
                  }`}
                >
                  <Hash className="w-4 h-4" />
                  <span>Hashtags</span>
                </button>
                <button
                  onClick={() => setActiveTab('posts')}
                  className={`flex items-center space-x-2 px-4 py-2 rounded-md text-sm font-medium transition-colors flex-1 justify-center ${
                    activeTab === 'posts'
                      ? 'bg-white text-gray-900 shadow-sm'
                      : 'text-gray-600 hover:text-gray-900'
                  }`}
                >
                  <MessageSquare className="w-4 h-4" />
                  <span>Trending Posts</span>
                  {activeTab === 'posts' && (
                    <span className="ml-1 px-1.5 py-0.5 text-xs bg-orange-100 text-orange-600 rounded-full">
                      Hot
                    </span>
                  )}
                </button>
              </div>

              {/* Time Period Tabs */}
              <div className="flex border-b border-gray-200">
                <button
                  onClick={() => setActivePeriod('now')}
                  className={`flex-1 py-3 px-4 text-center font-medium transition-colors ${
                    activePeriod === 'now'
                      ? 'text-orange-600 border-b-2 border-orange-600'
                      : 'text-gray-500 hover:text-gray-700'
                  }`}
                >
                  <Zap className="w-4 h-4 inline mr-2" />
                  Right Now
                </button>
                <button
                  onClick={() => setActivePeriod('today')}
                  className={`flex-1 py-3 px-4 text-center font-medium transition-colors ${
                    activePeriod === 'today'
                      ? 'text-orange-600 border-b-2 border-orange-600'
                      : 'text-gray-500 hover:text-gray-700'
                  }`}
                >
                  <Clock className="w-4 h-4 inline mr-2" />
                  Today
                </button>
                <button
                  onClick={() => setActivePeriod('week')}
                  className={`flex-1 py-3 px-4 text-center font-medium transition-colors ${
                    activePeriod === 'week'
                      ? 'text-orange-600 border-b-2 border-orange-600'
                      : 'text-gray-500 hover:text-gray-700'
                  }`}
                >
                  <Calendar className="w-4 h-4 inline mr-2" />
                  This Week
                </button>
              </div>
            </div>

            {/* Trending Content */}
            <div className="bg-white">
              {isLoading ? (
                // Loading skeleton
                <div className="divide-y divide-gray-200">
                  {Array.from({ length: 10 }).map((_, i) => (
                    <div key={i} className="p-4">
                      <div className="flex items-center space-x-3">
                        <div className="w-12 h-12 bg-gray-200 rounded-full animate-pulse"></div>
                        <div className="flex-1 space-y-2">
                          <div className="h-4 bg-gray-200 rounded w-1/3 animate-pulse"></div>
                          <div className="h-3 bg-gray-200 rounded w-1/4 animate-pulse"></div>
                        </div>
                        <div className="h-6 bg-gray-200 rounded w-12 animate-pulse"></div>
                      </div>
                    </div>
                  ))}
                </div>
              ) : activeTab === 'hashtags' && trendingTags && trendingTags.length > 0 ? (
                <div className="divide-y divide-gray-200">
                  {trendingTags.map((tag, index) => (
                    <Link
                      key={tag.id}
                      href={`/hashtag/${tag.name}`}
                      className="block p-4 hover:bg-gray-50 transition-colors"
                    >
                      <div className="flex items-center space-x-3">
                        {/* Trending Rank */}
                        <div className="flex-shrink-0 w-8 text-center">
                          <span className={`text-sm font-bold ${
                            index < 3 ? 'text-orange-500' : 'text-gray-400'
                          }`}>
                            #{index + 1}
                          </span>
                        </div>

                        {/* Hashtag Icon */}
                        <div className={`w-12 h-12 rounded-full flex items-center justify-center ${
                          index === 0 ? 'bg-gradient-to-br from-orange-400 to-red-500' :
                          index === 1 ? 'bg-gradient-to-br from-yellow-400 to-orange-500' :
                          index === 2 ? 'bg-gradient-to-br from-green-400 to-blue-500' :
                          'bg-gradient-to-br from-blue-400 to-purple-500'
                        }`}>
                          <Hash className="w-6 h-6 text-white" />
                        </div>

                        {/* Hashtag Info */}
                        <div className="flex-1 min-w-0">
                          <p className="font-semibold text-gray-900 text-lg">
                            #{tag.name}
                          </p>
                          <div className="flex items-center space-x-4 text-sm text-gray-500">
                            <span className="flex items-center">
                              <Users className="w-3 h-3 mr-1" />
                              {formatNumber(tag.postCount || 0)} {(tag.postCount || 0) === 1 ? 'post' : 'posts'}
                            </span>
                            {activePeriod === 'now' && (
                              <span className="text-orange-500 text-xs font-medium">
                                TRENDING
                              </span>
                            )}
                          </div>
                          {/* Trending description based on rank */}
                          {index < 3 && (
                            <p className="text-xs text-gray-400 mt-1">
                              {index === 0 && "🔥 Most popular hashtag"}
                              {index === 1 && "⭐ Second most popular"}
                              {index === 2 && "🚀 Third most popular"}
                            </p>
                          )}
                        </div>

                        {/* Trending Indicator & Action */}
                        <div className="flex-shrink-0 flex flex-col items-end space-y-2">
                          {index < 5 && (
                            <div className="flex items-center space-x-1">
                              <TrendingUp className={`w-4 h-4 ${
                                index < 3 ? 'text-orange-500' : 'text-green-500'
                              }`} />
                              <span className={`text-xs font-medium ${
                                index < 3 ? 'text-orange-500' : 'text-green-500'
                              }`}>
                                {index < 3 ? 'Hot' : 'Rising'}
                              </span>
                            </div>
                          )}
                          <button
                            onClick={(e) => {
                              e.preventDefault();
                              window.open(`/hashtag/${tag.name}`, '_blank');
                            }}
                            className="text-xs text-blue-600 hover:text-blue-800 font-medium"
                          >
                            View posts →
                          </button>
                        </div>
                      </div>
                    </Link>
                  ))}
                </div>
              ) : activeTab === 'posts' && trendingPosts && trendingPosts.length > 0 ? (
                <div className="divide-y divide-gray-200">
                  {trendingPosts.map((post, index) => (
                    <div key={post.id} className="p-4 hover:bg-gray-50 transition-colors">
                      <div className="flex items-start space-x-3">
                        {/* Trending Rank */}
                        <div className="flex-shrink-0 w-8 text-center mt-1">
                          <span className={`text-sm font-bold ${
                            index < 3 ? 'text-orange-500' : 'text-gray-400'
                          }`}>
                            #{index + 1}
                          </span>
                        </div>

                        {/* Post Preview */}
                        <div className="flex-1 min-w-0">
                          <div className="flex items-center space-x-2 mb-2">
                            <span className="font-medium text-gray-900">{post.user.username}</span>
                            <span className="text-gray-500 text-sm">•</span>
                            <span className="text-gray-500 text-sm">
                              {new Date(post.createdAt).toLocaleDateString()}
                            </span>
                          </div>
                          <p className="text-gray-800 text-sm line-clamp-2 mb-2">
                            {post.content}
                          </p>
                          <div className="flex items-center space-x-4 text-xs text-gray-500">
                            <span>{post.likeCount} likes</span>
                            <span>{post.commentCount} comments</span>
                            <span>{post.repostCount} reposts</span>
                          </div>
                        </div>

                        {/* Trending Indicator */}
                        <div className="flex-shrink-0">
                          {index < 5 && (
                            <div className="flex items-center space-x-1">
                              <TrendingUp className={`w-4 h-4 ${
                                index < 3 ? 'text-orange-500' : 'text-green-500'
                              }`} />
                              <span className={`text-xs font-medium ${
                                index < 3 ? 'text-orange-500' : 'text-green-500'
                              }`}>
                                {index < 3 ? 'Hot' : 'Rising'}
                              </span>
                            </div>
                          )}
                        </div>
                      </div>
                    </div>
                  ))}

                  {/* View All Link */}
                  <div className="p-4 text-center border-t border-gray-200">
                    <Link
                      href="/trending/posts"
                      className="text-blue-600 hover:text-blue-800 text-sm font-medium"
                    >
                      View all trending posts →
                    </Link>
                  </div>
                </div>
              ) : (
                // Empty state
                <div className="p-8 text-center">
                  <TrendingUp className="w-16 h-16 text-gray-300 mx-auto mb-4" />
                  <h2 className="text-xl font-semibold text-gray-900 mb-2">
                    {activeTab === 'hashtags' ? 'No trending hashtags yet' : 'No trending posts found'}
                  </h2>
                  <p className="text-gray-600 mb-4">
                    {activeTab === 'hashtags'
                      ? 'Start using hashtags in your posts to see them trend!'
                      : 'Posts need likes, comments, and reposts to start trending. Be the first to create viral content!'
                    }
                  </p>
                  <Link
                    href="/"
                    className="inline-block bg-blue-500 text-white px-6 py-2 rounded-full hover:bg-blue-600 transition-colors"
                  >
                    Create a post
                  </Link>
                </div>
              )}
            </div>

            {/* Footer Info */}
            {trendingTags && trendingTags.length > 0 && (
              <div className="bg-white border-t border-gray-200 p-4 space-y-3">
                <div className="flex items-center justify-between text-xs text-gray-500">
                  <span>
                    Showing {trendingTags.length} trending hashtags
                  </span>
                  <span>
                    Last updated: {new Date().toLocaleTimeString()}
                  </span>
                </div>
                <p className="text-xs text-gray-500 text-center">
                  {activePeriod === 'now' && 'Rankings update every 30 seconds based on recent activity'}
                  {activePeriod === 'today' && 'Rankings based on activity from the last 24 hours'}
                  {activePeriod === 'week' && 'Rankings based on activity from the last 7 days'}
                </p>
                <div className="flex justify-center space-x-4 text-xs">
                  <Link href="/search" className="text-blue-600 hover:text-blue-800">
                    Search hashtags
                  </Link>
                  <span className="text-gray-300">•</span>
                  <Link href="/" className="text-blue-600 hover:text-blue-800">
                    Create post
                  </Link>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
