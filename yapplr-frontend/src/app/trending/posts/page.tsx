'use client';

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { trendingApi } from '@/lib/api';
import { TrendingUp, Clock, Calendar, Zap } from 'lucide-react';
import PostCard from '@/components/PostCard';
import Sidebar from '@/components/Sidebar';
import { Post } from '@/types';

type TrendingPeriod = 'now' | 'today' | 'week';

export default function TrendingPostsPage() {
  const [activePeriod, setActivePeriod] = useState<TrendingPeriod>('today');

  // Get trending posts based on selected period
  const { data: trendingPosts, isLoading, error, refetch } = useQuery({
    queryKey: ['trending-posts', activePeriod],
    queryFn: () => {
      switch (activePeriod) {
        case 'now':
          return trendingApi.getTrendingPostsNow(20);
        case 'today':
          return trendingApi.getTrendingPostsToday(20);
        case 'week':
          return trendingApi.getTrendingPostsWeek(20);
        default:
          return trendingApi.getTrendingPostsToday(20);
      }
    },
    refetchInterval: activePeriod === 'now' ? 30000 : 60000, // More frequent refresh for "now"
  });



  const getPeriodLabel = (period: TrendingPeriod): string => {
    switch (period) {
      case 'now': return 'Right Now';
      case 'today': return 'Today';
      case 'week': return 'This Week';
      default: return 'Today';
    }
  };

  const getPeriodIcon = (period: TrendingPeriod) => {
    switch (period) {
      case 'now': return <Zap className="w-4 h-4" />;
      case 'today': return <Clock className="w-4 h-4" />;
      case 'week': return <Calendar className="w-4 h-4" />;
      default: return <Clock className="w-4 h-4" />;
    }
  };

  const handleRefresh = () => {
    refetch();
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
          <div className="max-w-2xl mx-auto">
            {/* Header */}
            <div className="bg-white border-b border-gray-200 px-6 py-4 sticky top-0 z-20">
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-3">
                  <TrendingUp className="w-6 h-6 text-orange-500" />
                  <div>
                    <h1 className="text-xl font-bold text-gray-900">Trending Posts</h1>
                    <p className="text-sm text-gray-500">
                      Discover what's popular
                    </p>
                  </div>
                </div>
                <button
                  onClick={handleRefresh}
                  className="p-2 text-gray-400 hover:text-gray-600 rounded-full hover:bg-gray-100 transition-colors"
                  title="Refresh"
                >
                  <TrendingUp className="w-5 h-5" />
                </button>
              </div>



              {/* Period Selector */}
              <div className="mt-4 flex space-x-1 bg-gray-100 rounded-lg p-1">
                {(['now', 'today', 'week'] as TrendingPeriod[]).map((period) => (
                  <button
                    key={period}
                    onClick={() => setActivePeriod(period)}
                    className={`flex items-center space-x-2 px-4 py-2 rounded-md text-sm font-medium transition-colors ${
                      activePeriod === period
                        ? 'bg-white text-gray-900 shadow-sm'
                        : 'text-gray-600 hover:text-gray-900'
                    }`}
                  >
                    {getPeriodIcon(period)}
                    <span>{getPeriodLabel(period)}</span>
                  </button>
                ))}
              </div>
            </div>

            {/* Content */}
            <div className="px-6 py-4">
              {isLoading ? (
                <div className="space-y-4">
                  {[...Array(5)].map((_, i) => (
                    <div key={i} className="bg-white rounded-lg border border-gray-200 p-6">
                      <div className="animate-pulse">
                        <div className="flex items-center space-x-3 mb-4">
                          <div className="w-10 h-10 bg-gray-200 rounded-full"></div>
                          <div className="flex-1">
                            <div className="h-4 bg-gray-200 rounded w-1/4 mb-2"></div>
                            <div className="h-3 bg-gray-200 rounded w-1/6"></div>
                          </div>
                        </div>
                        <div className="space-y-2">
                          <div className="h-4 bg-gray-200 rounded"></div>
                          <div className="h-4 bg-gray-200 rounded w-3/4"></div>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              ) : error ? (
                <div className="text-center py-12">
                  <TrendingUp className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                  <h3 className="text-lg font-medium text-gray-900 mb-2">Unable to load trending posts</h3>
                  <p className="text-gray-500 mb-4">There was an error loading the trending content.</p>
                  <button
                    onClick={handleRefresh}
                    className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 transition-colors"
                  >
                    Try Again
                  </button>
                </div>
              ) : trendingPosts && trendingPosts.length > 0 ? (
                <div className="space-y-4">
                  {trendingPosts.map((post: Post, index: number) => (
                    <div key={post.id} className="relative">
                      {/* Trending Rank Badge */}
                      <div className="absolute -left-2 top-4 z-10">
                        <div className={`w-8 h-8 rounded-full flex items-center justify-center text-xs font-bold text-white ${
                          index === 0 ? 'bg-gradient-to-br from-yellow-400 to-orange-500' :
                          index === 1 ? 'bg-gradient-to-br from-gray-300 to-gray-500' :
                          index === 2 ? 'bg-gradient-to-br from-orange-400 to-red-500' :
                          'bg-gradient-to-br from-blue-400 to-purple-500'
                        }`}>
                          {index + 1}
                        </div>
                      </div>
                      
                      {/* Post Card */}
                      <div className="ml-6">
                        <PostCard
                          post={post}
                        />
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="text-center py-12">
                  <TrendingUp className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                  <h3 className="text-lg font-medium text-gray-900 mb-2">No trending posts found</h3>
                  <p className="text-gray-500">
                    No posts are trending in the selected time period.
                  </p>
                </div>
              )}
            </div>

            {/* Footer Info */}
            <div className="px-6 py-4 border-t border-gray-200 bg-gray-50">
              <p className="text-xs text-gray-500 text-center">
                Trending posts are updated every {activePeriod === 'now' ? '30 seconds' : 'minute'} • 
                Last updated {new Date().toLocaleTimeString()}
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
