import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { createYapplrApi } from '../api/client';
import { YapplrApi, User, LoginData, RegisterData } from '../types';

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (data: LoginData) => Promise<void>;
  register: (data: RegisterData) => Promise<void>;
  registerWithoutLogin: (data: RegisterData) => Promise<void>;
  logout: () => Promise<void>;
  updateUser: (user: User) => void;
  api: YapplrApi;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const TOKEN_KEY = 'yapplr_token';
// Use your computer's IP address instead of localhost for mobile devices
const API_BASE_URL = 'http://192.168.254.181:5161'; // Replace with your computer's IP

// Alternative URLs to try if the main one fails
const FALLBACK_URLS = [
  'http://192.168.254.181:5161',
  'http://localhost:5161',
  'http://127.0.0.1:5161'
];

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [token, setToken] = useState<string | null>(null);

  // Create API instance
  const api = createYapplrApi({
    baseURL: API_BASE_URL,
    getToken: () => token,
    onUnauthorized: () => {
      logout();
    },
  });

  const isAuthenticated = !!user;

  // Load token and user on app start
  useEffect(() => {
    loadStoredAuth();
  }, []);

  // Update user when token changes
  useEffect(() => {
    if (token) {
      loadCurrentUser();
    } else {
      setUser(null);
      setIsLoading(false);
    }
  }, [token]);

  const loadStoredAuth = async () => {
    try {
      const storedToken = await AsyncStorage.getItem(TOKEN_KEY);
      if (storedToken) {
        setToken(storedToken);
      } else {
        setIsLoading(false);
      }
    } catch (error) {
      console.error('Error loading stored auth:', error);
      setIsLoading(false);
    }
  };

  const loadCurrentUser = async () => {
    try {
      const currentUser = await api.auth.getCurrentUser();
      setUser(currentUser);
    } catch (error) {
      console.error('Error loading current user:', error);
      // Token might be invalid, clear it
      await AsyncStorage.removeItem(TOKEN_KEY);
      setToken(null);
    } finally {
      setIsLoading(false);
    }
  };

  const login = async (data: LoginData) => {
    try {
      console.log('Attempting login to:', API_BASE_URL);
      const response = await api.auth.login(data);
      console.log('Login response:', response);

      // Check if this is an error response (has status field instead of token)
      if (response && 'status' in response && response.status === 403) {
        // This is an error response, create a proper error object
        const error = new Error(response.detail || 'Email verification required');
        (error as any).response = {
          status: response.status,
          data: {
            message: response.detail,
            title: response.title,
            type: response.type
          }
        };
        throw error;
      }

      // Only proceed if we have a valid response with token
      if (response && response.token) {
        await AsyncStorage.setItem(TOKEN_KEY, response.token);
        setToken(response.token);
        setUser(response.user);
      } else {
        throw new Error('Invalid login response - missing token');
      }
    } catch (error) {
      if (error instanceof Error) {
        if (error.message?.includes('verified')) {
          // Email verification required - this is expected behavior, not an error
          console.warn('Login redirected to email verification:', error.message);
        } else {
          console.error('Login error:', error);
          if (error.message?.includes('Network')) {
            console.error('Network error during login - check API server and network connection');
          }
        }
      }
      throw error;
    }
  };

  const register = async (data: RegisterData) => {
    try {
      const response = await api.auth.register(data);
      await AsyncStorage.setItem(TOKEN_KEY, response.token);
      setToken(response.token);
      setUser(response.user);
    } catch (error) {
      console.error('Register error:', error);
      throw error;
    }
  };

  const registerWithoutLogin = async (data: RegisterData) => {
    try {
      // Register but don't store token or set user
      await api.auth.register(data);
    } catch (error) {
      console.error('Register error:', error);
      throw error;
    }
  };

  const logout = async () => {
    try {
      await AsyncStorage.removeItem(TOKEN_KEY);
      setToken(null);
      setUser(null);
    } catch (error) {
      console.error('Logout error:', error);
    }
  };

  const updateUser = (updatedUser: User) => {
    setUser(updatedUser);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isLoading,
        isAuthenticated,
        login,
        register,
        registerWithoutLogin,
        logout,
        updateUser,
        api,
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
