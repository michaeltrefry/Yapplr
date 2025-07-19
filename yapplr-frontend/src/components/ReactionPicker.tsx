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
  [ReactionType.Heart]: { emoji: '❤️', icon: Heart, color: 'text-red-500', hoverColor: 'hover:text-red-500', bgHoverColor: 'hover:bg-red-100' },
  [ReactionType.ThumbsUp]: { emoji: '👍', icon: ThumbsUp, color: 'text-blue-500', hoverColor: 'hover:text-blue-500', bgHoverColor: 'hover:bg-blue-100' },
  [ReactionType.Laugh]: { emoji: '😂', icon: Laugh, color: 'text-yellow-500', hoverColor: 'hover:text-yellow-500', bgHoverColor: 'hover:bg-yellow-100' },
  [ReactionType.Surprised]: { emoji: '😮', icon: Meh, color: 'text-purple-500', hoverColor: 'hover:text-purple-500', bgHoverColor: 'hover:bg-purple-100' },
  [ReactionType.Sad]: { emoji: '😢', icon: Frown, color: 'text-blue-400', hoverColor: 'hover:text-blue-400', bgHoverColor: 'hover:bg-blue-100' },
  [ReactionType.Angry]: { emoji: '😡', icon: Angry, color: 'text-red-600', hoverColor: 'hover:text-red-600', bgHoverColor: 'hover:bg-red-100' }
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
            className={`w-5 h-5 ${currentUserReaction ? currentReactionConfig?.color || 'text-red-500' : ''}`}
          />
        </div>
        <span className="text-sm">{formatNumber(totalReactionCount)}</span>
      </button>

      {showPicker && (
        <div className="absolute bottom-full left-0 mb-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 rounded-lg shadow-lg p-2 flex space-x-1 z-10">
          {Object.entries(reactionConfig).map(([type, config]) => {
            const reactionType = parseInt(type) as ReactionType;
            const count = reactionCounts?.find(r => r.reactionType === reactionType)?.count || 0;
            const Icon = config.icon;

            return (
              <button
                key={type}
                onClick={() => handleReactionClick(reactionType)}
                className={`p-2 rounded-full transition-all duration-200 ${config.bgHoverColor} hover:scale-110 relative group`}
                title={`${config.emoji} ${count > 0 ? `(${count})` : ''}`}
              >
                {/* SVG Icon - hidden by default, colored and visible on hover */}
                <Icon className={`w-6 h-6 absolute inset-0 m-auto opacity-0 group-hover:opacity-100 transition-all duration-200 ${config.color}`} />

                {/* Emoji - visible by default, grayscale to color transition on hover */}
                <span className="text-2xl filter grayscale group-hover:grayscale-0 group-hover:opacity-0 transition-all duration-200">
                  {config.emoji}
                </span>

                {count > 0 && (
                  <span className="absolute -top-1 -right-1 bg-gray-600 text-white text-xs rounded-full w-4 h-4 flex items-center justify-center z-10">
                    {count > 9 ? '9+' : count}
                  </span>
                )}
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}
