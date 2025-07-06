import React from 'react';
import { View, Text, TouchableOpacity, Image, Linking, StyleSheet } from 'react-native';
import { LinkPreview as LinkPreviewType, LinkPreviewStatus } from '../types';
import { useThemeColors } from '../hooks/useThemeColors';

interface LinkPreviewProps {
  linkPreview: LinkPreviewType;
  style?: any;
}

export default function LinkPreview({ linkPreview, style }: LinkPreviewProps) {
  const colors = useThemeColors();
  const styles = createStyles(colors);
  
  const { url, title, description, imageUrl, siteName, status, errorMessage } = linkPreview;

  const handlePress = () => {
    Linking.openURL(url);
  };

  // Don't render anything for pending status
  if (status === LinkPreviewStatus.Pending) {
    return (
      <View style={[styles.container, styles.pendingContainer, style]}>
        <Text style={styles.pendingText}>Loading preview...</Text>
      </View>
    );
  }

  // Handle error states
  if (status !== LinkPreviewStatus.Success) {
    return (
      <TouchableOpacity 
        style={[styles.container, styles.errorContainer, style]}
        onPress={handlePress}
        activeOpacity={0.7}
      >
        <View style={styles.errorContent}>
          <Text style={styles.errorUrl} numberOfLines={1}>
            {url}
          </Text>
          <Text style={styles.errorMessage}>
            {getErrorMessage(status, errorMessage)}
          </Text>
        </View>
      </TouchableOpacity>
    );
  }

  // Success state - render full preview
  return (
    <TouchableOpacity 
      style={[styles.container, styles.successContainer, style]}
      onPress={handlePress}
      activeOpacity={0.7}
    >
      {imageUrl && (
        <Image
          source={{ uri: imageUrl }}
          style={styles.image}
          resizeMode="cover"
          onError={() => {
            // Handle image load error silently
          }}
        />
      )}
      
      <View style={styles.content}>
        <Text style={styles.siteName} numberOfLines={1}>
          {siteName || new URL(url).hostname}
        </Text>
        
        {title && (
          <Text style={styles.title} numberOfLines={2}>
            {title}
          </Text>
        )}
        
        {description && (
          <Text style={styles.description} numberOfLines={2}>
            {description}
          </Text>
        )}
        
        {!title && !description && (
          <Text style={styles.fallbackUrl} numberOfLines={1}>
            {url}
          </Text>
        )}
      </View>
    </TouchableOpacity>
  );
}

function getErrorMessage(status: LinkPreviewStatus, errorMessage?: string): string {
  if (errorMessage) {
    return errorMessage;
  }

  switch (status) {
    case LinkPreviewStatus.NotFound:
      return 'Page not found (404)';
    case LinkPreviewStatus.Unauthorized:
      return 'Authentication required (401)';
    case LinkPreviewStatus.Forbidden:
      return 'Access forbidden (403)';
    case LinkPreviewStatus.Timeout:
      return 'Request timed out';
    case LinkPreviewStatus.NetworkError:
      return 'Network error occurred';
    case LinkPreviewStatus.InvalidUrl:
      return 'Invalid URL format';
    case LinkPreviewStatus.TooLarge:
      return 'Content too large';
    case LinkPreviewStatus.UnsupportedContent:
      return 'Unsupported content type';
    default:
      return 'Unable to load preview';
  }
}

const createStyles = (colors: any) => StyleSheet.create({
  container: {
    borderRadius: 8,
    overflow: 'hidden',
    marginVertical: 4,
  },
  pendingContainer: {
    backgroundColor: colors.background,
    borderWidth: 1,
    borderColor: colors.border,
    padding: 12,
  },
  pendingText: {
    color: colors.textSecondary,
    fontSize: 14,
  },
  errorContainer: {
    backgroundColor: '#fef2f2',
    borderWidth: 1,
    borderColor: '#fecaca',
    padding: 12,
  },
  errorContent: {
    flex: 1,
  },
  errorUrl: {
    color: '#dc2626',
    fontSize: 14,
    fontWeight: '500',
    marginBottom: 4,
  },
  errorMessage: {
    color: '#dc2626',
    fontSize: 12,
  },
  successContainer: {
    backgroundColor: colors.background,
    borderWidth: 1,
    borderColor: colors.border,
  },
  image: {
    width: '100%',
    height: 150,
    backgroundColor: colors.background,
  },
  content: {
    padding: 12,
  },
  siteName: {
    color: colors.textSecondary,
    fontSize: 12,
    marginBottom: 4,
  },
  title: {
    color: colors.text,
    fontSize: 14,
    fontWeight: '600',
    marginBottom: 4,
  },
  description: {
    color: colors.textSecondary,
    fontSize: 12,
    lineHeight: 16,
  },
  fallbackUrl: {
    color: colors.textSecondary,
    fontSize: 12,
  },
});
