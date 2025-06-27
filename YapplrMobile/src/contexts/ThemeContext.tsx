import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuth } from './AuthContext';

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
  const { api, isAuthenticated } = useAuth();
  const queryClient = useQueryClient();
  const [isDarkMode, setIsDarkMode] = useState(false);

  // Fetch user preferences
  const { data: preferences, isLoading } = useQuery({
    queryKey: ['preferences'],
    queryFn: () => api.preferences.get(),
    enabled: isAuthenticated,
    retry: 1,
  });

  // Update preferences mutation
  const updatePreferencesMutation = useMutation({
    mutationFn: (darkMode: boolean) => api.preferences.update({ darkMode }),
    onSuccess: (data) => {
      setIsDarkMode(data.darkMode);
      queryClient.setQueryData(['preferences'], data);
    },
    onError: (error) => {
      console.error('Failed to update preferences:', error);
      // Revert the local state on error
      setIsDarkMode(!isDarkMode);
    },
  });

  // Update local state when preferences are loaded
  useEffect(() => {
    if (preferences) {
      setIsDarkMode(preferences.darkMode);
    }
  }, [preferences]);

  const toggleDarkMode = () => {
    const newDarkMode = !isDarkMode;
    setIsDarkMode(newDarkMode); // Optimistic update
    updatePreferencesMutation.mutate(newDarkMode);
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

// Theme colors
export const lightTheme = {
  background: '#FFFFFF',
  surface: '#F9FAFB',
  border: '#E5E7EB',
  text: '#1F2937',
  textSecondary: '#6B7280',
  textMuted: '#9CA3AF',
  primary: '#3B82F6',
  primaryText: '#FFFFFF',
  success: '#10B981',
  error: '#EF4444',
  warning: '#F59E0B',
  card: '#FFFFFF',
  input: '#FFFFFF',
  inputBorder: '#D1D5DB',
  shadow: 'rgba(0, 0, 0, 0.1)',
};

export const darkTheme = {
  background: '#111827',
  surface: '#1F2937',
  border: '#374151',
  text: '#F9FAFB',
  textSecondary: '#D1D5DB',
  textMuted: '#9CA3AF',
  primary: '#3B82F6',
  primaryText: '#FFFFFF',
  success: '#10B981',
  error: '#EF4444',
  warning: '#F59E0B',
  card: '#1F2937',
  input: '#374151',
  inputBorder: '#4B5563',
  shadow: 'rgba(0, 0, 0, 0.3)',
};

export type Theme = typeof lightTheme;
