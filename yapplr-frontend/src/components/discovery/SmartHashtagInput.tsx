'use client';

import React, { useState, useEffect, useRef } from 'react';
import { enhancedTrendingApi } from '@/lib/api';
import { TrendingHashtagDto } from '@/types';

interface SmartHashtagInputProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  className?: string;
  maxSuggestions?: number;
  onHashtagSelect?: (hashtag: string) => void;
}

export default function SmartHashtagInput({
  value,
  onChange,
  placeholder = "Add hashtags...",
  className = '',
  maxSuggestions = 8,
  onHashtagSelect
}: SmartHashtagInputProps) {
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [trendingSuggestions, setTrendingSuggestions] = useState<TrendingHashtagDto[]>([]);
  const [filteredSuggestions, setFilteredSuggestions] = useState<TrendingHashtagDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [selectedIndex, setSelectedIndex] = useState(-1);
  
  const inputRef = useRef<HTMLInputElement>(null);
  const suggestionsRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    loadTrendingSuggestions();
  }, []);

  useEffect(() => {
    const currentInput = getCurrentHashtagInput();
    if (currentInput.length > 0) {
      const filtered = trendingSuggestions.filter(hashtag =>
        hashtag.name.toLowerCase().includes(currentInput.toLowerCase())
      ).slice(0, maxSuggestions);
      setFilteredSuggestions(filtered);
      setShowSuggestions(true);
    } else {
      setFilteredSuggestions(trendingSuggestions.slice(0, maxSuggestions));
      setShowSuggestions(false);
    }
    setSelectedIndex(-1);
  }, [value, trendingSuggestions, maxSuggestions]);

  const loadTrendingSuggestions = async () => {
    try {
      setLoading(true);
      const trending = await enhancedTrendingApi.getVelocityTrendingHashtags(20, 24);
      setTrendingSuggestions(trending);
    } catch (error) {
      console.error('Failed to load trending suggestions:', error);
    } finally {
      setLoading(false);
    }
  };

  const getCurrentHashtagInput = () => {
    const cursorPosition = inputRef.current?.selectionStart || 0;
    const textBeforeCursor = value.substring(0, cursorPosition);
    const lastHashtagMatch = textBeforeCursor.match(/#(\w*)$/);
    return lastHashtagMatch ? lastHashtagMatch[1] : '';
  };

  const insertHashtag = (hashtag: string) => {
    const cursorPosition = inputRef.current?.selectionStart || 0;
    const textBeforeCursor = value.substring(0, cursorPosition);
    const textAfterCursor = value.substring(cursorPosition);
    
    // Find the start of the current hashtag being typed
    const lastHashtagMatch = textBeforeCursor.match(/#(\w*)$/);
    if (lastHashtagMatch) {
      const hashtagStart = lastHashtagMatch.index!;
      const newValue = 
        value.substring(0, hashtagStart) + 
        `#${hashtag} ` + 
        textAfterCursor;
      onChange(newValue);
      
      // Set cursor position after the inserted hashtag
      setTimeout(() => {
        if (inputRef.current) {
          const newCursorPosition = hashtagStart + hashtag.length + 2;
          inputRef.current.setSelectionRange(newCursorPosition, newCursorPosition);
          inputRef.current.focus();
        }
      }, 0);
    } else {
      // If no hashtag being typed, append to the end
      const newValue = value + (value.endsWith(' ') ? '' : ' ') + `#${hashtag} `;
      onChange(newValue);
    }

    setShowSuggestions(false);
    onHashtagSelect?.(hashtag);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (!showSuggestions || filteredSuggestions.length === 0) return;

    switch (e.key) {
      case 'ArrowDown':
        e.preventDefault();
        setSelectedIndex(prev => 
          prev < filteredSuggestions.length - 1 ? prev + 1 : 0
        );
        break;
      case 'ArrowUp':
        e.preventDefault();
        setSelectedIndex(prev => 
          prev > 0 ? prev - 1 : filteredSuggestions.length - 1
        );
        break;
      case 'Enter':
      case 'Tab':
        if (selectedIndex >= 0) {
          e.preventDefault();
          insertHashtag(filteredSuggestions[selectedIndex].name);
        }
        break;
      case 'Escape':
        setShowSuggestions(false);
        setSelectedIndex(-1);
        break;
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    onChange(e.target.value);
  };

  const handleFocus = () => {
    if (trendingSuggestions.length > 0) {
      setShowSuggestions(true);
    }
  };

  const handleBlur = () => {
    // Delay hiding suggestions to allow for clicks
    setTimeout(() => setShowSuggestions(false), 200);
  };

  const getVelocityIcon = (velocity: number) => {
    if (velocity > 0.7) return '🔥';
    if (velocity > 0.4) return '📈';
    return '📊';
  };

  return (
    <div className={`relative ${className}`}>
      <input
        ref={inputRef}
        type="text"
        value={value}
        onChange={handleInputChange}
        onKeyDown={handleKeyDown}
        onFocus={handleFocus}
        onBlur={handleBlur}
        placeholder={placeholder}
        className="w-full px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent bg-white dark:bg-gray-800 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400"
      />

      {showSuggestions && (
        <div 
          ref={suggestionsRef}
          className="absolute z-50 w-full mt-1 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg shadow-lg max-h-64 overflow-y-auto"
        >
          {loading ? (
            <div className="p-4 text-center text-gray-500 dark:text-gray-400">
              Loading suggestions...
            </div>
          ) : (
            <>
              {getCurrentHashtagInput().length === 0 && (
                <div className="p-3 border-b border-gray-200 dark:border-gray-700">
                  <h4 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-2">
                    🔥 Trending Now
                  </h4>
                </div>
              )}
              
              {filteredSuggestions.map((hashtag, index) => (
                <button
                  key={hashtag.name}
                  onClick={() => insertHashtag(hashtag.name)}
                  className={`w-full text-left px-4 py-3 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors ${
                    index === selectedIndex ? 'bg-blue-50 dark:bg-blue-900/20' : ''
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <div className="flex items-center space-x-2">
                      <span className="text-blue-600 dark:text-blue-400 font-medium">
                        #{hashtag.name}
                      </span>
                      <span className="text-sm">
                        {getVelocityIcon(hashtag.velocity)}
                      </span>
                      {hashtag.velocity > 0.4 && (
                        <span className="px-2 py-1 text-xs bg-orange-100 dark:bg-orange-900/30 text-orange-600 dark:text-orange-400 rounded-full">
                          +{(hashtag.velocity * 100).toFixed(0)}%
                        </span>
                      )}
                    </div>
                    <div className="text-xs text-gray-500 dark:text-gray-400">
                      {hashtag.postCount.toLocaleString()} posts
                    </div>
                  </div>
                  
                  {hashtag.category && (
                    <div className="text-xs text-gray-400 dark:text-gray-500 mt-1">
                      {hashtag.category}
                    </div>
                  )}
                </button>
              ))}
              
              {filteredSuggestions.length === 0 && getCurrentHashtagInput().length > 0 && (
                <div className="p-4 text-center text-gray-500 dark:text-gray-400">
                  No matching hashtags found
                </div>
              )}
            </>
          )}
        </div>
      )}
    </div>
  );
}
