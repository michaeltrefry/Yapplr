'use client';

import { useState } from 'react';
import Link from 'next/link';
import { Group, GroupMemberRole } from '@/types';
import { groupApi } from '@/lib/api';
import { useAuth } from '@/contexts/AuthContext';
import { Settings, Users, Calendar, Edit } from 'lucide-react';

interface GroupHeaderProps {
  group: Group;
  onGroupUpdate?: (updatedGroup: Group) => void;
  onEditClick?: () => void;
}

export default function GroupHeader({ group, onGroupUpdate, onEditClick }: GroupHeaderProps) {
  const { user } = useAuth();
  const [isJoining, setIsJoining] = useState(false);
  const [isLeaving, setIsLeaving] = useState(false);
  const [isMember, setIsMember] = useState(group.isCurrentUserMember);
  const [memberCount, setMemberCount] = useState(group.memberCount);

  const isOwner = user?.id === group.user.id;

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
      month: 'long',
      day: 'numeric'
    });
  };

  const getImageUrl = (imageFileName?: string) => {
    if (!imageFileName) return '';
    const baseUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5161';
    return `${baseUrl}/api/images/${imageFileName}`;
  };

  return (
    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md overflow-hidden">
      {/* Cover Image */}
      <div className="h-48 bg-gradient-to-r from-blue-500 to-purple-600 relative">
        {group.imageFileName ? (
          <img
            src={getImageUrl(group.imageFileName)}
            alt={group.name}
            className="w-full h-full object-cover"
          />
        ) : (
          <div className="w-full h-full flex items-center justify-center">
            <div className="text-white text-6xl font-bold">
              {group.name.charAt(0).toUpperCase()}
            </div>
          </div>
        )}
        
        {/* Overlay gradient */}
        <div className="absolute inset-0 bg-gradient-to-t from-black/50 to-transparent"></div>
      </div>

      {/* Group Info */}
      <div className="p-6">
        <div className="flex items-start justify-between mb-4">
          <div className="flex-1">
            <h1 className="text-2xl font-bold text-gray-900 dark:text-white mb-2">
              {group.name}
            </h1>
            
            {group.description && (
              <p className="text-gray-600 dark:text-gray-300 mb-4">
                {group.description}
              </p>
            )}

            {/* Group Stats */}
            <div className="flex items-center space-x-6 text-sm text-gray-500 dark:text-gray-400">
              <div className="flex items-center space-x-1">
                <Users size={16} />
                <span>{memberCount} {memberCount === 1 ? 'member' : 'members'}</span>
              </div>
              
              <div className="flex items-center space-x-1">
                <Calendar size={16} />
                <span>Created {formatDate(group.createdAt)}</span>
              </div>
              
              <div>
                <span>{group.postCount} {group.postCount === 1 ? 'post' : 'posts'}</span>
              </div>
            </div>

            {/* Group Owner */}
            <div className="mt-3 text-sm text-gray-600 dark:text-gray-300">
              Created by{' '}
              <Link 
                href={`/profile/${group.user.username}`}
                className="font-medium text-blue-600 dark:text-blue-400 hover:underline"
              >
                @{group.user.username}
              </Link>
            </div>
          </div>

          {/* Action Buttons */}
          <div className="flex items-center space-x-3 ml-4">
            {user && (
              <>
                {isOwner ? (
                  <button
                    onClick={onEditClick}
                    className="flex items-center space-x-2 px-4 py-2 bg-gray-100 hover:bg-gray-200 dark:bg-gray-700 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-300 rounded-md transition-colors"
                  >
                    <Edit size={16} />
                    <span>Edit Group</span>
                  </button>
                ) : isMember ? (
                  <button
                    onClick={handleLeaveGroup}
                    disabled={isLeaving}
                    className="px-4 py-2 bg-red-100 text-red-700 hover:bg-red-200 dark:bg-red-900 dark:text-red-300 dark:hover:bg-red-800 rounded-md transition-colors disabled:opacity-50"
                  >
                    {isLeaving ? 'Leaving...' : 'Leave Group'}
                  </button>
                ) : (
                  <button
                    onClick={handleJoinGroup}
                    disabled={isJoining}
                    className="px-4 py-2 bg-blue-600 text-white hover:bg-blue-700 rounded-md transition-colors disabled:opacity-50"
                  >
                    {isJoining ? 'Joining...' : 'Join Group'}
                  </button>
                )}
              </>
            )}
          </div>
        </div>

        {/* Group Status */}
        <div className="flex items-center justify-between pt-4 border-t border-gray-200 dark:border-gray-700">
          <div className="flex items-center space-x-2">
            <div className="w-3 h-3 bg-green-500 rounded-full"></div>
            <span className="text-sm text-gray-600 dark:text-gray-300">
              {group.isOpen ? 'Open Group' : 'Closed Group'}
            </span>
            <span className="text-xs text-gray-500 dark:text-gray-400">
              • Anyone can join
            </span>
          </div>

          {isMember && (
            <div className="text-sm text-green-600 dark:text-green-400 font-medium">
              ✓ Member
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
