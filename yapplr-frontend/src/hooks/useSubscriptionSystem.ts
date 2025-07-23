import { useState, useEffect } from 'react';
import api from '@/lib/api';

interface SubscriptionSystemStatus {
  enabled: boolean;
}

export function useSubscriptionSystem() {
  const [isEnabled, setIsEnabled] = useState<boolean | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    checkSubscriptionSystemStatus();
  }, []);

  const checkSubscriptionSystemStatus = async () => {
    try {
      setLoading(true);
      setError(null);
      
      // For non-authenticated users, we'll assume the system is enabled
      // The middleware will handle blocking access to actual endpoints
      const response = await api.get('/admin/subscription-system/status');
      setIsEnabled(response.data.enabled);
    } catch (error: any) {
      // If we can't check the status (e.g., not authenticated), assume enabled
      // The middleware will handle the actual blocking
      if (error.response?.status === 401 || error.response?.status === 403) {
        setIsEnabled(true);
      } else {
        setError('Failed to check subscription system status');
        setIsEnabled(true); // Default to enabled on error
      }
    } finally {
      setLoading(false);
    }
  };

  return {
    isEnabled,
    loading,
    error,
    refresh: checkSubscriptionSystemStatus
  };
}
