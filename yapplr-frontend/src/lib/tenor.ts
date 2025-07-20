// Tenor API configuration and utilities
// Use backend proxy to avoid CORS issues
const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5161';
const TENOR_PROXY_URL = `${API_BASE_URL}/api/gif`;

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
    mp4?: {
      url: string;
      duration: number;
      dims: [number, number];
      size: number;
    };
    loopedmp4?: {
      url: string;
      duration: number;
      dims: [number, number];
      size: number;
    };
    tinymp4?: {
      url: string;
      duration: number;
      dims: [number, number];
      size: number;
    };
    nanomp4?: {
      url: string;
      duration: number;
      dims: [number, number];
      size: number;
    };
    webm?: {
      url: string;
      duration: number;
      dims: [number, number];
      size: number;
    };
    tinywebm?: {
      url: string;
      duration: number;
      dims: [number, number];
      size: number;
    };
    nanowebm?: {
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

export interface TenorCategoriesResponse {
  tags: Array<{
    searchterm: string;
    path: string;
    image: string;
    name: string;
  }>;
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
    q: query,
    limit: limit.toString(),
  });

  if (pos) {
    params.append('pos', pos);
  }

  // Get auth token from localStorage
  const token = localStorage.getItem('token');

  const response = await fetch(`${TENOR_PROXY_URL}/search?${params}`, {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  });

  if (!response.ok) {
    throw new Error(`Tenor API error: ${response.status}`);
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

  // Get auth token from localStorage
  const token = localStorage.getItem('token');

  const response = await fetch(`${TENOR_PROXY_URL}/trending?${params}`, {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  });

  if (!response.ok) {
    throw new Error(`Tenor API error: ${response.status}`);
  }

  return response.json();
}

// Get GIF categories
export async function getGifCategories(): Promise<TenorCategoriesResponse> {
  // Get auth token from localStorage
  const token = localStorage.getItem('token');

  const response = await fetch(`${TENOR_PROXY_URL}/categories`, {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  });

  if (!response.ok) {
    throw new Error(`Tenor API error: ${response.status}`);
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
