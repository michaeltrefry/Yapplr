'use client';

import { TimelineItem } from '@/types';
import PostCard from './PostCard';
import UserAvatar from './UserAvatar';
import { Repeat2, Trash2 } from 'lucide-react';
import { formatDate } from '@/lib/utils';
import { useAuth } from '@/contexts/AuthContext';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { postApi } from '@/lib/api';
import { useState } from 'react';

interface TimelineItemCardProps {
  item: TimelineItem;
}

export default function TimelineItemCard({ item }: TimelineItemCardProps) {
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const deleteRepostMutation = useMutation({
    mutationFn: () => postApi.unrepost(item.post.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['userTimeline'] });
      setShowDeleteConfirm(false);
    },
  });

  const handleDeleteRepost = () => {
    deleteRepostMutation.mutate();
  };

  if (item.type === 'post') {
    // Regular post
    return <PostCard post={item.post} />;
  }

  // Repost
  const isRepostOwner = user && user.id === item.repostedBy!.id;

  return (
    <>
      <div className="border-b border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
        {/* Repost header */}
        <div className="flex items-center px-4 pt-3 pb-2 text-gray-500 dark:text-gray-400 text-sm">
          <Repeat2 className="w-4 h-4 mr-2" />
          <UserAvatar user={item.repostedBy!} size="sm" />
          <span className="ml-2">
            <span className="font-semibold text-gray-700 dark:text-gray-300">{item.repostedBy!.username}</span> reyapped
          </span>
          <span className="ml-2">·</span>
          <span className="ml-2">{formatDate(item.createdAt)}</span>
          {isRepostOwner && (
            <div className="ml-auto">
              <button
                onClick={() => setShowDeleteConfirm(true)}
                className="text-gray-400 dark:text-gray-500 hover:text-red-600 dark:hover:text-red-400 p-1 rounded-full hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
                title="Delete repost"
                disabled={deleteRepostMutation.isPending}
              >
                <Trash2 className="w-3 h-3" />
              </button>
            </div>
          )}
        </div>

        {/* Original post content */}
        <div className="ml-4 border-l-2 border-gray-100 dark:border-gray-700 pl-4">
          <PostCard post={item.post} showBorder={false} />
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      {showDeleteConfirm && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-gray-800 rounded-lg p-6 max-w-sm w-full mx-4">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">Delete Repost</h3>
            <p className="text-gray-600 dark:text-gray-300 mb-4">
              Are you sure you want to delete this repost? This action cannot be undone.
            </p>
            <div className="flex space-x-3">
              <button
                onClick={() => setShowDeleteConfirm(false)}
                className="flex-1 px-4 py-2 text-gray-700 dark:text-gray-300 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
                disabled={deleteRepostMutation.isPending}
              >
                Cancel
              </button>
              <button
                onClick={handleDeleteRepost}
                className="flex-1 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors disabled:opacity-50"
                disabled={deleteRepostMutation.isPending}
              >
                {deleteRepostMutation.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
