'use client';

import { useEffect, useState } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import SubscriptionSystemGuard from '@/components/SubscriptionSystemGuard';
import { Check, Crown, Star, Zap, ArrowRight } from 'lucide-react';
import { SubscriptionTier } from '@/types';
import { subscriptionApi } from '@/lib/api';

export default function OnboardingSubscriptionPage() {
  const { user, isLoading } = useAuth();
  const router = useRouter();
  const [tiers, setTiers] = useState<SubscriptionTier[]>([]);
  const [loading, setLoading] = useState(true);
  const [selecting, setSelecting] = useState<number | null>(null);

  useEffect(() => {
    if (!isLoading && !user) {
      router.push('/login');
    }
  }, [user, isLoading, router]);

  useEffect(() => {
    if (user) {
      fetchTiers();
    }
  }, [user]);

  const fetchTiers = async () => {
    try {
      setLoading(true);
      const tiersData = await subscriptionApi.getActiveSubscriptionTiers();
      setTiers(tiersData);
    } catch (error) {
      console.error('Failed to fetch subscription tiers:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectTier = async (tierId: number) => {
    if (selecting) return;

    try {
      setSelecting(tierId);
      await subscriptionApi.assignSubscriptionTier(tierId);
      
      // Redirect to home page after successful selection
      router.push('/');
    } catch (error) {
      console.error('Failed to select subscription tier:', error);
      alert('Failed to select subscription tier. Please try again.');
    } finally {
      setSelecting(null);
    }
  };

  const handleSkip = () => {
    // For now, just redirect to home. In the future, we might assign a default tier
    router.push('/');
  };

  const getTierIcon = (tier: SubscriptionTier) => {
    if (tier.hasVerifiedBadge) {
      return <Crown className="w-8 h-8 text-yellow-500" />;
    }
    if (tier.price === 0) {
      return <Star className="w-8 h-8 text-gray-500" />;
    }
    return <Zap className="w-8 h-8 text-blue-500" />;
  };

  const formatPrice = (price: number, currency: string, billingCycleMonths: number) => {
    if (price === 0) return 'Free';
    
    const cycle = billingCycleMonths === 1 ? 'month' : 
                  billingCycleMonths === 12 ? 'year' : 
                  `${billingCycleMonths} months`;
    
    return `$${price.toFixed(2)} / ${cycle}`;
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
    <SubscriptionSystemGuard fallbackPath="/" fallbackText="Back to Home">
      <div className="min-h-screen bg-gray-50">
        <div className="max-w-4xl mx-auto py-12 px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="text-center mb-12">
          <h1 className="text-4xl font-bold text-gray-900 mb-4">
            Welcome to Yapplr, {user.username}!
          </h1>
          <p className="text-xl text-gray-600 mb-2">
            Choose your subscription tier to get started
          </p>
          <p className="text-gray-500">
            You can always change this later in your settings
          </p>
        </div>

        {/* Subscription Tiers */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8 mb-12">
          {tiers.map((tier) => (
            <div
              key={tier.id}
              className={`relative bg-white rounded-xl shadow-lg border-2 transition-all hover:shadow-xl ${
                tier.isDefault ? 'border-blue-500 ring-2 ring-blue-200' : 'border-gray-200'
              }`}
            >
              {/* Popular Badge */}
              {tier.isDefault && (
                <div className="absolute -top-4 left-1/2 transform -translate-x-1/2">
                  <span className="bg-blue-500 text-white px-4 py-2 rounded-full text-sm font-medium">
                    Most Popular
                  </span>
                </div>
              )}

              <div className="p-8">
                {/* Header */}
                <div className="text-center mb-8">
                  <div className="flex justify-center mb-4">
                    {getTierIcon(tier)}
                  </div>
                  <h3 className="text-2xl font-bold text-gray-900 mb-2">
                    {tier.name}
                  </h3>
                  <div className="text-4xl font-bold text-gray-900 mb-2">
                    {formatPrice(tier.price, tier.currency, tier.billingCycleMonths)}
                  </div>
                </div>

                {/* Description */}
                <p className="text-gray-600 text-center mb-8">
                  {tier.description}
                </p>

                {/* Features */}
                <div className="space-y-4 mb-8">
                  <div className="flex items-center">
                    <Check className="w-5 h-5 text-green-500 mr-3 flex-shrink-0" />
                    <span className="text-gray-700">
                      {tier.showAdvertisements ? 'Advertiser supported' : 'No advertisements'}
                    </span>
                  </div>
                  {tier.hasVerifiedBadge && (
                    <div className="flex items-center">
                      <Check className="w-5 h-5 text-green-500 mr-3 flex-shrink-0" />
                      <span className="text-gray-700">
                        Verified creator badge
                      </span>
                    </div>
                  )}
                  <div className="flex items-center">
                    <Check className="w-5 h-5 text-green-500 mr-3 flex-shrink-0" />
                    <span className="text-gray-700">
                      Full platform access
                    </span>
                  </div>
                  <div className="flex items-center">
                    <Check className="w-5 h-5 text-green-500 mr-3 flex-shrink-0" />
                    <span className="text-gray-700">
                      Create and share posts
                    </span>
                  </div>
                  <div className="flex items-center">
                    <Check className="w-5 h-5 text-green-500 mr-3 flex-shrink-0" />
                    <span className="text-gray-700">
                      Join groups and conversations
                    </span>
                  </div>
                </div>

                {/* Action Button */}
                <button
                  onClick={() => handleSelectTier(tier.id)}
                  disabled={selecting !== null}
                  className={`w-full py-4 px-6 rounded-lg font-semibold text-lg transition-colors ${
                    tier.isDefault
                      ? 'bg-blue-600 text-white hover:bg-blue-700'
                      : 'bg-gray-900 text-white hover:bg-gray-800'
                  } disabled:opacity-50 disabled:cursor-not-allowed`}
                >
                  {selecting === tier.id ? (
                    <div className="flex items-center justify-center">
                      <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-white mr-2"></div>
                      Selecting...
                    </div>
                  ) : (
                    <div className="flex items-center justify-center">
                      Choose {tier.name}
                      <ArrowRight className="w-5 h-5 ml-2" />
                    </div>
                  )}
                </button>
              </div>
            </div>
          ))}
        </div>

        {/* Skip Option */}
        <div className="text-center">
          <button
            onClick={handleSkip}
            disabled={selecting !== null}
            className="text-gray-600 hover:text-gray-800 font-medium disabled:opacity-50"
          >
            Skip for now
          </button>
          <p className="text-sm text-gray-500 mt-2">
            You can choose a subscription tier later in your settings
          </p>
        </div>

        {/* Note */}
        <div className="mt-12 p-6 bg-blue-50 rounded-lg">
          <p className="text-sm text-blue-800 text-center">
            <strong>Note:</strong> Payment processing is not yet implemented. 
            Subscription tier selection is for demonstration purposes only.
            All tiers currently provide the same functionality.
          </p>
        </div>
        </div>
      </div>
    </SubscriptionSystemGuard>
  );
}
