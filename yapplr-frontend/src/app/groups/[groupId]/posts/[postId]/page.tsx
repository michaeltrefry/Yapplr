'use client';

import { useState, useEffect, use } from 'react';
import { useRouter, useSearchParams } from 'next/navigation';
import { useQuery } from '@tanstack/react-query';
import { Post, Group } from '@/types';
import { groupApi, postApi } from '@/lib/api';
import { useAuth } from '@/contexts/AuthContext';
import PostCard from '@/components/PostCard';
import Sidebar from '@/components/Sidebar';
import { ArrowLeft, Users } from 'lucide-react';
import Link from 'next/link';

interface GroupPostPageProps {
  params: Promise<{
    groupId: string;
    postId: string;
  }>;
}

export default function GroupPostPage({ params }: GroupPostPageProps) {
  const { user } = useAuth();
  const { groupId, postId } = use(params);
  const router = useRouter();
  const searchParams = useSearchParams();
  const scrollToComment = searchParams?.get('scrollToComment');

  const groupIdNum = parseInt(groupId);
  const postIdNum = parseInt(postId);

  // Fetch group data
  const { data: group, isLoading: groupLoading, error: groupError } = useQuery({
    queryKey: ['group', groupIdNum],
    queryFn: () => groupApi.getGroup(groupIdNum),
  });

  // Fetch post data
  const { data: post, isLoading: postLoading, error: postError } = useQuery({
    queryKey: ['groupPost', groupIdNum, postIdNum],
    queryFn: () => groupApi.getGroupPost(groupIdNum, postIdNum),
    enabled: !!group, // Only fetch post after group is loaded
  });

  // Scroll to comment if specified in URL
  useEffect(() => {
    if (post && scrollToComment) {
      const timer = setTimeout(() => {
        const commentElement = document.getElementById(`comment-${scrollToComment}`);
        if (commentElement) {
          commentElement.scrollIntoView({
            behavior: 'smooth',
            block: 'center'
          });
        }
      }, 500);

      return () => clearTimeout(timer);
    }
  }, [post, scrollToComment]);

  // Verify that the post belongs to this group
  useEffect(() => {
    if (post && group && post.group?.id !== group.id) {
      // Post doesn't belong to this group, redirect to correct location
      if (post.group) {
        router.replace(`/groups/${post.group.id}/posts/${post.id}`);
      } else {
        router.replace(`/yap/${post.id}`);
      }
    }
  }, [post, group, router]);

  const handlePostUpdate = (updatedPost: Post) => {
    // Update the post in the query cache
    // This will be handled by React Query's automatic updates
  };

  const handlePostDelete = (deletedPostId: number) => {
    // Redirect back to group after deletion
    router.push(`/groups/${groupId}`);
  };

  if (groupLoading || postLoading) {
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
        <div className="max-w-7xl mx-auto flex">
          {user && (
            <div className="w-16 lg:w-64 fixed h-full z-10">
              <Sidebar />
            </div>
          )}
          <div className={`flex-1 ${user ? 'ml-16 lg:ml-64' : ''}`}>
            <div className="max-w-2xl mx-auto px-4 py-8">
              <div className="animate-pulse">
                <div className="h-8 bg-gray-200 dark:bg-gray-700 rounded mb-4"></div>
                <div className="h-64 bg-gray-200 dark:bg-gray-700 rounded"></div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (groupError || postError) {
    return (
      <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
        <div className="max-w-7xl mx-auto flex">
          {user && (
            <div className="w-16 lg:w-64 fixed h-full z-10">
              <Sidebar />
            </div>
          )}
          <div className={`flex-1 ${user ? 'ml-16 lg:ml-64' : ''}`}>
            <div className="max-w-2xl mx-auto px-4 py-8">
              <div className="text-center">
                <h1 className="text-2xl font-bold text-gray-900 dark:text-white mb-4">
                  {groupError ? 'Group Not Found' : 'Post Not Found'}
                </h1>
                <p className="text-gray-600 dark:text-gray-400 mb-6">
                  {groupError 
                    ? 'The group you are looking for does not exist or you do not have access to it.'
                    : 'The post you are looking for does not exist or has been deleted.'
                  }
                </p>
                <Link
                  href="/groups"
                  className="inline-flex items-center px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
                >
                  <ArrowLeft className="h-4 w-4 mr-2" />
                  Back to Groups
                </Link>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (!group || !post) {
    return null;
  }

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
          <div className="max-w-2xl mx-auto px-4 py-8">
            {/* Header with group context */}
            <div className="mb-6">
              <div className="flex items-center space-x-3 mb-4">
                <Link
                  href={`/groups/${group.id}`}
                  className="flex items-center text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 transition-colors"
                >
                  <ArrowLeft className="h-4 w-4 mr-1" />
                  Back to {group.name}
                </Link>
              </div>
              
              <div className="bg-white dark:bg-gray-800 rounded-lg p-4 border border-gray-200 dark:border-gray-700">
                <div className="flex items-center space-x-3">
                  <Users className="h-5 w-5 text-purple-600" />
                  <div>
                    <h1 className="text-lg font-semibold text-gray-900 dark:text-white">
                      Post in {group.name}
                    </h1>
                    <p className="text-sm text-gray-600 dark:text-gray-400">
                      {group.description}
                    </p>
                  </div>
                </div>
              </div>
            </div>

            {/* Post */}
            <div className="space-y-6">
              <PostCard
                post={post}
                showCommentsDefault={true}
                onPostUpdate={handlePostUpdate}
                onPostDelete={handlePostDelete}
              />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
