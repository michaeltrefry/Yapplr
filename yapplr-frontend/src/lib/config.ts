/**
 * Centralized configuration for the frontend application
 * This ensures consistent API URL handling across all components
 */

/**
 * Get the API base URL with proper fallback and debugging
 */
export function getApiBaseUrl(): string {
  const envUrl = process.env.NEXT_PUBLIC_API_URL;
  
  // Log for debugging (only in development)
  if (process.env.NODE_ENV === 'development') {
    console.log('🔧 API URL Debug:', {
      NEXT_PUBLIC_API_URL: envUrl,
      NODE_ENV: process.env.NODE_ENV,
      fallbackUsed: !envUrl
    });
  }
  
  // If no environment variable is set, throw an error instead of using a fallback
  if (!envUrl) {
    console.error('❌ NEXT_PUBLIC_API_URL environment variable is not set!');
    console.error('Please check your .env.local file or environment configuration');
    throw new Error('NEXT_PUBLIC_API_URL environment variable is required');
  }
  
  return envUrl;
}

/**
 * DEPRECATED: URL construction functions removed
 * The API now returns complete URLs for all media.
 * Use the imageUrl, videoUrl, videoThumbnailUrl properties directly from API responses.
 */

/**
 * Configuration object for easy access to all settings
 */
export const config = {
  get apiBaseUrl() { return getApiBaseUrl(); },
  signalREnabled: process.env.NEXT_PUBLIC_ENABLE_SIGNALR === 'true',
  tenorApiKey: process.env.NEXT_PUBLIC_TENOR_API_KEY,
  isDevelopment: process.env.NODE_ENV === 'development',
  isProduction: process.env.NODE_ENV === 'production',
} as const;
