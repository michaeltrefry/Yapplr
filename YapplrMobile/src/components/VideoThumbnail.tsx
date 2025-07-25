import React from 'react';
import {
  View,
  StyleSheet,
  TouchableOpacity,
  Image,
  Text,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useThemeColors } from '../hooks/useThemeColors';

interface VideoThumbnailProps {
  thumbnailUrl?: string;
  style?: any;
  onPress?: () => void;
  showPlayButton?: boolean;
  duration?: string;
  size?: 'small' | 'medium' | 'large';
  width?: number; // Video width for aspect ratio calculation
  height?: number; // Video height for aspect ratio calculation
}

export default function VideoThumbnail({
  thumbnailUrl,
  style,
  onPress,
  showPlayButton = true,
  duration,
  size = 'medium',
  width,
  height,
}: VideoThumbnailProps) {
  const colors = useThemeColors();

  // Calculate aspect ratio from video dimensions
  const aspectRatio = (width && height) ? width / height : undefined;

  const styles = createStyles(colors, aspectRatio);

  const getSizeStyles = () => {
    // If we have aspect ratio, use dynamic sizing, otherwise use fixed sizes
    if (aspectRatio) {
      const baseWidth = size === 'small' ? 80 : size === 'large' ? 160 : 120;
      const calculatedHeight = baseWidth / aspectRatio;

      return {
        container: { width: baseWidth, height: calculatedHeight },
        playIcon: size === 'small' ? 24 : size === 'large' ? 48 : 32,
      };
    }

    // Fallback to fixed sizes
    switch (size) {
      case 'small':
        return {
          container: styles.smallContainer,
          playIcon: 24,
        };
      case 'large':
        return {
          container: styles.largeContainer,
          playIcon: 48,
        };
      default:
        return {
          container: styles.mediumContainer,
          playIcon: 32,
        };
    }
  };

  const sizeStyles = getSizeStyles();

  const content = (
    <View style={[styles.container, sizeStyles.container, style]}>
      {thumbnailUrl ? (
        <Image
          source={{ uri: thumbnailUrl }}
          style={styles.thumbnail}
          resizeMode="cover"
        />
      ) : (
        <View style={styles.placeholderContainer}>
          <Ionicons name="videocam-outline" size={sizeStyles.playIcon} color={colors.textSecondary} />
        </View>
      )}
      
      {showPlayButton && (
        <View style={styles.playButtonOverlay}>
          <View style={styles.playButton}>
            <Ionicons name="play" size={sizeStyles.playIcon * 0.6} color="#fff" />
          </View>
        </View>
      )}

      {duration && (
        <View style={styles.durationBadge}>
          <Text style={styles.durationText}>{duration}</Text>
        </View>
      )}
    </View>
  );

  if (onPress) {
    return (
      <TouchableOpacity onPress={onPress} activeOpacity={0.8}>
        {content}
      </TouchableOpacity>
    );
  }

  return content;
}

const createStyles = (colors: any, aspectRatio?: number) => StyleSheet.create({
  container: {
    backgroundColor: colors.surface,
    borderRadius: 8,
    overflow: 'hidden',
    position: 'relative',
  },
  smallContainer: {
    width: 80,
    height: 60,
  },
  mediumContainer: {
    width: 120,
    height: 90,
  },
  largeContainer: {
    width: 160,
    height: 120,
  },
  thumbnail: {
    width: '100%',
    height: '100%',
  },
  placeholderContainer: {
    width: '100%',
    height: '100%',
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: colors.border,
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
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
    borderRadius: 20,
    width: 40,
    height: 40,
    justifyContent: 'center',
    alignItems: 'center',
  },
  durationBadge: {
    position: 'absolute',
    bottom: 4,
    right: 4,
    backgroundColor: 'rgba(0, 0, 0, 0.7)',
    paddingHorizontal: 4,
    paddingVertical: 2,
    borderRadius: 4,
  },
  durationText: {
    color: '#fff',
    fontSize: 10,
    fontWeight: '600',
  },
});
