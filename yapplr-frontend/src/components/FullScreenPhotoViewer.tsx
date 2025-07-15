'use client';

import { useState, useEffect, useCallback } from 'react';
import { Post, PostPrivacy, PostMedia, MediaType } from '@/types';
import { formatDate, formatNumber } from '@/lib/utils';
import { Heart, MessageCircle, Repeat2, Share, Users, Lock, Globe, X, ChevronLeft, ChevronRight } from 'lucide-react';
import { PhotoItem } from './PhotoGrid';
import { postApi } from '@/lib/api';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import Link from 'next/link';
import UserAvatar from './UserAvatar';
import ShareModal from './ShareModal';
import { useAuth } from '@/contexts/AuthContext';
import { ContentHighlight } from '@/utils/contentUtils';

interface FullScreenPhotoViewerProps {
  post: Post;
  mediaItem?: PostMedia;
  allPhotos?: PhotoItem[];
  currentIndex?: number;
  isOpen: boolean;
  onClose: () => void;
  onNavigate?: (newIndex: number) => void;
}

export default function FullScreenPhotoViewer({
  post,
  mediaItem,
  allPhotos,
  currentIndex,
  isOpen,
  onClose,
  onNavigate
}: FullScreenPhotoViewerProps) {
  const [showShareModal, setShowShareModal] = useState(false);
  const queryClient = useQueryClient();
  const { user } = useAuth();

  // Navigation functions
  const canNavigatePrev = allPhotos && currentIndex !== undefined && currentIndex > 0;
  const canNavigateNext = allPhotos && currentIndex !== undefined && currentIndex < allPhotos.length - 1;

  const navigatePrev = useCallback(() => {
    if (canNavigatePrev && onNavigate && currentIndex !== undefined) {
      onNavigate(currentIndex - 1);
    }
  }, [canNavigatePrev, onNavigate, currentIndex]);

  const navigateNext = useCallback(() => {
    if (canNavigateNext && onNavigate && currentIndex !== undefined) {
      onNavigate(currentIndex + 1);
    }
  }, [canNavigateNext, onNavigate, currentIndex]);

  // Keyboard navigation
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (!isOpen) return;

      switch (event.key) {
        case 'ArrowLeft':
          event.preventDefault();
          navigatePrev();
          break;
        case 'ArrowRight':
          event.preventDefault();
          navigateNext();
          break;
        case 'Escape':
          event.preventDefault();
          onClose();
          break;
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, navigatePrev, navigateNext, onClose]);

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
          className="absolute top-4 right-4 z-10 p-2 text-foreground hover:bg-surface/50 rounded-full transition-colors"
        >
          <X className="w-6 h-6" />
        </button>

        {/* Image container */}
        <div className="flex flex-col h-full">
          {/* Image area with navigation */}
          <div className="flex-1 flex items-center justify-center p-4 relative">
            {/* Previous button */}
            {canNavigatePrev && (
              <button
                onClick={navigatePrev}
                className="absolute left-4 top-1/2 transform -translate-y-1/2 z-10 bg-black/30 hover:bg-black/60 text-white p-3 rounded-full transition-all duration-200 backdrop-blur-sm opacity-80 hover:opacity-100 hover:scale-110"
                aria-label="Previous photo"
              >
                <ChevronLeft className="w-6 h-6" />
              </button>
            )}

            {/* Next button */}
            {canNavigateNext && (
              <button
                onClick={navigateNext}
                className="absolute right-4 top-1/2 transform -translate-y-1/2 z-10 bg-black/30 hover:bg-black/60 text-white p-3 rounded-full transition-all duration-200 backdrop-blur-sm opacity-80 hover:opacity-100 hover:scale-110"
                aria-label="Next photo"
              >
                <ChevronRight className="w-6 h-6" />
              </button>
            )}

            {/* Photo counter and navigation hint */}
            {allPhotos && currentIndex !== undefined && (
              <div className="absolute top-4 left-4 flex flex-col items-start space-y-2">
                <div className="bg-black/50 text-white px-3 py-1 rounded-full text-sm backdrop-blur-sm">
                  {currentIndex + 1} of {allPhotos.length}
                </div>
                {allPhotos.length > 1 && (
                  <div className="bg-black/30 text-white px-2 py-1 rounded text-xs backdrop-blur-sm opacity-70">
                    Use ← → keys or click arrows
                  </div>
                )}
              </div>
            )}

            {(() => {
              // Determine which image to show
              let imageUrl: string | undefined;

              if (mediaItem && mediaItem.imageUrl) {
                // Show the specific media item that was clicked
                imageUrl = mediaItem.imageUrl;
              } else if (post.mediaItems && post.mediaItems.length > 0) {
                // Show the first image from mediaItems
                const firstImage = post.mediaItems.find(item =>
                  item.mediaType === MediaType.Image && item.imageUrl
                );
                imageUrl = firstImage?.imageUrl;
              } else if (post.imageUrl) {
                // Fallback to legacy imageUrl
                imageUrl = post.imageUrl;
              }

              return imageUrl ? (
                <img
                  src={imageUrl}
                  alt="Photo"
                  className="max-w-full max-h-full object-contain"
                />
              ) : (
                <div className="text-gray-500">No image available</div>
              );
            })()}
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
