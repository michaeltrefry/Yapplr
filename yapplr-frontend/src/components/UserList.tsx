'use client';

import { useQuery } from '@tanstack/react-query';
import { userApi } from '@/lib/api';
import UserAvatar from './UserAvatar';

import { useRouter } from 'next/navigation';

interface UserListProps {
  userId: number;
  type: 'following' | 'followers';
}

export default function UserList({ userId, type }: UserListProps) {
  const router = useRouter();

  const { data: users, isLoading, error } = useQuery({
    queryKey: ['userList', userId, type],
    queryFn: () => type === 'following' ? userApi.getUserFollowing(userId) : userApi.getUserFollowers(userId),
  });

  const handleUserClick = (username: string) => {
    router.push(`/profile/${username}`);
  };

  if (isLoading) {
    return (
      <div className="p-8 text-center">
        <div className="text-gray-500">Loading...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-8 text-center">
        <div className="text-red-500">Failed to load {type}</div>
      </div>
    );
  }

  if (!users || users.length === 0) {
    return (
      <div className="p-8 text-center">
        <div className="text-gray-500">
          {type === 'following' ? 'Not following anyone yet' : 'No followers yet'}
        </div>
      </div>
    );
  }

  return (
    <div className="divide-y divide-gray-200">
      {users.map((user) => (
        <div
          key={user.id}
          onClick={() => handleUserClick(user.username)}
          className="flex items-center gap-4 p-4 hover:bg-gray-50 cursor-pointer transition-colors"
        >
          <UserAvatar
            user={user}
            size="md"
            clickable={false}
          />
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2">
              <h3 className="font-semibold text-gray-900 truncate">
                @{user.username}
                {user.pronouns && (
                  <span className="text-sm text-gray-500 font-normal"> ({user.pronouns})</span>
                )}
              </h3>
            </div>
            {user.bio && (
              <p className="text-sm text-gray-700 mt-1 truncate">{user.bio}</p>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}
