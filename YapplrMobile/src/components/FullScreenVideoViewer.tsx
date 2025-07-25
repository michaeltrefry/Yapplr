import React, { useState, useRef } from 'react';
import {
  View,
  StyleSheet,
  TouchableOpacity,
  Modal,
  SafeAreaView,
  StatusBar,
  Dimensions,
  Text,
  ActivityIndicator,
  Image,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useThemeColors } from '../hooks/useThemeColors';
import { PostMedia, MediaType } from '../types';

// Try to import expo-video, but provide fallback if not available
let VideoView: any = null;
let useVideoPlayer: any = null;

try {
  const expoVideo = require('expo-video');
  VideoView = expoVideo.VideoView;
  useVideoPlayer = expoVideo.useVideoPlayer;
  console.log('✅ expo-video successfully imported in FullScreenVideoViewer:', {
    VideoView: !!VideoView,
    useVideoPlayer: !!useVideoPlayer
  });
} catch (error) {
  console.log('❌ expo-video not available in FullScreenVideoViewer, using fallback');
  console.error('expo-video import error in FullScreenVideoViewer:', error);
}

interface FullScreenVideoViewerProps {
  visible: boolean;
  videoUrl: string;
  thumbnailUrl?: string;
  onClose: () => void;
  mediaItem?: PostMedia;
}



// Main component that decides whether to use expo-video or fallback
export default function FullScreenVideoViewer({
  visible,
  videoUrl,
  thumbnailUrl,
  onClose,
  mediaItem,
}: FullScreenVideoViewerProps) {
  // If expo-video is not available, show fallback immediately
  if (!VideoView || !useVideoPlayer) {
    return <FullScreenVideoFallback visible={visible} thumbnailUrl={thumbnailUrl} onClose={onClose} />;
  }

  // If expo-video is available, use the video player component
  return (
    <FullScreenVideoPlayer
      visible={visible}
      videoUrl={videoUrl}
      thumbnailUrl={thumbnailUrl}
      onClose={onClose}
      mediaItem={mediaItem}
    />
  );
}

