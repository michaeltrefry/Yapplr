'use client';

import { useState } from 'react';
import { Post, PostPrivacy, VideoProcessingStatus, ReactionType } from '@/types';
import { formatDate, formatNumber } from '@/lib/utils';
import { Heart, MessageCircle, Repeat2, Share, Users, Lock, Trash2, Edit3, Globe, ChevronDown, Flag, Play, Smile, X } from 'lucide-react';
import { postApi } from '@/lib/api';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import Link from 'next/link';
import CommentList from './CommentList';
import UserAvatar from './UserAvatar';
import ShareModal from './ShareModal';
import ReportModal from './ReportModal';
import RepostModal from './RepostModal';
import { useAuth } from '@/contexts/AuthContext';
import { ContentHighlight } from '@/utils/contentUtils';
import MediaGallery from './MediaGallery';
import ReactionPicker from './ReactionPicker';
import ReactionCountsDisplay from './ReactionCountsDisplay';
import LinkPreviewList from './LinkPreviewList';
import HiddenPostBanner from './HiddenPostBanner';
import GifPicker from './GifPicker';
import ContentWithGifs from './ContentWithGifs';
import type { SelectedGif } from '@/lib/tenor';

interface PostCardProps {
  post: Post;
  showCommentsDefault?: boolean; // For profile pages
  showBorder?: boolean;
  onPostUpdate?: (updatedPost: Post) => void;
  onPostDelete?: (deletedPostId: number) => void;
}

