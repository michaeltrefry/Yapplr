'use client';

import { useState, useEffect } from 'react';
import { GroupList as GroupListType, PaginatedResult } from '@/types';
import { groupApi } from '@/lib/api';
import GroupCard from './GroupCard';
import { useInView } from 'react-intersection-observer';

interface GroupListProps {
  searchQuery?: string;
  showMyGroups?: boolean;
  userId?: number;
  className?: string;
}

export default function GroupList({ searchQuery, showMyGroups = false, userId, className = '' }: GroupListProps) {
  // Initialize with empty array to prevent undefined access
  const [groups, setGroups] = useState<GroupListType[]>([]);
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

  const loadGroups = async (pageNum: number, reset = false) => {
    try {
      if (pageNum === 1) {
        setLoading(true);
        setError(null);
      } else {
        setLoadingMore(true);
      }

      let result: PaginatedResult<GroupListType>;

      if (searchQuery) {
        result = await groupApi.searchGroups(searchQuery, pageNum, pageSize);
      } else if (showMyGroups) {
        result = await groupApi.getMyGroups(pageNum, pageSize);
      } else if (userId) {
        result = await groupApi.getUserGroups(userId, pageNum, pageSize);
      } else {
        result = await groupApi.getGroups(pageNum, pageSize);
      }

      // Ensure result has the expected structure
      if (!result || typeof result !== 'object' || !Array.isArray(result.items)) {
        console.error('GroupList: Invalid API response structure', result);
        throw new Error('Invalid API response structure');
      }

      if (reset || pageNum === 1) {
        setGroups(result.items || []);
      } else {
        setGroups(prev => [...(prev || []), ...(result.items || [])]);
      }

      setHasMore(result.hasNextPage || false);
      setPage(pageNum);
    } catch (err) {
      console.error('Failed to load groups:', err);
      setError('Failed to load groups. Please try again.');
    } finally {
      setLoading(false);
      setLoadingMore(false);
    }
  };

  // Load more when scrolling to bottom
  useEffect(() => {
    if (inView && hasMore && !loadingMore && !loading) {
      loadGroups(page + 1);
    }
  }, [inView, hasMore, loadingMore, loading, page]);

  // Reset and reload when search query or filters change
  useEffect(() => {
    setPage(1);
    loadGroups(1, true);
  }, [searchQuery, showMyGroups, userId]);

  const handleGroupUpdate = (updatedGroup: GroupListType | any) => {
    setGroups(prev =>
      prev.map(group =>
        group.id === updatedGroup.id ? { ...group, ...updatedGroup } : group
      )
    );
  };

  if (loading && groups.length === 0) {
    return (
      <div className={`space-y-4 ${className}`}>
        {[...Array(6)].map((_, i) => (
          <div key={i} className="bg-white dark:bg-gray-800 rounded-lg shadow-md overflow-hidden animate-pulse">
            <div className="h-32 bg-gray-300 dark:bg-gray-700"></div>
            <div className="p-4">
              <div className="h-6 bg-gray-300 dark:bg-gray-700 rounded mb-2"></div>
              <div className="h-4 bg-gray-300 dark:bg-gray-700 rounded mb-3 w-3/4"></div>
              <div className="flex justify-between">
                <div className="h-4 bg-gray-300 dark:bg-gray-700 rounded w-1/3"></div>
                <div className="h-4 bg-gray-300 dark:bg-gray-700 rounded w-1/4"></div>
              </div>
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
          onClick={() => loadGroups(1, true)}
          className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
        >
          Try Again
        </button>
      </div>
    );
  }

  // Ensure groups is always an array and handle edge cases
  if (!groups || typeof groups !== 'object') {
    return (
      <div className={`text-center py-8 ${className}`}>
        <div className="text-red-500 dark:text-red-400">
          Error loading groups. Please refresh the page.
        </div>
      </div>
    );
  }

  const safeGroups = Array.isArray(groups) ? groups : [];

  if (safeGroups.length === 0) {
    return (
      <div className={`text-center py-8 ${className}`}>
        <div className="text-gray-500 dark:text-gray-400 mb-4">
          {searchQuery ? (
            `No groups found for "${searchQuery}"`
          ) : showMyGroups ? (
            "You haven't joined any groups yet"
          ) : userId ? (
            "This user hasn't joined any groups yet"
          ) : (
            "No groups found"
          )}
        </div>
        {!searchQuery && !userId && (
          <p className="text-sm text-gray-400 dark:text-gray-500">
            Be the first to create a group!
          </p>
        )}
      </div>
    );
  }

  return (
    <div className={`space-y-4 ${className}`}>
      {safeGroups.map((group) => (
        <GroupCard
          key={group.id}
          group={group}
          onGroupUpdate={handleGroupUpdate}
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

      {/* End of list indicator */}
      {!hasMore && groups.length > 0 && (
        <div className="text-center py-4 text-gray-500 dark:text-gray-400 text-sm">
          You've reached the end of the list
        </div>
      )}
    </div>
  );
}
