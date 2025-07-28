import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  TouchableOpacity,
  Alert,
  ActivityIndicator,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNavigation } from '@react-navigation/native';
import { Crown, Star, Zap, Check, ArrowLeft } from 'lucide-react-native';
import { SubscriptionTier, UserSubscription } from '../types';
import SubscriptionTierBadge from '../components/SubscriptionTierBadge';

// Mock API calls - these would be replaced with actual API calls
const subscriptionApi = {
  getActiveSubscriptionTiers: async (): Promise<SubscriptionTier[]> => {
    // This would be replaced with actual API call
    return [
      {
        id: 1,
        name: 'Free',
        description: 'Free tier with advertisements. Perfect for getting started on Yapplr.',
        price: 0,
        currency: 'USD',
        billingCycleMonths: 1,
        isActive: true,
        isDefault: true,
        sortOrder: 0,
        showAdvertisements: true,
        hasVerifiedBadge: false,
        features: undefined,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      },
      {
        id: 2,
        name: 'Subscriber',
        description: 'No advertisements and enhanced experience. Support Yapplr while enjoying an ad-free experience.',
        price: 3.00,
        currency: 'USD',
        billingCycleMonths: 1,
        isActive: true,
        isDefault: false,
        sortOrder: 1,
        showAdvertisements: false,
        hasVerifiedBadge: false,
        features: '{"adFree": true, "prioritySupport": false}',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      },
      {
        id: 3,
        name: 'Verified Creator',
        description: 'No advertisements, verified badge, and creator tools. Perfect for content creators and influencers.',
        price: 6.00,
        currency: 'USD',
        billingCycleMonths: 1,
        isActive: true,
        isDefault: false,
        sortOrder: 2,
        showAdvertisements: false,
        hasVerifiedBadge: true,
        features: '{"adFree": true, "verifiedBadge": true, "prioritySupport": true, "creatorTools": true}',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      },
    ];
  },

  getMySubscription: async (): Promise<UserSubscription | null> => {
    // This would be replaced with actual API call
    return null;
  },

  assignSubscriptionTier: async (subscriptionTierId: number): Promise<void> => {
    // This would be replaced with actual API call
    console.log('Assigning subscription tier:', subscriptionTierId);
  },
};

