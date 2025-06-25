'use client';

import { useQuery } from '@tanstack/react-query';
import { userApi } from '@/lib/api';
import Link from 'next/link';
import UserAvatar from './UserAvatar';

export default function FollowingList() {
  const { data: following, isLoading } = useQuery({
    queryKey: ['following'],
    queryFn: () => userApi.getFollowing(),
  });

  if (isLoading) {
    return (
      <div className="px-3 py-2">
        <div className="text-sm text-gray-500">Loading...</div>
      </div>
    );
  }

  if (!following || following.length === 0) {
    return (
      <div className="px-3 py-2">
        <div className="text-sm text-gray-500">No following yet</div>
      </div>
    );
  }

  return (
    <div className="space-y-1">
      {following.map((user) => (
        <Link
          key={user.id}
          href={`/profile/${user.username}`}
          className="flex items-center space-x-3 px-3 py-2 rounded-lg hover:bg-gray-100 transition-colors"
        >
          <UserAvatar user={user} size="sm" clickable={false} />
          <div className="flex-1 min-w-0 hidden lg:block">
            <p className="text-sm font-medium text-gray-900 truncate">
              {user.username}
            </p>
          </div>
        </Link>
      ))}
    </div>
  );
}
