'use client';

import { useQuery } from '@tanstack/react-query';
import { postApi } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { Comment } from '@/types';
import Link from 'next/link';
import UserAvatar from './UserAvatar';

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
        <CommentItem key={comment.id} comment={comment} />
      ))}
    </div>
  );
}

interface CommentItemProps {
  comment: Comment;
}

function CommentItem({ comment }: CommentItemProps) {
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
          </span>
        </div>

        {/* Comment Text */}
        <div className="mt-1">
          <p className="text-gray-900 text-sm whitespace-pre-wrap">{comment.content}</p>
        </div>
      </div>
    </div>
  );
}
