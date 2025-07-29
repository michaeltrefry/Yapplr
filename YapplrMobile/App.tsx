import React from 'react';
import { StatusBar } from 'expo-status-bar';
import { LogBox } from 'react-native';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { AuthProvider, useAuth } from './src/contexts/AuthContext';
import { ThemeProvider } from './src/contexts/ThemeContext';
import { NotificationProvider } from './src/contexts/NotificationContext';

import NotificationBannerManager from './src/components/NotificationBannerManager';
import AppNavigator from './src/navigation/AppNavigator';

// Suppress useInsertionEffect warnings in development (React 19 + Expo compatibility issue)
if (__DEV__) {
  LogBox.ignoreLogs([
    'Warning: useInsertionEffect must not schedule updates',
    'useInsertionEffect must not schedule updates',
  ]);
}

import { API_BASE_URL } from './src/config/api';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 2,
      staleTime: 5 * 60 * 1000, // 5 minutes
    },
  },
});

function AppWithNotifications() {
  const { api, isAuthenticated } = useAuth();

  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <ThemeProvider>
        {isAuthenticated ? (
          <NotificationProvider baseURL={API_BASE_URL} apiClient={api}>
            <NotificationBannerManager>
              <AppNavigator />
              <StatusBar style="auto" />
            </NotificationBannerManager>
          </NotificationProvider>
        ) : (
          <>
            <AppNavigator />
            <StatusBar style="auto" />
          </>
        )}
      </ThemeProvider>
    </GestureHandlerRootView>
  );
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <AppWithNotifications />
      </AuthProvider>
    </QueryClientProvider>
  );
}
