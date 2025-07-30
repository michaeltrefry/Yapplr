import React, { useState, useRef } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  Image,
  Alert,
  ActivityIndicator,
  Modal,
  Dimensions,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useThemeColors } from '../hooks/useThemeColors';
import { useAuth } from '../contexts/AuthContext';
import { videoCoordinationService } from '../services/VideoCoordinationService';
import { TimelineItem, Post, VideoProcessingStatus, ReactionType, MediaType } from '../types';
import ImageViewer from './ImageViewer';
import VideoPlayer, { VideoPlayerRef } from './VideoPlayer';
import FullScreenVideoViewer from './FullScreenVideoViewer';

// Simple time formatting function
const formatTimeAgo = (dateString: string): string => {
  const now = new Date();
  const date = new Date(dateString);
  const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);

  if (diffInSeconds < 60) return `${diffInSeconds}s`;
  if (diffInSeconds < 3600) return `${Math.floor(diffInSeconds / 60)}m`;
  if (diffInSeconds < 86400) return `${Math.floor(diffInSeconds / 3600)}h`;
  if (diffInSeconds < 2592000) return `${Math.floor(diffInSeconds / 86400)}d`;
  return date.toLocaleDateString();
};
import { ContentHighlight } from '../utils/contentUtils';
import LinkPreviewList from './LinkPreviewList';
import ReportModal from './ReportModal';
import ReactionPicker from './ReactionPicker';
import ReactionCountsDisplay from './ReactionCountsDisplay';
import ContentWithGifs from './ContentWithGifs';
import RepostModal from './RepostModal';

interface PostCardProps {
  item: TimelineItem;
  onLike: (postId: number) => void; // Legacy - will be replaced by onReact
  onReact?: (postId: number, reactionType: ReactionType) => void;
  onRemoveReaction?: (postId: number) => void;
  onUserPress?: (username: string) => void;
  onCommentPress?: (post: Post) => void;
  onCommentCountUpdate?: (postId: number, newCount: number) => void;
  onDelete?: () => void;
  onUnrepost?: () => void;
  onHashtagPress?: (hashtag: string) => void;
}

