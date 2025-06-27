'use client';

import { useState } from 'react';
import { Post, PostPrivacy } from '@/types';
import { formatDate, formatNumber } from '@/lib/utils';
import { Heart, MessageCircle, Repeat2, Share, Users, Lock, Trash2, Edit3 } from 'lucide-react';
import { postApi } from '@/lib/api';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import Link from 'next/link';
import Image from 'next/image';
import CommentList from './CommentList';
import UserAvatar from './UserAvatar';
import ShareModal from './ShareModal';
import { useAuth } from '@/contexts/AuthContext';

interface PostCardProps {
  post: Post;
  showCommentsDefault?: boolean; // For profile pages
  showBorder?: boolean;
}

export default function PostCard({ post, showCommentsDefault = false, showBorder = true }: PostCardProps) {
  const [showComments, setShowComments] = useState(showCommentsDefault);
  const [commentText, setCommentText] = useState('');
  const [showShareModal, setShowShareModal] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [editContent, setEditContent] = useState(post.content);
  const [editPrivacy, setEditPrivacy] = useState(post.privacy);
  const queryClient = useQueryClient();
  const { user } = useAuth();

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

  const editMutation = useMutation({
    mutationFn: () => postApi.updatePost(post.id, { content: editContent, privacy: editPrivacy }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['post', post.id] });
      queryClient.invalidateQueries({ queryKey: ['userPosts'] });
      queryClient.invalidateQueries({ queryKey: ['userTimeline'] });
      setIsEditing(false);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: () => postApi.deletePost(post.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['userPosts'] });
      queryClient.invalidateQueries({ queryKey: ['userTimeline'] });
      setShowDeleteConfirm(false);
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

  const handleEdit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!editContent.trim()) return;
    editMutation.mutate();
  };

  const handleCancelEdit = () => {
    setIsEditing(false);
    setEditContent(post.content);
    setEditPrivacy(post.privacy);
  };

  const handleDelete = () => {
    deleteMutation.mutate();
  };

  const isOwner = user && user.id === post.user.id;

  return (
    <>
      <article className={`p-4 hover:bg-gray-50/50 dark:hover:bg-gray-800/50 transition-colors bg-white dark:bg-gray-800 ${showBorder ? 'border-b border-gray-200 dark:border-gray-700' : ''}`}>
      <div className="flex space-x-3">
        {/* Avatar */}
        <UserAvatar user={post.user} size="lg" />

        {/* Content */}
        <div className="flex-1 min-w-0">
          {/* Header */}
          <div className="flex items-center space-x-2">
            <Link
              href={`/profile/${post.user.username}`}
              className="font-semibold text-gray-900 dark:text-white hover:underline"
            >
              {post.user.username}
            </Link>
            <span className="text-gray-500 dark:text-gray-400">@{post.user.username}</span>
            <span className="text-gray-500 dark:text-gray-400">·</span>
            <span className="text-gray-500 dark:text-gray-400 text-sm">
              {formatDate(post.createdAt)}
              {post.isEdited && <span className="ml-1">(edited)</span>}
            </span>
            {/* Privacy Indicator */}
            {post.privacy !== PostPrivacy.Public && (
              <>
                <span className="text-gray-500 dark:text-gray-400">·</span>
                <span className="text-gray-500 dark:text-gray-400 text-sm flex items-center">
                  {post.privacy === PostPrivacy.Followers ? (
                    <Users className="w-3 h-3 mr-1" />
                  ) : (
                    <Lock className="w-3 h-3 mr-1" />
                  )}
                  {post.privacy === PostPrivacy.Followers ? 'Followers' : 'Private'}
                </span>
              </>
            )}
            {isOwner && (
              <div className="ml-auto flex space-x-1">
                <button
                  onClick={() => setIsEditing(true)}
                  className="text-gray-400 dark:text-gray-500 hover:text-blue-600 dark:hover:text-blue-400 p-1 rounded-full hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-colors"
                  title="Edit post"
                  disabled={editMutation.isPending || isEditing}
                >
                  <Edit3 className="w-4 h-4" />
                </button>
                <button
                  onClick={() => setShowDeleteConfirm(true)}
                  className="text-gray-400 dark:text-gray-500 hover:text-red-600 dark:hover:text-red-400 p-1 rounded-full hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
                  title="Delete post"
                  disabled={deleteMutation.isPending}
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            )}
          </div>

          {/* Post Content */}
          <div className="mt-1">
            {isEditing ? (
              <form onSubmit={handleEdit} className="space-y-3">
                <textarea
                  value={editContent}
                  onChange={(e) => setEditContent(e.target.value)}
                  className="w-full text-gray-900 dark:text-white bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
                  rows={3}
                  maxLength={256}
                />
                <div className="flex items-center justify-between">
                  <select
                    value={editPrivacy}
                    onChange={(e) => setEditPrivacy(Number(e.target.value) as PostPrivacy)}
                    className="text-sm border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white rounded px-2 py-1 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  >
                    <option value={PostPrivacy.Public}>Public</option>
                    <option value={PostPrivacy.Followers}>Followers</option>
                    <option value={PostPrivacy.Private}>Private</option>
                  </select>
                  <div className="flex space-x-2">
                    <button
                      type="button"
                      onClick={handleCancelEdit}
                      className="px-3 py-1 text-sm text-gray-600 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200 transition-colors"
                      disabled={editMutation.isPending}
                    >
                      Cancel
                    </button>
                    <button
                      type="submit"
                      disabled={!editContent.trim() || editMutation.isPending}
                      className="px-4 py-1 text-sm bg-blue-500 dark:bg-blue-600 text-white rounded-full hover:bg-blue-600 dark:hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                    >
                      {editMutation.isPending ? 'Saving...' : 'Save'}
                    </button>
                  </div>
                </div>
              </form>
            ) : (
              <p className="text-gray-900 dark:text-white whitespace-pre-wrap">{post.content}</p>
            )}
            
            {/* Image */}
            {post.imageUrl && (
              <div className="mt-3">
                <Image
                  src={post.imageUrl}
                  alt="Post image"
                  width={500}
                  height={300}
                  className="max-w-full h-auto rounded-lg border border-gray-200"
                />
              </div>
            )}
          </div>

          {/* Actions */}
          <div className="flex items-center justify-between mt-3 max-w-md">
            <button
              onClick={() => setShowComments(!showComments)}
              className="flex items-center space-x-2 text-gray-500 dark:text-gray-400 hover:text-blue-500 dark:hover:text-blue-400 transition-colors group"
            >
              <div className="p-2 rounded-full group-hover:bg-blue-50 dark:group-hover:bg-blue-900/20">
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
                  : 'text-gray-500 dark:text-gray-400 hover:text-green-500'
              }`}
            >
              <div className="p-2 rounded-full group-hover:bg-green-50 dark:group-hover:bg-green-900/20">
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
                  : 'text-gray-500 dark:text-gray-400 hover:text-red-500'
              }`}
            >
              <div className="p-2 rounded-full group-hover:bg-red-50 dark:group-hover:bg-red-900/20">
                <Heart
                  className={`w-5 h-5 ${post.isLikedByCurrentUser ? 'fill-current' : ''}`}
                />
              </div>
              <span className="text-sm">{formatNumber(post.likeCount)}</span>
            </button>

            <button
              onClick={() => setShowShareModal(true)}
              className="flex items-center space-x-2 text-gray-500 dark:text-gray-400 hover:text-blue-500 dark:hover:text-blue-400 transition-colors group"
            >
              <div className="p-2 rounded-full group-hover:bg-blue-50 dark:group-hover:bg-blue-900/20">
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
                <div className="w-8 h-8 bg-gray-300 dark:bg-gray-600 rounded-full flex-shrink-0"></div>
                <div className="flex-1">
                  <textarea
                    value={commentText}
                    onChange={(e) => setCommentText(e.target.value)}
                    placeholder="Yap your reply"
                    className="w-full text-sm border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
                    rows={2}
                    maxLength={256}
                  />
                  <div className="flex justify-end mt-2">
                    <button
                      type="submit"
                      disabled={!commentText.trim() || commentMutation.isPending}
                      className="bg-blue-500 dark:bg-blue-600 text-white px-4 py-1 rounded-full text-sm font-semibold hover:bg-blue-600 dark:hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
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

      {/* Delete Confirmation Modal */}
      {showDeleteConfirm && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white dark:bg-gray-800 rounded-lg p-6 max-w-sm w-full mx-4">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">Delete Post</h3>
            <p className="text-gray-600 dark:text-gray-300 mb-4">
              Are you sure you want to delete this post? This action cannot be undone.
            </p>
            <div className="flex space-x-3">
              <button
                onClick={() => setShowDeleteConfirm(false)}
                className="flex-1 px-4 py-2 text-gray-700 dark:text-gray-300 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
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
    </>
  );
}
