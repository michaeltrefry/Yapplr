'use client';

import { useEffect, useState } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import Sidebar from '@/components/Sidebar';
import SubscriptionSystemGuard from '@/components/SubscriptionSystemGuard';
import Link from 'next/link';
import { ArrowLeft, Check, Crown, Star, Zap } from 'lucide-react';
import { SubscriptionTier, UserSubscription } from '@/types';
import { subscriptionApi } from '@/lib/api';

export default function SubscriptionPage() {
  const { user, isLoading } = useAuth();
  const router = useRouter();
  const [tiers, setTiers] = useState<SubscriptionTier[]>([]);
  const [currentSubscription, setCurrentSubscription] = useState<UserSubscription | null>(null);
  const [loading, setLoading] = useState(true);
  const [updating, setUpdating] = useState<number | null>(null);

  useEffect(() => {
    if (!isLoading && !user) {
      router.push('/login');
    }
  }, [user, isLoading, router]);

  useEffect(() => {
    if (user) {
      fetchData();
    }
  }, [user]);

  const fetchData = async () => {
    try {
      setLoading(true);
      const [tiersData, subscriptionData] = await Promise.all([
        subscriptionApi.getActiveSubscriptionTiers(),
        subscriptionApi.getMySubscription(),
      ]);
      setTiers(tiersData);
      setCurrentSubscription(subscriptionData);
    } catch (error) {
      console.error('Failed to fetch subscription data:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectTier = async (tierId: number) => {
    if (updating || currentSubscription?.subscriptionTier?.id === tierId) {
      return;
    }

    try {
      setUpdating(tierId);
      await subscriptionApi.assignSubscriptionTier(tierId);
      await fetchData(); // Refresh data
    } catch (error) {
      console.error('Failed to update subscription:', error);
      alert('Failed to update subscription. Please try again.');
    } finally {
      setUpdating(null);
    }
  };

  const getTierIcon = (tier: SubscriptionTier) => {
    if (tier.hasVerifiedBadge) {
      return <Crown className="w-6 h-6 text-yellow-500" />;
    }
    if (tier.price === 0) {
      return <Star className="w-6 h-6 text-gray-500" />;
    }
    return <Zap className="w-6 h-6 text-blue-500" />;
  };

  const formatPrice = (price: number, currency: string, billingCycleMonths: number) => {
    if (price === 0) return 'Free';
    
    const cycle = billingCycleMonths === 1 ? 'month' : 
                  billingCycleMonths === 12 ? 'year' : 
                  `${billingCycleMonths} months`;
    
    return `$${price.toFixed(2)} / ${cycle}`;
  };

  const isCurrentTier = (tierId: number) => {
    return currentSubscription?.subscriptionTier?.id === tierId;
  };

  if (isLoading || loading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-lg text-gray-900">Loading...</div>
      </div>
    );
  }

  if (!user) {
    return null;
  }

  return (
    <SubscriptionSystemGuard fallbackPath="/settings" fallbackText="Back to Settings">
      <div className="min-h-screen bg-gray-50">
        <div className="max-w-6xl mx-auto flex">
          {/* Sidebar */}
          <div className="w-16 lg:w-64 fixed h-full z-10">
            <Sidebar />
          </div>

        {/* Main Content */}
        <div className="flex-1 ml-16 lg:ml-64">
          <div className="max-w-4xl mx-auto lg:border-x border-gray-200 min-h-screen bg-white">
            {/* Header */}
            <div className="sticky top-0 bg-white/80 backdrop-blur-md border-b border-gray-200 p-4">
              <div className="flex items-center space-x-3">
                <Link href="/settings" className="p-2 hover:bg-gray-100 rounded-full transition-colors">
                  <ArrowLeft className="w-5 h-5 text-gray-600" />
                </Link>
                <h1 className="text-xl font-bold text-gray-900">Subscription</h1>
              </div>
            </div>

            {/* Content */}
            <div className="p-6">
              {/* Current Subscription */}
              {currentSubscription?.subscriptionTier && (
                <div className="mb-8 p-4 bg-blue-50 border border-blue-200 rounded-lg">
                  <div className="flex items-center space-x-3">
                    {getTierIcon(currentSubscription.subscriptionTier)}
                    <div>
                      <h3 className="font-semibold text-gray-900">
                        Current Plan: {currentSubscription.subscriptionTier.name}
                      </h3>
                      <p className="text-sm text-gray-600">
                        {formatPrice(
                          currentSubscription.subscriptionTier.price,
                          currentSubscription.subscriptionTier.currency,
                          currentSubscription.subscriptionTier.billingCycleMonths
                        )}
                      </p>
                    </div>
                  </div>
                </div>
              )}

              {/* Subscription Tiers */}
              <div className="space-y-6">
                <div>
                  <h2 className="text-2xl font-bold text-gray-900 mb-2">Choose Your Plan</h2>
                  <p className="text-gray-600">
                    Select the subscription tier that best fits your needs.
                  </p>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                  {tiers.map((tier) => (
                    <div
                      key={tier.id}
                      className={`relative bg-white rounded-lg shadow border-2 transition-all ${
                        isCurrentTier(tier.id)
                          ? 'border-blue-500 ring-2 ring-blue-200'
                          : 'border-gray-200 hover:border-gray-300'
                      } ${tier.isDefault ? 'ring-2 ring-yellow-200' : ''}`}
                    >
                      {/* Popular Badge */}
                      {tier.isDefault && (
                        <div className="absolute -top-3 left-1/2 transform -translate-x-1/2">
                          <span className="bg-yellow-500 text-white px-3 py-1 rounded-full text-sm font-medium">
                            Most Popular
                          </span>
                        </div>
                      )}

                      <div className="p-6">
                        {/* Header */}
                        <div className="text-center mb-6">
                          <div className="flex justify-center mb-3">
                            {getTierIcon(tier)}
                          </div>
                          <h3 className="text-xl font-semibold text-gray-900 mb-2">
                            {tier.name}
                          </h3>
                          <div className="text-3xl font-bold text-gray-900 mb-1">
                            {formatPrice(tier.price, tier.currency, tier.billingCycleMonths)}
                          </div>
                        </div>

                        {/* Description */}
                        <p className="text-gray-600 text-center mb-6">
                          {tier.description}
                        </p>

                        {/* Features */}
                        <div className="space-y-3 mb-6">
                          <div className="flex items-center">
                            <Check className="w-4 h-4 text-green-500 mr-3" />
                            <span className="text-sm text-gray-700">
                              {tier.showAdvertisements ? 'Advertiser supported' : 'No advertisements'}
                            </span>
                          </div>
                          {tier.hasVerifiedBadge && (
                            <div className="flex items-center">
                              <Check className="w-4 h-4 text-green-500 mr-3" />
                              <span className="text-sm text-gray-700">
                                Verified creator badge
                              </span>
                            </div>
                          )}
                          <div className="flex items-center">
                            <Check className="w-4 h-4 text-green-500 mr-3" />
                            <span className="text-sm text-gray-700">
                              Full platform access
                            </span>
                          </div>
                        </div>

                        {/* Action Button */}
                        <button
                          onClick={() => handleSelectTier(tier.id)}
                          disabled={updating !== null || isCurrentTier(tier.id)}
                          className={`w-full py-3 px-4 rounded-lg font-medium transition-colors ${
                            isCurrentTier(tier.id)
                              ? 'bg-green-100 text-green-800 cursor-default'
                              : updating === tier.id
                              ? 'bg-gray-100 text-gray-500 cursor-not-allowed'
                              : 'bg-blue-600 text-white hover:bg-blue-700'
                          }`}
                        >
                          {updating === tier.id
                            ? 'Updating...'
                            : isCurrentTier(tier.id)
                            ? 'Current Plan'
                            : 'Select Plan'}
                        </button>
                      </div>
                    </div>
                  ))}
                </div>

                {/* Note */}
                <div className="mt-8 p-4 bg-gray-50 rounded-lg">
                  <p className="text-sm text-gray-600 text-center">
                    <strong>Note:</strong> Payment processing is not yet implemented. 
                    Subscription tier selection is for demonstration purposes only.
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
        </div>
      </div>
    </SubscriptionSystemGuard>
  );
}