export default function PostCard({ item, onLike, onReact, onRemoveReaction, onUserPress, onCommentPress, onCommentCountUpdate, onDelete, onUnrepost, onHashtagPress }: PostCardProps) {
  const colors = useThemeColors();
  const { user, api } = useAuth();


  const [imageLoading, setImageLoading] = useState(true);
  const [imageError, setImageError] = useState(false);
  const [showImageViewer, setShowImageViewer] = useState(false);
  const [showVideoViewer, setShowVideoViewer] = useState(false);
  const [selectedVideoUrl, setSelectedVideoUrl] = useState<string>('');
  const [selectedVideoThumbnail, setSelectedVideoThumbnail] = useState<string>('');
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [showReportModal, setShowReportModal] = useState(false);
  const [showRepostModal, setShowRepostModal] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [imageDimensions, setImageDimensions] = useState<{ [key: string]: { width: number; height: number } }>({});

  // Refs for video players to control them externally
  const videoPlayerRefs = useRef<{ [key: string]: VideoPlayerRef | null }>({});

  const styles = createStyles(colors);

  // Helper function to calculate image height based on aspect ratio
  const getImageHeight = (imageKey: string) => {
    const screenWidth = Dimensions.get('window').width;
    const containerWidth = screenWidth - 32; // Account for padding
    const dimensions = imageDimensions[imageKey];

    if (dimensions) {
      const aspectRatio = dimensions.width / dimensions.height;
      const calculatedHeight = containerWidth / aspectRatio;

      // Constrain height between min and max values
      return Math.max(200, Math.min(400, calculatedHeight));
    }

    // Default height if dimensions not loaded yet
    return 200;
  };

  // Helper function to handle image load and store dimensions
  const handleImageLoad = (imageKey: string, event: any) => {
    const { width, height } = event.nativeEvent.source;
    setImageDimensions(prev => ({
      ...prev,
      [imageKey]: { width, height }
    }));
  };

  const openVideoViewer = (videoUrl: string, thumbnailUrl?: string) => {
    console.log('🎥 PostCard: openVideoViewer called with URL:', videoUrl);

    // Pause all inline video players before opening fullscreen
    Object.values(videoPlayerRefs.current).forEach(playerRef => {
      if (playerRef && playerRef.isPlaying()) {
        console.log('🎥 PostCard: Pausing inline video player before fullscreen');
        playerRef.pause();
      }
    });

    setSelectedVideoUrl(videoUrl);
    setSelectedVideoThumbnail(thumbnailUrl || '');
    setShowVideoViewer(true);
    console.log('🎥 PostCard: Video viewer should now be visible');
  };

  const closeVideoViewer = () => {
    setShowVideoViewer(false);
    setSelectedVideoUrl('');
    setSelectedVideoThumbnail('');
  };

  // Check ownership
  const isPostOwner = user && user.id === item.post.user.id;
  const isRepostOwner = user && item.type === 'repost' && item.repostedBy && user.id === item.repostedBy.id;

  // Delete handlers
  const handleDeletePost = async () => {
    if (!isPostOwner) return;

    setIsDeleting(true);
    try {
      await api.posts.deletePost(item.post.id);
      setShowDeleteConfirm(false);
      onDelete?.();
    } catch (error) {
      console.error('Error deleting post:', error);
      Alert.alert('Error', 'Failed to delete post. Please try again.');
    } finally {
      setIsDeleting(false);
    }
  };

  const handleUnrepost = async () => {
    if (!isRepostOwner) return;

    setIsDeleting(true);
    try {
      await api.posts.deletePost(item.post.id);
      setShowDeleteConfirm(false);
      onUnrepost?.();
    } catch (error) {
      console.error('Error removing repost:', error);
      Alert.alert('Error', 'Failed to remove repost. Please try again.');
    } finally {
      setIsDeleting(false);
    }
  };

  const handleDeleteConfirm = () => {
    if (item.type === 'repost' && isRepostOwner) {
      handleUnrepost();
    } else if (isPostOwner) {
      handleDeletePost();
    }
  };

  return (
    <View style={styles.postCard}>
      <View style={styles.postHeader}>
        <TouchableOpacity
          style={styles.userInfo}
          onPress={() => onUserPress?.(item.post.user.username)}
          activeOpacity={0.7}
        >
          <View style={styles.avatar}>
            {item.post.user.profileImageUrl ? (
              <Image
                source={{ uri: item.post.user.profileImageUrl }}
                style={styles.profileImage}
                onError={() => {
                  console.log('Failed to load profile image in timeline');
                }}
              />
            ) : (
              <Text style={styles.avatarText}>
                {item.post.user.username.charAt(0).toUpperCase()}
              </Text>
            )}
          </View>
          <View>
            <Text style={styles.username}>@{item.post.user.username}</Text>
            <Text style={styles.timestamp}>
              {new Date(item.post.createdAt).toLocaleDateString()}
            </Text>
          </View>
        </TouchableOpacity>

        <View style={styles.headerRight}>
          {item.type === 'repost' && item.repostedBy && (
            <TouchableOpacity
              style={styles.repostBadge}
              onPress={() => onUserPress?.(item.repostedBy!.username)}
              activeOpacity={0.7}
            >
              <Ionicons name="repeat" size={14} color="#10B981" />
              <Text style={styles.repostText}>
                Reposted by @{item.repostedBy.username}
              </Text>
            </TouchableOpacity>
          )}

          {(isPostOwner || isRepostOwner) && (
            <TouchableOpacity
              style={styles.deleteButton}
              onPress={() => setShowDeleteConfirm(true)}
              activeOpacity={0.7}
            >
              <Ionicons name="trash-outline" size={18} color="#EF4444" />
            </TouchableOpacity>
          )}
        </View>
      </View>

      {/* Display repost content if it exists */}
      {item.type === 'repost' && item.post.content && item.post.content.trim() && (
        <ContentWithGifs
          content={item.post.content}
          style={styles.postContent}
          textStyle={{ color: colors.text }}
          onMentionPress={onUserPress}
          onHashtagPress={onHashtagPress}
          onLinkPress={(url) => {
            // Handle link press - could open in browser or in-app
            console.log('Link pressed:', url);
          }}
          maxGifWidth={300}
        />
      )}

      {/* Display original post content for non-reposts */}
      {item.type !== 'repost' && item.post.content && item.post.content.trim() && (
        <ContentWithGifs
          content={item.post.content}
          style={styles.postContent}
          textStyle={{ color: colors.text }}
          onMentionPress={onUserPress}
          onHashtagPress={onHashtagPress}
          onLinkPress={(url) => {
            // Handle link press - could open in browser or in-app
            console.log('Link pressed:', url);
          }}
          maxGifWidth={300}
        />
      )}

      {/* Display original post content for reposts */}
      {item.type === 'repost' && item.post.repostedPost && (
        <View style={styles.repostedPostContainer}>
          {/* Repost's own media (if any) */}
          {item.post.mediaItems && item.post.mediaItems.length > 0 && (
            <View style={styles.mediaContainer}>
              {item.post.mediaItems.map((media, index) => (
                <View key={media.id} style={styles.mediaItem}>
                  {media.mediaType === MediaType.Image && media.imageUrl && (
                    <TouchableOpacity
                      activeOpacity={0.9}
                      onPress={() => setShowImageViewer(true)}
                    >
                      <Image
                        source={{ uri: media.imageUrl }}
                        style={[
                          styles.postImage,
                          { height: getImageHeight(`media-${media.id}`) }
                        ]}
                        resizeMode="contain"
                        onLoad={(event) => {
                          handleImageLoad(`media-${media.id}`, event);
                          setImageLoading(false);
                        }}
                        onError={() => {
                          setImageLoading(false);
                          setImageError(true);
                        }}
                        onLoadStart={() => {
                          setImageLoading(true);
                          setImageError(false);
                        }}
                      />
                    </TouchableOpacity>
                  )}
                  {media.mediaType === MediaType.Gif && media.gifUrl && (
                    <View style={styles.gifContainer}>
                      <Image
                        source={{ uri: media.gifUrl }}
                        style={[
                          styles.postImage,
                          { height: getImageHeight(`gif-${media.id}`) }
                        ]}
                        resizeMode="contain"
                        onLoad={(event) => handleImageLoad(`gif-${media.id}`, event)}
                      />
                      <View style={styles.gifBadge}>
                        <Text style={styles.gifBadgeText}>GIF</Text>
                      </View>
                    </View>
                  )}
                  {media.mediaType === MediaType.Video && media.videoUrl && (
                    <VideoPlayer
                      ref={(ref) => {
                        if (ref) {
                          videoPlayerRefs.current[media.id] = ref;
                        }
                      }}
                      videoUrl={media.videoUrl}
                      thumbnailUrl={media.videoThumbnailUrl}
                      aspectRatio={media.width && media.height ? media.width / media.height : 16/9}
                      onPress={() => {
                        setSelectedVideo({
                          url: media.videoUrl!,
                          thumbnail: media.videoThumbnailUrl,
                          aspectRatio: media.width && media.height ? media.width / media.height : 16/9
                        });
                        setShowFullScreenVideo(true);
                      }}
                      style={styles.postVideo}
                    />
                  )}
                </View>
              ))}
            </View>
          )}

          {/* Original author attribution */}
          <View style={styles.originalAuthorHeader}>
            <TouchableOpacity
              style={styles.originalAuthorInfo}
              onPress={() => onUserPress(item.post.repostedPost!.user.username)}
            >
              <Image
                source={{ uri: item.post.repostedPost.user.profileImageUrl || 'https://via.placeholder.com/40' }}
                style={styles.originalAuthorAvatar}
              />
              <View style={styles.originalAuthorText}>
                <Text style={styles.originalAuthorHandle}>
                  @{item.post.repostedPost.user.username}
                </Text>
              </View>
            </TouchableOpacity>
            <Text style={styles.originalPostTimestamp}>
              {formatTimeAgo(item.post.repostedPost.createdAt)}
            </Text>
          </View>

          {/* Original post content */}
          {item.post.repostedPost.content && item.post.repostedPost.content.trim() && (
            <ContentWithGifs
              content={item.post.repostedPost.content}
              style={styles.postContent}
              textStyle={{ color: colors.text }}
              onMentionPress={onUserPress}
              onHashtagPress={onHashtagPress}
              onLinkPress={(url) => {
                // Handle link press - could open in browser or in-app
                console.log('Link pressed:', url);
              }}
              maxGifWidth={300}
            />
          )}

          {/* Original post media */}
          {item.post.repostedPost.mediaItems && item.post.repostedPost.mediaItems.length > 0 && (
            <View style={styles.mediaContainer}>
              {item.post.repostedPost.mediaItems.map((media, index) => (
                <View key={media.id} style={styles.mediaItem}>
                  {media.mediaType === MediaType.Image && media.imageUrl && (
                    <TouchableOpacity
                      activeOpacity={0.9}
                      onPress={() => setShowImageViewer(true)}
                    >
                      <Image
                        source={{ uri: media.imageUrl }}
                        style={[
                          styles.postImage,
                          { height: getImageHeight(`media-${media.id}`) }
                        ]}
                        resizeMode="contain"
                        onLoad={(event) => {
                          handleImageLoad(`media-${media.id}`, event);
                          setImageLoading(false);
                        }}
                        onError={() => {
                          setImageLoading(false);
                          setImageError(true);
                        }}
                        onLoadStart={() => {
                          setImageLoading(true);
                          setImageError(false);
                        }}
                      />
                    </TouchableOpacity>
                  )}
                  {media.mediaType === MediaType.Gif && media.gifUrl && (
                    <View style={styles.gifContainer}>
                      <Image
                        source={{ uri: media.gifUrl }}
                        style={[
                          styles.postImage,
                          { height: getImageHeight(`gif-${media.id}`) }
                        ]}
                        resizeMode="contain"
                        onLoad={(event) => handleImageLoad(`gif-${media.id}`, event)}
                      />
                      <View style={styles.gifBadge}>
                        <Text style={styles.gifBadgeText}>GIF</Text>
                      </View>
                    </View>
                  )}
                  {media.mediaType === MediaType.Video && media.videoUrl && (
                    <VideoPlayer
                      ref={(ref) => {
                        if (ref) {
                          videoPlayerRefs.current[media.id] = ref;
                        }
                      }}
                      videoUrl={media.videoUrl}
                      thumbnailUrl={media.videoThumbnailUrl}
                      aspectRatio={media.width && media.height ? media.width / media.height : 16/9}
                      onPress={() => {
                        setSelectedVideo({
                          url: media.videoUrl!,
                          thumbnail: media.videoThumbnailUrl,
                          aspectRatio: media.width && media.height ? media.width / media.height : 16/9
                        });
                        setShowFullScreenVideo(true);
                      }}
                      style={styles.postVideo}
                    />
                  )}
                </View>
              ))}
            </View>
          )}
        </View>
      )}



      {/* Media Display - only for non-reposts (repost media is handled inside the reposted post container) */}
      {item.type !== 'repost' && item.post.mediaItems && item.post.mediaItems.length > 0 && (
        <View style={styles.mediaContainer}>
          {item.post.mediaItems.map((media, index) => (
            <View key={media.id} style={styles.mediaItem}>
              {media.mediaType === MediaType.Image && media.imageUrl && (
                <TouchableOpacity
                  activeOpacity={0.9}
                  onPress={() => setShowImageViewer(true)}
                >
                  <Image
                    source={{ uri: media.imageUrl }}
                    style={[
                      styles.postImage,
                      { height: getImageHeight(`media-${media.id}`) }
                    ]}
                    resizeMode="contain"
                    onLoad={(event) => {
                      handleImageLoad(`media-${media.id}`, event);
                      setImageLoading(false);
                    }}
                    onError={() => {
                      setImageLoading(false);
                      setImageError(true);
                    }}
                    onLoadStart={() => {
                      setImageLoading(true);
                      setImageError(false);
                    }}
                  />
                </TouchableOpacity>
              )}
              {media.mediaType === MediaType.Video && (
                <>
                  {/* Debug logging for video media */}
                  {console.log('🎥 PostCard: Video media debug:', {
                    mediaId: media.id,
                    videoUrl: media.videoUrl,
                    videoThumbnailUrl: media.videoThumbnailUrl,
                    videoProcessingStatus: media.videoProcessingStatus,
                    width: media.width,
                    height: media.height,
                    hasVideoUrl: !!media.videoUrl,
                    videoUrlLength: media.videoUrl?.length || 0
                  })}
                  {media.videoProcessingStatus === VideoProcessingStatus.Completed && media.videoUrl && media.width && media.height ? (
                    <VideoPlayer
                      ref={(ref) => {
                        if (ref) {
                          videoPlayerRefs.current[`media-${media.id}`] = ref;
                        }
                      }}
                      playerId={`media-${media.id}`}
                      videoUrl={media.videoUrl}
                      thumbnailUrl={media.videoThumbnailUrl}
                      style={styles.postImage}
                      autoPlay={false}
                      showControls={true}
                      width={media.width}
                      height={media.height}
                      onFullscreenPress={() => openVideoViewer(media.videoUrl!, media.videoThumbnailUrl)}
                    />
                  ) : (
                    <View style={styles.videoProcessingContainer}>
                      <View style={styles.videoProcessingContent}>
                        <Ionicons name="play-outline" size={20} color="#6B7280" />
                        <View style={styles.videoProcessingText}>
                          {(media.videoProcessingStatus === VideoProcessingStatus.Pending ||
                            media.videoProcessingStatus === VideoProcessingStatus.Processing) && (
                            <Text style={styles.videoProcessingMessage}>
                              Your video is processing. It will be available when completed.
                            </Text>
                          )}
                          {media.videoProcessingStatus === VideoProcessingStatus.Failed && (
                            <Text style={[styles.videoProcessingMessage, styles.videoProcessingError]}>
                              Video processing failed. Please try uploading again.
                            </Text>
                          )}
                          {media.videoThumbnailUrl && (
                            <Image
                              source={{ uri: media.videoThumbnailUrl }}
                              style={styles.videoThumbnail}
                              resizeMode="cover"
                            />
                          )}
                        </View>
                      </View>
                    </View>
                  )}
                </>
              )}
              {media.mediaType === MediaType.Gif && media.gifUrl && (
                <View style={styles.gifContainer}>
                  <Image
                    source={{ uri: media.gifUrl }}
                    style={[
                      styles.postImage,
                      { height: getImageHeight(`gif-${media.id}`) }
                    ]}
                    resizeMode="contain"
                    onLoad={(event) => handleImageLoad(`gif-${media.id}`, event)}
                  />
                  <View style={styles.gifBadge}>
                    <Text style={styles.gifBadgeText}>GIF</Text>
                  </View>
                </View>
              )}
            </View>
          ))}
        </View>
      )}

      {/* Legacy Image Display (for backward compatibility) */}
      {!item.post.mediaItems && item.post.imageUrl && (
        <View style={styles.imageContainer}>
          {!imageError ? (
            <>
              <TouchableOpacity
                activeOpacity={0.9}
                onPress={() => setShowImageViewer(true)}
              >
                <Image
                  source={{ uri: item.post.imageUrl }}
                  style={[
                    styles.postImage,
                    { height: getImageHeight(`legacy-${item.post.id}`) }
                  ]}
                  resizeMode="contain"
                  onLoad={(event) => {
                    console.log('Image loaded successfully:', item.post.imageUrl);
                    handleImageLoad(`legacy-${item.post.id}`, event);
                    setImageLoading(false);
                  }}
                  onError={(error) => {
                    console.warn('Failed to load image:', item.post.imageUrl);
                    console.warn('Image error details:', error.nativeEvent.error);
                    setImageLoading(false);
                    setImageError(true);
                  }}
                  onLoadStart={() => {
                    console.log('Started loading image:', item.post.imageUrl);
                    setImageLoading(true);
                    setImageError(false);
                  }}
                />
              </TouchableOpacity>
              {imageLoading && (
                <View style={styles.imageLoadingOverlay}>
                  <ActivityIndicator size="large" color="#3B82F6" />
                </View>
              )}
            </>
          ) : (
            <View style={styles.imageErrorContainer}>
              <Text style={styles.imageErrorText}>Failed to load image</Text>
            </View>
          )}
        </View>
      )}

      {/* Legacy Video Display (for backward compatibility) */}
      {/* Debug logging for legacy video */}
      {!item.post.mediaItems && console.log('🎥 PostCard: Legacy video debug:', {
        postId: item.post.id,
        videoUrl: item.post.videoUrl,
        videoThumbnailUrl: item.post.videoThumbnailUrl,
        videoProcessingStatus: item.post.videoProcessingStatus,
        videoWidth: item.post.videoWidth,
        videoHeight: item.post.videoHeight,
        hasVideoUrl: !!item.post.videoUrl,
        videoUrlLength: item.post.videoUrl?.length || 0
      })}
      {!item.post.mediaItems && item.post.videoUrl && item.post.videoProcessingStatus === VideoProcessingStatus.Completed && (
        <View style={styles.mediaContainer}>
          <VideoPlayer
            ref={(ref) => {
              if (ref) {
                videoPlayerRefs.current[`legacy-${item.post.id}`] = ref;
              }
            }}
            playerId={`legacy-${item.post.id}`}
            videoUrl={item.post.videoUrl}
            thumbnailUrl={item.post.videoThumbnailUrl}
            style={styles.postImage}
            autoPlay={false}
            showControls={true}
            width={item.post.videoWidth}
            height={item.post.videoHeight}
            onFullscreenPress={() => openVideoViewer(item.post.videoUrl!, item.post.videoThumbnailUrl)}
          />
        </View>
      )}

      {/* Link Previews */}
      {item.post.linkPreviews && item.post.linkPreviews.length > 0 && (
        <LinkPreviewList
          linkPreviews={item.post.linkPreviews}
          style={styles.linkPreviewContainer}
        />
      )}

      {/* Quoted Post Display */}
      {item.post.quotedPost && (
        <View style={[styles.quotedPost, { borderColor: colors.border, backgroundColor: colors.cardBackground }]}>
          <View style={styles.quotedPostHeader}>
            <Image
              source={{ uri: item.post.quotedPost.user.profileImageUrl || 'https://via.placeholder.com/32' }}
              style={styles.quotedAvatar}
            />
            <TouchableOpacity onPress={() => onUserPress?.(item.post.quotedPost?.user.username)}>
              <Text style={[styles.quotedUsername, { color: colors.text }]}>
                {item.post.quotedPost?.user.username}
              </Text>
            </TouchableOpacity>
            <Text style={[styles.quotedDate, { color: colors.textSecondary }]}>
              {new Date(item.post.quotedPost.createdAt).toLocaleDateString()}
            </Text>
          </View>

          {item.post.quotedPost.content && (
            <ContentWithGifs
              content={item.post.quotedPost.content}
              style={styles.quotedContent}
              textStyle={{ color: colors.text }}
              onMentionPress={onUserPress}
              onHashtagPress={onHashtagPress}
              maxGifWidth={250}
            />
          )}

          {item.post.quotedPost.mediaItems && item.post.quotedPost.mediaItems.length > 0 && (
            <View style={styles.quotedMedia}>
              <Image
                source={{
                  uri: item.post.quotedPost.mediaItems[0].imageUrl ||
                       item.post.quotedPost.mediaItems[0].videoThumbnailUrl
                }}
                style={styles.quotedMediaImage}
                resizeMode="cover"
              />
              {item.post.quotedPost.mediaItems.length > 1 && (
                <View style={styles.mediaCountBadge}>
                  <Text style={styles.mediaCountText}>
                    +{item.post.quotedPost.mediaItems.length - 1}
                  </Text>
                </View>
              )}
            </View>
          )}
        </View>
      )}

      {/* Reaction counts display */}
      <ReactionCountsDisplay reactionCounts={item.post.reactionCounts} overlap={true} />

      <View style={styles.postActions}>
        <ReactionPicker
          reactionCounts={item.post.reactionCounts}
          currentUserReaction={item.post.currentUserReaction}
          totalReactionCount={item.post.totalReactionCount || item.post.likeCount || 0}
          onReact={(reactionType) => onReact ? onReact(item.post.id, reactionType) : onLike(item.post.id)}
          onRemoveReaction={() => onRemoveReaction ? onRemoveReaction(item.post.id) : onLike(item.post.id)}
        />

        <TouchableOpacity
          style={styles.actionButton}
          onPress={() => onCommentPress?.(item.post)}
        >
          <Ionicons name="chatbubble-outline" size={20} color="#6B7280" />
          <Text style={styles.actionText}>{item.post.commentCount}</Text>
        </TouchableOpacity>

        <TouchableOpacity
          style={styles.actionButton}
          onPress={() => setShowRepostModal(true)}
        >
          <Ionicons name="repeat-outline" size={20} color="#6B7280" />
          <Text style={styles.actionText}>{item.post.repostCount}</Text>
        </TouchableOpacity>

        {/* Report button - only show for other users' posts */}
        {!isPostOwner && (
          <TouchableOpacity
            style={styles.actionButton}
            onPress={() => setShowReportModal(true)}
          >
            <Ionicons name="flag-outline" size={20} color="#6B7280" />
          </TouchableOpacity>
        )}
      </View>

      {/* Full-Screen Image Viewer */}
      {item.post.imageUrl && (
        <ImageViewer
          visible={showImageViewer}
          imageUrl={item.post.imageUrl}
          onClose={() => setShowImageViewer(false)}
        />
      )}

      {/* Full-Screen Video Viewer */}
      <FullScreenVideoViewer
        visible={showVideoViewer}
        videoUrl={selectedVideoUrl}
        thumbnailUrl={selectedVideoThumbnail}
        onClose={closeVideoViewer}
      />

      {/* Delete Confirmation Modal */}
      <Modal
        visible={showDeleteConfirm}
        transparent={true}
        animationType="fade"
        onRequestClose={() => setShowDeleteConfirm(false)}
      >
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <Text style={styles.modalTitle}>
              {item.type === 'repost' && isRepostOwner ? 'Remove Repost' : 'Delete Post'}
            </Text>
            <Text style={styles.modalMessage}>
              {item.type === 'repost' && isRepostOwner
                ? 'Are you sure you want to remove this repost? This action cannot be undone.'
                : 'Are you sure you want to delete this post? This action cannot be undone.'
              }
            </Text>
            <View style={styles.modalButtons}>
              <TouchableOpacity
                style={styles.cancelButton}
                onPress={() => setShowDeleteConfirm(false)}
                disabled={isDeleting}
              >
                <Text style={styles.cancelButtonText}>Cancel</Text>
              </TouchableOpacity>
              <TouchableOpacity
                style={styles.deleteConfirmButton}
                onPress={handleDeleteConfirm}
                disabled={isDeleting}
              >
                {isDeleting ? (
                  <ActivityIndicator size="small" color="#FFFFFF" />
                ) : (
                  <Text style={styles.deleteButtonText}>
                    {item.type === 'repost' && isRepostOwner ? 'Remove' : 'Delete'}
                  </Text>
                )}
              </TouchableOpacity>
            </View>
          </View>
        </View>
      </Modal>

      {/* Report Modal */}
      <ReportModal
        visible={showReportModal}
        onClose={() => setShowReportModal(false)}
        postId={item.post.id}
        contentType="post"
        contentPreview={item.post.content}
      />

      {/* Repost Modal */}
      <RepostModal
        visible={showRepostModal}
        onClose={() => setShowRepostModal(false)}
        repostedPost={item.post}
      />
    </View>
  );
}