// Component that uses expo-video (always calls hooks consistently)
function FullScreenVideoPlayer({
  visible,
  videoUrl,
  thumbnailUrl,
  onClose,
  mediaItem,
}: FullScreenVideoViewerProps) {
  const colors = useThemeColors();
  const [isLoading, setIsLoading] = useState(true);
  const [hasError, setHasError] = useState(false);
  const [showControls, setShowControls] = useState(true);
  const [currentTime, setCurrentTime] = useState(0);
  const [duration, setDuration] = useState(0);
  const controlsTimeoutRef = useRef<NodeJS.Timeout | null>(null);
  const progressUpdateRef = useRef<NodeJS.Timeout | null>(null);

  const styles = createStyles(colors);
  const { width: screenWidth, height: screenHeight } = Dimensions.get('window');

  // Format time in MM:SS format
  const formatTime = (seconds: number) => {
    const mins = Math.floor(seconds / 60);
    const secs = Math.floor(seconds % 60);
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  // Update progress periodically
  const updateProgress = () => {
    try {
      if (player && player.playing) {
        setCurrentTime(player.currentTime || 0);
        setDuration(player.duration || 0);
      }
    } catch (error) {
      // Player might be disposed, stop updates
      console.log('🎥 FullScreenVideoViewer: Player disposed, stopping progress updates');
      stopProgressUpdates();
    }
  };

  // Start/stop progress updates
  const startProgressUpdates = () => {
    if (progressUpdateRef.current) {
      clearInterval(progressUpdateRef.current);
    }
    progressUpdateRef.current = setInterval(updateProgress, 500);
  };

  const stopProgressUpdates = () => {
    if (progressUpdateRef.current) {
      clearInterval(progressUpdateRef.current);
      progressUpdateRef.current = null;
    }
  };

  // Handle progress bar tap for seeking
  const handleProgressPress = (event: any) => {
    if (!player || duration === 0) return;

    // Get the layout of the progress bar
    event.currentTarget.measure((x: number, y: number, width: number, height: number) => {
      const { locationX } = event.nativeEvent;
      const percentage = locationX / width;
      const seekTime = percentage * duration;

      // Use currentTime property for precise seeking
      player.currentTime = Math.max(0, Math.min(seekTime, duration));
      setCurrentTime(seekTime);

      console.log(`🎥 FullScreenVideoViewer: Seeked to ${formatTime(seekTime)} via progress bar`);
    });
  };

  // Handle seeking by seconds
  const handleSeek = async (seconds: number) => {
    if (!player) return;

    try {
      // Use seekBy method for relative seeking (more efficient)
      player.seekBy(seconds);
      console.log(`🎥 FullScreenVideoViewer: Seeked ${seconds}s using seekBy()`);
    } catch (error) {
      console.error('🎥 FullScreenVideoViewer: Error seeking:', error);
    }
  };

  // Always call the hook (Rules of Hooks) - this component only renders when expo-video is available
  const player = useVideoPlayer(videoUrl, (player: any) => {
    console.log('🎥 FullScreenVideoViewer: Creating player for URL:', videoUrl);
    player.loop = false;
    player.muted = false;

    // Add event listeners for debugging and state management
    player.addListener('statusChange', (status: any) => {
      console.log('🎥 FullScreenVideoViewer status change:', status);

      // Update loading state based on video status
      if (status.status === 'readyToPlay' || status.status === 'idle') {
        setIsLoading(false);
        // Update duration when ready
        setDuration(player.duration || 0);
      } else if (status.status === 'loading') {
        setIsLoading(true);
      }
    });

    // Listen for playing state changes
    player.addListener('playingChange', (playingState: any) => {
      console.log('🎥 FullScreenVideoViewer playing change:', playingState);
      if (playingState.isPlaying) {
        startProgressUpdates();
      } else {
        stopProgressUpdates();
      }
    });

    player.addListener('playbackError', (error: any) => {
      console.error('🎥 FullScreenVideoViewer playback error:', error);
      setIsLoading(false);
      setHasError(true);
    });
  });

  // Handle visibility changes
  React.useEffect(() => {
    if (player) {
      if (!visible) {
        // Pause when modal is hidden
        player.pause();
        stopProgressUpdates();
      }
    }
  }, [visible, player]);



  const handlePlayPress = async () => {
    console.log('🎥 FullScreenVideoViewer: handlePlayPress called');
    console.log('🎥 FullScreenVideoViewer: player exists:', !!player);
    console.log('🎥 FullScreenVideoViewer: player.playing:', player?.playing);

    if (player) {
      try {
        if (player.playing) {
          console.log('🎥 FullScreenVideoViewer: Attempting to pause video');
          player.pause();
          console.log('🎥 FullScreenVideoViewer: Video paused');
        } else {
          console.log('🎥 FullScreenVideoViewer: Attempting to play video');
          player.play();
          console.log('🎥 FullScreenVideoViewer: Video play() called');
        }
      } catch (error) {
        console.error('🎥 FullScreenVideoViewer: Error controlling video playback:', error);
        setHasError(true);
      }
    } else {
      console.log('🎥 FullScreenVideoViewer: No player available');
    }
  };

  const handleScreenPress = () => {
    setShowControls(!showControls);

    // Auto-hide controls after 3 seconds when playing
    if (controlsTimeoutRef.current) {
      clearTimeout(controlsTimeoutRef.current);
    }

    if (player?.playing && !showControls) {
      controlsTimeoutRef.current = setTimeout(() => {
        setShowControls(false);
      }, 3000);
    }
  };



  const handleClose = () => {
    if (player) {
      player.pause();
    }
    setIsLoading(true);
    setHasError(false);
    setShowControls(true);
    onClose();
  };



  // Cleanup timeouts and intervals on unmount
  React.useEffect(() => {
    return () => {
      if (controlsTimeoutRef.current) {
        clearTimeout(controlsTimeoutRef.current);
      }
      if (progressUpdateRef.current) {
        clearInterval(progressUpdateRef.current);
      }
    };
  }, []);

  if (hasError) {
    return (
      <Modal visible={visible} animationType="fade" onRequestClose={handleClose}>
        <StatusBar hidden />
        <SafeAreaView style={styles.container}>
          <View style={styles.errorContainer}>
            <Ionicons name="play-circle-outline" size={80} color={colors.textSecondary} />
            <Text style={styles.errorText}>Failed to load video</Text>
            <TouchableOpacity style={styles.closeButton} onPress={handleClose}>
              <Text style={styles.closeButtonText}>Close</Text>
            </TouchableOpacity>
          </View>
        </SafeAreaView>
      </Modal>
    );
  }

  return (
    <Modal visible={visible} animationType="fade" onRequestClose={handleClose}>
      <StatusBar hidden />
      <SafeAreaView style={styles.container}>
        <TouchableOpacity
          style={styles.videoContainer}
          onPress={handleScreenPress}
          activeOpacity={1}
        >
          <VideoView
            style={[styles.video, { width: screenWidth, height: screenHeight }]}
            player={player}
            allowsFullscreen={true}
            allowsPictureInPicture={false}
            contentFit="contain"
            nativeControls={false}
          />

          {/* Thumbnail overlay when not loaded */}
          {thumbnailUrl && isLoading && (
            <Image
              source={{ uri: thumbnailUrl }}
              style={[styles.video, { width: screenWidth, height: screenHeight }]}
              resizeMode="contain"
            />
          )}

          {/* Loading indicator */}
          {isLoading && (
            <View style={styles.loadingOverlay}>
              <ActivityIndicator size="large" color={colors.primary} />
            </View>
          )}

          {/* Controls overlay */}
          {showControls && !isLoading && (
            <>
              {/* Close button */}
              <TouchableOpacity
                style={styles.closeButtonContainer}
                onPress={handleClose}
                activeOpacity={0.7}
              >
                <View style={styles.controlButton}>
                  <Ionicons name="close" size={24} color="#fff" />
                </View>
              </TouchableOpacity>

              {/* Center Play/Pause button - only show when paused */}
              {!player?.playing && (
                <TouchableOpacity
                  style={styles.playButtonContainer}
                  onPress={handlePlayPress}
                  activeOpacity={0.7}
                >
                  <View style={styles.playButton}>
                    <Ionicons
                      name="play"
                      size={40}
                      color="#fff"
                    />
                  </View>
                </TouchableOpacity>
              )}

              {/* Bottom controls with progress bar */}
              <View style={styles.bottomControlsOverlay}>
                {/* Progress bar */}
                <View style={styles.progressContainer}>
                  <Text style={styles.timeText}>{formatTime(currentTime)}</Text>
                  <TouchableOpacity
                    style={styles.progressBar}
                    onPress={handleProgressPress}
                    activeOpacity={0.8}
                  >
                    <View style={styles.progressTrack}>
                      <View
                        style={[
                          styles.progressFill,
                          { width: duration > 0 ? `${(currentTime / duration) * 100}%` : '0%' }
                        ]}
                      >
                        {duration > 0 && currentTime > 0 && (
                          <View style={styles.progressThumb} />
                        )}
                      </View>
                    </View>
                  </TouchableOpacity>
                  <Text style={styles.timeText}>{formatTime(duration)}</Text>
                </View>

                {/* Control buttons */}
                <View style={styles.controlsRow}>
                  <TouchableOpacity
                    style={styles.seekButton}
                    onPress={() => handleSeek(-10)}
                    activeOpacity={0.7}
                  >
                    <Ionicons name="play-back" size={24} color="#fff" />
                  </TouchableOpacity>

                  <TouchableOpacity
                    style={styles.seekButton}
                    onPress={handlePlayPress}
                    activeOpacity={0.7}
                  >
                    <Ionicons
                      name={player?.playing ? "pause" : "play"}
                      size={24}
                      color="#fff"
                    />
                  </TouchableOpacity>

                  <TouchableOpacity
                    style={styles.seekButton}
                    onPress={() => handleSeek(10)}
                    activeOpacity={0.7}
                  >
                    <Ionicons name="play-forward" size={24} color="#fff" />
                  </TouchableOpacity>
                </View>
              </View>
            </>
          )}
        </TouchableOpacity>
      </SafeAreaView>
    </Modal>
  );
}

// Fallback component when expo-video is not available
function FullScreenVideoFallback({
  visible,
  thumbnailUrl,
  onClose,
}: {
  visible: boolean;
  thumbnailUrl?: string;
  onClose: () => void;
}) {
  const colors = useThemeColors();
  const styles = createStyles(colors);

  return (
    <Modal visible={visible} animationType="fade" onRequestClose={onClose}>
      <StatusBar hidden />
      <SafeAreaView style={styles.container}>
        <View style={styles.fallbackContainer}>
          <TouchableOpacity
            style={styles.closeButtonContainer}
            onPress={onClose}
            activeOpacity={0.7}
          >
            <View style={styles.controlButton}>
              <Ionicons name="close" size={24} color="#fff" />
            </View>
          </TouchableOpacity>

          {thumbnailUrl ? (
            <Image
              source={{ uri: thumbnailUrl }}
              style={styles.fallbackImage}
              resizeMode="contain"
            />
          ) : (
            <View style={styles.fallbackPlaceholder}>
              <Ionicons name="videocam-outline" size={80} color="#666" />
            </View>
          )}

          <View style={styles.fallbackMessage}>
            <Text style={styles.fallbackText}>
              Video playback requires a development build with expo-video configured.
            </Text>
            <Text style={styles.fallbackSubtext}>
              Please rebuild your development build to enable video playback.
            </Text>
          </View>
        </View>
      </SafeAreaView>
    </Modal>
  );
}

const createStyles = (colors: any) => StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#000',
  },
  videoContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  video: {
    flex: 1,
  },
  loadingOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
  },
  closeButtonContainer: {
    position: 'absolute',
    top: 50,
    right: 20,
    zIndex: 1,
  },
  controlButton: {
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
    borderRadius: 25,
    width: 50,
    height: 50,
    justifyContent: 'center',
    alignItems: 'center',
  },
  playButtonContainer: {
    position: 'absolute',
    top: '50%',
    left: '50%',
    marginTop: -50,
    marginLeft: -50,
    zIndex: 1,
  },
  playButton: {
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
    borderRadius: 50,
    width: 100,
    height: 100,
    justifyContent: 'center',
    alignItems: 'center',
  },
  errorContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: colors.background,
    padding: 20,
  },
  errorText: {
    color: colors.textSecondary,
    fontSize: 16,
    marginTop: 16,
    marginBottom: 24,
    textAlign: 'center',
  },
  closeButton: {
    backgroundColor: colors.primary,
    paddingHorizontal: 24,
    paddingVertical: 12,
    borderRadius: 8,
  },
  closeButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  fallbackContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: '#000',
    padding: 20,
  },
  fallbackImage: {
    width: '100%',
    height: '60%',
  },
  fallbackPlaceholder: {
    width: '100%',
    height: '60%',
    justifyContent: 'center',
    alignItems: 'center',
  },
  fallbackMessage: {
    marginTop: 20,
    alignItems: 'center',
  },
  fallbackText: {
    color: '#fff',
    fontSize: 16,
    textAlign: 'center',
    marginBottom: 8,
  },
  fallbackSubtext: {
    color: '#ccc',
    fontSize: 14,
    textAlign: 'center',
  },
  bottomControlsOverlay: {
    position: 'absolute',
    bottom: 40,
    left: 20,
    right: 20,
    flexDirection: 'column',
    backgroundColor: 'rgba(0, 0, 0, 0.8)',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderRadius: 12,
  },
  progressContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 8,
  },
  progressBar: {
    flex: 1,
    height: 20,
    justifyContent: 'center',
    marginHorizontal: 8,
  },
  progressTrack: {
    height: 4,
    backgroundColor: 'rgba(255, 255, 255, 0.3)',
    borderRadius: 2,
    position: 'relative',
  },
  progressFill: {
    height: '100%',
    backgroundColor: '#fff',
    borderRadius: 2,
    position: 'relative',
  },
  progressThumb: {
    position: 'absolute',
    right: -6,
    top: -4,
    width: 12,
    height: 12,
    backgroundColor: '#fff',
    borderRadius: 6,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.3,
    shadowRadius: 2,
    elevation: 3,
  },
  timeText: {
    color: '#fff',
    fontSize: 12,
    fontWeight: '500',
    minWidth: 40,
    textAlign: 'center',
  },
  controlsRow: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
  },
  seekButton: {
    padding: 12,
    marginHorizontal: 8,
    borderRadius: 20,
    minWidth: 48,
    minHeight: 48,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: 'rgba(255, 255, 255, 0.1)',
  },
});
