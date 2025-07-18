'use client';

import { useState, useEffect, useCallback, useRef } from 'react';
import { Post, PostPrivacy, PostMedia, MediaType } from '@/types';
import { formatDate, formatNumber } from '@/lib/utils';
import { Heart, MessageCircle, Repeat2, Share, Users, Lock, Globe, X, ChevronLeft, ChevronRight } from 'lucide-react';
import { VideoItem } from './VideoGrid';
import { postApi } from '@/lib/api';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import Link from 'next/link';
import UserAvatar from './UserAvatar';
import ShareModal from './ShareModal';
import { useAuth } from '@/contexts/AuthContext';
import { ContentHighlight } from '@/utils/contentUtils';
import { useRouter } from 'next/navigation';

interface FullScreenVideoViewerProps {
  post: Post;
  mediaItem?: PostMedia;
  allVideos?: VideoItem[];
  currentIndex?: number;
  isOpen: boolean;
  onClose: () => void;
  onNavigate?: (newIndex: number) => void;
}

export default function FullScreenVideoViewer({
  post,
  mediaItem,
  allVideos,
  currentIndex,
  isOpen,
  onClose,
  onNavigate
}: FullScreenVideoViewerProps) {
  const [showShareModal, setShowShareModal] = useState(false);
  const [localPost, setLocalPost] = useState(post);
  const videoRef = useRef<HTMLVideoElement>(null);
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const router = useRouter();

  // Update local post when prop changes
  useEffect(() => {
    console.log('FullScreenVideoViewer: Post prop changed', {
      postId: post.id,
      isLiked: post.isLikedByCurrentUser,
      likeCount: post.likeCount,
      isReposted: post.isRepostedByCurrentUser,
      repostCount: post.repostCount
    });
    setLocalPost(post);
  }, [post]);

  // Debug current local post state
  useEffect(() => {
    console.log('FullScreenVideoViewer: Local post state', {
      postId: localPost.id,
      isLiked: localPost.isLikedByCurrentUser,
      likeCount: localPost.likeCount,
      isReposted: localPost.isRepostedByCurrentUser,
      repostCount: localPost.repostCount
    });
  }, [localPost]);

  // Navigation functions
  const canNavigatePrev = allVideos && currentIndex !== undefined && currentIndex > 0;
  const canNavigateNext = allVideos && currentIndex !== undefined && currentIndex < allVideos.length - 1;

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
        case ' ':
          event.preventDefault();
          if (videoRef.current) {
            if (videoRef.current.paused) {
              videoRef.current.play();
            } else {
              videoRef.current.pause();
            }
          }
          break;
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, navigatePrev, navigateNext, onClose]);

  const likeMutation = useMutation({
    mutationFn: async (isCurrentlyLiked: boolean) => {
      console.log('API call - isCurrentlyLiked:', isCurrentlyLiked);

      if (isCurrentlyLiked) {
        return await postApi.unlikePost(localPost.id);
      } else {
        return await postApi.likePost(localPost.id);
      }
    },
    onMutate: async () => {
      // Store the previous state for potential rollback and API call
      const previousState = {
        isLikedByCurrentUser: localPost.isLikedByCurrentUser,
        likeCount: localPost.likeCount
      };

      console.log('Like mutation onMutate - before optimistic update:', previousState);

      // Optimistic update
      setLocalPost(prev => ({
        ...prev,
        isLikedByCurrentUser: !prev.isLikedByCurrentUser,
        likeCount: prev.isLikedByCurrentUser ? prev.likeCount - 1 : prev.likeCount + 1
      }));

      return { previousState };
    },
    onSuccess: () => {
      console.log('Like mutation success');
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['userVideos'] });
      queryClient.invalidateQueries({ queryKey: ['post', localPost.id] });
    },
    onError: (error, variables, context) => {
      console.log('Like mutation error:', error);
      console.log('Reverting to:', context?.previousState);
      // Revert to the previous state
      if (context?.previousState) {
        setLocalPost(prev => ({
          ...prev,
          isLikedByCurrentUser: context.previousState.isLikedByCurrentUser,
          likeCount: context.previousState.likeCount
        }));
      }
    },
  });

  const repostMutation = useMutation({
    mutationFn: async (isCurrentlyReposted: boolean) => {
      console.log('Repost API call - isCurrentlyReposted:', isCurrentlyReposted);

      if (isCurrentlyReposted) {
        return await postApi.unrepost(localPost.id);
      } else {
        return await postApi.repost(localPost.id);
      }
    },
    onMutate: async () => {
      // Store the previous state for potential rollback
      const previousState = {
        isRepostedByCurrentUser: localPost.isRepostedByCurrentUser,
        repostCount: localPost.repostCount
      };

      console.log('Repost mutation onMutate - before optimistic update:', previousState);

      // Optimistic update
      setLocalPost(prev => ({
        ...prev,
        isRepostedByCurrentUser: !prev.isRepostedByCurrentUser,
        repostCount: prev.isRepostedByCurrentUser ? prev.repostCount - 1 : prev.repostCount + 1
      }));

      return { previousState };
    },
    onSuccess: () => {
      console.log('Repost mutation success');
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['userVideos'] });
      queryClient.invalidateQueries({ queryKey: ['post', localPost.id] });
    },
    onError: (error, variables, context) => {
      console.log('Repost mutation error:', error);
      console.log('Reverting to:', context?.previousState);
      // Revert to the previous state
      if (context?.previousState) {
        setLocalPost(prev => ({
          ...prev,
          isRepostedByCurrentUser: context.previousState.isRepostedByCurrentUser,
          repostCount: context.previousState.repostCount
        }));
      }
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
      <div
        className="fixed inset-0 z-50 bg-background/95 backdrop-blur-sm"
        onClick={onClose}
      >
        {/* Close button */}
        <button
          onClick={(e) => {
            e.stopPropagation();
            onClose();
          }}
          className="absolute top-4 right-4 z-10 p-2 text-foreground hover:bg-surface/50 rounded-full transition-colors"
        >
          <X className="w-6 h-6" />
        </button>

        {/* Video container */}
        <div className="flex flex-col h-full">
          {/* Video area with navigation */}
          <div className="flex-1 flex items-center justify-center p-4 relative">
            {/* Previous button */}
            {canNavigatePrev && (
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  navigatePrev();
                }}
                className="absolute left-4 top-1/2 transform -translate-y-1/2 z-10 bg-black/30 hover:bg-black/60 text-white p-3 rounded-full transition-all duration-200 backdrop-blur-sm opacity-80 hover:opacity-100 hover:scale-110"
                aria-label="Previous video"
              >
                <ChevronLeft className="w-6 h-6" />
              </button>
            )}

            {/* Next button */}
            {canNavigateNext && (
              <button
                onClick={(e) => {
                  e.stopPropagation();
                  navigateNext();
                }}
                className="absolute right-4 top-1/2 transform -translate-y-1/2 z-10 bg-black/30 hover:bg-black/60 text-white p-3 rounded-full transition-all duration-200 backdrop-blur-sm opacity-80 hover:opacity-100 hover:scale-110"
                aria-label="Next video"
              >
                <ChevronRight className="w-6 h-6" />
              </button>
            )}

            {/* Video counter and navigation hint */}
            {allVideos && currentIndex !== undefined && (
              <div
                className="absolute top-4 left-4 flex flex-col items-start space-y-2"
                onClick={(e) => e.stopPropagation()}
              >
                <div className="bg-black/50 text-white px-3 py-1 rounded-full text-sm backdrop-blur-sm">
                  {currentIndex + 1} of {allVideos.length}
                </div>
                {allVideos.length > 1 && (
                  <div className="bg-black/30 text-white px-2 py-1 rounded text-xs backdrop-blur-sm opacity-70">
                    Use ← → keys or click arrows
                  </div>
                )}
                <div className="bg-black/30 text-white px-2 py-1 rounded text-xs backdrop-blur-sm opacity-70">
                  Press space to play/pause
                </div>
              </div>
            )}

            {(() => {
              // Determine which video to show
              let videoUrl: string | undefined;

              if (mediaItem && mediaItem.videoUrl) {
                // Show the specific media item that was clicked
                videoUrl = mediaItem.videoUrl;
              } else if (post.mediaItems && post.mediaItems.length > 0) {
                // Show the first video from mediaItems
                const firstVideo = post.mediaItems.find(item =>
                  item.mediaType === MediaType.Video && item.videoUrl
                );
                videoUrl = firstVideo?.videoUrl;
              } else if (post.videoUrl) {
                // Fallback to legacy videoUrl
                videoUrl = post.videoUrl;
              }

              return videoUrl ? (
                <video
                  ref={videoRef}
                  src={videoUrl}
                  controls
                  autoPlay
                  className="max-w-full max-h-full object-contain"
                  onClick={(e) => e.stopPropagation()}
                  onError={() => console.error('Video failed to load:', videoUrl)}
                />
              ) : (
                <div className="text-gray-500" onClick={(e) => e.stopPropagation()}>No video available</div>
              );
            })()}
          </div>

          {/* Post details at bottom */}
          <div
            className="bg-surface/90 backdrop-blur-sm border-t border-border"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="max-w-4xl mx-auto p-4">
              <div className="flex space-x-3">
                {/* Avatar */}
                <UserAvatar user={localPost.user} size="md" />

                {/* Content */}
                <div className="flex-1 min-w-0">
                  {/* Header */}
                  <div className="flex items-center space-x-2 mb-2">
                    <Link
                      href={`/profile/${localPost.user.username}`}
                      className="font-semibold text-foreground hover:underline"
                      onClick={onClose}
                    >
                      @{localPost.user.username}
                    </Link>
                    <span className="text-text-secondary">·</span>
                    <span className="text-text-secondary text-sm">
                      {formatDate(localPost.createdAt)}
                      {localPost.isEdited && <span className="ml-1">(edited)</span>}
                    </span>
                    {/* Privacy Indicator */}
                    {localPost.privacy !== PostPrivacy.Public && (
                      <>
                        <span className="text-text-secondary">·</span>
                        <span className="text-text-secondary text-sm flex items-center">
                          {(() => {
                            const PrivacyIcon = getPrivacyIcon(localPost.privacy);
                            return <PrivacyIcon className="w-3 h-3 mr-1" />;
                          })()}
                          {getPrivacyLabel(localPost.privacy)}
                        </span>
                      </>
                    )}
                  </div>

                  {/* Post content */}
                  {localPost.content && (
                    <p className="text-foreground whitespace-pre-wrap mb-3">
                      <ContentHighlight content={localPost.content} />
                    </p>
                  )}

                  {/* Action buttons */}
                  <div className="flex items-center space-x-6">
                    <button
                      onClick={() => {
                        onClose();
                        router.push(`/yap/${localPost.id}`);
                      }}
                      className="flex items-center space-x-2 text-text-secondary hover:text-blue-400 transition-colors group"
                    >
                      <div className="p-2 rounded-full group-hover:bg-blue-500/10">
                        <MessageCircle className="w-5 h-5" />
                      </div>
                      <span className="text-sm">{formatNumber(localPost.commentCount)}</span>
                    </button>

                    <button
                      onClick={() => repostMutation.mutate(localPost.isRepostedByCurrentUser)}
                      disabled={repostMutation.isPending}
                      className={`flex items-center space-x-2 transition-colors group ${
                        localPost.isRepostedByCurrentUser
                          ? 'text-green-400 hover:text-green-500'
                          : 'text-text-secondary hover:text-green-400'
                      }`}
                    >
                      <div className="p-2 rounded-full group-hover:bg-green-500/10">
                        <Repeat2 className="w-5 h-5" />
                      </div>
                      <span className="text-sm">{formatNumber(localPost.repostCount)}</span>
                    </button>

                    <button
                      onClick={() => likeMutation.mutate(localPost.isLikedByCurrentUser)}
                      disabled={likeMutation.isPending}
                      className={`flex items-center space-x-2 transition-colors group ${
                        localPost.isLikedByCurrentUser
                          ? 'text-red-400 hover:text-red-500'
                          : 'text-text-secondary hover:text-red-400'
                      }`}
                    >
                      <div className="p-2 rounded-full group-hover:bg-red-500/10">
                        <Heart
                          className={`w-5 h-5 ${localPost.isLikedByCurrentUser ? 'fill-current' : ''}`}
                        />
                      </div>
                      <span className="text-sm">{formatNumber(localPost.likeCount)}</span>
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
        post={localPost}
      />
    </>
  );
}
