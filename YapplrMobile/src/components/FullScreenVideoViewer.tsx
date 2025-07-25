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

// Component that uses expo-video hooks
function VideoPlayerComponent({
  videoUrl,
  visible,
  onStatusChange,
  onError,
}: {
  videoUrl: string;
  visible: boolean;
  onStatusChange: (loading: boolean) => void;
  onError: () => void;
}) {
  const player = useVideoPlayer(videoUrl, (player: any) => {
    console.log('🎥 FullScreenVideoViewer: Creating player for URL:', videoUrl);
    player.loop = false;
    player.muted = false;

    // Add event listeners for debugging and state management
    player.addListener('statusChange', (status: any) => {
      console.log('🎥 FullScreenVideoViewer status change:', status);

      // Update loading state based on video status
      if (status.status === 'readyToPlay' || status.status === 'idle') {
        onStatusChange(false);
      } else if (status.status === 'loading') {
        onStatusChange(true);
      }
    });

    player.addListener('playbackError', (error: any) => {
      console.error('🎥 FullScreenVideoViewer playback error:', error);
      onStatusChange(false);
      onError();
    });
  });

  // Handle visibility changes
  React.useEffect(() => {
    if (player) {
      if (!visible) {
        // Pause when modal is hidden
        player.pause();
      }
    }
  }, [visible, player]);

  return player;
}

export default function FullScreenVideoViewer({
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
  const controlsTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  const styles = createStyles(colors);
  const { width: screenWidth, height: screenHeight } = Dimensions.get('window');

  // Only create player component if expo-video is available
  const player = useVideoPlayer ? VideoPlayerComponent({
    videoUrl,
    visible,
    onStatusChange: setIsLoading,
    onError: () => setHasError(true),
  }) : null;

  // If expo-video is not available, show a fallback modal
  if (!VideoView || !useVideoPlayer) {
    return (
      <Modal visible={visible} animationType="fade" onRequestClose={handleClose}>
        <StatusBar hidden />
        <SafeAreaView style={styles.container}>
          <View style={styles.fallbackContainer}>
            <TouchableOpacity
              style={styles.closeButtonContainer}
              onPress={handleClose}
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
                Video playback requires a development build with expo-av configured.
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

  // Handle video loading and errors
  React.useEffect(() => {
    if (player && visible) {
      const subscription = player.addListener('statusChange', (status: any) => {
        if (status.isLoaded) {
          setIsLoading(false);
        }
      });

      const errorSubscription = player.addListener('playbackError', (error: any) => {
        setIsLoading(false);
        setHasError(true);
        console.error('Full-screen video playback error:', error);
      });

      return () => {
        subscription?.remove();
        errorSubscription?.remove();
      };
    }
  }, [player, visible]);

  // Cleanup timeout on unmount
  React.useEffect(() => {
    return () => {
      if (controlsTimeoutRef.current) {
        clearTimeout(controlsTimeoutRef.current);
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

              {/* Play/Pause button */}
              <TouchableOpacity
                style={styles.playButtonContainer}
                onPress={handlePlayPress}
                activeOpacity={0.7}
              >
                <View style={styles.playButton}>
                  <Ionicons
                    name={player?.playing ? "pause" : "play"}
                    size={40}
                    color="#fff"
                  />
                </View>
              </TouchableOpacity>
            </>
          )}
        </TouchableOpacity>
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
});
