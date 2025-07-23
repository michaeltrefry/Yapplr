'use client';

import { useState, useEffect } from 'react';
import { CreditCard, AlertTriangle, ArrowLeft } from 'lucide-react';
import Link from 'next/link';
import { subscriptionApi } from '@/lib/api';

interface SubscriptionSystemGuardProps {
  children: React.ReactNode;
  fallbackPath?: string;
  fallbackText?: string;
}

export default function SubscriptionSystemGuard({ 
  children, 
  fallbackPath = '/settings',
  fallbackText = 'Back to Settings'
}: SubscriptionSystemGuardProps) {
  const [isEnabled, setIsEnabled] = useState<boolean | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    checkSubscriptionSystemStatus();
  }, []);

  const checkSubscriptionSystemStatus = async () => {
    try {
      setLoading(true);
      
      // Try to fetch subscription tiers - if this fails with 404, system is disabled
      await subscriptionApi.getActiveSubscriptionTiers();
      setIsEnabled(true);
    } catch (error: any) {
      // If we get a 404, the subscription system is disabled
      if (error.response?.status === 404) {
        setIsEnabled(false);
      } else {
        // For other errors (network, auth, etc.), assume enabled and let the page handle it
        setIsEnabled(true);
      }
    } finally {
      setLoading(false);
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500 mx-auto mb-4"></div>
          <p className="text-gray-600">Loading...</p>
        </div>
      </div>
    );
  }

  if (isEnabled === false) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="max-w-md mx-auto text-center">
          <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-8">
            <div className="w-16 h-16 bg-gray-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <CreditCard className="w-8 h-8 text-gray-400" />
            </div>
            
            <h1 className="text-2xl font-bold text-gray-900 mb-2">
              Subscriptions Unavailable
            </h1>
            
            <p className="text-gray-600 mb-6">
              The subscription system is currently disabled. Please check back later or contact support if you need assistance.
            </p>
            
            <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4 mb-6">
              <div className="flex items-center space-x-2">
                <AlertTriangle className="w-4 h-4 text-yellow-600" />
                <span className="text-sm text-yellow-800">
                  This feature is temporarily unavailable
                </span>
              </div>
            </div>
            
            <Link
              href={fallbackPath}
              className="inline-flex items-center px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
            >
              <ArrowLeft className="w-4 h-4 mr-2" />
              {fallbackText}
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}
