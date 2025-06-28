'use client';

import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { blockApi } from '@/lib/api';
import { User } from '@/types';
import UserAvatar from './UserAvatar';
import { ShieldOff } from 'lucide-react';

export default function BlockedUsersList() {
  const [unblockingUserId, setUnblockingUserId] = useState<number | null>(null);
  const queryClient = useQueryClient();

  const { data: blockedUsers, isLoading, error } = useQuery({
    queryKey: ['blockedUsers'],
    queryFn: () => blockApi.getBlockedUsers(),
  });

  const unblockMutation = useMutation({
    mutationFn: (userId: number) => blockApi.unblockUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['blockedUsers'] });
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['following'] });
      setUnblockingUserId(null);
    },
    onError: () => {
      setUnblockingUserId(null);
    },
  });

  const handleUnblock = (user: User) => {
    setUnblockingUserId(user.id);
    unblockMutation.mutate(user.id);
  };

  if (isLoading) {
    return (
      <div className="space-y-4">
        {[...Array(3)].map((_, i) => (
          <div key={i} className="flex items-center justify-between p-4 bg-gray-50 rounded-lg animate-pulse">
            <div className="flex items-center space-x-3">
              <div className="w-12 h-12 bg-gray-300 rounded-full"></div>
              <div className="space-y-2">
                <div className="h-4 bg-gray-300 rounded w-24"></div>
                <div className="h-3 bg-gray-300 rounded w-32"></div>
              </div>
            </div>
            <div className="w-20 h-8 bg-gray-300 rounded"></div>
          </div>
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center py-8">
        <div className="text-red-500 mb-2">Failed to load blocked users</div>
        <button
          onClick={() => queryClient.invalidateQueries({ queryKey: ['blockedUsers'] })}
          className="text-blue-600 hover:text-blue-800 text-sm"
        >
          Try again
        </button>
      </div>
    );
  }

  if (!blockedUsers || blockedUsers.length === 0) {
    return (
      <div className="text-center py-12">
        <div className="text-gray-500 mb-2">No blocked users</div>
        <p className="text-sm text-gray-400">
          Users you block will appear here
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {blockedUsers.map((user) => (
        <div
          key={user.id}
          className="flex items-center justify-between p-4 bg-white border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
        >
          <div className="flex items-center space-x-3">
            <UserAvatar user={user} size="md" clickable={false} />
            <div>
              <h3 className="font-semibold text-gray-900">@{user.username}</h3>
              {user.bio && (
                <p className="text-sm text-gray-600 truncate max-w-xs">
                  {user.bio}
                </p>
              )}
            </div>
          </div>

          <button
            onClick={() => handleUnblock(user)}
            disabled={unblockingUserId === user.id}
            className="flex items-center space-x-2 px-4 py-2 bg-green-100 text-green-800 hover:bg-green-200 border border-green-300 rounded-full font-semibold transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            title="Unblock user"
          >
            {unblockingUserId === user.id ? (
              <span className="text-sm">Unblocking...</span>
            ) : (
              <>
                <ShieldOff className="w-4 h-4" />
                <span className="text-sm">Unblock</span>
              </>
            )}
          </button>
        </div>
      ))}
    </div>
  );
}
