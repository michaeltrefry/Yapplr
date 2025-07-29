import React, { useState, useEffect, useCallback } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  FlatList,
  Image,
  StyleSheet,
  Dimensions,
  ActivityIndicator,
  Alert,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { searchGifs, getTrendingGifs, convertTenorGifToSelected, type SelectedGif, type TenorGif } from '../lib/tenor';

interface GifPickerProps {
  visible: boolean;
  onClose: () => void;
  onSelectGif: (gif: SelectedGif) => void;
}

const POPULAR_SEARCHES = [
  'happy', 'sad', 'excited', 'love', 'laugh', 'cry', 'dance', 'party',
  'thumbs up', 'clap', 'wave', 'heart', 'fire', 'cool', 'wow', 'yes'
];

const { width: screenWidth } = Dimensions.get('window');
const itemWidth = (screenWidth - 60) / 2; // 2 columns with padding

export default function GifPicker({ visible, onClose, onSelectGif }: GifPickerProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [gifs, setGifs] = useState<TenorGif[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'trending' | 'search'>('trending');
  const [nextPos, setNextPos] = useState<string | undefined>();
  const [hasMore, setHasMore] = useState(true);

  // Debug logging
  console.log('=== GifPicker render ===');
  console.log('visible:', visible);
  console.log('loading:', loading);
  console.log('error:', error);
  console.log('gifs.length:', gifs.length);

  // Load trending GIFs on mount
  useEffect(() => {
    console.log('=== GifPicker useEffect ===');
    console.log('visible:', visible);
    console.log('activeTab:', activeTab);
    if (visible && activeTab === 'trending') {
      console.log('Loading trending GIFs...');
      loadTrendingGifs();
    }
  }, [visible, activeTab]);

  const loadTrendingGifs = async (loadMore = false) => {
    try {
      console.log('=== loadTrendingGifs start ===');
      setLoading(true);
      setError(null);

      console.log('Calling getTrendingGifs...');
      const response = await getTrendingGifs(20, loadMore ? nextPos : undefined);
      console.log('getTrendingGifs response:', response);

      if (loadMore) {
        setGifs(prev => [...prev, ...response.results]);
      } else {
        setGifs(response.results);
      }

      setNextPos(response.next);
      setHasMore(!!response.next);
      console.log('=== loadTrendingGifs success ===');
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

  const renderGifItem = ({ item }: { item: TenorGif }) => (
    <TouchableOpacity
      style={styles.gifItem}
      onPress={() => handleGifSelect(item)}
    >
      <Image
        source={{ uri: item.media_formats?.tinygif?.url || item.media_formats?.nanogif?.url }}
        style={styles.gifImage}
        resizeMode="cover"
      />
    </TouchableOpacity>
  );

  const renderPopularSearch = ({ item }: { item: string }) => (
    <TouchableOpacity
      style={styles.popularSearchItem}
      onPress={() => handlePopularSearch(item)}
    >
      <Text style={styles.popularSearchText}>{item}</Text>
    </TouchableOpacity>
  );

  return (
    <View style={styles.container}>
        {/* Header */}
        <View style={styles.header}>
          <Text style={styles.title}>Choose a GIF</Text>
          <TouchableOpacity onPress={onClose} style={styles.closeButton}>
            <Ionicons name="close" size={24} color="#666" />
          </TouchableOpacity>
        </View>

        {/* Search */}
        <View style={styles.searchContainer}>
          <View style={styles.searchInputContainer}>
            <Ionicons name="search" size={20} color="#666" style={styles.searchIcon} />
            <TextInput
              style={styles.searchInput}
              value={searchQuery}
              onChangeText={setSearchQuery}
              placeholder="Search for GIFs..."
              onSubmitEditing={() => searchQuery.trim() && handleSearch(searchQuery)}
              returnKeyType="search"
            />
          </View>
        </View>

        {/* Tabs */}
        <View style={styles.tabContainer}>
          <TouchableOpacity
            style={[styles.tab, activeTab === 'trending' && styles.activeTab]}
            onPress={() => handleTabChange('trending')}
          >
            <Ionicons name="trending-up" size={16} color={activeTab === 'trending' ? '#007AFF' : '#666'} />
            <Text style={[styles.tabText, activeTab === 'trending' && styles.activeTabText]}>
              Trending
            </Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={[styles.tab, activeTab === 'search' && styles.activeTab]}
            onPress={() => handleTabChange('search')}
          >
            <Ionicons name="grid" size={16} color={activeTab === 'search' ? '#007AFF' : '#666'} />
            <Text style={[styles.tabText, activeTab === 'search' && styles.activeTabText]}>
              Search
            </Text>
          </TouchableOpacity>
        </View>

        {/* Popular searches */}
        {activeTab === 'search' && !searchQuery.trim() && (
          <View style={styles.popularSearchContainer}>
            <Text style={styles.popularSearchTitle}>Popular searches:</Text>
            <FlatList
              data={POPULAR_SEARCHES}
              renderItem={renderPopularSearch}
              keyExtractor={(item) => item}
              horizontal
              showsHorizontalScrollIndicator={false}
              contentContainerStyle={styles.popularSearchList}
            />
          </View>
        )}

        {/* Content */}
        <View style={styles.content}>
          {error ? (
            <View style={styles.errorContainer}>
              <Text style={styles.errorText}>{error}</Text>
            </View>
          ) : (
            <FlatList
              data={gifs}
              renderItem={renderGifItem}
              keyExtractor={(item) => item.id}
              numColumns={2}
              onEndReached={loadMore}
              onEndReachedThreshold={0.5}
              ListFooterComponent={
                loading ? (
                  <View style={styles.loadingContainer}>
                    <ActivityIndicator size="small" color="#007AFF" />
                  </View>
                ) : null
              }
              ListEmptyComponent={
                !loading && activeTab === 'search' && searchQuery.trim() ? (
                  <View style={styles.emptyContainer}>
                    <Text style={styles.emptyText}>No GIFs found for "{searchQuery}"</Text>
                  </View>
                ) : null
              }
            />
          )}
        </View>
      </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#fff',
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#e0e0e0',
  },
  title: {
    fontSize: 18,
    fontWeight: '600',
    color: '#000',
  },
  closeButton: {
    padding: 4,
  },
  searchContainer: {
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#e0e0e0',
  },
  searchInputContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#f5f5f5',
    borderRadius: 8,
    paddingHorizontal: 12,
  },
  searchIcon: {
    marginRight: 8,
  },
  searchInput: {
    flex: 1,
    height: 40,
    fontSize: 16,
    color: '#000',
  },
  tabContainer: {
    flexDirection: 'row',
    borderBottomWidth: 1,
    borderBottomColor: '#e0e0e0',
  },
  tab: {
    flex: 1,
    flexDirection: 'row',
    justifyContent: 'center',
    alignItems: 'center',
    paddingVertical: 12,
    gap: 8,
  },
  activeTab: {
    borderBottomWidth: 2,
    borderBottomColor: '#007AFF',
  },
  tabText: {
    fontSize: 14,
    fontWeight: '500',
    color: '#666',
  },
  activeTabText: {
    color: '#007AFF',
  },
  popularSearchContainer: {
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#e0e0e0',
  },
  popularSearchTitle: {
    fontSize: 14,
    color: '#666',
    marginBottom: 8,
  },
  popularSearchList: {
    gap: 8,
  },
  popularSearchItem: {
    backgroundColor: '#f0f0f0',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 16,
  },
  popularSearchText: {
    fontSize: 14,
    color: '#333',
  },
  content: {
    flex: 1,
    padding: 16,
  },
  gifItem: {
    width: itemWidth,
    height: itemWidth,
    marginRight: 8,
    marginBottom: 8,
    borderRadius: 8,
    overflow: 'hidden',
  },
  gifImage: {
    width: '100%',
    height: '100%',
  },
  loadingContainer: {
    padding: 16,
    alignItems: 'center',
  },
  errorContainer: {
    padding: 16,
    alignItems: 'center',
  },
  errorText: {
    color: '#ff3b30',
    fontSize: 16,
  },
  emptyContainer: {
    padding: 32,
    alignItems: 'center',
  },
  emptyText: {
    color: '#666',
    fontSize: 16,
  },
});
