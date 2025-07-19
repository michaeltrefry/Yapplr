'use client';

import { ReactionCount } from './ReactionPicker';

interface ReactionCountsDisplayProps {
  reactionCounts?: ReactionCount[];
}

export default function ReactionCountsDisplay({ reactionCounts = [] }: ReactionCountsDisplayProps) {
  // Filter out reactions with 0 counts and show top 3
  const visibleReactions = reactionCounts
    .filter(r => r.count > 0)
    .sort((a, b) => b.count - a.count) // Sort by count descending
    .slice(0, 3);

  if (visibleReactions.length === 0) {
    return null;
  }

  return (
    <div className="flex items-center space-x-2 text-sm text-gray-600 mb-2">
      {visibleReactions.map(reaction => (
        <div
          key={reaction.reactionType}
          className="flex items-center space-x-1 bg-gray-100 dark:bg-gray-700 rounded-full px-2 py-1"
        >
          <span className="text-sm">{reaction.emoji}</span>
          <span className="text-xs font-medium">{reaction.count}</span>
        </div>
      ))}
    </div>
  );
}
