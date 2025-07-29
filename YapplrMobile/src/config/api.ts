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
 * DEPRECATED: URL construction functions removed
 * The API now returns complete URLs for all media.
 * Use the imageUrl, videoUrl, videoThumbnailUrl, profileImageUrl properties directly from API responses.
 */

/**
 * Validate if a URL is properly formatted for video playback
 * Updated to be more flexible and not assume specific ports or endpoints
 */
export function validateVideoUrl(url: string): boolean {
  if (!url) return false;

  const isValidUrl = url.startsWith('http');

  console.log('🎥 Video URL validation:', {
    url,
    isValidUrl,
    urlLength: url.length
  });

  return isValidUrl;
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
