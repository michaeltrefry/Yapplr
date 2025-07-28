import React, { useState, useRef } from 'react';
import {
  View,
  StyleSheet,
  TouchableOpacity,
  Dimensions,
  ActivityIndicator,
  Text,
  Image,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useThemeColors } from '../hooks/useThemeColors';

// Try to import expo-video, but provide fallback if not available
let VideoView: any = null;
let useVideoPlayer: any = null;

try {
  const expoVideo = require('expo-video');
  VideoView = expoVideo.VideoView;
  useVideoPlayer = expoVideo.useVideoPlayer;
  console.log('✅ expo-video successfully imported:', {
    VideoView: !!VideoView,
    useVideoPlayer: !!useVideoPlayer,
    expoVideoModule: !!expoVideo
  });
} catch (error) {
  console.log('❌ expo-video not available, using fallback video component');
  console.error('expo-video import error:', error);
}

interface VideoPlayerProps {
  videoUrl: string;
  thumbnailUrl?: string;
  style?: any;
  autoPlay?: boolean;
  showControls?: boolean;
  resizeMode?: 'contain' | 'cover' | 'fill' | 'scaleDown';
  onError?: (error: string) => void;
  onLoad?: () => void;
  onFullscreenPress?: () => void;
}

export default function VideoPlayer({
  videoUrl,
  thumbnailUrl,
  style,
  autoPlay = false,
  showControls = true,
  resizeMode = 'contain',
  onError,
  onLoad,
  onFullscreenPress,
}: VideoPlayerProps) {
  const colors = useThemeColors();
  const [isLoading, setIsLoading] = useState(true);
  const [hasError, setHasError] = useState(false);
  const [showPlayButton, setShowPlayButton] = useState(!autoPlay);
  const [showControlsOverlay, setShowControlsOverlay] = useState(true);
  const controlsTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  const styles = createStyles(colors);

  // Create video player using the new expo-video hook
  const player = useVideoPlayer ? useVideoPlayer(videoUrl, (player: any) => {
    console.log('🎥 VideoPlayer: Creating player for URL:', videoUrl);
    console.log('🎥 VideoPlayer: URL validation:', {
      isValidUrl: videoUrl.startsWith('http'),
      hasCorrectPort: videoUrl.includes(':8080'),
      urlLength: videoUrl.length
    });
    player.loop = false;
    player.muted = false;

    // Add event listeners for debugging and state management
    player.addListener('statusChange', (status: any) => {
      console.log('🎥 VideoPlayer status change:', status);

      // Update loading state based on video status
      if (status.status === 'readyToPlay' || status.status === 'idle') {
        setIsLoading(false);
        if (onLoad && isLoading) {
          onLoad();
        }
      } else if (status.status === 'loading') {
        setIsLoading(true);
      }
    });

    player.addListener('playbackError', (error: any) => {
      console.error('🎥 VideoPlayer playback error:', error);
      setIsLoading(false);
      setHasError(true);
      if (onError) {
        onError(`Video playback error: ${error.message || 'Unknown error'}`);
      }
    });

    if (autoPlay) {
      player.play();
    }
  }) : null;

  console.log('VideoPlayer: useVideoPlayer available:', !!useVideoPlayer);
  console.log('VideoPlayer: VideoView available:', !!VideoView);
  console.log('VideoPlayer: player created:', !!player);

  // If expo-video is not available, show a fallback component
  if (!VideoView || !useVideoPlayer) {
    return (
      <View style={[styles.container, style]}>
        <TouchableOpacity
          style={styles.videoContainer}
          onPress={() => {
            if (onError) {
              onError('Video playback not available - expo-av not configured');
            }
          }}
          activeOpacity={0.9}
        >
          {thumbnailUrl ? (
            <Image
              source={{ uri: thumbnailUrl }}
              style={styles.video}
              resizeMode="cover"
            />
          ) : (
            <View style={styles.placeholderContainer}>
              <Ionicons name="videocam-outline" size={64} color={colors.textSecondary} />
            </View>
          )}

          <View style={styles.playButtonContainer}>
            <View style={styles.playButton}>
              <Ionicons name="play" size={32} color="#fff" />
            </View>
          </View>

          <View style={styles.fallbackBadge}>
            <Text style={styles.fallbackText}>Video</Text>
          </View>
        </TouchableOpacity>
      </View>
    );
  }



  const handleSeek = async (seconds: number) => {
    if (!player) return;

    try {
      // Use seekBy method for relative seeking (more efficient)
      player.seekBy(seconds);
      console.log(`🎥 VideoPlayer: Seeked ${seconds}s using seekBy()`);
    } catch (error) {
      console.error('🎥 VideoPlayer: Error seeking:', error);
    }
  };

  const hideControlsAfterDelay = () => {
    if (controlsTimeoutRef.current) {
      clearTimeout(controlsTimeoutRef.current);
    }

    if (player?.playing) {
      controlsTimeoutRef.current = setTimeout(() => {
        setShowControlsOverlay(false);
        setShowPlayButton(false);
      }, 3000);
    }
  };

  const handleVideoPress = async () => {
    console.log('🎥 VideoPlayer: handleVideoPress called');

    // Toggle controls visibility
    setShowControlsOverlay(!showControlsOverlay);

    // If showing controls and video is playing, auto-hide after delay
    if (!showControlsOverlay && player?.playing) {
      hideControlsAfterDelay();
    }
  };

  const handlePlayPress = async () => {
    if (!player) return;

    try {
      if (player.playing) {
        // Pause the video
        console.log('🎥 VideoPlayer: Attempting to pause video');
        player.pause();
        setShowPlayButton(true);
        setShowControlsOverlay(true); // Show controls when paused
        console.log('🎥 VideoPlayer: Video paused');
      } else {
        // Play the video
        console.log('🎥 VideoPlayer: Attempting to play video');
        player.play();
        setShowPlayButton(false);
        console.log('🎥 VideoPlayer: Video play() called');

        // Auto-hide controls after delay when playing
        hideControlsAfterDelay();
      }
    } catch (error) {
      console.error('🎥 VideoPlayer: Error toggling playback:', error);
      setHasError(true);
      if (onError) {
        onError(error instanceof Error ? error.message : 'Playback control error');
      }
    }
  };

  // Clean up timeouts when component unmounts
  React.useEffect(() => {
    return () => {
      if (controlsTimeoutRef.current) {
        clearTimeout(controlsTimeoutRef.current);
      }
    };
  }, []);

  if (hasError) {
    return (
      <View style={[styles.container, styles.errorContainer, style]}>
        <Ionicons name="play-circle-outline" size={64} color={colors.textSecondary} />
        <Text style={styles.errorText}>Failed to load video</Text>
      </View>
    );
  }

  return (
    <View style={[styles.container, style]}>
      <TouchableOpacity
        style={styles.videoContainer}
        onPress={handleVideoPress}
        activeOpacity={0.9}
      >
        <VideoView
          style={styles.video}
          player={player}
          allowsFullscreen={false}
          allowsPictureInPicture={false}
          contentFit={resizeMode}
          nativeControls={false}
        />

        {/* Thumbnail overlay when not loaded */}
        {thumbnailUrl && isLoading && (
          <Image
            source={{ uri: thumbnailUrl }}
            style={styles.video}
            resizeMode="cover"
          />
        )}

        {/* Loading indicator */}
        {isLoading && (
          <View style={styles.overlay}>
            <ActivityIndicator size="large" color={colors.primary} />
          </View>
        )}

        {/* Video Controls */}
        {showControls && !isLoading && (
          <View style={styles.controlsOverlay}>
            {/* Center play/pause button */}
            {(showPlayButton || !player?.playing) && (
              <TouchableOpacity
                style={styles.centerPlayButton}
                onPress={handlePlayPress}
                activeOpacity={0.7}
              >
                <View style={styles.playButton}>
                  <Ionicons
                    name={player?.playing ? "pause" : "play"}
                    size={32}
                    color="#fff"
                  />
                </View>
              </TouchableOpacity>
            )}

            {/* Bottom controls bar */}
            {showControlsOverlay && (
              <View style={styles.bottomControls}>
              <TouchableOpacity
                style={styles.controlButton}
                onPress={() => handleSeek(-10)}
                activeOpacity={0.7}
              >
                <Ionicons name="play-back" size={20} color="#fff" />
              </TouchableOpacity>

              <TouchableOpacity
                style={styles.controlButton}
                onPress={handlePlayPress}
                activeOpacity={0.7}
              >
                <Ionicons
                  name={player?.playing ? "pause" : "play"}
                  size={20}
                  color="#fff"
                />
              </TouchableOpacity>

              <TouchableOpacity
                style={styles.controlButton}
                onPress={() => handleSeek(10)}
                activeOpacity={0.7}
              >
                <Ionicons name="play-forward" size={20} color="#fff" />
              </TouchableOpacity>

              <View style={styles.spacer} />

              {onFullscreenPress && (
                <TouchableOpacity
                  style={styles.controlButton}
                  onPress={onFullscreenPress}
                  activeOpacity={0.7}
                >
                  <Ionicons name="expand" size={20} color="#fff" />
                </TouchableOpacity>
              )}
              </View>
            )}
          </View>
        )}
      </TouchableOpacity>
    </View>
  );
}

