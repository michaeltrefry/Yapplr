'use client';

import { useState, useEffect } from 'react';
import { Post, PaginatedResult } from '@/types';
import PostCard from './PostCard';
import { useInView } from 'react-intersection-observer';

interface GroupTimelineProps {
  groupId: number;
  apiCall: (groupId: number, page: number, pageSize: number) => Promise<PaginatedResult<Post>>;
  emptyMessage?: string;
  emptySubMessage?: string;
  className?: string;
}

export default function GroupTimeline({ 
  groupId, 
  apiCall, 
  emptyMessage = "No posts found", 
  emptySubMessage,
  className = '' 
}: GroupTimelineProps) {
  const [posts, setPosts] = useState<Post[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hasMore, setHasMore] = useState(false);
  const [page, setPage] = useState(1);
  const pageSize = 20;

  const { ref, inView } = useInView({
    threshold: 0,
    triggerOnce: false,
  });

  const loadPosts = async (pageNum: number, reset = false) => {
    try {
      if (pageNum === 1) {
        setLoading(true);
        setError(null);
      } else {
        setLoadingMore(true);
      }

      const result = await apiCall(groupId, pageNum, pageSize);

      if (reset || pageNum === 1) {
        setPosts(result.items);
      } else {
        setPosts(prev => [...prev, ...result.items]);
      }

      setHasMore(result.hasNextPage);
      setPage(pageNum);
    } catch (err) {
      console.error('Failed to load posts:', err);
      setError('Failed to load posts. Please try again.');
    } finally {
      setLoading(false);
      setLoadingMore(false);
    }
  };

  // Load more when scrolling to bottom
  useEffect(() => {
    if (inView && hasMore && !loadingMore && !loading) {
      loadPosts(page + 1);
    }
  }, [inView, hasMore, loadingMore, loading, page]);

  // Initial load and reload when groupId changes
  useEffect(() => {
    setPage(1);
    loadPosts(1, true);
  }, [groupId]);

  const handlePostUpdate = (updatedPost: Post) => {
    setPosts(prev => 
      prev.map(post => 
        post.id === updatedPost.id ? updatedPost : post
      )
    );
  };

  const handlePostDelete = (deletedPostId: number) => {
    setPosts(prev => prev.filter(post => post.id !== deletedPostId));
  };

  if (loading && posts.length === 0) {
    return (
      <div className={`space-y-4 ${className}`}>
        {[...Array(3)].map((_, i) => (
          <div key={i} className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 animate-pulse">
            <div className="flex items-center space-x-3 mb-4">
              <div className="w-10 h-10 bg-gray-300 dark:bg-gray-700 rounded-full"></div>
              <div className="flex-1">
                <div className="h-4 bg-gray-300 dark:bg-gray-700 rounded mb-1 w-1/4"></div>
                <div className="h-3 bg-gray-300 dark:bg-gray-700 rounded w-1/6"></div>
              </div>
            </div>
            <div className="space-y-2">
              <div className="h-4 bg-gray-300 dark:bg-gray-700 rounded"></div>
              <div className="h-4 bg-gray-300 dark:bg-gray-700 rounded w-3/4"></div>
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <div className={`text-center py-8 ${className}`}>
        <div className="text-red-600 dark:text-red-400 mb-4">{error}</div>
        <button
          onClick={() => loadPosts(1, true)}
          className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
        >
          Try Again
        </button>
      </div>
    );
  }

  if (posts.length === 0) {
    return (
      <div className={`text-center py-12 bg-white dark:bg-gray-800 rounded-lg ${className}`}>
        <div className="text-gray-500 dark:text-gray-400 mb-2 text-lg">
          {emptyMessage}
        </div>
        {emptySubMessage && (
          <p className="text-sm text-gray-400 dark:text-gray-500">
            {emptySubMessage}
          </p>
        )}
      </div>
    );
  }

  return (
    <div className={`space-y-4 ${className}`}>
      {posts.map((post) => (
        <PostCard
          key={post.id}
          post={post}
          onPostUpdate={handlePostUpdate}
          onPostDelete={handlePostDelete}
        />
      ))}

      {/* Loading more indicator */}
      {hasMore && (
        <div ref={ref} className="py-4">
          {loadingMore ? (
            <div className="flex justify-center">
              <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600"></div>
            </div>
          ) : (
            <div className="h-4"></div>
          )}
        </div>
      )}

      {/* End of timeline indicator */}
      {!hasMore && posts.length > 0 && (
        <div className="text-center py-4 text-gray-500 dark:text-gray-400 text-sm">
          You've reached the end
        </div>
      )}
    </div>
  );
}
