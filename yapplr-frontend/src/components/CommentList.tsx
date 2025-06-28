'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { postApi } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { Comment } from '@/types';
import Link from 'next/link';
import UserAvatar from './UserAvatar';
import { useAuth } from '@/contexts/AuthContext';
import { Trash2, Edit3 } from 'lucide-react';
import { useState } from 'react';

interface CommentListProps {
  postId: number;
  showComments: boolean;
}

export default function CommentList({ postId, showComments }: CommentListProps) {
  const { data: comments, isLoading } = useQuery({
    queryKey: ['comments', postId],
    queryFn: () => postApi.getComments(postId),
    enabled: showComments,
  });

  if (!showComments) {
    return null;
  }

  if (isLoading) {
    return (
      <div className="mt-4 border-t border-gray-100 pt-4">
        <div className="text-center text-gray-500 text-sm">Loading comments...</div>
      </div>
    );
  }

  if (!comments || comments.length === 0) {
    return (
      <div className="mt-4 border-t border-gray-100 pt-4">
        <div className="text-center text-gray-500 text-sm">No comments yet</div>
      </div>
    );
  }

  return (
    <div className="mt-4 border-t border-gray-100 pt-4 space-y-4">
      {comments.map((comment) => (
        <CommentItem key={comment.id} comment={comment} postId={postId} />
      ))}
    </div>
  );
}

interface CommentItemProps {
  comment: Comment;
  postId: number;
}

function CommentItem({ comment, postId }: CommentItemProps) {
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [editContent, setEditContent] = useState(comment.content);
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const editMutation = useMutation({
    mutationFn: () => postApi.updateComment(comment.id, { content: editContent }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['comments', postId] });
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['post', postId] });
      setIsEditing(false);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: () => postApi.deleteComment(comment.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['comments', postId] });
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['post', postId] });
      setShowDeleteConfirm(false);
    },
  });

  const handleEdit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!editContent.trim()) return;
    editMutation.mutate();
  };

  const handleCancelEdit = () => {
    setIsEditing(false);
    setEditContent(comment.content);
  };

  const handleDelete = () => {
    deleteMutation.mutate();
  };

  const isOwner = user && user.id === comment.user.id;
  return (
    <div className="flex space-x-3">
      {/* Avatar */}
      <UserAvatar user={comment.user} size="sm" />

      {/* Comment Content */}
      <div className="flex-1 min-w-0">
        {/* Header */}
        <div className="flex items-center space-x-2">
          <Link
            href={`/profile/${comment.user.username}`}
            className="font-semibold text-gray-900 hover:underline text-sm"
          >
            {comment.user.username}
          </Link>
          <span className="text-gray-500 text-sm">@{comment.user.username}</span>
          <span className="text-gray-500 text-sm">·</span>
          <span className="text-gray-500 text-xs">
            {formatDate(comment.createdAt)}
            {comment.isEdited && <span className="ml-1">(edited)</span>}
          </span>
          {isOwner && (
            <div className="ml-auto flex space-x-1">
              <button
                onClick={() => setIsEditing(true)}
                className="text-gray-400 hover:text-blue-600 p-1 rounded-full hover:bg-blue-50 transition-colors"
                title="Edit comment"
                disabled={editMutation.isPending || isEditing}
              >
                <Edit3 className="w-3 h-3" />
              </button>
              <button
                onClick={() => setShowDeleteConfirm(true)}
                className="text-gray-400 hover:text-red-600 p-1 rounded-full hover:bg-red-50 transition-colors"
                title="Delete comment"
                disabled={deleteMutation.isPending}
              >
                <Trash2 className="w-3 h-3" />
              </button>
            </div>
          )}
        </div>

        {/* Comment Text */}
        <div className="mt-1">
          {isEditing ? (
            <form onSubmit={handleEdit} className="space-y-2">
              <textarea
                value={editContent}
                onChange={(e) => setEditContent(e.target.value)}
                className="w-full text-sm text-gray-900 bg-white border border-gray-200 rounded px-2 py-1 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
                rows={2}
                maxLength={256}
              />
              <div className="flex justify-end space-x-2">
                <button
                  type="button"
                  onClick={handleCancelEdit}
                  className="px-2 py-1 text-xs text-gray-600 hover:text-gray-800 transition-colors"
                  disabled={editMutation.isPending}
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={!editContent.trim() || editMutation.isPending}
                  className="px-3 py-1 text-xs bg-blue-500 text-white rounded hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  {editMutation.isPending ? 'Saving...' : 'Save'}
                </button>
              </div>
            </form>
          ) : (
            <p className="text-gray-900 text-sm whitespace-pre-wrap">{comment.content}</p>
          )}
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      {showDeleteConfirm && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-sm w-full mx-4">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">Delete Comment</h3>
            <p className="text-gray-600 mb-4">
              Are you sure you want to delete this comment? This action cannot be undone.
            </p>
            <div className="flex space-x-3">
              <button
                onClick={() => setShowDeleteConfirm(false)}
                className="flex-1 px-4 py-2 text-gray-700 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
                disabled={deleteMutation.isPending}
              >
                Cancel
              </button>
              <button
                onClick={handleDelete}
                className="flex-1 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors disabled:opacity-50"
                disabled={deleteMutation.isPending}
              >
                {deleteMutation.isPending ? 'Deleting...' : 'Delete'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
