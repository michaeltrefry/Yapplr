'use client';

import { useState, useEffect } from 'react';
import Link from 'next/link';
import { GroupMember, PaginatedResult, GroupMemberRole } from '@/types';
import { groupApi } from '@/lib/api';
import { useInView } from 'react-intersection-observer';
import UserAvatar from './UserAvatar';

interface GroupMembersListProps {
  groupId: number;
  className?: string;
}

export default function GroupMembersList({ groupId, className = '' }: GroupMembersListProps) {
  const [members, setMembers] = useState<GroupMember[]>([]);
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

  const loadMembers = async (pageNum: number, reset = false) => {
    try {
      if (pageNum === 1) {
        setLoading(true);
        setError(null);
      } else {
        setLoadingMore(true);
      }

      const result: PaginatedResult<GroupMember> = await groupApi.getGroupMembers(groupId, pageNum, pageSize);

      // Ensure result has the expected structure
      if (!result || typeof result !== 'object' || !Array.isArray(result.items)) {
        throw new Error('Invalid API response structure');
      }

      if (reset || pageNum === 1) {
        setMembers(result.items || []);
      } else {
        setMembers(prev => [...prev, ...(result.items || [])]);
      }

      setHasMore(result.hasNextPage);
      setPage(pageNum);
    } catch (err) {
      console.error('Failed to load group members:', err);
      setError('Failed to load group members. Please try again.');
    } finally {
      setLoading(false);
      setLoadingMore(false);
    }
  };

  // Load more when scrolling to bottom
  useEffect(() => {
    if (inView && hasMore && !loadingMore && !loading) {
      loadMembers(page + 1);
    }
  }, [inView, hasMore, loadingMore, loading, page]);

  // Initial load
  useEffect(() => {
    loadMembers(1, true);
  }, [groupId]);

  const getRoleBadge = (role: GroupMemberRole) => {
    switch (role) {
      case GroupMemberRole.Admin:
        return (
          <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200">
            Admin
          </span>
        );
      case GroupMemberRole.Moderator:
        return (
          <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200">
            Moderator
          </span>
        );
      default:
        return null;
    }
  };

  const formatJoinDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  if (loading && members.length === 0) {
    return (
      <div className={`space-y-3 ${className}`}>
        {[...Array(8)].map((_, i) => (
          <div key={i} className="flex items-center space-x-3 p-3 bg-white dark:bg-gray-800 rounded-lg animate-pulse">
            <div className="w-10 h-10 bg-gray-300 dark:bg-gray-700 rounded-full"></div>
            <div className="flex-1">
              <div className="h-4 bg-gray-300 dark:bg-gray-700 rounded mb-1 w-1/3"></div>
              <div className="h-3 bg-gray-300 dark:bg-gray-700 rounded w-1/4"></div>
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
          onClick={() => loadMembers(1, true)}
          className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
        >
          Try Again
        </button>
      </div>
    );
  }

  if (members.length === 0) {
    return (
      <div className={`text-center py-8 ${className}`}>
        <div className="text-gray-500 dark:text-gray-400">
          No members found
        </div>
      </div>
    );
  }

  return (
    <div className={`space-y-3 ${className}`}>
      {members.map((member) => (
        <div key={member.id} className="flex items-center justify-between p-3 bg-white dark:bg-gray-800 rounded-lg shadow-sm hover:shadow-md transition-shadow">
          <div className="flex items-center space-x-3">
            <UserAvatar 
              user={member.user} 
              size="md"
            />
            <div>
              <div className="flex items-center space-x-2">
                <Link 
                  href={`/profile/${member.user.username}`}
                  className="font-medium text-gray-900 dark:text-white hover:text-blue-600 dark:hover:text-blue-400 transition-colors"
                >
                  {member.user.username}
                </Link>
                {getRoleBadge(member.role)}
              </div>
              <div className="text-sm text-gray-500 dark:text-gray-400">
                Joined {formatJoinDate(member.joinedAt)}
              </div>
            </div>
          </div>

          {/* User bio if available */}
          {member.user.bio && (
            <div className="hidden md:block max-w-xs">
              <p className="text-sm text-gray-600 dark:text-gray-300 truncate">
                {member.user.bio}
              </p>
            </div>
          )}
        </div>
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
      {!hasMore && members.length > 0 && (
        <div className="text-center py-4 text-gray-500 dark:text-gray-400 text-sm">
          All members loaded
        </div>
      )}
    </div>
  );
}
