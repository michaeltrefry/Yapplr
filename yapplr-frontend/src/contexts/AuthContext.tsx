'use client';

import React, { createContext, useContext, useEffect, useState } from 'react';
import { User, AuthResponse, RegisterResponse, LoginData, RegisterData } from '@/types';
import { authApi, userApi } from '@/lib/api';

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  login: (data: LoginData) => Promise<void>;
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
      const token = localStorage.getItem('token');
      if (token) {
        try {
          const userData = await userApi.getCurrentUser();
          setUser(userData);
        } catch {
          localStorage.removeItem('token');
        }
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
