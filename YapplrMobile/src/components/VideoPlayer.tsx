import React, { useState, useRef } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  Dimensions,
  ActivityIndicator,
} from 'react-native';
import { Video, ResizeMode, AVPlaybackStatus } from 'expo-av';
import { Ionicons } from '@expo/vector-icons';
import { useThemeColors } from '../hooks/useThemeColors';

interface VideoPlayerProps {
  videoUrl: string;
  thumbnailUrl?: string;
  duration?: number;
  style?: any;
  autoPlay?: boolean;
  muted?: boolean;
}

export default function VideoPlayer({
  videoUrl,
  thumbnailUrl,
  duration,
  style,
  autoPlay = false,
  muted = true,
}: VideoPlayerProps) {
  const colors = useThemeColors();
  const styles = createStyles(colors);
  const videoRef = useRef<Video>(null);
  
  const [isPlaying, setIsPlaying] = useState(autoPlay);
  const [isMuted, setIsMuted] = useState(muted);
  const [isLoading, setIsLoading] = useState(true);
  const [hasError, setHasError] = useState(false);
  const [showControls, setShowControls] = useState(true);
  const [currentTime, setCurrentTime] = useState(0);
  const [videoDuration, setVideoDuration] = useState(duration || 0);

  const handlePlaybackStatusUpdate = (status: AVPlaybackStatus) => {
    if (status.isLoaded) {
      setIsLoading(false);
      setCurrentTime(status.positionMillis || 0);
      setVideoDuration(status.durationMillis || 0);
      setIsPlaying(status.isPlaying);
      setIsMuted(status.isMuted);
    } else if (status.error) {
      setHasError(true);
      setIsLoading(false);
      console.error('Video playback error:', status.error);
    }
  };

  const togglePlay = async () => {
    if (videoRef.current) {
      if (isPlaying) {
        await videoRef.current.pauseAsync();
      } else {
        await videoRef.current.playAsync();
      }
    }
  };

  const toggleMute = async () => {
    if (videoRef.current) {
      await videoRef.current.setIsMutedAsync(!isMuted);
    }
  };

  const formatTime = (timeMillis: number) => {
    const totalSeconds = Math.floor(timeMillis / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return `${minutes}:${seconds.toString().padStart(2, '0')}`;
  };

  const handleVideoPress = () => {
    setShowControls(!showControls);
    
    // Auto-hide controls after 3 seconds if playing
    if (isPlaying) {
      setTimeout(() => {
        setShowControls(false);
      }, 3000);
    }
  };

  if (hasError) {
    return (
      <View style={[styles.container, style]}>
        <View style={styles.errorContainer}>
          <Ionicons name="alert-circle" size={48} color={colors.error} />
          <Text style={styles.errorText}>Failed to load video</Text>
        </View>
      </View>
    );
  }

  return (
    <View style={[styles.container, style]}>
      <TouchableOpacity 
        style={styles.videoContainer}
        onPress={handleVideoPress}
        activeOpacity={1}
      >
        <Video
          ref={videoRef}
          source={{ uri: videoUrl }}
          style={styles.video}
          resizeMode={ResizeMode.CONTAIN}
          shouldPlay={autoPlay}
          isMuted={muted}
          onPlaybackStatusUpdate={handlePlaybackStatusUpdate}
          posterSource={thumbnailUrl ? { uri: thumbnailUrl } : undefined}
          usePoster={!!thumbnailUrl}
        />

        {/* Loading overlay */}
        {isLoading && (
          <View style={styles.loadingOverlay}>
            <ActivityIndicator size="large" color="white" />
          </View>
        )}

        {/* Play button overlay (when paused) */}
        {!isPlaying && !isLoading && showControls && (
          <View style={styles.playButtonOverlay}>
            <TouchableOpacity style={styles.playButton} onPress={togglePlay}>
              <Ionicons name="play" size={48} color="white" />
            </TouchableOpacity>
          </View>
        )}

        {/* Controls */}
        {showControls && !isLoading && (
          <View style={styles.controlsContainer}>
            {/* Progress bar */}
            <View style={styles.progressContainer}>
              <View style={styles.progressBar}>
                <View
                  style={[
                    styles.progressFill,
                    {
                      width: videoDuration > 0 
                        ? `${(currentTime / videoDuration) * 100}%` 
                        : '0%'
                    }
                  ]}
                />
              </View>
            </View>

            {/* Control buttons */}
            <View style={styles.controlButtons}>
              <View style={styles.leftControls}>
                <TouchableOpacity onPress={togglePlay} style={styles.controlButton}>
                  <Ionicons 
                    name={isPlaying ? "pause" : "play"} 
                    size={24} 
                    color="white" 
                  />
                </TouchableOpacity>

                <TouchableOpacity onPress={toggleMute} style={styles.controlButton}>
                  <Ionicons 
                    name={isMuted ? "volume-mute" : "volume-high"} 
                    size={24} 
                    color="white" 
                  />
                </TouchableOpacity>

                <Text style={styles.timeText}>
                  {formatTime(currentTime)} / {formatTime(videoDuration)}
                </Text>
              </View>
            </View>
          </View>
        )}
      </TouchableOpacity>
    </View>
  );
}

const createStyles = (colors: any) => StyleSheet.create({
  container: {
    backgroundColor: 'black',
    borderRadius: 12,
    overflow: 'hidden',
  },
  videoContainer: {
    position: 'relative',
    width: '100%',
    aspectRatio: 16 / 9,
  },
  video: {
    width: '100%',
    height: '100%',
  },
  loadingOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  playButtonOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    justifyContent: 'center',
    alignItems: 'center',
  },
  playButton: {
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    borderRadius: 40,
    width: 80,
    height: 80,
    justifyContent: 'center',
    alignItems: 'center',
  },
  controlsContainer: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    backgroundColor: 'linear-gradient(transparent, rgba(0, 0, 0, 0.7))',
    padding: 16,
  },
  progressContainer: {
    marginBottom: 12,
  },
  progressBar: {
    height: 4,
    backgroundColor: 'rgba(255, 255, 255, 0.3)',
    borderRadius: 2,
  },
  progressFill: {
    height: '100%',
    backgroundColor: 'white',
    borderRadius: 2,
  },
  controlButtons: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  leftControls: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  controlButton: {
    marginRight: 16,
  },
  timeText: {
    color: 'white',
    fontSize: 12,
  },
  errorContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: colors.surface,
    padding: 32,
  },
  errorText: {
    color: colors.error,
    marginTop: 8,
    textAlign: 'center',
  },
});