const createStyles = (colors: any) => StyleSheet.create({
  postCard: {
    backgroundColor: colors.card,
    paddingTop: 16,
    paddingHorizontal: 16,
    paddingBottom: 4, // Reduced bottom padding to bring border line closer
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  postHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'flex-start',
    marginBottom: 8,
  },
  headerRight: {
    alignItems: 'flex-end',
  },
  userInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 4,
  },
  avatar: {
    width: 40,
    height: 40,
    borderRadius: 20,
    backgroundColor: colors.primary,
    alignItems: 'center',
    justifyContent: 'center',
    marginRight: 12,
    overflow: 'hidden',
  },
  profileImage: {
    width: 40,
    height: 40,
    borderRadius: 20,
  },
  avatarText: {
    color: colors.primaryText,
    fontWeight: '600',
    fontSize: 16,
  },
  username: {
    fontSize: 16,
    fontWeight: '600',
    color: colors.text,
    marginBottom: 2,
  },
  timestamp: {
    fontSize: 14,
    color: colors.textSecondary,
  },
  repostBadge: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: 4,
  },
  repostText: {
    fontSize: 12,
    color: colors.success,
    marginLeft: 4,
    fontWeight: '500',
  },
  postContent: {
    fontSize: 16,
    lineHeight: 22,
    color: colors.text,
    marginBottom: 12,
  },
  imageContainer: {
    marginBottom: 12,
    borderRadius: 12,
    overflow: 'hidden',
  },
  postImage: {
    width: '100%',
    backgroundColor: colors.surface,
    // Height will be set dynamically based on image aspect ratio
  },
  postActions: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    paddingTop: 6, // Reduced from 8
    paddingBottom: 2, // Minimal bottom padding to bring line closer
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  actionButton: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 6, // Reduced from 8
    paddingHorizontal: 12,
  },
  actionText: {
    marginLeft: 6,
    fontSize: 14,
    color: colors.textSecondary,
    fontWeight: '500',
  },
  imageLoadingOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: colors.background === '#FFFFFF' ? 'rgba(255, 255, 255, 0.8)' : 'rgba(17, 24, 39, 0.8)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  imageErrorContainer: {
    height: 200, // Fixed height for error state
    backgroundColor: colors.surface,
    justifyContent: 'center',
    alignItems: 'center',
    borderRadius: 12,
  },
  imageErrorText: {
    color: colors.textSecondary,
    fontSize: 14,
    fontWeight: '500',
  },
  deleteButton: {
    padding: 8,
    borderRadius: 20,
    backgroundColor: colors.surface,
    marginTop: 4,
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'center',
    alignItems: 'center',
    padding: 20,
  },
  modalContent: {
    backgroundColor: colors.card,
    borderRadius: 12,
    padding: 24,
    width: '100%',
    maxWidth: 320,
  },
  modalTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: colors.text,
    marginBottom: 12,
    textAlign: 'center',
  },
  modalMessage: {
    fontSize: 14,
    color: colors.textSecondary,
    marginBottom: 24,
    textAlign: 'center',
    lineHeight: 20,
  },
  modalButtons: {
    flexDirection: 'row',
    gap: 12,
  },
  cancelButton: {
    flex: 1,
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.surface,
  },
  cancelButtonText: {
    color: colors.text,
    fontSize: 16,
    fontWeight: '500',
    textAlign: 'center',
  },
  deleteConfirmButton: {
    flex: 1,
    paddingVertical: 12,
    paddingHorizontal: 16,
    borderRadius: 8,
    backgroundColor: '#EF4444',
    justifyContent: 'center',
    alignItems: 'center',
  },
  deleteButtonText: {
    color: '#FFFFFF',
    fontSize: 16,
    fontWeight: '500',
    textAlign: 'center',
  },
  linkPreviewContainer: {
    marginTop: 12,
  },
  videoProcessingContainer: {
    marginTop: 12,
    padding: 16,
    backgroundColor: colors.surface,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: colors.border,
  },
  videoProcessingContent: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: 8,
  },
  videoProcessingText: {
    flex: 1,
  },
  videoProcessingMessage: {
    fontSize: 14,
    color: colors.textSecondary,
    lineHeight: 20,
  },
  videoProcessingError: {
    color: '#EF4444',
  },
  videoThumbnail: {
    width: 120,
    height: 90,
    borderRadius: 6,
    marginTop: 8,
  },
  mediaContainer: {
    marginTop: 12,
    marginBottom: 12, // Add bottom margin for spacing before post actions
  },
  mediaItem: {
    marginBottom: 8,
  },
  gifContainer: {
    position: 'relative',
  },
  gifBadge: {
    position: 'absolute',
    bottom: 8,
    left: 8,
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
    paddingHorizontal: 6,
    paddingVertical: 3,
    borderRadius: 4,
  },
  gifBadgeText: {
    color: '#fff',
    fontSize: 10,
    fontWeight: '600',
  },
  quotedPost: {
    borderWidth: 1,
    borderRadius: 12,
    padding: 12,
    marginTop: 12,
  },
  quotedPostHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 8,
  },
  quotedAvatar: {
    width: 24,
    height: 24,
    borderRadius: 12,
    marginRight: 8,
  },
  quotedUsername: {
    fontSize: 14,
    fontWeight: '600',
    marginRight: 8,
  },
  quotedDate: {
    fontSize: 12,
    marginLeft: 'auto',
  },
  quotedContent: {
    marginBottom: 8,
  },
  quotedMedia: {
    position: 'relative',
  },
  quotedMediaImage: {
    width: '100%',
    height: 120,
    borderRadius: 8,
  },
  mediaCountBadge: {
    position: 'absolute',
    bottom: 8,
    right: 8,
    backgroundColor: 'rgba(0,0,0,0.7)',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 12,
  },
  mediaCountText: {
    color: 'white',
    fontSize: 12,
    fontWeight: '600',
  },
  repostedPostContainer: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: 8,
    padding: 12,
    marginTop: 8,
    backgroundColor: colors.card,
  },
  originalAuthorHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    marginBottom: 8,
  },
  originalAuthorInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  originalAuthorAvatar: {
    width: 32,
    height: 32,
    borderRadius: 16,
    marginRight: 8,
  },
  originalAuthorText: {
    flex: 1,
  },
  originalAuthorHandle: {
    fontSize: 14,
    fontWeight: '500',
    color: colors.text,
  },
  originalPostTimestamp: {
    fontSize: 12,
    color: colors.textSecondary,
  },
});