const createStyles = (colors: any) => StyleSheet.create({
  container: {
    backgroundColor: colors.surface,
    borderRadius: 8,
    overflow: 'hidden',
  },
  videoContainer: {
    position: 'relative',
    width: '100%',
    aspectRatio: 16 / 9, // Default aspect ratio
  },
  video: {
    width: '100%',
    height: '100%',
  },
  overlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(0, 0, 0, 0.3)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  playButtonContainer: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    justifyContent: 'center',
    alignItems: 'center',
  },
  playButton: {
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
    borderRadius: 40,
    width: 80,
    height: 80,
    justifyContent: 'center',
    alignItems: 'center',
  },
  fullscreenButtonContainer: {
    position: 'absolute',
    bottom: 10,
    right: 10,
  },
  fullscreenButton: {
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
    borderRadius: 15,
    width: 30,
    height: 30,
    justifyContent: 'center',
    alignItems: 'center',
  },
  controlsOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    justifyContent: 'flex-end',
    pointerEvents: 'box-none', // Allow touches to pass through to video
  },
  centerPlayButton: {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: [{ translateX: -40 }, { translateY: -40 }],
  },
  bottomControls: {
    position: 'absolute',
    bottom: 12,
    left: 12,
    right: 12,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: 'rgba(0, 0, 0, 0.7)',
    paddingHorizontal: 16,
    paddingVertical: 10,
    borderRadius: 25,
  },
  controlButton: {
    padding: 12,
    marginHorizontal: 6,
    borderRadius: 20,
    minWidth: 44,
    minHeight: 44,
    justifyContent: 'center',
    alignItems: 'center',
  },
  spacer: {
    flex: 1,
  },
  errorContainer: {
    justifyContent: 'center',
    alignItems: 'center',
    aspectRatio: 16 / 9,
    backgroundColor: colors.surface,
  },
  errorText: {
    color: colors.textSecondary,
    fontSize: 14,
    marginTop: 8,
    textAlign: 'center',
  },
  placeholderContainer: {
    width: '100%',
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: colors.border,
  },
  fallbackBadge: {
    position: 'absolute',
    bottom: 8,
    left: 8,
    backgroundColor: 'rgba(0, 0, 0, 0.7)',
    paddingHorizontal: 8,
    paddingVertical: 4,
    borderRadius: 4,
  },
  fallbackText: {
    color: '#fff',
    fontSize: 12,
    fontWeight: '600',
  },
});
