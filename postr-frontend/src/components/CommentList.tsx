'use client';

import { useQuery } from '@tanstack/react-query';
import { postApi } from '@/lib/api';
import { formatDate } from '@/lib/utils';
import { Comment } from '@/types';
import Link from 'next/link';

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
      <Link href={`/profile/${comment.user.username}`}>
        <div className="w-8 h-8 bg-blue-600 rounded-full flex items-center justify-center flex-shrink-0 cursor-pointer hover:bg-blue-700 transition-colors">
          <span className="text-white font-semibold text-xs">
            {comment.user.username.charAt(0).toUpperCase()}
          </span>
        </div>
      </Link>

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
