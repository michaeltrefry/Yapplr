'use client';

import { useState } from 'react';
import { Post, PostPrivacy } from '@/types';
import { formatDate, formatNumber } from '@/lib/utils';
import { Heart, MessageCircle, Repeat2, Share, MoreHorizontal, Users, Lock } from 'lucide-react';
import { postApi } from '@/lib/api';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import Link from 'next/link';
import CommentList from './CommentList';
import UserAvatar from './UserAvatar';
import ShareModal from './ShareModal';

interface PostCardProps {
  post: Post;
  showCommentsDefault?: boolean; // For profile pages
  showBorder?: boolean;
}

export default function PostCard({ post, showCommentsDefault = false, showBorder = true }: PostCardProps) {
  const [showComments, setShowComments] = useState(showCommentsDefault);
  const [commentText, setCommentText] = useState('');
  const [showShareModal, setShowShareModal] = useState(false);
  const queryClient = useQueryClient();

  const likeMutation = useMutation({
    mutationFn: () => 
      post.isLikedByCurrentUser 
        ? postApi.unlikePost(post.id)
        : postApi.likePost(post.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['post', post.id] });
    },
  });

  const repostMutation = useMutation({
    mutationFn: () => 
      post.isRepostedByCurrentUser 
        ? postApi.unrepost(post.id)
        : postApi.repost(post.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['post', post.id] });
    },
  });

  const commentMutation = useMutation({
    mutationFn: () => postApi.addComment(post.id, { content: commentText }),
    onSuccess: () => {
      setCommentText('');
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['post', post.id] });
      queryClient.invalidateQueries({ queryKey: ['comments', post.id] });
    },
  });

  const handleLike = () => {
    likeMutation.mutate();
  };

  const handleRepost = () => {
    repostMutation.mutate();
  };

  const handleComment = (e: React.FormEvent) => {
    e.preventDefault();
    if (!commentText.trim()) return;
    commentMutation.mutate();
  };

  return (
    <>
      <article className={`p-4 hover:bg-gray-50/50 transition-colors ${showBorder ? 'border-b border-gray-200' : ''}`}>
      <div className="flex space-x-3">
        {/* Avatar */}
        <UserAvatar user={post.user} size="lg" />

        {/* Content */}
        <div className="flex-1 min-w-0">
          {/* Header */}
          <div className="flex items-center space-x-2">
            <Link 
              href={`/profile/${post.user.username}`}
              className="font-semibold text-gray-900 hover:underline"
            >
              {post.user.username}
            </Link>
            <span className="text-gray-500">@{post.user.username}</span>
            <span className="text-gray-500">·</span>
            <span className="text-gray-500 text-sm">
              {formatDate(post.createdAt)}
            </span>
            {/* Privacy Indicator */}
            {post.privacy !== PostPrivacy.Public && (
              <>
                <span className="text-gray-500">·</span>
                <span className="text-gray-500 text-sm flex items-center">
                  {post.privacy === PostPrivacy.Followers ? (
                    <Users className="w-3 h-3 mr-1" />
                  ) : (
                    <Lock className="w-3 h-3 mr-1" />
                  )}
                  {post.privacy === PostPrivacy.Followers ? 'Followers' : 'Private'}
                </span>
              </>
            )}
            <div className="ml-auto">
              <button className="text-gray-400 hover:text-gray-600 p-1 rounded-full hover:bg-gray-100">
                <MoreHorizontal className="w-4 h-4" />
              </button>
            </div>
          </div>

          {/* Post Content */}
          <div className="mt-1">
            <p className="text-gray-900 whitespace-pre-wrap">{post.content}</p>
            
            {/* Image */}
            {post.imageUrl && (
              <div className="mt-3">
                <img
                  src={post.imageUrl}
                  alt="Post image"
                  className="max-w-full h-auto rounded-lg border border-gray-200"
                />
              </div>
            )}
          </div>

          {/* Actions */}
          <div className="flex items-center justify-between mt-3 max-w-md">
            <button
              onClick={() => setShowComments(!showComments)}
              className="flex items-center space-x-2 text-gray-500 hover:text-blue-500 transition-colors group"
            >
              <div className="p-2 rounded-full group-hover:bg-blue-50">
                <MessageCircle className="w-5 h-5" />
              </div>
              <span className="text-sm">{formatNumber(post.commentCount)}</span>
            </button>

            <button
              onClick={handleRepost}
              disabled={repostMutation.isPending}
              className={`flex items-center space-x-2 transition-colors group ${
                post.isRepostedByCurrentUser
                  ? 'text-green-500'
                  : 'text-gray-500 hover:text-green-500'
              }`}
            >
              <div className="p-2 rounded-full group-hover:bg-green-50">
                <Repeat2 className="w-5 h-5" />
              </div>
              <span className="text-sm">{formatNumber(post.repostCount)}</span>
            </button>

            <button
              onClick={handleLike}
              disabled={likeMutation.isPending}
              className={`flex items-center space-x-2 transition-colors group ${
                post.isLikedByCurrentUser
                  ? 'text-red-500'
                  : 'text-gray-500 hover:text-red-500'
              }`}
            >
              <div className="p-2 rounded-full group-hover:bg-red-50">
                <Heart 
                  className={`w-5 h-5 ${post.isLikedByCurrentUser ? 'fill-current' : ''}`} 
                />
              </div>
              <span className="text-sm">{formatNumber(post.likeCount)}</span>
            </button>

            <button
              onClick={() => setShowShareModal(true)}
              className="flex items-center space-x-2 text-gray-500 hover:text-blue-500 transition-colors group"
            >
              <div className="p-2 rounded-full group-hover:bg-blue-50">
                <Share className="w-5 h-5" />
              </div>
            </button>
          </div>

          {/* Comments Section */}
          <CommentList postId={post.id} showComments={showComments} />

          {/* Comment Form */}
          {showComments && (
            <div className="mt-4 border-t border-gray-100 pt-4">
              <form onSubmit={handleComment} className="flex space-x-3">
                <div className="w-8 h-8 bg-gray-300 rounded-full flex-shrink-0"></div>
                <div className="flex-1">
                  <textarea
                    value={commentText}
                    onChange={(e) => setCommentText(e.target.value)}
                    placeholder="Post your reply"
                    className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
                    rows={2}
                    maxLength={256}
                  />
                  <div className="flex justify-end mt-2">
                    <button
                      type="submit"
                      disabled={!commentText.trim() || commentMutation.isPending}
                      className="bg-blue-500 text-white px-4 py-1 rounded-full text-sm font-semibold hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                    >
                      {commentMutation.isPending ? 'Replying...' : 'Reply'}
                    </button>
                  </div>
                </div>
              </form>
            </div>
          )}
        </div>
      </div>
    </article>

      {/* Share Modal */}
      <ShareModal
        isOpen={showShareModal}
        onClose={() => setShowShareModal(false)}
        post={post}
      />
    </>
  );
}