export default function PostCard({ post, showCommentsDefault = false, showBorder = true, onPostUpdate, onPostDelete }: PostCardProps) {
  const [showComments, setShowComments] = useState(showCommentsDefault);
  const [commentText, setCommentText] = useState('');
  const [showShareModal, setShowShareModal] = useState(false);
  const [showReportModal, setShowReportModal] = useState(false);
  const [showRepostModal, setShowRepostModal] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [editContent, setEditContent] = useState(post.content);

  // Temporary debug logging
  if (post.id && ((post.mediaItems && post.mediaItems.length > 0) || post.imageUrl)) {
    console.log('PostCard - Post with media:', {
      postId: post.id,
      content: post.content?.substring(0, 50),
      imageUrl: post.imageUrl,
      mediaItems: post.mediaItems,
      mediaItemsCount: post.mediaItems?.length || 0
    });
  }

  const [editPrivacy, setEditPrivacy] = useState(post.privacy);
  const [showEditPrivacyDropdown, setShowEditPrivacyDropdown] = useState(false);
  const [replyingTo, setReplyingTo] = useState<string | null>(null);

  // GIF state for comments
  const [showCommentGifPicker, setShowCommentGifPicker] = useState(false);
  const [selectedCommentGif, setSelectedCommentGif] = useState<SelectedGif | null>(null);

  const queryClient = useQueryClient();
  const { user } = useAuth();

  const likeMutation = useMutation({
    mutationFn: async (isCurrentlyLiked: boolean) => {
      if (isCurrentlyLiked) {
        return await postApi.unlikePost(post.id);
      } else {
        return await postApi.likePost(post.id);
      }
    },
    onMutate: async () => {
      // Store the previous state for potential rollback
      const previousState = {
        isLikedByCurrentUser: post.isLikedByCurrentUser,
        likeCount: post.likeCount
      };

      // Create optimistically updated post
      const updatedPost = {
        ...post,
        isLikedByCurrentUser: !post.isLikedByCurrentUser,
        likeCount: post.isLikedByCurrentUser ? post.likeCount - 1 : post.likeCount + 1
      };

      // Update parent component's state immediately
      onPostUpdate?.(updatedPost);

      return { previousState };
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['userTimeline'] });
      queryClient.invalidateQueries({ queryKey: ['post', post.id] });
    },
    onError: (error, variables, context) => {
      // Revert to the previous state
      if (context?.previousState) {
        const revertedPost = {
          ...post,
          isLikedByCurrentUser: context.previousState.isLikedByCurrentUser,
          likeCount: context.previousState.likeCount
        };
        onPostUpdate?.(revertedPost);
      }
    },
  });

  const reactMutation = useMutation({
    mutationFn: async (reactionType: ReactionType) => {
      return await postApi.reactToPost(post.id, reactionType);
    },
    onMutate: async (reactionType: ReactionType) => {
      // Store the previous state for potential rollback
      const previousState = {
        currentUserReaction: post.currentUserReaction,
        reactionCounts: post.reactionCounts,
        totalReactionCount: post.totalReactionCount || 0
      };

      // Update reaction counts optimistically
      const currentReactionCounts = post.reactionCounts || [];
      const newReactionCounts = [...currentReactionCounts];
      const existingReactionIndex = newReactionCounts.findIndex(r => r.reactionType === reactionType);

      // Remove previous reaction count if user had one
      if (post.currentUserReaction) {
        const prevReactionIndex = newReactionCounts.findIndex(r => r.reactionType === post.currentUserReaction);
        if (prevReactionIndex >= 0) {
          newReactionCounts[prevReactionIndex] = {
            ...newReactionCounts[prevReactionIndex],
            count: Math.max(0, newReactionCounts[prevReactionIndex].count - 1)
          };
        }
      }

      // Add new reaction count
      if (existingReactionIndex >= 0) {
        newReactionCounts[existingReactionIndex] = {
          ...newReactionCounts[existingReactionIndex],
          count: newReactionCounts[existingReactionIndex].count + 1
        };
      } else {
        // Add new reaction type
        newReactionCounts.push({
          reactionType,
          emoji: getReactionEmoji(reactionType),
          displayName: getReactionDisplayName(reactionType),
          count: 1
        });
      }

      const newTotalCount = post.currentUserReaction
        ? (post.totalReactionCount || 0) // Same total if changing reaction
        : (post.totalReactionCount || 0) + 1; // +1 if adding new reaction

      const updatedPost = {
        ...post,
        currentUserReaction: reactionType,
        reactionCounts: newReactionCounts,
        totalReactionCount: newTotalCount
      };

      onPostUpdate?.(updatedPost);
      return { previousState };
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['userTimeline'] });
      queryClient.invalidateQueries({ queryKey: ['post', post.id] });
    },
    onError: (error, variables, context) => {
      if (context?.previousState) {
        const revertedPost = {
          ...post,
          currentUserReaction: context.previousState.currentUserReaction,
          reactionCounts: context.previousState.reactionCounts,
          totalReactionCount: context.previousState.totalReactionCount
        };
        onPostUpdate?.(revertedPost);
      }
    },
  });

  const removeReactionMutation = useMutation({
    mutationFn: async () => {
      return await postApi.removePostReaction(post.id);
    },
    onMutate: async () => {
      const previousState = {
        currentUserReaction: post.currentUserReaction,
        reactionCounts: post.reactionCounts,
        totalReactionCount: post.totalReactionCount || 0
      };

      if (post.currentUserReaction) {
        const currentReactionCounts = post.reactionCounts || [];
        const newReactionCounts = [...currentReactionCounts];
        const reactionIndex = newReactionCounts.findIndex(r => r.reactionType === post.currentUserReaction);

        if (reactionIndex >= 0) {
          newReactionCounts[reactionIndex] = {
            ...newReactionCounts[reactionIndex],
            count: Math.max(0, newReactionCounts[reactionIndex].count - 1)
          };
        }

        const updatedPost = {
          ...post,
          currentUserReaction: null,
          reactionCounts: newReactionCounts,
          totalReactionCount: Math.max(0, (post.totalReactionCount || 0) - 1)
        };

        onPostUpdate?.(updatedPost);
      }

      return { previousState };
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['userTimeline'] });
      queryClient.invalidateQueries({ queryKey: ['post', post.id] });
    },
    onError: (error, variables, context) => {
      if (context?.previousState) {
        const revertedPost = {
          ...post,
          currentUserReaction: context.previousState.currentUserReaction,
          reactionCounts: context.previousState.reactionCounts,
          totalReactionCount: context.previousState.totalReactionCount
        };
        onPostUpdate?.(revertedPost);
      }
    },
  });

  // Helper functions for reactions
  const getReactionEmoji = (reactionType: ReactionType): string => {
    switch (reactionType) {
      case ReactionType.Heart: return '❤️';
      case ReactionType.ThumbsUp: return '👍';
      case ReactionType.Laugh: return '😂';
      case ReactionType.Surprised: return '😮';
      case ReactionType.Sad: return '😢';
      case ReactionType.Angry: return '😡';
      default: return '❤️';
    }
  };

  const getReactionDisplayName = (reactionType: ReactionType): string => {
    switch (reactionType) {
      case ReactionType.Heart: return 'Love';
      case ReactionType.ThumbsUp: return 'Like';
      case ReactionType.Laugh: return 'Laugh';
      case ReactionType.Surprised: return 'Surprised';
      case ReactionType.Sad: return 'Sad';
      case ReactionType.Angry: return 'Angry';
      default: return 'Love';
    }
  };



  const commentMutation = useMutation({
    mutationFn: (finalCommentText: string) => {
      return postApi.addComment(post.id, { content: finalCommentText });
    },
    onSuccess: () => {
      setCommentText('');
      setSelectedCommentGif(null);
      setReplyingTo(null);
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['userTimeline'] });
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
      onPostDelete?.(post.id);
    },
  });

  const handleLike = () => {
    likeMutation.mutate(post.isLikedByCurrentUser);
  };

  const handleRepost = () => {
    setShowRepostModal(true);
  };

  const handleComment = (e: React.FormEvent) => {
    e.preventDefault();

    // Check if we have either text or GIF
    const hasText = commentText.trim();
    const hasGif = selectedCommentGif;

    console.log('handleComment called:', { hasText: !!hasText, hasGif: !!hasGif, commentText, selectedCommentGif });

    if (!hasText && !hasGif) {
      console.log('No text or GIF, returning early');
      return;
    }

    // Prepare final comment content
    let finalCommentText = commentText;

    // If we have a GIF, embed it in the text
    if (selectedCommentGif) {
      console.log('Creating GIF embed with:', selectedCommentGif);
      // Clean the title to remove problematic characters that could break parsing
      const cleanTitle = selectedCommentGif.title.trim().replace(/[\|\]\.\s]+$/g, '');
      console.log('Cleaned title:', cleanTitle);
      const gifEmbed = `[GIF|${selectedCommentGif.id}|${selectedCommentGif.url}|${selectedCommentGif.previewUrl}|${selectedCommentGif.width}|${selectedCommentGif.height}|${cleanTitle}]`;
      console.log('Generated GIF embed:', gifEmbed);
      finalCommentText = hasText ? `${commentText} ${gifEmbed}` : gifEmbed;
      console.log('Final comment text:', finalCommentText);
    }

    // Ensure @username is still present if replying
    if (replyingTo && !finalCommentText.startsWith(`@${replyingTo}`)) {
      finalCommentText = `@${replyingTo} ${finalCommentText}`;
    }

    // Pass the final comment text directly to the mutation
    commentMutation.mutate(finalCommentText);
  };

  const handleCommentTextChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const newText = e.target.value;

    // If replying and user tries to remove the @username, prevent it
    if (replyingTo) {
      const expectedStart = `@${replyingTo} `;
      if (!newText.startsWith(expectedStart) && newText.length < expectedStart.length) {
        // User is trying to delete the @username, restore it
        setCommentText(expectedStart);
        return;
      }
    }

    setCommentText(newText);
  };

  const handleCommentGifSelect = (gif: SelectedGif) => {
    setSelectedCommentGif(gif);
    setShowCommentGifPicker(false);
  };

  const removeCommentGif = () => {
    setSelectedCommentGif(null);
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

  const handleReply = (username: string) => {
    setReplyingTo(username);
    setCommentText(`@${username} `);
    setShowComments(true);
    // Focus the comment textarea after a short delay to ensure it's rendered
    setTimeout(() => {
      const textarea = document.querySelector(`[data-post-id="${post.id}"] textarea`);
      if (textarea) {
        (textarea as HTMLTextAreaElement).focus();
        // Set cursor position after the @username
        (textarea as HTMLTextAreaElement).setSelectionRange(username.length + 2, username.length + 2);
      }
    }, 100);
  };

  const handleCancelReply = () => {
    setReplyingTo(null);
    setCommentText('');
  };

  const handleReact = (reactionType: ReactionType) => {
    reactMutation.mutate(reactionType);
  };

  const handleRemoveReaction = () => {
    removeReactionMutation.mutate();
  };

  const isOwner = user && user.id === post.user.id;

  // Privacy helper functions
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

  const privacyOptions = [
    { value: PostPrivacy.Public, label: 'Public', description: 'Anyone can see this post' },
    { value: PostPrivacy.Followers, label: 'Followers', description: 'Only your followers can see this post' },
    { value: PostPrivacy.Private, label: 'Private', description: 'Only you can see this post' },
  ];

  return (
    <>
      <article className={`p-4 bg-white ${showBorder ? 'border-b border-gray-200' : ''}`}>
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
              @{post.user.username}
            </Link>
            {/* Group Context */}
            {post.group && (
              <>
                <span className="text-gray-500">in</span>
                <Link
                  href={`/groups/${post.group.id}`}
                  className="font-medium text-blue-600 hover:text-blue-800 hover:underline flex items-center space-x-1"
                >
                  <Users className="w-3 h-3" />
                  <span>{post.group.name}</span>
                </Link>
              </>
            )}
            <span className="text-gray-500">·</span>
            <span className="text-gray-500 text-sm">
              {formatDate(post.createdAt)}
              {post.isEdited && <span className="ml-1">(edited)</span>}
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
            {isOwner && (
              <div className="ml-auto flex space-x-1">
                <button
                  onClick={() => setIsEditing(true)}
                  className="text-gray-400 hover:text-blue-600 p-1 rounded-full hover:bg-blue-50 transition-colors"
                  title="Edit post"
                  disabled={editMutation.isPending || isEditing}
                >
                  <Edit3 className="w-4 h-4" />
                </button>
                <button
                  onClick={() => setShowDeleteConfirm(true)}
                  className="text-gray-400 hover:text-red-600 p-1 rounded-full hover:bg-red-50 transition-colors"
                  title="Delete post"
                  disabled={deleteMutation.isPending}
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            )}
          </div>

          {/* Hidden Post Banner */}
          {post.moderationInfo?.isHidden && user?.id === post.user.id && (
            <div className="mt-3">
              <HiddenPostBanner
                moderationInfo={post.moderationInfo}
                postId={post.id}
                onAppealSubmitted={() => {
                  // Refresh the page to show updated appeal status
                  window.location.reload();
                }}
              />
            </div>
          )}

          {/* Post Content */}
          <div className="mt-1">
            {isEditing ? (
              <form onSubmit={handleEdit} className="space-y-3">
                <textarea
                  value={editContent}
                  onChange={(e) => setEditContent(e.target.value)}
                  className="w-full text-gray-900 bg-white border border-gray-200 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
                  rows={3}
                  maxLength={256}
                />
                <div className="flex items-center justify-between">
                  <div className="relative">
                    <button
                      type="button"
                      onClick={() => setShowEditPrivacyDropdown(!showEditPrivacyDropdown)}
                      className="flex items-center space-x-2 text-sm border border-gray-200 bg-white text-gray-900 rounded px-2 py-1 focus:outline-none focus:ring-2 focus:ring-blue-500 hover:bg-gray-50 transition-colors"
                    >
                      {(() => {
                        const IconComponent = getPrivacyIcon(editPrivacy);
                        return <IconComponent className="w-3 h-3" />;
                      })()}
                      <span>{getPrivacyLabel(editPrivacy)}</span>
                      <ChevronDown className="w-3 h-3" />
                    </button>

                    {showEditPrivacyDropdown && (
                      <div className="absolute top-full left-0 mt-1 w-56 bg-white border border-gray-200 rounded-lg shadow-lg z-10">
                        {privacyOptions.map((option) => {
                          const IconComponent = getPrivacyIcon(option.value);
                          return (
                            <button
                              key={option.value}
                              type="button"
                              onClick={() => {
                                setEditPrivacy(option.value);
                                setShowEditPrivacyDropdown(false);
                              }}
                              className={`w-full flex items-start space-x-2 px-3 py-2 text-left hover:bg-gray-50 transition-colors text-sm ${
                                editPrivacy === option.value ? 'bg-blue-50 border-l-2 border-blue-500' : ''
                              }`}
                            >
                              <IconComponent className="w-3 h-3 mt-0.5 text-gray-600" />
                              <div className="flex-1">
                                <div className="font-medium text-gray-900">{option.label}</div>
                                <div className="text-xs text-gray-500">{option.description}</div>
                              </div>
                            </button>
                          );
                        })}
                      </div>
                    )}
                  </div>
                  <div className="flex space-x-2">
                    <button
                      type="button"
                      onClick={handleCancelEdit}
                      className="px-3 py-1 text-sm text-gray-600 hover:text-gray-800 transition-colors"
                      disabled={editMutation.isPending}
                    >
                      Cancel
                    </button>
                    <button
                      type="submit"
                      disabled={!editContent.trim() || editMutation.isPending}
                      className="px-4 py-1 text-sm bg-blue-500 text-white rounded-full hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                    >
                      {editMutation.isPending ? 'Saving...' : 'Save'}
                    </button>
                  </div>
                </div>
              </form>
            ) : (
              <>
                {/* Repost content with improved context */}
                {post.content && post.content.trim() && (
                  <div className="text-gray-900">
                    {/* Add context for hashtag-only reposts */}
                    {post.repostedPost && post.content.trim().match(/^(#\w+\s*)+$/g) && (
                      <span className="text-gray-600 text-sm mr-2">Reposted with:</span>
                    )}
                    <ContentWithGifs content={post.content} />
                  </div>
                )}
              </>
            )}
            
            {/* Media Gallery - New multiple media support */}
            {post.mediaItems && post.mediaItems.length > 0 ? (
              <MediaGallery mediaItems={post.mediaItems} post={post} />
            ) : (
              <>
                {/* Legacy single image support */}
                {post.imageUrl && (
                  <div className="mt-3">
                    <img
                      src={post.imageUrl}
                      alt="Post image"
                      className="max-w-full h-auto rounded-lg border border-gray-200"
                    />
                  </div>
                )}

                {/* Legacy single video support */}
                {post.videoUrl && post.videoProcessingStatus === VideoProcessingStatus.Completed && (
                  <div className="mt-3">
                    <div
                      className="flex items-center justify-center bg-black rounded-lg border border-gray-200"
                      style={{
                        maxHeight: '400px',
                        height: '400px',
                        width: '100%'
                      }}
                    >
                      <video
                        src={post.videoUrl}
                        poster={post.videoThumbnailUrl}
                        controls
                        className="max-w-full max-h-full object-contain"
                      >
                        Your browser does not support the video tag.
                      </video>
                    </div>
                  </div>
                )}

                {/* Legacy video processing status */}
                {post.videoProcessingStatus !== null && post.videoProcessingStatus !== VideoProcessingStatus.Completed && (
                  <div className="mt-3 p-4 bg-gray-50 rounded-lg border border-gray-200">
                    <div className="flex items-center space-x-2">
                      <Play className="w-5 h-5 text-gray-400" />
                      <div>
                        {(post.videoProcessingStatus === VideoProcessingStatus.Pending || post.videoProcessingStatus === VideoProcessingStatus.Processing) && (
                          <p className="text-sm text-gray-600">Your video is processing. It will be available on your feed when it has completed.</p>
                        )}
                        {post.videoProcessingStatus === VideoProcessingStatus.Failed && (
                          <p className="text-sm text-red-600">Video processing failed. Please try uploading again.</p>
                        )}
                        {post.videoThumbnailUrl && (
                          <div className="mt-2">
                            <img
                              src={post.videoThumbnailUrl}
                              alt="Video thumbnail"
                              className="w-48 h-auto rounded border border-gray-200"
                            />
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                )}
              </>
            )}

            {/* Link Previews */}
            {post.linkPreviews && post.linkPreviews.length > 0 && (
              <div className="mt-3">
                <LinkPreviewList linkPreviews={post.linkPreviews} />
              </div>
            )}

            {/* Reposted Post Display (includes former quote tweets) */}
            {post.repostedPost && (
              <div className="mt-3 border border-gray-200 rounded-lg p-3 bg-gray-50">
                <div className="flex items-start space-x-3">
                  <UserAvatar user={post.repostedPost.user} size="sm" />
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center space-x-2 mb-1">
                      <Link href={`/profile/${post.repostedPost.user.username}`} className="font-medium text-gray-900 hover:underline">
                        {post.repostedPost.user.username}
                      </Link>
                      <span className="text-gray-500">@{post.repostedPost.user.username}</span>
                      <span className="text-gray-500">·</span>
                      <span className="text-gray-500 text-sm">{formatDate(post.repostedPost.createdAt)}</span>
                    </div>

                    {post.repostedPost.content && (
                      <div className="text-gray-900 mb-2 whitespace-pre-wrap break-words">
                        <ContentWithGifs content={post.repostedPost.content} />
                      </div>
                    )}

                    {/* Reposted Post Media */}
                    {post.repostedPost.mediaItems && post.repostedPost.mediaItems.length > 0 && (
                      <div className="mb-2">
                        <MediaGallery mediaItems={post.repostedPost.mediaItems} post={post.repostedPost} />
                      </div>
                    )}

                    {/* Reposted Post Link Previews */}
                    {post.repostedPost.linkPreviews && post.repostedPost.linkPreviews.length > 0 && (
                      <div className="mb-2">
                        <LinkPreviewList linkPreviews={post.repostedPost.linkPreviews} />
                      </div>
                    )}
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Reaction Counts Display */}
          <ReactionCountsDisplay reactionCounts={post.reactionCounts || []} />

          {/* Actions */}
          <div className="flex items-center justify-between mt-3 max-w-md">
            <ReactionPicker
              reactionCounts={post.reactionCounts || []}
              currentUserReaction={post.currentUserReaction || null}
              totalReactionCount={post.totalReactionCount || post.likeCount || 0}
              onReact={handleReact}
              onRemoveReaction={handleRemoveReaction}
              disabled={reactMutation.isPending || removeReactionMutation.isPending}
            />

            <button
              onClick={() => setShowComments(!showComments)}
              className="flex items-center space-x-2 text-gray-500 hover:text-blue-500 transition-colors group"
            >
              <div className="p-2 rounded-full hover:bg-gray-100">
                <MessageCircle className="w-5 h-5" />
              </div>
              <span className="text-sm">{formatNumber(post.commentCount)}</span>
            </button>

            <button
              onClick={handleRepost}
              className="flex items-center space-x-2 text-gray-500 hover:text-green-500 transition-colors group"
              title="Repost"
            >
              <div className="p-2 rounded-full hover:bg-gray-100">
                <Repeat2 className="w-5 h-5" />
              </div>
              <span className="text-sm">{formatNumber(post.repostCount)}</span>
            </button>

            <button
              onClick={() => setShowShareModal(true)}
              className="flex items-center space-x-2 text-gray-500 hover:text-blue-500 transition-colors group"
            >
              <div className="p-2 rounded-full hover:bg-gray-100">
                <Share className="w-5 h-5" />
              </div>
            </button>

            {/* Report button - only show for other users' posts */}
            {!isOwner && (
              <button
                onClick={() => setShowReportModal(true)}
                className="flex items-center space-x-2 text-gray-500 hover:text-red-500 transition-colors group"
                title="Report this post"
              >
                <div className="p-2 rounded-full hover:bg-gray-100">
                  <Flag className="w-5 h-5" />
                </div>
              </button>
            )}
          </div>

          {/* Comments Section */}
          <CommentList postId={post.id} showComments={showComments} onReply={handleReply} />

          {/* Comment Form */}
          {showComments && (
            <div className="mt-4 border-t border-gray-100 pt-4" data-post-id={post.id}>
              {replyingTo && (
                <div className="mb-3 p-2 bg-blue-50 border border-blue-200 rounded-lg flex items-center justify-between">
                  <span className="text-sm text-blue-700">
                    Replying to <span className="font-semibold">@{replyingTo}</span>
                  </span>
                  <button
                    onClick={handleCancelReply}
                    className="text-blue-600 hover:text-blue-800 text-sm font-medium"
                  >
                    Cancel
                  </button>
                </div>
              )}
              <form onSubmit={handleComment} className="flex space-x-3">
                <div className="w-8 h-8 bg-gray-300 rounded-full flex-shrink-0"></div>
                <div className="flex-1">
                  <textarea
                    value={commentText}
                    onChange={handleCommentTextChange}
                    placeholder={replyingTo ? `Reply to @${replyingTo}` : "Yap your reply"}
                    className="w-full text-sm border border-gray-200 bg-white text-gray-900 placeholder-gray-500 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
                    rows={2}
                    maxLength={256}
                  />

                  {/* GIF Preview */}
                  {selectedCommentGif && (
                    <div className="mt-2 relative inline-block">
                      <img
                        src={selectedCommentGif.previewUrl}
                        alt={selectedCommentGif.title}
                        className="max-w-32 h-auto rounded border border-gray-200"
                      />
                      <button
                        type="button"
                        onClick={removeCommentGif}
                        className="absolute -top-1 -right-1 bg-gray-800 bg-opacity-75 text-white rounded-full p-1 hover:bg-opacity-100 transition-opacity"
                      >
                        <X className="w-3 h-3" />
                      </button>
                      <div className="absolute bottom-1 left-1 bg-gray-800 bg-opacity-75 text-white text-xs px-1 rounded">
                        GIF
                      </div>
                    </div>
                  )}

                  <div className="flex justify-between items-center mt-2">
                    <div className="flex items-center space-x-2">
                      <span className="text-xs text-gray-500">
                        {commentText.length}/256
                      </span>
                      <button
                        type="button"
                        onClick={() => setShowCommentGifPicker(true)}
                        className="text-gray-500 hover:text-gray-700 p-1 rounded transition-colors"
                        title="Add GIF"
                        disabled={selectedCommentGif !== null}
                      >
                        <Smile className="w-4 h-4" />
                      </button>
                    </div>
                    <div className="flex space-x-2">
                      {replyingTo && (
                        <button
                          type="button"
                          onClick={handleCancelReply}
                          className="text-gray-500 hover:text-gray-700 px-3 py-1 rounded-full text-sm transition-colors"
                        >
                          Cancel
                        </button>
                      )}
                      <button
                        type="submit"
                        disabled={(!commentText.trim() && !selectedCommentGif) || commentMutation.isPending}
                        className="bg-blue-500 text-white px-4 py-1 rounded-full text-sm font-semibold hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                        onClick={(e) => {
                          console.log('Reply button clicked:', {
                            disabled: (!commentText.trim() && !selectedCommentGif) || commentMutation.isPending,
                            commentText: commentText.trim(),
                            selectedCommentGif,
                            isPending: commentMutation.isPending
                          });
                        }}
                      >
                        {commentMutation.isPending ? 'Replying...' : 'Reply'}
                      </button>
                    </div>
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

      {/* Report Modal */}
      <ReportModal
        isOpen={showReportModal}
        onClose={() => setShowReportModal(false)}
        postId={post.id}
        contentType="post"
        contentPreview={post.content}
      />

      {/* Repost Modal */}
      <RepostModal
        isOpen={showRepostModal}
        onClose={() => setShowRepostModal(false)}
        repostedPost={post}
        onRepostCreated={() => {
          // Refresh the timeline to show the new repost
          queryClient.invalidateQueries({ queryKey: ['timeline'] });
          queryClient.invalidateQueries({ queryKey: ['publicTimeline'] });
        }}
      />

      {/* Delete Confirmation Modal */}
      {showDeleteConfirm && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-sm w-full mx-4">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">Delete Post</h3>
            <p className="text-gray-600 mb-4">
              Are you sure you want to delete this post? This action cannot be undone.
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

      {/* Comment GIF Picker Modal */}
      <GifPicker
        isOpen={showCommentGifPicker}
        onClose={() => setShowCommentGifPicker(false)}
        onSelectGif={handleCommentGifSelect}
      />
    </>
  );
}
