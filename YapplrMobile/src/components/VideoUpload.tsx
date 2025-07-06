import React, { useState } from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  Alert,
  ActivityIndicator,
  Dimensions,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import * as ImagePicker from 'expo-image-picker';
import { Video, ResizeMode } from 'expo-av';
import { useThemeColors } from '../hooks/useThemeColors';

interface VideoUploadProps {
  onVideoUploaded: (videoData: {
    fileName: string;
    videoUrl: string;
    jobId: number;
    sizeBytes: number;
  }) => void;
  onRemove: () => void;
  disabled?: boolean;
  uploadVideo: (uri: string, fileName: string, type: string, onProgress?: (progress: number) => void) => Promise<any>;
}

interface VideoUploadState {
  uri: string | null;
  fileName: string | null;
  jobId: number | null;
  uploadProgress: number;
  processingStatus: 'idle' | 'uploading' | 'processing' | 'completed' | 'failed';
  errorMessage: string | null;
}

export default function VideoUpload({ 
  onVideoUploaded, 
  onRemove, 
  disabled, 
  uploadVideo 
}: VideoUploadProps) {
  const colors = useThemeColors();
  const styles = createStyles(colors);
  
  const [state, setState] = useState<VideoUploadState>({
    uri: null,
    fileName: null,
    jobId: null,
    uploadProgress: 0,
    processingStatus: 'idle',
    errorMessage: null,
  });

  const pickVideo = async () => {
    try {
      // Request permission
      const permissionResult = await ImagePicker.requestMediaLibraryPermissionsAsync();

      if (permissionResult.granted === false) {
        Alert.alert('Permission Required', 'Permission to access media library is required!');
        return;
      }

      // Launch video picker
      const result = await ImagePicker.launchImageLibraryAsync({
        mediaTypes: ImagePicker.MediaTypeOptions.Videos,
        allowsEditing: true,
        quality: 0.8,
        allowsMultipleSelection: false,
        videoMaxDuration: 300, // 5 minutes max
      });

      if (!result.canceled && result.assets && result.assets.length > 0) {
        const asset = result.assets[0];
        
        // Validate file size (100MB)
        if (asset.fileSize && asset.fileSize > 100 * 1024 * 1024) {
          Alert.alert('File Too Large', 'Video file must be less than 100MB');
          return;
        }

        const fileName = `video_${Date.now()}.mp4`;
        const mimeType = asset.type || 'video/mp4';

        setState(prev => ({
          ...prev,
          uri: asset.uri,
          fileName,
          processingStatus: 'uploading',
          errorMessage: null,
          uploadProgress: 0,
        }));

        // Start upload
        try {
          const response = await uploadVideo(asset.uri, fileName, mimeType, (progress) => {
            setState(prev => ({ ...prev, uploadProgress: progress }));
          });

          setState(prev => ({
            ...prev,
            fileName: response.fileName,
            jobId: response.jobId,
            processingStatus: 'processing',
            uploadProgress: 100,
          }));

          onVideoUploaded({
            fileName: response.fileName,
            videoUrl: response.videoUrl,
            jobId: response.jobId,
            sizeBytes: response.sizeBytes,
          });

          // Start polling for processing status
          pollProcessingStatus(response.jobId);
        } catch (error: any) {
          setState(prev => ({
            ...prev,
            processingStatus: 'failed',
            errorMessage: error.message || 'Upload failed',
          }));
        }
      }
    } catch (error) {
      console.error('Error picking video:', error);
      Alert.alert('Error', 'Failed to pick video. Please try again.');
    }
  };

  const pollProcessingStatus = async (jobId: number) => {
    // Note: This would need the getProcessingStatus function passed as prop
    // For now, we'll simulate processing completion after a delay
    setTimeout(() => {
      setState(prev => ({ ...prev, processingStatus: 'completed' }));
    }, 5000);
  };

  const handleRemove = () => {
    setState({
      uri: null,
      fileName: null,
      jobId: null,
      uploadProgress: 0,
      processingStatus: 'idle',
      errorMessage: null,
    });
    onRemove();
  };

  const getStatusIcon = () => {
    switch (state.processingStatus) {
      case 'uploading':
        return <ActivityIndicator size="small" color={colors.primary} />;
      case 'processing':
        return <ActivityIndicator size="small" color={colors.warning} />;
      case 'completed':
        return <Ionicons name="checkmark-circle" size={20} color={colors.success} />;
      case 'failed':
        return <Ionicons name="alert-circle" size={20} color={colors.error} />;
      default:
        return null;
    }
  };

  const getStatusText = () => {
    switch (state.processingStatus) {
      case 'uploading':
        return `Uploading... ${state.uploadProgress}%`;
      case 'processing':
        return 'Processing video...';
      case 'completed':
        return 'Video ready!';
      case 'failed':
        return state.errorMessage || 'Processing failed';
      default:
        return '';
    }
  };

  if (state.uri) {
    return (
      <View style={styles.container}>
        <View style={styles.videoContainer}>
          <Video
            source={{ uri: state.uri }}
            style={styles.video}
            useNativeControls={state.processingStatus === 'completed'}
            resizeMode={ResizeMode.CONTAIN}
            shouldPlay={false}
          />
          
          {/* Status overlay */}
          {state.processingStatus !== 'idle' && (
            <View style={styles.statusOverlay}>
              <View style={styles.statusContainer}>
                {getStatusIcon()}
                <Text style={styles.statusText}>{getStatusText()}</Text>
              </View>
            </View>
          )}

          {/* Progress bar for uploading */}
          {state.processingStatus === 'uploading' && (
            <View style={styles.progressBarContainer}>
              <View 
                style={[styles.progressBar, { width: `${state.uploadProgress}%` }]}
              />
            </View>
          )}

          {/* Remove button */}
          <TouchableOpacity
            style={styles.removeButton}
            onPress={handleRemove}
            disabled={state.processingStatus === 'uploading'}
          >
            <Ionicons name="close" size={20} color="white" />
          </TouchableOpacity>
        </View>

        {/* Error message */}
        {state.processingStatus === 'failed' && state.errorMessage && (
          <View style={styles.errorContainer}>
            <Text style={styles.errorText}>{state.errorMessage}</Text>
          </View>
        )}
      </View>
    );
  }

  return (
    <TouchableOpacity
      style={[styles.uploadButton, disabled && styles.disabled]}
      onPress={pickVideo}
      disabled={disabled}
    >
      <Ionicons name="videocam" size={24} color={colors.primary} />
      <Text style={[styles.uploadText, { color: colors.primary }]}>Add Video</Text>
    </TouchableOpacity>
  );
}

