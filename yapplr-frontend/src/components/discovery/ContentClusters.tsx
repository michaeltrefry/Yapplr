'use client';

import React, { useState, useEffect } from 'react';
import { exploreApi } from '@/lib/api';
import { ContentClusterDto, InterestBasedContentDto } from '@/types';
import PostCard from '@/components/PostCard';
import UserAvatar from '@/components/UserAvatar';
import Link from 'next/link';

interface ContentClustersProps {
  className?: string;
  limit?: number;
}

export default function ContentClusters({ 
  className = '', 
  limit = 5 
}: ContentClustersProps) {
  const [clusters, setClusters] = useState<ContentClusterDto[]>([]);
  const [interestContent, setInterestContent] = useState<InterestBasedContentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'clusters' | 'interests'>('clusters');
  const [selectedCluster, setSelectedCluster] = useState<ContentClusterDto | null>(null);

  useEffect(() => {
    loadContentData();
  }, [limit]);

  const loadContentData = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const [clustersData, interestsData] = await Promise.all([
        exploreApi.getContentClusters(limit),
        exploreApi.getInterestBasedContent(limit)
      ]);
      
      setClusters(clustersData);
      setInterestContent(interestsData);
    } catch (err) {
      console.error('Failed to load content data:', err);
      setError('Failed to load content data');
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className={`content-clusters ${className}`}>
        <div className="animate-pulse space-y-6">
          <div className="h-8 bg-gray-200 dark:bg-gray-700 rounded"></div>
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            {[...Array(4)].map((_, i) => (
              <div key={i} className="h-64 bg-gray-200 dark:bg-gray-700 rounded-lg"></div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={`content-clusters ${className}`}>
        <div className="text-center py-12">
          <div className="text-6xl mb-4">😕</div>
          <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
            Something went wrong
          </h3>
          <p className="text-gray-500 dark:text-gray-400 mb-4">{error}</p>
          <button 
            onClick={loadContentData}
            className="px-6 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className={`content-clusters ${className}`}>
      {/* Header */}
      <div className="mb-6">
        <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
          📝 Content Discovery
        </h3>
        <p className="text-gray-600 dark:text-gray-400">
          Explore content organized by topics and your interests
        </p>
      </div>

      {/* Tab Navigation */}
      <div className="mb-6">
        <div className="flex space-x-1 bg-gray-100 dark:bg-gray-800 rounded-lg p-1">
          <button
            onClick={() => setActiveTab('clusters')}
            className={`px-4 py-2 rounded-md font-medium transition-colors ${
              activeTab === 'clusters'
                ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
                : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
            }`}
          >
            🎯 Topic Clusters
          </button>
          <button
            onClick={() => setActiveTab('interests')}
            className={`px-4 py-2 rounded-md font-medium transition-colors ${
              activeTab === 'interests'
                ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
                : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
            }`}
          >
            ✨ Your Interests
          </button>
        </div>
      </div>

      {/* Content */}
      {activeTab === 'clusters' && (
        <div className="space-y-6">
          {clusters.length === 0 ? (
            <div className="text-center py-8">
              <div className="text-4xl mb-4">🎯</div>
              <p className="text-gray-500 dark:text-gray-400">
                No content clusters available at the moment
              </p>
            </div>
          ) : (
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {clusters.map((cluster) => (
                <div 
                  key={cluster.topic}
                  className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow cursor-pointer"
                  onClick={() => setSelectedCluster(cluster)}
                >
                  <div className="flex items-center justify-between mb-4">
                    <h4 className="text-lg font-semibold text-gray-900 dark:text-white">
                      {cluster.topic}
                    </h4>
                    <div className="flex items-center space-x-2">
                      <span className="text-sm text-gray-500 dark:text-gray-400">
                        Score: {cluster.clusterScore.toFixed(1)}
                      </span>
                      <div className="w-2 h-2 bg-green-500 rounded-full"></div>
                    </div>
                  </div>

                  <p className="text-gray-600 dark:text-gray-400 text-sm mb-4">
                    {cluster.description}
                  </p>

                  {/* Related Hashtags */}
                  <div className="flex flex-wrap gap-1 mb-4">
                    {cluster.relatedHashtags.slice(0, 3).map((hashtag) => (
                      <span 
                        key={hashtag.name}
                        className="px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 rounded-full text-xs"
                      >
                        #{hashtag.name}
                      </span>
                    ))}
                    {cluster.relatedHashtags.length > 3 && (
                      <span className="text-xs text-gray-500 dark:text-gray-400">
                        +{cluster.relatedHashtags.length - 3} more
                      </span>
                    )}
                  </div>

                  {/* Top Contributors */}
                  <div className="flex items-center justify-between mb-4">
                    <div className="flex -space-x-2">
                      {cluster.topContributors.slice(0, 3).map((user) => (
                        <UserAvatar 
                          key={user.id}
                          user={user} 
                          size="sm" 
                          className="border-2 border-white dark:border-gray-800"
                        />
                      ))}
                      {cluster.topContributors.length > 3 && (
                        <div className="w-8 h-8 bg-gray-200 dark:bg-gray-600 rounded-full flex items-center justify-center text-xs font-medium text-gray-600 dark:text-gray-400 border-2 border-white dark:border-gray-800">
                          +{cluster.topContributors.length - 3}
                        </div>
                      )}
                    </div>
                    <span className="text-sm text-gray-500 dark:text-gray-400">
                      {cluster.totalPosts} posts
                    </span>
                  </div>

                  {/* Sample Posts Preview */}
                  <div className="text-sm text-gray-500 dark:text-gray-400">
                    Latest: "{cluster.posts[0]?.content?.substring(0, 60)}..."
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {activeTab === 'interests' && (
        <div className="space-y-6">
          {interestContent.length === 0 ? (
            <div className="text-center py-8">
              <div className="text-4xl mb-4">✨</div>
              <p className="text-gray-500 dark:text-gray-400">
                No interest-based content available. Interact more to get personalized recommendations!
              </p>
            </div>
          ) : (
            <div className="space-y-6">
              {interestContent.map((interest) => (
                <div 
                  key={interest.interest}
                  className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6"
                >
                  <div className="flex items-center justify-between mb-4">
                    <div className="flex items-center space-x-3">
                      <h4 className="text-lg font-semibold text-gray-900 dark:text-white">
                        {interest.interest}
                      </h4>
                      {interest.isGrowing && (
                        <span className="px-2 py-1 bg-green-100 dark:bg-green-900/30 text-green-600 dark:text-green-400 rounded-full text-xs font-medium">
                          📈 Growing
                        </span>
                      )}
                    </div>
                    <div className="text-sm text-gray-500 dark:text-gray-400">
                      Strength: {(interest.interestStrength * 100).toFixed(0)}%
                    </div>
                  </div>

                  {/* Top Creators */}
                  <div className="mb-4">
                    <h5 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                      Top Creators
                    </h5>
                    <div className="flex space-x-3">
                      {interest.topCreators.slice(0, 5).map((creator) => (
                        <Link 
                          key={creator.id}
                          href={`/profile/${creator.username}`}
                          className="flex flex-col items-center space-y-1 hover:opacity-80"
                        >
                          <UserAvatar user={creator} size="sm" />
                          <span className="text-xs text-gray-600 dark:text-gray-400 truncate max-w-16">
                            {creator.username}
                          </span>
                        </Link>
                      ))}
                    </div>
                  </div>

                  {/* Recommended Posts */}
                  <div>
                    <h5 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">
                      Recommended Posts
                    </h5>
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                      {interest.recommendedPosts.slice(0, 3).map((post) => (
                        <PostCard key={post.id} post={post} compact />
                      ))}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Cluster Detail Modal */}
      {selectedCluster && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white dark:bg-gray-800 rounded-lg max-w-4xl w-full max-h-[90vh] overflow-y-auto">
            <div className="p-6">
              <div className="flex items-center justify-between mb-6">
                <h3 className="text-2xl font-bold text-gray-900 dark:text-white">
                  {selectedCluster.topic}
                </h3>
                <button
                  onClick={() => setSelectedCluster(null)}
                  className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                >
                  <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                  </svg>
                </button>
              </div>

              <p className="text-gray-600 dark:text-gray-400 mb-6">
                {selectedCluster.description}
              </p>

              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {selectedCluster.posts.map((post) => (
                  <PostCard key={post.id} post={post} compact />
                ))}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
