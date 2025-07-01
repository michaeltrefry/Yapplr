'use client';

import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { preferencesApi } from '@/lib/api';
import { useAuth } from '@/contexts/AuthContext';

interface ThemeContextType {
  isDarkMode: boolean;
  toggleDarkMode: () => void;
  isLoading: boolean;
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

interface ThemeProviderProps {
  children: ReactNode;
}

export function ThemeProvider({ children }: ThemeProviderProps) {
  const { user, isLoading: authLoading } = useAuth();
  const [isDarkMode, setIsDarkMode] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  // Load user preferences on mount or when user changes
  useEffect(() => {
    const loadPreferences = async () => {
      try {
        // Only try to load from API if user is authenticated
        if (user) {
          const preferences = await preferencesApi.get();
          setIsDarkMode(preferences.darkMode);
        } else {
          // If not authenticated, check localStorage for a fallback
          const savedTheme = localStorage.getItem('darkMode');
          if (savedTheme) {
            setIsDarkMode(savedTheme === 'true');
          }
        }
      } catch (error) {
        console.error('Failed to load preferences:', error);
        // If there's an error, check localStorage for a fallback
        const savedTheme = localStorage.getItem('darkMode');
        if (savedTheme) {
          setIsDarkMode(savedTheme === 'true');
        }
      } finally {
        setIsLoading(false);
      }
    };

    // Only load preferences after auth has finished loading
    if (!authLoading) {
      loadPreferences();
    }
  }, [user, authLoading]);

  // Apply theme to document
  useEffect(() => {
    console.log('ThemeContext: Applying theme, isDarkMode:', isDarkMode);
    if (isDarkMode) {
      document.documentElement.classList.add('dark');
      console.log('ThemeContext: Added dark class to document');
    } else {
      document.documentElement.classList.remove('dark');
      console.log('ThemeContext: Removed dark class from document');
    }

    // Save to localStorage as fallback
    localStorage.setItem('darkMode', isDarkMode.toString());
    console.log('ThemeContext: Saved to localStorage:', isDarkMode);
  }, [isDarkMode]);

  const toggleDarkMode = async () => {
    const newDarkMode = !isDarkMode;
    console.log('ThemeContext: Toggling dark mode from', isDarkMode, 'to', newDarkMode);
    setIsDarkMode(newDarkMode); // Optimistic update

    try {
      // Only try to update via API if user is authenticated
      if (user) {
        console.log('ThemeContext: Updating preferences via API');
        await preferencesApi.update({ darkMode: newDarkMode });
        console.log('ThemeContext: Successfully updated preferences');
      } else {
        console.log('ThemeContext: User not authenticated, skipping API update');
      }
    } catch (error) {
      console.error('Failed to update preferences:', error);
      // Revert on error
      setIsDarkMode(!newDarkMode);
    }
  };

  const value: ThemeContextType = {
    isDarkMode,
    toggleDarkMode,
    isLoading,
  };

  return (
    <ThemeContext.Provider value={value}>
      {children}
    </ThemeContext.Provider>
  );
}

export function useTheme(): ThemeContextType {
  const context = useContext(ThemeContext);
  if (context === undefined) {
    throw new Error('useTheme must be used within a ThemeProvider');
  }
  return context;
}
