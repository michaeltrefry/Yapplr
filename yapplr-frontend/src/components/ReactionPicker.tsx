'use client';

import { useState, useRef, useEffect } from 'react';
import { Heart, ThumbsUp, Laugh, Frown, Angry, Meh } from 'lucide-react';
import { formatNumber } from '@/lib/utils';

export enum ReactionType {
  Heart = 1,
  ThumbsUp = 2,
  Laugh = 3,
  Surprised = 4,
  Sad = 5,
  Angry = 6
}

export interface ReactionCount {
  reactionType: ReactionType;
  emoji: string;
  displayName: string;
  count: number;
}

interface ReactionPickerProps {
  reactionCounts?: ReactionCount[];
  currentUserReaction?: ReactionType | null;
  totalReactionCount: number;
  onReact: (reactionType: ReactionType) => void;
  onRemoveReaction: () => void;
  disabled?: boolean;
}

const reactionConfig = {
  [ReactionType.Heart]: { emoji: '❤️', icon: Heart, color: 'text-red-500', hoverColor: 'hover:text-red-500' },
  [ReactionType.ThumbsUp]: { emoji: '👍', icon: ThumbsUp, color: 'text-blue-500', hoverColor: 'hover:text-blue-500' },
  [ReactionType.Laugh]: { emoji: '😂', icon: Laugh, color: 'text-yellow-500', hoverColor: 'hover:text-yellow-500' },
  [ReactionType.Surprised]: { emoji: '😮', icon: Meh, color: 'text-purple-500', hoverColor: 'hover:text-purple-500' },
  [ReactionType.Sad]: { emoji: '😢', icon: Frown, color: 'text-blue-400', hoverColor: 'hover:text-blue-400' },
  [ReactionType.Angry]: { emoji: '😡', icon: Angry, color: 'text-red-600', hoverColor: 'hover:text-red-600' }
};

export default function ReactionPicker({
  reactionCounts = [],
  currentUserReaction,
  totalReactionCount,
  onReact,
  onRemoveReaction,
  disabled = false
}: ReactionPickerProps) {
  const [showPicker, setShowPicker] = useState(false);
  const pickerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (pickerRef.current && !pickerRef.current.contains(event.target as Node)) {
        setShowPicker(false);
      }
    }

    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, []);

  const handleReactionClick = (reactionType: ReactionType) => {
    if (currentUserReaction === reactionType) {
      onRemoveReaction();
    } else {
      onReact(reactionType);
    }
    setShowPicker(false);
  };

  const handleMainButtonClick = () => {
    if (currentUserReaction) {
      onRemoveReaction();
    } else {
      setShowPicker(!showPicker);
    }
  };

  const currentReactionConfig = currentUserReaction ? reactionConfig[currentUserReaction] : null;
  const CurrentIcon = currentReactionConfig?.icon || Heart;

  return (
    <div className="relative" ref={pickerRef}>
      <button
        onClick={handleMainButtonClick}
        disabled={disabled}
        className={`flex items-center space-x-2 transition-colors group ${
          currentUserReaction
            ? currentReactionConfig?.color || 'text-red-500'
            : 'text-gray-500 hover:text-red-500'
        }`}
      >
        <div className="p-2 rounded-full hover:bg-gray-100">
          <CurrentIcon
            className={`w-5 h-5 ${currentUserReaction ? 'fill-current' : ''}`}
          />
        </div>
        <span className="text-sm">{formatNumber(totalReactionCount)}</span>
      </button>

      {showPicker && (
        <div className="absolute bottom-full left-0 mb-2 bg-white border border-gray-200 rounded-lg shadow-lg p-2 flex space-x-1 z-10">
          {Object.entries(reactionConfig).map(([type, config]) => {
            const reactionType = parseInt(type) as ReactionType;
            const count = reactionCounts?.find(r => r.reactionType === reactionType)?.count || 0;
            const Icon = config.icon;
            
            return (
              <button
                key={type}
                onClick={() => handleReactionClick(reactionType)}
                className={`p-2 rounded-full transition-colors ${config.hoverColor} hover:bg-gray-100 relative group`}
                title={`${config.emoji} ${count > 0 ? `(${count})` : ''}`}
              >
                <Icon className="w-5 h-5" />
                {count > 0 && (
                  <span className="absolute -top-1 -right-1 bg-gray-600 text-white text-xs rounded-full w-4 h-4 flex items-center justify-center">
                    {count > 9 ? '9+' : count}
                  </span>
                )}
              </button>
            );
          })}
        </div>
      )}

      {/* Reaction counts display */}
      {reactionCounts && reactionCounts.length > 0 && (
        <div className="flex space-x-1 mt-1">
          {reactionCounts
            .filter(r => r.count > 0)
            .slice(0, 3) // Show top 3 reactions
            .map(reaction => (
              <span
                key={reaction.reactionType}
                className="text-xs text-gray-500 flex items-center space-x-1"
              >
                <span>{reaction.emoji}</span>
                <span>{reaction.count}</span>
              </span>
            ))}
        </div>
      )}
    </div>
  );
}
