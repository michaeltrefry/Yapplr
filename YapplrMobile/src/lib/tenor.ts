// Tenor API configuration and utilities for React Native
const TENOR_API_KEY = 'LIVDSRZULELA'; // Demo key - replace with your own
const TENOR_BASE_URL = 'https://g.tenor.com/v1';

export interface TenorGif {
  id: string;
  title: string;
  content_description: string;
  content_rating: string;
  h1_title: string;
  media: Array<{
    gif?: {
      url: string;
      duration: number;
      preview: string;
      dims: [number, number];
      size: number;
    };
    mediumgif?: {
      url: string;
      duration: number;
      preview: string;
      dims: [number, number];
      size: number;
    };
    tinygif?: {
      url: string;
      duration: number;
      preview: string;
      dims: [number, number];
      size: number;
    };
    nanogif?: {
      url: string;
      duration: number;
      preview: string;
      dims: [number, number];
      size: number;
    };
  }>;
  created: number;
  itemurl: string;
  url: string;
  tags: string[];
  flags: string[];
  hasaudio: boolean;
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

// Search for GIFs
export async function searchGifs(query: string, limit: number = 20, pos?: string): Promise<TenorSearchResponse> {
  const params = new URLSearchParams({
    key: TENOR_API_KEY,
    q: query,
    limit: limit.toString(),
    media_filter: 'gif,tinygif',
    ar_range: 'all',
  });

  if (pos) {
    params.append('pos', pos);
  }

  const response = await fetch(`${TENOR_BASE_URL}/search?${params}`);
  
  if (!response.ok) {
    throw new Error(`Tenor API error: ${response.status}`);
  }

  return response.json();
}

// Get trending GIFs
export async function getTrendingGifs(limit: number = 20, pos?: string): Promise<TenorSearchResponse> {
  const params = new URLSearchParams({
    key: TENOR_API_KEY,
    limit: limit.toString(),
    media_filter: 'gif,tinygif',
    ar_range: 'all',
  });

  if (pos) {
    params.append('pos', pos);
  }

  const response = await fetch(`${TENOR_BASE_URL}/trending?${params}`);
  
  if (!response.ok) {
    throw new Error(`Tenor API error: ${response.status}`);
  }

  return response.json();
}

// Convert Tenor GIF to our SelectedGif format
export function convertTenorGifToSelected(gif: TenorGif): SelectedGif {
  // Get the first media object (Tenor API returns an array with one object)
  const media = gif.media[0];

  // Use tinygif for preview and gif for full size
  const preview = media.tinygif || media.nanogif;
  const full = media.gif || media.mediumgif;

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
