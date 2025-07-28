// Centralized API configuration for the mobile app
// This ensures all API calls use the same base URL and port

// Use your computer's IP address instead of localhost for mobile devices
export const API_BASE_URL = 'http://192.168.254.181:8080'; // Replace with your computer's IP

// Alternative URLs to try if the main one fails
export const FALLBACK_URLS = [
  'http://192.168.254.181:8080',
  'http://localhost:8080',
  'http://127.0.0.1:8080'
];

/**
 * Generate image URL from filename
 */
export function getImageUrl(fileName: string): string {
  if (!fileName) return '';
  return `${API_BASE_URL}/api/images/${fileName}`;
}

/**
 * Generate video URL from filename
 */
export function getVideoUrl(fileName: string): string {
  if (!fileName) return '';
  return `${API_BASE_URL}/api/videos/processed/${fileName}`;
}

/**
 * Generate video thumbnail URL from filename
 */
export function getVideoThumbnailUrl(fileName: string): string {
  if (!fileName) return '';
  return `${API_BASE_URL}/api/videos/thumbnails/${fileName}`;
}

/**
 * Generate profile image URL from filename
 */
export function getProfileImageUrl(fileName: string): string {
  if (!fileName) return '';
  return `${API_BASE_URL}/api/images/${fileName}`;
}

/**
 * Validate if a URL is properly formatted for video playback
 */
export function validateVideoUrl(url: string): boolean {
  if (!url) return false;
  
  const isValidUrl = url.startsWith('http');
  const hasCorrectPort = url.includes(':8080');
  const isVideoEndpoint = url.includes('/api/videos/');
  
  console.log('🎥 Video URL validation:', {
    url,
    isValidUrl,
    hasCorrectPort,
    isVideoEndpoint,
    urlLength: url.length
  });
  
  return isValidUrl && hasCorrectPort && isVideoEndpoint;
}

/**
 * Log API configuration for debugging
 */
export function logApiConfig(): void {
  console.log('📡 API Configuration:', {
    baseUrl: API_BASE_URL,
    fallbackUrls: FALLBACK_URLS
  });
}
