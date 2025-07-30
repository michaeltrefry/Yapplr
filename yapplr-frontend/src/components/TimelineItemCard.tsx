'use client';

import { TimelineItem, Post } from '@/types';
import PostCard from './PostCard';
import UserAvatar from './UserAvatar';
import { Repeat2, Trash2, Quote } from 'lucide-react';
import { formatDate } from '@/lib/utils';
import { useAuth } from '@/contexts/AuthContext';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { postApi } from '@/lib/api';
import { useState, useEffect } from 'react';

interface TimelineItemCardProps {
  item: TimelineItem;
}

export default function TimelineItemCard({ item }: TimelineItemCardProps) {
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [localItem, setLocalItem] = useState(item);
  const { user } = useAuth();
  const queryClient = useQueryClient();

  // Update local state when item prop changes
  useEffect(() => {
    setLocalItem(item);
  }, [item]);

  // Handle post updates from child PostCard components
  const handlePostUpdate = (updatedPost: Post) => {
    setLocalItem(prev => ({
      ...prev,
      post: updatedPost
    }));
  };

  const deleteRepostMutation = useMutation({
    mutationFn: () => postApi.unrepost(localItem.post.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['userTimeline'] });
      setShowDeleteConfirm(false);
    },
  });

  const handleDeleteRepost = () => {
    deleteRepostMutation.mutate();
  };

  if (localItem.type === 'post') {
    // Regular post
    return <PostCard post={localItem.post} onPostUpdate={handlePostUpdate} />;
  }

  // Repost (both simple reposts and reposts with content now have repostedBy)
  const isRepostOwner = user && localItem.repostedBy && user.id === localItem.repostedBy.id;

  return (
    <>
      <div className="border-b border-gray-200 bg-white">
        {/* Repost header */}
        <div className="flex items-center px-4 pt-3 pb-2 text-gray-500 text-sm">
          <Repeat2 className="w-4 h-4 mr-2" />
          <UserAvatar user={localItem.repostedBy!} size="sm" />
          <span className="ml-2">
            <span className="font-semibold text-gray-700">{localItem.repostedBy!.username}</span> reyapped
          </span>
          <span className="ml-2">·</span>
          <span className="ml-2">{formatDate(localItem.createdAt)}</span>
          {isRepostOwner && (
            <div className="ml-auto">
              <button
                onClick={() => setShowDeleteConfirm(true)}
                className="text-gray-400 hover:text-red-600 p-1 rounded-full hover:bg-red-50 transition-colors"
                title="Delete repost"
                disabled={deleteRepostMutation.isPending}
              >
                <Trash2 className="w-3 h-3" />
              </button>
            </div>
          )}
        </div>

        {/* Original post content */}
        <div className="ml-4 border-l-2 border-gray-100 pl-4">
          <PostCard post={localItem.post} showBorder={false} onPostUpdate={handlePostUpdate} />
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      {showDeleteConfirm && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-sm w-full mx-4">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">Delete Repost</h3>
            <p className="text-gray-600 mb-4">
              Are you sure you want to delete this repost? This action cannot be undone.
            </p>
            <div className="flex space-x-3">
              <button
                onClick={() => setShowDeleteConfirm(false)}
                className="flex-1 px-4 py-2 text-gray-700 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
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
