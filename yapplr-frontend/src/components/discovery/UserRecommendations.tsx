'use client';

import React, { useState, useEffect } from 'react';
import { exploreApi } from '@/lib/api';
import { UserRecommendationDto } from '@/types';
import UserAvatar from '@/components/UserAvatar';
import Link from 'next/link';

interface UserRecommendationsProps {
  users?: UserRecommendationDto[];
  compact?: boolean;
  className?: string;
  limit?: number;
}

export default function UserRecommendations({ 
  users: initialUsers, 
  compact = false, 
  className = '',
  limit = 10
}: UserRecommendationsProps) {
  const [users, setUsers] = useState<UserRecommendationDto[]>(initialUsers || []);
  const [loading, setLoading] = useState(!initialUsers);
  const [error, setError] = useState<string | null>(null);
  const [followingStates, setFollowingStates] = useState<Record<number, boolean>>({});

  useEffect(() => {
    if (!initialUsers) {
      loadUserRecommendations();
    }
  }, [initialUsers, limit]);

  const loadUserRecommendations = async () => {
    try {
      setLoading(true);
      setError(null);
      const recommendations = await exploreApi.getUserRecommendations(limit, 0.1);
      setUsers(recommendations);
    } catch (err) {
      console.error('Failed to load user recommendations:', err);
      setError('Failed to load user recommendations');
    } finally {
      setLoading(false);
    }
  };

  const handleFollow = async (userId: number) => {
    try {
      // This would call the follow API
      // await userApi.followUser(userId);
      setFollowingStates(prev => ({ ...prev, [userId]: true }));
    } catch (err) {
      console.error('Failed to follow user:', err);
    }
  };

  const getSimilarityColor = (score: number) => {
    if (score > 0.8) return 'text-green-500';
    if (score > 0.6) return 'text-blue-500';
    if (score > 0.4) return 'text-yellow-500';
    return 'text-gray-500';
  };

  const getSimilarityLabel = (score: number) => {
    if (score > 0.8) return 'Excellent match';
    if (score > 0.6) return 'Good match';
    if (score > 0.4) return 'Fair match';
    return 'Low match';
  };

  if (loading) {
    return (
      <div className={`user-recommendations ${className}`}>
        <div className="animate-pulse space-y-4">
          {[...Array(compact ? 3 : 6)].map((_, i) => (
            <div key={i} className="flex items-center space-x-4 p-4 bg-gray-100 dark:bg-gray-700 rounded-lg">
              <div className="w-12 h-12 bg-gray-200 dark:bg-gray-600 rounded-full"></div>
              <div className="flex-1">
                <div className="h-4 bg-gray-200 dark:bg-gray-600 rounded mb-2"></div>
                <div className="h-3 bg-gray-200 dark:bg-gray-600 rounded w-2/3"></div>
              </div>
              <div className="w-20 h-8 bg-gray-200 dark:bg-gray-600 rounded"></div>
            </div>
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={`user-recommendations ${className}`}>
        <div className="text-center py-8">
          <p className="text-red-500 mb-4">{error}</p>
          <button 
            onClick={loadUserRecommendations}
            className="px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  if (users.length === 0) {
    return (
      <div className={`user-recommendations ${className}`}>
        <div className="text-center py-8">
          <div className="text-4xl mb-4">👥</div>
          <p className="text-gray-500 dark:text-gray-400">
            No user recommendations available at the moment
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className={`user-recommendations ${className}`}>
      {!compact && (
        <div className="mb-6">
          <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
            👥 People You Might Like
          </h3>
          <p className="text-gray-600 dark:text-gray-400">
            Discover users with similar interests and connections
          </p>
        </div>
      )}

      <div className={`space-y-4 ${compact ? '' : 'max-h-96 overflow-y-auto'}`}>
        {users.map((recommendation) => (
          <div 
            key={recommendation.user.id}
            className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700 hover:shadow-md transition-shadow"
          >
            <div className="flex items-start space-x-4">
              <Link href={`/profile/${recommendation.user.username}`}>
                <UserAvatar 
                  user={recommendation.user} 
                  size={compact ? 'sm' : 'md'} 
                  className="cursor-pointer"
                />
              </Link>

              <div className="flex-1 min-w-0">
                <div className="flex items-center justify-between mb-2">
                  <Link 
                    href={`/profile/${recommendation.user.username}`}
                    className="font-semibold text-gray-900 dark:text-white hover:text-blue-500 truncate"
                  >
                    {recommendation.user.username}
                  </Link>
                  
                  {!compact && (
                    <div className={`text-sm font-medium ${getSimilarityColor(recommendation.similarityScore)}`}>
                      {(recommendation.similarityScore * 100).toFixed(0)}% match
                    </div>
                  )}
                </div>

                {recommendation.user.bio && (
                  <p className="text-sm text-gray-600 dark:text-gray-400 mb-2 line-clamp-2">
                    {recommendation.user.bio}
                  </p>
                )}

                {!compact && (
                  <div className="mb-3">
                    <p className="text-sm text-gray-500 dark:text-gray-400 mb-2">
                      {recommendation.recommendationReason}
                    </p>
                    
                    {recommendation.commonInterests.length > 0 && (
                      <div className="flex flex-wrap gap-1 mb-2">
                        {recommendation.commonInterests.slice(0, 3).map((interest) => (
                          <span 
                            key={interest}
                            className="px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 rounded-full text-xs"
                          >
                            #{interest}
                          </span>
                        ))}
                        {recommendation.commonInterests.length > 3 && (
                          <span className="text-xs text-gray-500 dark:text-gray-400">
                            +{recommendation.commonInterests.length - 3} more
                          </span>
                        )}
                      </div>
                    )}

                    {recommendation.mutualFollows.length > 0 && (
                      <div className="text-xs text-gray-500 dark:text-gray-400">
                        Followed by {recommendation.mutualFollows[0].username}
                        {recommendation.mutualFollows.length > 1 && (
                          <span> and {recommendation.mutualFollows.length - 1} others</span>
                        )}
                      </div>
                    )}
                  </div>
                )}

                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-4 text-xs text-gray-500 dark:text-gray-400">
                    {recommendation.isNewUser && (
                      <span className="px-2 py-1 bg-green-100 dark:bg-green-900/30 text-green-600 dark:text-green-400 rounded-full">
                        New
                      </span>
                    )}
                    <span>Activity: {(recommendation.activityScore * 100).toFixed(0)}%</span>
                  </div>

                  <button
                    onClick={() => handleFollow(recommendation.user.id)}
                    disabled={followingStates[recommendation.user.id]}
                    className={`px-4 py-1 rounded-full text-sm font-medium transition-colors ${
                      followingStates[recommendation.user.id]
                        ? 'bg-gray-200 dark:bg-gray-600 text-gray-500 dark:text-gray-400 cursor-not-allowed'
                        : 'bg-blue-500 text-white hover:bg-blue-600'
                    }`}
                  >
                    {followingStates[recommendation.user.id] ? 'Following' : 'Follow'}
                  </button>
                </div>
              </div>
            </div>
          </div>
        ))}
      </div>

      {!compact && users.length > 0 && (
        <div className="mt-6 text-center">
          <button 
            onClick={loadUserRecommendations}
            className="px-6 py-2 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg hover:bg-gray-300 dark:hover:bg-gray-600 transition-colors"
          >
            Load More Recommendations
          </button>
        </div>
      )}
    </div>
  );
}
