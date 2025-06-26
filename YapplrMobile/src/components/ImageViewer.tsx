import React, { useState, useEffect } from 'react';
import {
  Modal,
  View,
  Image,
  TouchableOpacity,
  StyleSheet,
  SafeAreaView,
  StatusBar,
  Dimensions,
  ActivityIndicator,
  Text,
  ScrollView,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';

interface ImageViewerProps {
  visible: boolean;
  imageUrl: string;
  onClose: () => void;
}

const { width: screenWidth, height: screenHeight } = Dimensions.get('window');

export default function ImageViewer({ visible, imageUrl, onClose }: ImageViewerProps) {
  const [imageLoading, setImageLoading] = useState(false); // Start with false
  const [imageError, setImageError] = useState(false);

  const onImageLoadStart = () => {
    console.log('ImageViewer: onLoadStart fired, showing spinner');
    setImageLoading(true);
    setImageError(false);
  };

  const onImageLoad = () => {
    console.log('ImageViewer: onLoad fired, hiding spinner');
    setImageLoading(false);
    setImageError(false);
  };

  const onImageLoadEnd = () => {
    console.log('ImageViewer: onLoadEnd fired, hiding spinner');
    setImageLoading(false);
  };

  const onImageError = () => {
    console.log('ImageViewer: Image error, hiding spinner');
    setImageLoading(false);
    setImageError(true);
  };

  const onModalShow = () => {
    console.log('ImageViewer: Modal shown');
    // Reset states when modal opens
    setImageLoading(false); // Start with no spinner
    setImageError(false);
  };

  return (
    <Modal
      visible={visible}
      transparent={true}
      animationType="fade"
      onShow={onModalShow}
      onRequestClose={onClose}
    >
      <StatusBar hidden />
      <View style={styles.container}>
        <SafeAreaView style={styles.safeArea}>
          {/* Close Button */}
          <TouchableOpacity style={styles.closeButton} onPress={onClose}>
            <Ionicons name="close" size={30} color="#fff" />
          </TouchableOpacity>

          {/* Image Container with ScrollView for zoom */}
          <ScrollView
            style={styles.scrollView}
            contentContainerStyle={styles.imageContainer}
            maximumZoomScale={3}
            minimumZoomScale={1}
            showsHorizontalScrollIndicator={false}
            showsVerticalScrollIndicator={false}
            centerContent={true}
          >
            {imageError ? (
              <View style={styles.errorContainer}>
                <Ionicons name="image-outline" size={64} color="#666" />
                <Text style={styles.errorText}>Failed to load image</Text>
              </View>
            ) : (
              <TouchableOpacity
                style={styles.imageWrapper}
                activeOpacity={1}
                onPress={onClose}
              >
                <Image
                  source={{ uri: imageUrl }}
                  style={styles.image}
                  resizeMode="contain"
                  onLoadStart={onImageLoadStart}
                  onLoad={onImageLoad}
                  onLoadEnd={onImageLoadEnd}
                  onError={onImageError}
                />
              </TouchableOpacity>
            )}

            {/* Loading Indicator */}
            {imageLoading && !imageError && (
              <View style={styles.loadingContainer}>
                <ActivityIndicator size="large" color="#fff" />
                <Text style={styles.loadingText}>Loading...</Text>
              </View>
            )}
          </ScrollView>

          {/* Instructions */}
          {!imageLoading && !imageError && (
            <TouchableOpacity
              style={styles.instructionsContainer}
              activeOpacity={1}
              onPress={onClose}
            >
              <Text style={styles.instructionsText}>
                Pinch to zoom • Tap image to close
              </Text>
            </TouchableOpacity>
          )}
        </SafeAreaView>
      </View>
    </Modal>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: 'rgba(0, 0, 0, 0.95)',
  },
  safeArea: {
    flex: 1,
  },
  closeButton: {
    position: 'absolute',
    top: 50,
    right: 20,
    zIndex: 10,
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    borderRadius: 20,
    width: 40,
    height: 40,
    justifyContent: 'center',
    alignItems: 'center',
  },
  imageWrapper: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  scrollView: {
    flex: 1,
  },
  imageContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    minHeight: screenHeight,
  },
  image: {
    width: screenWidth,
    height: screenHeight * 0.8,
    backgroundColor: 'transparent',
  },
  loadingContainer: {
    position: 'absolute',
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    justifyContent: 'center',
    alignItems: 'center',
    backgroundColor: 'rgba(0, 0, 0, 0.3)',
  },
  loadingText: {
    color: '#fff',
    marginTop: 10,
    fontSize: 16,
  },
  errorContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  errorText: {
    color: '#666',
    marginTop: 10,
    fontSize: 16,
  },
  instructionsContainer: {
    position: 'absolute',
    bottom: 50,
    left: 0,
    right: 0,
    alignItems: 'center',
  },
  instructionsText: {
    color: 'rgba(255, 255, 255, 0.7)',
    fontSize: 14,
    textAlign: 'center',
    backgroundColor: 'rgba(0, 0, 0, 0.5)',
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 20,
  },
});
