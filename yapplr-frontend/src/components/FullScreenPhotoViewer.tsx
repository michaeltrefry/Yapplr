'use client';

import { useState } from 'react';
import { Post, PostPrivacy } from '@/types';
import { formatDate, formatNumber } from '@/lib/utils';
import { Heart, MessageCircle, Repeat2, Share, Users, Lock, Globe, X } from 'lucide-react';
import { postApi } from '@/lib/api';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import Link from 'next/link';
import UserAvatar from './UserAvatar';
import ShareModal from './ShareModal';
import { useAuth } from '@/contexts/AuthContext';
import { ContentHighlight } from '@/utils/contentUtils';

interface FullScreenPhotoViewerProps {
  post: Post;
  isOpen: boolean;
  onClose: () => void;
}

export default function FullScreenPhotoViewer({ post, isOpen, onClose }: FullScreenPhotoViewerProps) {
  const [showShareModal, setShowShareModal] = useState(false);
  const queryClient = useQueryClient();
  const { user } = useAuth();

  const likeMutation = useMutation({
    mutationFn: () => 
      post.isLikedByCurrentUser 
        ? postApi.unlikePost(post.id)
        : postApi.likePost(post.id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['userPhotos'] });
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
      queryClient.invalidateQueries({ queryKey: ['userPhotos'] });
      queryClient.invalidateQueries({ queryKey: ['post', post.id] });
    },
  });

  const getPrivacyIcon = (privacyLevel: PostPrivacy) => {
    switch (privacyLevel) {
      case PostPrivacy.Public:
        return Globe;
      case PostPrivacy.Followers:
        return Users;
      case PostPrivacy.Private:
        return Lock;
      default:
        return Globe;
    }
  };

  const getPrivacyLabel = (privacyLevel: PostPrivacy) => {
    switch (privacyLevel) {
      case PostPrivacy.Public:
        return 'Public';
      case PostPrivacy.Followers:
        return 'Followers';
      case PostPrivacy.Private:
        return 'Private';
      default:
        return 'Public';
    }
  };

  if (!isOpen) return null;

  return (
    <>
      {/* Full screen overlay */}
      <div className="fixed inset-0 z-50 bg-background/95 backdrop-blur-sm">
        {/* Close button */}
        <button
          onClick={onClose}
          className="absolute top-4 right-4 z-10 p-2 text-white hover:bg-white/10 rounded-full transition-colors"
        >
          <X className="w-6 h-6" />
        </button>

        {/* Image container */}
        <div className="flex flex-col h-full">
          {/* Image area */}
          <div className="flex-1 flex items-center justify-center p-4">
            {post.imageUrl && (
              <img
                src={post.imageUrl}
                alt="Photo"
                className="max-w-full max-h-full object-contain"
              />
            )}
          </div>

          {/* Post details at bottom */}
          <div className="bg-surface/90 backdrop-blur-sm border-t border-border">
            <div className="max-w-4xl mx-auto p-4">
              <div className="flex space-x-3">
                {/* Avatar */}
                <UserAvatar user={post.user} size="md" />

                {/* Content */}
                <div className="flex-1 min-w-0">
                  {/* Header */}
                  <div className="flex items-center space-x-2 mb-2">
                    <Link
                      href={`/profile/${post.user.username}`}
                      className="font-semibold text-foreground hover:underline"
                      onClick={onClose}
                    >
                      @{post.user.username}
                    </Link>
                    <span className="text-text-secondary">·</span>
                    <span className="text-text-secondary text-sm">
                      {formatDate(post.createdAt)}
                      {post.isEdited && <span className="ml-1">(edited)</span>}
                    </span>
                    {/* Privacy Indicator */}
                    {post.privacy !== PostPrivacy.Public && (
                      <>
                        <span className="text-text-secondary">·</span>
                        <span className="text-text-secondary text-sm flex items-center">
                          {(() => {
                            const PrivacyIcon = getPrivacyIcon(post.privacy);
                            return <PrivacyIcon className="w-3 h-3 mr-1" />;
                          })()}
                          {getPrivacyLabel(post.privacy)}
                        </span>
                      </>
                    )}
                  </div>

                  {/* Post content */}
                  {post.content && (
                    <p className="text-foreground whitespace-pre-wrap mb-3">
                      <ContentHighlight content={post.content} />
                    </p>
                  )}

                  {/* Action buttons */}
                  <div className="flex items-center space-x-6">
                    <button
                      onClick={() => onClose()}
                      className="flex items-center space-x-2 text-gray-400 hover:text-blue-400 transition-colors group"
                    >
                      <div className="p-2 rounded-full group-hover:bg-blue-500/10">
                        <MessageCircle className="w-5 h-5" />
                      </div>
                      <span className="text-sm">{formatNumber(post.commentCount)}</span>
                    </button>

                    <button
                      onClick={() => repostMutation.mutate()}
                      disabled={repostMutation.isPending}
                      className={`flex items-center space-x-2 transition-colors group ${
                        post.isRepostedByCurrentUser
                          ? 'text-green-400'
                          : 'text-gray-400 hover:text-green-400'
                      }`}
                    >
                      <div className="p-2 rounded-full group-hover:bg-green-500/10">
                        <Repeat2 className="w-5 h-5" />
                      </div>
                      <span className="text-sm">{formatNumber(post.repostCount)}</span>
                    </button>

                    <button
                      onClick={() => likeMutation.mutate()}
                      disabled={likeMutation.isPending}
                      className={`flex items-center space-x-2 transition-colors group ${
                        post.isLikedByCurrentUser
                          ? 'text-red-400'
                          : 'text-text-secondary hover:text-red-400'
                      }`}
                    >
                      <div className="p-2 rounded-full group-hover:bg-red-500/10">
                        <Heart
                          className={`w-5 h-5 ${post.isLikedByCurrentUser ? 'fill-current' : ''}`}
                        />
                      </div>
                      <span className="text-sm">{formatNumber(post.likeCount)}</span>
                    </button>

                    <button
                      onClick={() => setShowShareModal(true)}
                      className="flex items-center space-x-2 text-text-secondary hover:text-blue-400 transition-colors group"
                    >
                      <div className="p-2 rounded-full group-hover:bg-blue-500/10">
                        <Share className="w-5 h-5" />
                      </div>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* Share Modal */}
      <ShareModal
        isOpen={showShareModal}
        onClose={() => setShowShareModal(false)}
        post={post}
      />
    </>
  );
}
