'use client';

import { useState } from 'react';
import Link from 'next/link';
import { GroupList, Group } from '@/types';
import { groupApi } from '@/lib/api';
import { useAuth } from '@/contexts/AuthContext';

interface GroupCardProps {
  group: GroupList | Group;
  showJoinButton?: boolean;
  onGroupUpdate?: (updatedGroup: GroupList | Group) => void;
}

export default function GroupCard({ group, showJoinButton = true, onGroupUpdate }: GroupCardProps) {
  const { user } = useAuth();
  const [isJoining, setIsJoining] = useState(false);
  const [isLeaving, setIsLeaving] = useState(false);
  const [isMember, setIsMember] = useState(group.isCurrentUserMember);
  const [memberCount, setMemberCount] = useState(group.memberCount);

  const handleJoinGroup = async () => {
    if (!user) return;
    
    setIsJoining(true);
    try {
      await groupApi.joinGroup(group.id);
      setIsMember(true);
      setMemberCount(prev => prev + 1);
      
      const updatedGroup = { ...group, isCurrentUserMember: true, memberCount: memberCount + 1 };
      onGroupUpdate?.(updatedGroup);
    } catch (error) {
      console.error('Failed to join group:', error);
    } finally {
      setIsJoining(false);
    }
  };

  const handleLeaveGroup = async () => {
    if (!user) return;
    
    setIsLeaving(true);
    try {
      await groupApi.leaveGroup(group.id);
      setIsMember(false);
      setMemberCount(prev => prev - 1);
      
      const updatedGroup = { ...group, isCurrentUserMember: false, memberCount: memberCount - 1 };
      onGroupUpdate?.(updatedGroup);
    } catch (error) {
      console.error('Failed to leave group:', error);
    } finally {
      setIsLeaving(false);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  const getImageUrl = (imageFileName?: string) => {
    if (!imageFileName) return '';
    const baseUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:8080';
    return `${baseUrl}/api/images/${imageFileName}`;
  };

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md hover:shadow-lg transition-shadow duration-200 overflow-hidden">
      {/* Group Image */}
      <div className="h-32 bg-gradient-to-r from-blue-500 to-purple-600 relative">
        {group.imageFileName ? (
          <img
            src={getImageUrl(group.imageFileName)}
            alt={group.name}
            className="w-full h-full object-cover"
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center">
            <div className="text-white text-2xl font-bold">
              {group.name.charAt(0).toUpperCase()}
            </div>
          </div>
        )}
      </div>

      {/* Group Info */}
      <div className="p-4">
        <div className="flex items-start justify-between mb-2">
          <Link 
            href={`/groups/${group.id}`}
            className="text-lg font-semibold text-gray-900 dark:text-white hover:text-blue-600 dark:hover:text-blue-400 transition-colors"
          >
            {group.name}
          </Link>
          
          {showJoinButton && user && (
            <div className="ml-2">
              {isMember ? (
                <button
                  onClick={handleLeaveGroup}
                  disabled={isLeaving}
                  className="px-3 py-1 text-sm bg-red-100 text-red-700 hover:bg-red-200 dark:bg-red-900 dark:text-red-300 dark:hover:bg-red-800 rounded-md transition-colors disabled:opacity-50"
                >
                  {isLeaving ? 'Leaving...' : 'Leave'}
                </button>
              ) : (
                <button
                  onClick={handleJoinGroup}
                  disabled={isJoining}
                  className="px-3 py-1 text-sm bg-blue-100 text-blue-700 hover:bg-blue-200 dark:bg-blue-900 dark:text-blue-300 dark:hover:bg-blue-800 rounded-md transition-colors disabled:opacity-50"
                >
                  {isJoining ? 'Joining...' : 'Join'}
                </button>
              )}
            </div>
          )}
        </div>

        {/* Description */}
        {group.description && (
          <p className="text-gray-600 dark:text-gray-300 text-sm mb-3 line-clamp-2">
            {group.description}
          </p>
        )}

        {/* Stats */}
        <div className="flex items-center justify-between text-sm text-gray-500 dark:text-gray-400">
          <div className="flex items-center space-x-4">
            <span>{memberCount} {memberCount === 1 ? 'member' : 'members'}</span>
            <span>{group.postCount} {group.postCount === 1 ? 'post' : 'posts'}</span>
          </div>
          
          <div className="text-xs">
            {'creatorUsername' in group ? (
              <span>by @{group.creatorUsername}</span>
            ) : (
              <span>by @{group.user.username}</span>
            )}
          </div>
        </div>

        {/* Created Date */}
        <div className="mt-2 text-xs text-gray-400 dark:text-gray-500">
          Created {formatDate(group.createdAt)}
        </div>
      </div>
    </div>
  );
}
