'use client';

import { useState, useEffect, useRef, useCallback } from 'react';
import { Search, X, Smile, TrendingUp, Grid } from 'lucide-react';
import { searchGifs, getTrendingGifs, getGifCategories, convertTenorGifToSelected, type SelectedGif, type TenorGif } from '@/lib/tenor';

interface GifPickerProps {
  isOpen: boolean;
  onClose: () => void;
  onSelectGif: (gif: SelectedGif) => void;
}

const POPULAR_SEARCHES = [
  'happy', 'sad', 'excited', 'love', 'laugh', 'cry', 'dance', 'party',
  'thumbs up', 'clap', 'wave', 'heart', 'fire', 'cool', 'wow', 'yes'
];

export default function GifPicker({ isOpen, onClose, onSelectGif }: GifPickerProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [gifs, setGifs] = useState<TenorGif[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'trending' | 'search'>('trending');
  const [nextPos, setNextPos] = useState<string | undefined>();
  const [hasMore, setHasMore] = useState(true);
  
  const searchInputRef = useRef<HTMLInputElement>(null);
  const gridRef = useRef<HTMLDivElement>(null);

  // Load trending GIFs on mount
  useEffect(() => {
    if (isOpen && activeTab === 'trending') {
      loadTrendingGifs();
    }
  }, [isOpen, activeTab]);

  // Focus search input when opened
  useEffect(() => {
    if (isOpen && searchInputRef.current) {
      searchInputRef.current.focus();
    }
  }, [isOpen]);

  const loadTrendingGifs = async (loadMore = false) => {
    try {
      setLoading(true);
      setError(null);
      
      const response = await getTrendingGifs(20, loadMore ? nextPos : undefined);
      
      if (loadMore) {
        setGifs(prev => [...prev, ...response.results]);
      } else {
        setGifs(response.results);
      }
      
      setNextPos(response.next);
      setHasMore(!!response.next);
    } catch (err) {
      setError('Failed to load trending GIFs');
      console.error('Error loading trending GIFs:', err);
    } finally {
      setLoading(false);
    }
  };

  const searchForGifs = async (query: string, loadMore = false) => {
    if (!query.trim()) return;
    
    try {
      setLoading(true);
      setError(null);
      
      const response = await searchGifs(query, 20, loadMore ? nextPos : undefined);
      
      if (loadMore) {
        setGifs(prev => [...prev, ...response.results]);
      } else {
        setGifs(response.results);
      }
      
      setNextPos(response.next);
      setHasMore(!!response.next);
    } catch (err) {
      setError('Failed to search GIFs');
      console.error('Error searching GIFs:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (query: string) => {
    setSearchQuery(query);
    setActiveTab('search');
    setNextPos(undefined);
    searchForGifs(query);
  };

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (searchQuery.trim()) {
      handleSearch(searchQuery);
    }
  };

  const handlePopularSearch = (term: string) => {
    setSearchQuery(term);
    handleSearch(term);
  };

  const handleTabChange = (tab: 'trending' | 'search') => {
    setActiveTab(tab);
    setNextPos(undefined);
    setGifs([]);
    
    if (tab === 'trending') {
      loadTrendingGifs();
    } else if (tab === 'search' && searchQuery.trim()) {
      searchForGifs(searchQuery);
    }
  };

  const handleGifSelect = (gif: TenorGif) => {
    const selectedGif = convertTenorGifToSelected(gif);
    onSelectGif(selectedGif);
    onClose();
  };

  const loadMore = () => {
    if (hasMore && !loading) {
      if (activeTab === 'trending') {
        loadTrendingGifs(true);
      } else if (activeTab === 'search' && searchQuery.trim()) {
        searchForGifs(searchQuery, true);
      }
    }
  };

  // Infinite scroll
  const handleScroll = useCallback((e: React.UIEvent<HTMLDivElement>) => {
    const { scrollTop, scrollHeight, clientHeight } = e.currentTarget;
    if (scrollHeight - scrollTop <= clientHeight + 100) {
      loadMore();
    }
  }, [hasMore, loading, activeTab, searchQuery]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-2xl h-[600px] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200">
          <h2 className="text-lg font-semibold text-gray-900">Choose a GIF</h2>
          <button
            onClick={onClose}
            className="p-1 hover:bg-gray-100 rounded-full transition-colors"
          >
            <X className="w-5 h-5 text-gray-500" />
          </button>
        </div>

        {/* Search */}
        <div className="p-4 border-b border-gray-200">
          <form onSubmit={handleSearchSubmit} className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
            <input
              ref={searchInputRef}
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Search for GIFs..."
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
          </form>
        </div>

        {/* Tabs */}
        <div className="flex border-b border-gray-200">
          <button
            onClick={() => handleTabChange('trending')}
            className={`flex-1 px-4 py-2 text-sm font-medium flex items-center justify-center space-x-2 ${
              activeTab === 'trending'
                ? 'text-blue-600 border-b-2 border-blue-600'
                : 'text-gray-500 hover:text-gray-700'
            }`}
          >
            <TrendingUp className="w-4 h-4" />
            <span>Trending</span>
          </button>
          <button
            onClick={() => handleTabChange('search')}
            className={`flex-1 px-4 py-2 text-sm font-medium flex items-center justify-center space-x-2 ${
              activeTab === 'search'
                ? 'text-blue-600 border-b-2 border-blue-600'
                : 'text-gray-500 hover:text-gray-700'
            }`}
          >
            <Grid className="w-4 h-4" />
            <span>Search</span>
          </button>
        </div>

        {/* Popular searches (only show when search tab is active and no search query) */}
        {activeTab === 'search' && !searchQuery.trim() && (
          <div className="p-4 border-b border-gray-200">
            <p className="text-sm text-gray-600 mb-2">Popular searches:</p>
            <div className="flex flex-wrap gap-2">
              {POPULAR_SEARCHES.map((term) => (
                <button
                  key={term}
                  onClick={() => handlePopularSearch(term)}
                  className="px-3 py-1 text-sm bg-gray-100 hover:bg-gray-200 rounded-full transition-colors"
                >
                  {term}
                </button>
              ))}
            </div>
          </div>
        )}

        {/* Content */}
        <div className="flex-1 overflow-hidden">
          {error && (
            <div className="p-4 text-center text-red-600">
              {error}
            </div>
          )}

          {!error && (
            <div
              ref={gridRef}
              className="h-full overflow-y-auto p-4"
              onScroll={handleScroll}
            >
              {gifs.length === 0 && !loading && activeTab === 'search' && searchQuery.trim() && (
                <div className="text-center text-gray-500 py-8">
                  No GIFs found for "{searchQuery}"
                </div>
              )}

              <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
                {gifs.map((gif) => (
                  <button
                    key={gif.id}
                    onClick={() => handleGifSelect(gif)}
                    className="relative aspect-square bg-gray-100 rounded-lg overflow-hidden hover:ring-2 hover:ring-blue-500 transition-all group"
                  >
                    <img
                      src={gif.media[0]?.tinygif?.url || gif.media[0]?.nanogif?.url}
                      alt={gif.title || gif.content_description}
                      className="w-full h-full object-cover group-hover:scale-105 transition-transform"
                      loading="lazy"
                    />
                  </button>
                ))}
              </div>

              {loading && (
                <div className="text-center py-4">
                  <div className="inline-block animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600"></div>
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
