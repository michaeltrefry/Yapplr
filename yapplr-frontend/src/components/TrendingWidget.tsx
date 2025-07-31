'use client';

import { useQuery } from '@tanstack/react-query';
import { tagApi } from '@/lib/api';
import { TrendingUp, Hash, ExternalLink } from 'lucide-react';
import Link from 'next/link';
import { formatNumber } from '@/lib/utils';

interface TrendingWidgetProps {
  limit?: number;
  showViewAll?: boolean;
  className?: string;
}

export default function TrendingWidget({ 
  limit = 5, 
  showViewAll = true, 
  className = '' 
}: TrendingWidgetProps) {
  const { data: trendingTags, isLoading } = useQuery({
    queryKey: ['trending-tags-widget', limit],
    queryFn: () => tagApi.getTrendingTags(limit),
    refetchInterval: 60000, // Refresh every minute
  });

  if (isLoading) {
    return (
      <div className={`bg-white rounded-lg border border-gray-200 p-4 ${className}`}>
        <div className="flex items-center space-x-2 mb-4">
          <TrendingUp className="w-5 h-5 text-orange-500" />
          <h3 className="font-bold text-gray-900">Trending</h3>
        </div>
        <div className="space-y-3">
          {Array.from({ length: limit }).map((_, i) => (
            <div key={i} className="flex items-center space-x-3">
              <div className="w-8 h-8 bg-gray-200 rounded-full animate-pulse"></div>
              <div className="flex-1 space-y-1">
                <div className="h-4 bg-gray-200 rounded w-2/3 animate-pulse"></div>
                <div className="h-3 bg-gray-200 rounded w-1/3 animate-pulse"></div>
              </div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (!trendingTags || trendingTags.length === 0) {
    return (
      <div className={`bg-white rounded-lg border border-gray-200 p-4 ${className}`}>
        <div className="flex items-center space-x-2 mb-4">
          <TrendingUp className="w-5 h-5 text-orange-500" />
          <h3 className="font-bold text-gray-900">Trending</h3>
        </div>
        <p className="text-gray-500 text-sm text-center py-4">
          No trending hashtags yet
        </p>
      </div>
    );
  }

  return (
    <div className={`bg-white rounded-lg border border-gray-200 p-4 ${className}`}>
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center space-x-2">
          <TrendingUp className="w-5 h-5 text-orange-500" />
          <h3 className="font-bold text-gray-900">Trending</h3>
        </div>
        {showViewAll && (
          <Link
            href="/trending"
            className="text-blue-600 hover:text-blue-800 text-sm font-medium flex items-center space-x-1"
          >
            <span>Posts & More</span>
            <ExternalLink className="w-3 h-3" />
          </Link>
        )}
      </div>

      <div className="space-y-3">
        {trendingTags.slice(0, limit).map((tag, index) => (
          <Link
            key={tag.id}
            href={`/hashtag/${tag.name}`}
            className="flex items-center space-x-3 p-2 rounded-lg hover:bg-gray-50 transition-colors group"
          >
            {/* Rank */}
            <div className="flex-shrink-0 w-6 text-center">
              <span className={`text-sm font-bold ${
                index < 3 ? 'text-orange-500' : 'text-gray-400'
              }`}>
                {index + 1}
              </span>
            </div>

            {/* Hashtag Icon */}
            <div className={`w-8 h-8 rounded-full flex items-center justify-center ${
              index === 0 ? 'bg-gradient-to-br from-orange-400 to-red-500' :
              index === 1 ? 'bg-gradient-to-br from-yellow-400 to-orange-500' :
              index === 2 ? 'bg-gradient-to-br from-green-400 to-blue-500' :
              'bg-gradient-to-br from-blue-400 to-purple-500'
            }`}>
              <Hash className="w-4 h-4 text-white" />
            </div>

            {/* Hashtag Info */}
            <div className="flex-1 min-w-0">
              <p className="font-medium text-gray-900 group-hover:text-blue-600 transition-colors">
                #{tag.name}
              </p>
              <p className="text-xs text-gray-500">
                {formatNumber(tag.postCount || 0)} {(tag.postCount || 0) === 1 ? 'post' : 'posts'}
              </p>
            </div>

            {/* Trending Indicator */}
            {index < 3 && (
              <div className="flex-shrink-0">
                <TrendingUp className="w-3 h-3 text-orange-500" />
              </div>
            )}
          </Link>
        ))}
      </div>

      {showViewAll && (
        <div className="mt-4 pt-3 border-t border-gray-100 space-y-2">
          <Link
            href="/trending"
            className="block text-center text-blue-600 hover:text-blue-800 text-sm font-medium"
          >
            View all trending content →
          </Link>
          <p className="text-xs text-gray-500 text-center">
            Hashtags • Posts • Hot topics
          </p>
        </div>
      )}

      <div className="mt-3 pt-3 border-t border-gray-100">
        <p className="text-xs text-gray-400 text-center">
          Updated {new Date().toLocaleTimeString()}
        </p>
      </div>
    </div>
  );
}
