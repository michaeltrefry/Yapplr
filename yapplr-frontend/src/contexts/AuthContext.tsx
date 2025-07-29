'use client';

import React, { createContext, useContext, useEffect, useState } from 'react';
import { User, AuthResponse, RegisterResponse, LoginData, RegisterData } from '@/types';
import { authApi, userApi, subscriptionApi } from '@/lib/api';

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  login: (data: LoginData) => Promise<{ needsSubscriptionSelection: boolean }>;
  register: (data: RegisterData) => Promise<RegisterResponse>;
  logout: () => void;
  updateUser: (user: User) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const initAuth = async () => {
      console.log('🔐 Frontend AuthContext: Initializing authentication...');
      const token = localStorage.getItem('token');
      console.log('🔐 Frontend AuthContext: Token found:', !!token);

      if (token) {
        try {
          console.log('🔐 Frontend AuthContext: Fetching current user...');
          const userData = await userApi.getCurrentUser();
          console.log('🔐 Frontend AuthContext: User data received:', userData);
          setUser(userData);
        } catch (error) {
          console.error('🔐 Frontend AuthContext: Failed to get current user:', error);
          localStorage.removeItem('token');
        }
      } else {
        console.log('🔐 Frontend AuthContext: No token found, user not authenticated');
      }
      setIsLoading(false);
    };

    initAuth();
  }, []);

  // Listen for storage changes to handle token removal by API interceptor
  useEffect(() => {
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === 'token' && e.newValue === null) {
        // Token was removed, clear user state
        setUser(null);
      }
    };

    window.addEventListener('storage', handleStorageChange);
    return () => window.removeEventListener('storage', handleStorageChange);
  }, []);

  const login = async (data: LoginData) => {
    const response: AuthResponse = await authApi.login(data);
    localStorage.setItem('token', response.token);
    setUser(response.user);

    // Check if user needs to select a subscription tier
    try {
      const subscription = await subscriptionApi.getMySubscription();
      return { needsSubscriptionSelection: !subscription.subscriptionTier };
    } catch (error: any) {
      // If subscription system is disabled (404), don't require subscription selection
      if (error.response?.status === 404) {
        console.log('Subscription system is disabled, skipping subscription selection');
        return { needsSubscriptionSelection: false };
      }

      // For other errors, assume they need to select one
      console.warn('Could not fetch subscription info, assuming selection needed:', error);
      return { needsSubscriptionSelection: true };
    }
  };

  const register = async (data: RegisterData) => {
    const response = await authApi.register(data);
    // Registration no longer returns a token - user must verify email first
    // Don't set token or user state, just return the response for the UI to handle
    return response;
  };

  const logout = () => {
    localStorage.removeItem('token');
    setUser(null);
  };

  const updateUser = (updatedUser: User) => {
    setUser(updatedUser);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        login,
        register,
        logout,
        updateUser,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
