// Tenor API configuration and utilities for React Native
// Note: Mobile app calls Yapplr API endpoints, not Tenor directly
import AsyncStorage from '@react-native-async-storage/async-storage';

export interface TenorGif {
  id: string;
  title: string;
  content_description: string;
  media_formats: {
    gif?: {
      url: string;
      duration: number;
      dims: [number, number];
      size: number;
    };
    mediumgif?: {
      url: string;
      duration: number;
      dims: [number, number];
      size: number;
    };
    tinygif?: {
      url: string;
      duration: number;
      dims: [number, number];
      size: number;
    };
    nanogif?: {
      url: string;
      duration: number;
      dims: [number, number];
      size: number;
    };
    preview?: {
      url: string;
      duration: number;
      dims: [number, number];
      size: number;
    };
  };
  created: number;
  itemurl: string;
  url: string;
  tags: string[];
  flags: string;
  hasaudio: boolean;
  hascaption: boolean;
  bg_color: string;
}

export interface TenorSearchResponse {
  results: TenorGif[];
  next: string;
}

export interface SelectedGif {
  id: string;
  title: string;
  url: string;
  previewUrl: string;
  width: number;
  height: number;
}

// Import API base URL from centralized config
import { API_BASE_URL } from '../config/api';

// Get the API base URL from centralized config
const getApiBaseUrl = () => {
  return API_BASE_URL;
};

// Search for GIFs
export async function searchGifs(query: string, limit: number = 20, pos?: string): Promise<TenorSearchResponse> {
  const params = new URLSearchParams({
    q: query,
    limit: limit.toString(),
  });

  if (pos) {
    params.append('pos', pos);
  }

  // Get auth token from AsyncStorage
  const token = await AsyncStorage.getItem('yapplr_token');

  if (!token) {
    throw new Error('No authentication token found');
  }

  const response = await fetch(`${getApiBaseUrl()}/api/gif/search?${params}`, {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  });

  if (!response.ok) {
    throw new Error(`GIF API error: ${response.status}`);
  }

  return response.json();
}

// Get trending GIFs
export async function getTrendingGifs(limit: number = 20, pos?: string): Promise<TenorSearchResponse> {
  const params = new URLSearchParams({
    limit: limit.toString(),
  });

  if (pos) {
    params.append('pos', pos);
  }

  // Get auth token from AsyncStorage
  const token = await AsyncStorage.getItem('yapplr_token');

  if (!token) {
    throw new Error('No authentication token found');
  }

  const response = await fetch(`${getApiBaseUrl()}/api/gif/trending?${params}`, {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  });

  if (!response.ok) {
    throw new Error(`GIF API error: ${response.status}`);
  }

  return response.json();
}

// Convert Tenor GIF to our SelectedGif format
export function convertTenorGifToSelected(gif: TenorGif): SelectedGif {
  // In v2 API, media_formats is a direct object (not an array)
  const mediaFormats = gif.media_formats;

  // Use tinygif for preview and gif for full size
  const preview = mediaFormats.tinygif || mediaFormats.nanogif || mediaFormats.preview;
  const full = mediaFormats.gif || mediaFormats.mediumgif;

  if (!preview || !full) {
    throw new Error('GIF media formats not available');
  }

  return {
    id: gif.id,
    title: gif.title || gif.content_description,
    url: full.url,
    previewUrl: preview.url,
    width: full.dims[0],
    height: full.dims[1],
  };
}
