'use client';

import { useEffect, useState } from 'react';
import { adminApi } from '@/lib/api';
import { SubscriptionTier } from '@/types';
import SubscriptionTierForm from '@/components/admin/SubscriptionTierForm';
import {
  Plus,
  Edit,
  Trash2,
  DollarSign,
  Users,
  Star,
  Eye,
  EyeOff,
  Crown,
  Zap,
} from 'lucide-react';

export default function AdminSubscriptionsPage() {
  const [tiers, setTiers] = useState<SubscriptionTier[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [editingTier, setEditingTier] = useState<SubscriptionTier | null>(null);

  useEffect(() => {
    fetchTiers();
  }, []);

  const fetchTiers = async () => {
    try {
      setLoading(true);
      const data = await adminApi.getSubscriptionTiers(true); // Include inactive
      setTiers(data);
    } catch (error) {
      console.error('Failed to fetch subscription tiers:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateTier = () => {
    setEditingTier(null);
    setShowCreateForm(true);
  };

  const handleEditTier = (tier: SubscriptionTier) => {
    setEditingTier(tier);
    setShowCreateForm(true);
  };

  const handleDeleteTier = async (tierId: number) => {
    if (!confirm('Are you sure you want to delete this subscription tier? This action cannot be undone.')) {
      return;
    }

    try {
      await adminApi.deleteSubscriptionTier(tierId);
      await fetchTiers();
    } catch (error) {
      console.error('Failed to delete subscription tier:', error);
      alert('Failed to delete subscription tier. It may have users assigned to it.');
    }
  };

  const handleFormSubmit = async () => {
    setShowCreateForm(false);
    setEditingTier(null);
    await fetchTiers();
  };

  const getTierIcon = (tier: SubscriptionTier) => {
    if (tier.hasVerifiedBadge) {
      return <Crown className="w-5 h-5 text-yellow-500" />;
    }
    if (tier.price === 0) {
      return <Star className="w-5 h-5 text-gray-500" />;
    }
    return <Zap className="w-5 h-5 text-blue-500" />;
  };

  const formatPrice = (price: number, currency: string, billingCycleMonths: number) => {
    if (price === 0) return 'Free';
    
    const cycle = billingCycleMonths === 1 ? 'month' : 
                  billingCycleMonths === 12 ? 'year' : 
                  `${billingCycleMonths} months`;
    
    return `$${price.toFixed(2)} / ${cycle}`;
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Subscription Tiers</h1>
          <p className="text-gray-600">Manage subscription tiers and pricing</p>
        </div>
        <button
          onClick={handleCreateTier}
          className="flex items-center px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
        >
          <Plus className="w-4 h-4 mr-2" />
          Create Tier
        </button>
      </div>

      {/* Subscription Tiers Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {tiers.map((tier) => (
          <div
            key={tier.id}
            className={`bg-white rounded-lg shadow border-2 ${
              tier.isDefault ? 'border-blue-500' : 'border-gray-200'
            } ${!tier.isActive ? 'opacity-60' : ''}`}
          >
            <div className="p-6">
              {/* Header */}
              <div className="flex items-center justify-between mb-4">
                <div className="flex items-center space-x-2">
                  {getTierIcon(tier)}
                  <h3 className="text-lg font-semibold text-gray-900">{tier.name}</h3>
                  {tier.isDefault && (
                    <span className="px-2 py-1 text-xs bg-blue-100 text-blue-800 rounded-full">
                      Default
                    </span>
                  )}
                </div>
                <div className="flex items-center space-x-1">
                  {tier.isActive ? (
                    <Eye className="w-4 h-4 text-green-500" />
                  ) : (
                    <EyeOff className="w-4 h-4 text-gray-400" />
                  )}
                </div>
              </div>

              {/* Price */}
              <div className="mb-4">
                <div className="text-2xl font-bold text-gray-900">
                  {formatPrice(tier.price, tier.currency, tier.billingCycleMonths)}
                </div>
                <div className="text-sm text-gray-600">Sort Order: {tier.sortOrder}</div>
              </div>

              {/* Description */}
              <p className="text-gray-600 mb-4">{tier.description}</p>

              {/* Features */}
              <div className="space-y-2 mb-4">
                <div className="flex items-center text-sm">
                  <span className={`w-2 h-2 rounded-full mr-2 ${
                    tier.showAdvertisements ? 'bg-red-500' : 'bg-green-500'
                  }`}></span>
                  {tier.showAdvertisements ? 'Shows Advertisements' : 'No Advertisements'}
                </div>
                {tier.hasVerifiedBadge && (
                  <div className="flex items-center text-sm">
                    <span className="w-2 h-2 rounded-full bg-yellow-500 mr-2"></span>
                    Verified Badge
                  </div>
                )}
              </div>

              {/* Actions */}
              <div className="flex space-x-2">
                <button
                  onClick={() => handleEditTier(tier)}
                  className="flex-1 flex items-center justify-center px-3 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition-colors"
                >
                  <Edit className="w-4 h-4 mr-1" />
                  Edit
                </button>
                <button
                  onClick={() => handleDeleteTier(tier.id)}
                  className="flex items-center justify-center px-3 py-2 bg-red-100 text-red-700 rounded-lg hover:bg-red-200 transition-colors"
                >
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Empty State */}
      {tiers.length === 0 && (
        <div className="text-center py-12">
          <DollarSign className="w-12 h-12 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">No subscription tiers</h3>
          <p className="text-gray-600 mb-4">Get started by creating your first subscription tier.</p>
          <button
            onClick={handleCreateTier}
            className="inline-flex items-center px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
          >
            <Plus className="w-4 h-4 mr-2" />
            Create Tier
          </button>
        </div>
      )}

      {/* Create/Edit Form Modal */}
      {showCreateForm && (
        <SubscriptionTierForm
          tier={editingTier}
          onSubmit={handleFormSubmit}
          onCancel={() => {
            setShowCreateForm(false);
            setEditingTier(null);
          }}
        />
      )}
    </div>
  );
}
