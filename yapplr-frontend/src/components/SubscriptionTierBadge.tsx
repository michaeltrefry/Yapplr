import { SubscriptionTier } from '@/types';
import { Crown, Star, Zap } from 'lucide-react';

interface SubscriptionTierBadgeProps {
  tier: SubscriptionTier;
  size?: 'sm' | 'md' | 'lg';
  showName?: boolean;
  className?: string;
}

export default function SubscriptionTierBadge({ 
  tier, 
  size = 'md', 
  showName = false, 
  className = '' 
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

  const getTierColor = () => {
    if (tier.hasVerifiedBadge) {
      return 'text-yellow-500 bg-yellow-50 border-yellow-200';
    }
    if (tier.price === 0) {
      return 'text-gray-500 bg-gray-50 border-gray-200';
    }
    return 'text-blue-500 bg-blue-50 border-blue-200';
  };

  const getSizeClasses = () => {
    switch (size) {
      case 'sm':
        return 'w-3 h-3 text-xs px-1.5 py-0.5';
      case 'lg':
        return 'w-6 h-6 text-base px-3 py-1.5';
      default:
        return 'w-4 h-4 text-sm px-2 py-1';
    }
  };

  const Icon = getTierIcon();
  const colorClasses = getTierColor();
  const sizeClasses = getSizeClasses();

  if (!showName) {
    // Icon only badge
    return (
      <div 
        className={`inline-flex items-center justify-center rounded-full border ${colorClasses} ${sizeClasses} ${className}`}
        title={`${tier.name} - ${tier.description}`}
      >
        <Icon className={size === 'sm' ? 'w-2.5 h-2.5' : size === 'lg' ? 'w-4 h-4' : 'w-3 h-3'} />
      </div>
    );
  }

  // Badge with name
  return (
    <div 
      className={`inline-flex items-center rounded-full border ${colorClasses} ${sizeClasses} ${className}`}
      title={tier.description}
    >
      <Icon className={size === 'sm' ? 'w-2.5 h-2.5' : size === 'lg' ? 'w-4 h-4' : 'w-3 h-3'} />
      {showName && (
        <span className="ml-1 font-medium">
          {tier.name}
        </span>
      )}
    </div>
  );
}