export default function SubscriptionScreen() {
  const navigation = useNavigation();
  const [tiers, setTiers] = useState<SubscriptionTier[]>([]);
  const [currentSubscription, setCurrentSubscription] = useState<UserSubscription | null>(null);
  const [loading, setLoading] = useState(true);
  const [selecting, setSelecting] = useState<number | null>(null);

  useEffect(() => {
    fetchData();
  }, []);

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
      Alert.alert('Error', 'Failed to load subscription information');
    } finally {
      setLoading(false);
    }
  };

  const handleSelectTier = async (tierId: number) => {
    if (selecting || currentSubscription?.subscriptionTier?.id === tierId) {
      return;
    }

    try {
      setSelecting(tierId);
      await subscriptionApi.assignSubscriptionTier(tierId);
      await fetchData(); // Refresh data
      Alert.alert('Success', 'Subscription tier updated successfully!');
    } catch (error) {
      console.error('Failed to update subscription:', error);
      Alert.alert('Error', 'Failed to update subscription. Please try again.');
    } finally {
      setSelecting(null);
    }
  };

  const getTierIcon = (tier: SubscriptionTier) => {
    if (tier.hasVerifiedBadge) {
      return Crown;
    }
    if (tier.price === 0) {
      return Star;
    }
    return Zap;
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

  if (loading) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color="#3b82f6" />
          <Text style={styles.loadingText}>Loading subscription options...</Text>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      {/* Header */}
      <View style={styles.header}>
        <TouchableOpacity onPress={() => navigation.goBack()} style={styles.backButton}>
          <ArrowLeft size={24} color="#374151" />
        </TouchableOpacity>
        <Text style={styles.headerTitle}>Subscription</Text>
        <View style={styles.headerSpacer} />
      </View>

      <ScrollView style={styles.scrollView} showsVerticalScrollIndicator={false}>
        {/* Current Subscription */}
        {currentSubscription?.subscriptionTier && (
          <View style={styles.currentSubscriptionCard}>
            <Text style={styles.currentSubscriptionTitle}>Current Plan</Text>
            <View style={styles.currentSubscriptionInfo}>
              <SubscriptionTierBadge tier={currentSubscription.subscriptionTier} size="md" />
              <View style={styles.currentSubscriptionDetails}>
                <Text style={styles.currentSubscriptionName}>
                  {currentSubscription.subscriptionTier.name}
                </Text>
                <Text style={styles.currentSubscriptionPrice}>
                  {formatPrice(
                    currentSubscription.subscriptionTier.price,
                    currentSubscription.subscriptionTier.currency,
                    currentSubscription.subscriptionTier.billingCycleMonths
                  )}
                </Text>
              </View>
            </View>
          </View>
        )}

        {/* Title */}
        <View style={styles.titleContainer}>
          <Text style={styles.title}>Choose Your Plan</Text>
          <Text style={styles.subtitle}>
            Select the subscription tier that best fits your needs.
          </Text>
        </View>

        {/* Subscription Tiers */}
        <View style={styles.tiersContainer}>
          {tiers.map((tier) => {
            const Icon = getTierIcon(tier);
            const isCurrent = isCurrentTier(tier.id);
            const isSelecting = selecting === tier.id;

            return (
              <View
                key={tier.id}
                style={[
                  styles.tierCard,
                  isCurrent && styles.currentTierCard,
                  tier.isDefault && styles.popularTierCard,
                ]}
              >
                {/* Popular Badge */}
                {tier.isDefault && (
                  <View style={styles.popularBadge}>
                    <Text style={styles.popularBadgeText}>Most Popular</Text>
                  </View>
                )}

                <View style={styles.tierContent}>
                  {/* Icon and Name */}
                  <View style={styles.tierHeader}>
                    <Icon size={32} color={tier.hasVerifiedBadge ? '#eab308' : tier.price === 0 ? '#6b7280' : '#3b82f6'} />
                    <Text style={styles.tierName}>{tier.name}</Text>
                  </View>

                  {/* Price */}
                  <Text style={styles.tierPrice}>
                    {formatPrice(tier.price, tier.currency, tier.billingCycleMonths)}
                  </Text>

                  {/* Description */}
                  <Text style={styles.tierDescription}>{tier.description}</Text>

                  {/* Features */}
                  <View style={styles.featuresContainer}>
                    <View style={styles.feature}>
                      <Check size={16} color="#10b981" />
                      <Text style={styles.featureText}>
                        {tier.showAdvertisements ? 'Advertiser supported' : 'No advertisements'}
                      </Text>
                    </View>
                    {tier.hasVerifiedBadge && (
                      <View style={styles.feature}>
                        <Check size={16} color="#10b981" />
                        <Text style={styles.featureText}>Verified creator badge</Text>
                      </View>
                    )}
                    <View style={styles.feature}>
                      <Check size={16} color="#10b981" />
                      <Text style={styles.featureText}>Full platform access</Text>
                    </View>
                  </View>

                  {/* Action Button */}
                  <TouchableOpacity
                    style={[
                      styles.selectButton,
                      isCurrent && styles.currentButton,
                      isSelecting && styles.selectingButton,
                    ]}
                    onPress={() => handleSelectTier(tier.id)}
                    disabled={selecting !== null || isCurrent}
                  >
                    {isSelecting ? (
                      <ActivityIndicator size="small" color="#ffffff" />
                    ) : (
                      <Text style={[styles.selectButtonText, isCurrent && styles.currentButtonText]}>
                        {isCurrent ? 'Current Plan' : 'Select Plan'}
                      </Text>
                    )}
                  </TouchableOpacity>
                </View>
              </View>
            );
          })}
        </View>

        {/* Note */}
        <View style={styles.noteContainer}>
          <Text style={styles.noteText}>
            <Text style={styles.noteTextBold}>Note:</Text> Payment processing is not yet implemented. 
            Subscription tier selection is for demonstration purposes only.
          </Text>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f9fafb',
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
  },
  loadingText: {
    marginTop: 16,
    fontSize: 16,
    color: '#6b7280',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    backgroundColor: '#ffffff',
    borderBottomWidth: 1,
    borderBottomColor: '#e5e7eb',
  },
  backButton: {
    padding: 8,
  },
  headerTitle: {
    flex: 1,
    fontSize: 18,
    fontWeight: '600',
    color: '#111827',
    textAlign: 'center',
  },
  headerSpacer: {
    width: 40,
  },
  scrollView: {
    flex: 1,
  },
  currentSubscriptionCard: {
    margin: 16,
    padding: 16,
    backgroundColor: '#dbeafe',
    borderRadius: 12,
    borderWidth: 1,
    borderColor: '#93c5fd',
  },
  currentSubscriptionTitle: {
    fontSize: 14,
    fontWeight: '600',
    color: '#1e40af',
    marginBottom: 8,
  },
  currentSubscriptionInfo: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  currentSubscriptionDetails: {
    marginLeft: 12,
  },
  currentSubscriptionName: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1f2937',
  },
  currentSubscriptionPrice: {
    fontSize: 14,
    color: '#6b7280',
  },
  titleContainer: {
    paddingHorizontal: 16,
    paddingVertical: 8,
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#111827',
    marginBottom: 8,
  },
  subtitle: {
    fontSize: 16,
    color: '#6b7280',
  },
  tiersContainer: {
    paddingHorizontal: 16,
    paddingBottom: 16,
  },
  tierCard: {
    backgroundColor: '#ffffff',
    borderRadius: 16,
    marginBottom: 16,
    borderWidth: 2,
    borderColor: '#e5e7eb',
    overflow: 'hidden',
  },
  currentTierCard: {
    borderColor: '#3b82f6',
    backgroundColor: '#eff6ff',
  },
  popularTierCard: {
    borderColor: '#eab308',
  },
  popularBadge: {
    backgroundColor: '#eab308',
    paddingVertical: 8,
    alignItems: 'center',
  },
  popularBadgeText: {
    color: '#ffffff',
    fontSize: 12,
    fontWeight: '600',
  },
  tierContent: {
    padding: 20,
  },
  tierHeader: {
    alignItems: 'center',
    marginBottom: 16,
  },
  tierName: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#111827',
    marginTop: 8,
  },
  tierPrice: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#111827',
    textAlign: 'center',
    marginBottom: 16,
  },
  tierDescription: {
    fontSize: 14,
    color: '#6b7280',
    textAlign: 'center',
    marginBottom: 20,
    lineHeight: 20,
  },
  featuresContainer: {
    marginBottom: 24,
  },
  feature: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 8,
  },
  featureText: {
    marginLeft: 8,
    fontSize: 14,
    color: '#374151',
  },
  selectButton: {
    backgroundColor: '#3b82f6',
    paddingVertical: 12,
    borderRadius: 8,
    alignItems: 'center',
  },
  currentButton: {
    backgroundColor: '#10b981',
  },
  selectingButton: {
    backgroundColor: '#6b7280',
  },
  selectButtonText: {
    color: '#ffffff',
    fontSize: 16,
    fontWeight: '600',
  },
  currentButtonText: {
    color: '#ffffff',
  },
  noteContainer: {
    margin: 16,
    padding: 16,
    backgroundColor: '#dbeafe',
    borderRadius: 8,
  },
  noteText: {
    fontSize: 12,
    color: '#1e40af',
    textAlign: 'center',
    lineHeight: 16,
  },
  noteTextBold: {
    fontWeight: '600',
  },
});
