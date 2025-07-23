import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { Crown, Star, Zap } from 'lucide-react-native';
import { SubscriptionTier } from '../types';

interface SubscriptionTierBadgeProps {
  tier: SubscriptionTier;
  size?: 'sm' | 'md' | 'lg';
  showName?: boolean;
  style?: any;
}

export default function SubscriptionTierBadge({ 
  tier, 
  size = 'md', 
  showName = false, 
  style 
}: SubscriptionTierBadgeProps) {
  const getTierIcon = () => {
    if (tier.hasVerifiedBadge) {
      return Crown;
    }
    if (tier.price === 0) {
      return Star;
    }
    return Zap;
  };

  const getTierColors = () => {
    if (tier.hasVerifiedBadge) {
      return {
        backgroundColor: '#fefce8', // yellow-50
        borderColor: '#fde047', // yellow-300
        iconColor: '#eab308', // yellow-500
        textColor: '#a16207', // yellow-700
      };
    }
    if (tier.price === 0) {
      return {
        backgroundColor: '#f9fafb', // gray-50
        borderColor: '#d1d5db', // gray-300
        iconColor: '#6b7280', // gray-500
        textColor: '#374151', // gray-700
      };
    }
    return {
      backgroundColor: '#eff6ff', // blue-50
      borderColor: '#93c5fd', // blue-300
      iconColor: '#3b82f6', // blue-500
      textColor: '#1d4ed8', // blue-700
    };
  };

  const getSizeStyles = () => {
    switch (size) {
      case 'sm':
        return {
          container: { paddingHorizontal: 6, paddingVertical: 2, borderRadius: 12 },
          icon: { width: 12, height: 12 },
          text: { fontSize: 10, fontWeight: '500' as const },
        };
      case 'lg':
        return {
          container: { paddingHorizontal: 12, paddingVertical: 6, borderRadius: 20 },
          icon: { width: 20, height: 20 },
          text: { fontSize: 16, fontWeight: '600' as const },
        };
      default:
        return {
          container: { paddingHorizontal: 8, paddingVertical: 4, borderRadius: 16 },
          icon: { width: 16, height: 16 },
          text: { fontSize: 12, fontWeight: '500' as const },
        };
    }
  };

  const Icon = getTierIcon();
  const colors = getTierColors();
  const sizeStyles = getSizeStyles();

  const containerStyle = [
    styles.container,
    sizeStyles.container,
    {
      backgroundColor: colors.backgroundColor,
      borderColor: colors.borderColor,
    },
    style,
  ];

  return (
    <View style={containerStyle}>
      <Icon 
        size={sizeStyles.icon.width} 
        color={colors.iconColor}
        style={styles.icon}
      />
      {showName && (
        <Text style={[sizeStyles.text, { color: colors.textColor, marginLeft: 4 }]}>
          {tier.name}
        </Text>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
  },
  icon: {
    // Icon styles handled by size
  },
});