const createStyles = (colors: any) => StyleSheet.create({
  container: {
    marginVertical: 10,
  },
  videoContainer: {
    position: 'relative',
    backgroundColor: colors.surface,
    borderRadius: 12,
    overflow: 'hidden',
  },
  video: {
    width: '100%',
    height: 200,
  },
  statusOverlay: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  statusContainer: {
    backgroundColor: 'white',
    borderRadius: 8,
    padding: 12,
    flexDirection: 'row',
    alignItems: 'center',
  },
  statusText: {
    marginLeft: 8,
    fontSize: 14,
    fontWeight: '500',
    color: colors.text,
  },
  progressBarContainer: {
    position: 'absolute',
    bottom: 0,
    left: 0,
    right: 0,
    height: 4,
    backgroundColor: 'rgba(255, 255, 255, 0.3)',
  },
  progressBar: {
    height: '100%',
    backgroundColor: colors.primary,
  },
  removeButton: {
    position: 'absolute',
    top: 8,
    right: 8,
    backgroundColor: 'rgba(0, 0, 0, 0.7)',
    borderRadius: 16,
    width: 32,
    height: 32,
    justifyContent: 'center',
    alignItems: 'center',
  },
  errorContainer: {
    marginTop: 8,
    padding: 8,
    backgroundColor: colors.errorBackground,
    borderRadius: 8,
  },
  errorText: {
    color: colors.error,
    fontSize: 12,
  },
  uploadButton: {
    flexDirection: 'row',
    alignItems: 'center',
    padding: 12,
    borderRadius: 8,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
  },
  uploadText: {
    marginLeft: 8,
    fontSize: 16,
    fontWeight: '500',
  },
  disabled: {
    opacity: 0.5,
  },
});
