'use client';

import { useState, useEffect, use } from 'react';
import { Group, Post } from '@/types';
import { groupApi, postApi } from '@/lib/api';
import { useAuth } from '@/contexts/AuthContext';
import GroupHeader from '@/components/GroupHeader';
import GroupMembersList from '@/components/GroupMembersList';
import EditGroupModal from '@/components/EditGroupModal';
import GroupTimeline from '@/components/GroupTimeline';
import CreatePost from '@/components/CreatePost';
import Sidebar from '@/components/Sidebar';
import { MessageSquare, Users, FileText } from 'lucide-react';

interface GroupPageProps {
  params: Promise<{
    groupId: string;
  }>;
}

export default function GroupPage({ params }: GroupPageProps) {
  const { groupId } = use(params);
  const groupIdNum = parseInt(groupId);
  const { user } = useAuth();
  
  const [group, setGroup] = useState<Group | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'posts' | 'members'>('posts');
  const [showEditModal, setShowEditModal] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);

  const loadGroup = async () => {
    try {
      setLoading(true);
      setError(null);
      const groupData = await groupApi.getGroup(groupIdNum);
      setGroup(groupData);
    } catch (err: any) {
      console.error('Failed to load group:', err);
      if (err.response?.status === 404) {
        setError('Group not found');
      } else {
        setError('Failed to load group. Please try again.');
      }
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (groupIdNum) {
      loadGroup();
    }
  }, [groupIdNum]);

  const handleGroupUpdate = (updatedGroup: Group) => {
    setGroup(updatedGroup);
  };

  const handlePostCreated = () => {
    setRefreshKey(prev => prev + 1);
    // Refresh group data to update post count
    loadGroup();
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
        <div className="max-w-4xl mx-auto px-4 py-8">
          {/* Header Skeleton */}
          <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md overflow-hidden animate-pulse mb-6">
            <div className="h-48 bg-gray-300 dark:bg-gray-700"></div>
            <div className="p-6">
              <div className="h-8 bg-gray-300 dark:bg-gray-700 rounded mb-4 w-1/3"></div>
              <div className="h-4 bg-gray-300 dark:bg-gray-700 rounded mb-2 w-2/3"></div>
              <div className="h-4 bg-gray-300 dark:bg-gray-700 rounded w-1/2"></div>
            </div>
          </div>
          
          {/* Content Skeleton */}
          <div className="space-y-4">
            {[...Array(3)].map((_, i) => (
              <div key={i} className="bg-white dark:bg-gray-800 rounded-lg p-6 animate-pulse">
                <div className="h-4 bg-gray-300 dark:bg-gray-700 rounded mb-2"></div>
                <div className="h-4 bg-gray-300 dark:bg-gray-700 rounded w-3/4"></div>
              </div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  if (error || !group) {
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900 flex items-center justify-center">
        <div className="text-center">
          <div className="text-red-600 dark:text-red-400 mb-4 text-lg">
            {error || 'Group not found'}
          </div>
          <button
            onClick={loadGroup}
            className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  const isOwner = user?.id === group.user.id;
  const isMember = group.isCurrentUserMember;

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <div className="max-w-7xl mx-auto flex">
        {/* Left Sidebar */}
        {user && (
          <div className="w-16 lg:w-64 fixed h-full z-10">
            <Sidebar />
          </div>
        )}

        {/* Main Content */}
        <div className={`flex-1 ${user ? 'ml-16 lg:ml-64' : ''}`}>
          <div className="max-w-4xl mx-auto px-4 py-8">
        {/* Group Header */}
        <div className="mb-6">
          <GroupHeader
            group={group}
            onGroupUpdate={handleGroupUpdate}
            onEditClick={() => setShowEditModal(true)}
          />
        </div>

        {/* Navigation Tabs */}
        <div className="flex space-x-1 bg-gray-100 dark:bg-gray-800 rounded-lg p-1 mb-6">
          <button
            onClick={() => setActiveTab('posts')}
            className={`flex items-center space-x-2 px-4 py-2 rounded-md transition-colors ${
              activeTab === 'posts'
                ? 'bg-white dark:bg-gray-700 text-blue-600 dark:text-blue-400 shadow-sm'
                : 'text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white'
            }`}
          >
            <FileText size={16} />
            <span>Posts ({group.postCount})</span>
          </button>
          
          <button
            onClick={() => setActiveTab('members')}
            className={`flex items-center space-x-2 px-4 py-2 rounded-md transition-colors ${
              activeTab === 'members'
                ? 'bg-white dark:bg-gray-700 text-blue-600 dark:text-blue-400 shadow-sm'
                : 'text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white'
            }`}
          >
            <Users size={16} />
            <span>Members ({group.memberCount})</span>
          </button>
        </div>

        {/* Content */}
        <div className="space-y-6">
          {activeTab === 'posts' && (
            <div>
              {/* Create Post (only for members) */}
              {user && isMember && (
                <div className="mb-6">
                  <CreatePost 
                    groupId={group.id}
                    onPostCreated={handlePostCreated}
                  />
                </div>
              )}

              {/* Posts Timeline */}
              <div>
                <GroupTimeline
                  key={`group-posts-${refreshKey}`}
                  groupId={group.id}
                  apiCall={(groupId, page, pageSize) => groupApi.getGroupPosts(groupId, page, pageSize)}
                  emptyMessage="No posts in this group yet"
                  emptySubMessage={isMember ? "Be the first to post!" : "Join the group to start posting"}
                />
              </div>
            </div>
          )}

          {activeTab === 'members' && (
            <div>
              <GroupMembersList groupId={group.id} />
            </div>
          )}
        </div>

        {/* Edit Group Modal */}
        {isOwner && (
          <EditGroupModal
            isOpen={showEditModal}
            onClose={() => setShowEditModal(false)}
            group={group}
            onGroupUpdated={handleGroupUpdate}
          />
        )}
          </div>
        </div>
      </div>
    </div>
  );
}
