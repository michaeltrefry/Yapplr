import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { useThemeColors } from '../hooks/useThemeColors';

export interface ReactionCount {
  reactionType: number;
  emoji: string;
  displayName: string;
  count: number;
}

interface ReactionCountsDisplayProps {
  reactionCounts?: ReactionCount[];
  overlap?: boolean; // Whether to overlap the content above
}

export default function ReactionCountsDisplay({ reactionCounts = [], overlap = false }: ReactionCountsDisplayProps) {
  const colors = useThemeColors();
  const styles = createStyles(colors, overlap);

  // Filter out reactions with 0 counts and show top 3
  const visibleReactions = reactionCounts
    .filter(r => r.count > 0)
    .sort((a, b) => b.count - a.count) // Sort by count descending
    .slice(0, 3);

  if (visibleReactions.length === 0) {
    return null;
  }

  return (
    <View style={styles.container}>
      {visibleReactions.map(reaction => (
        <View key={reaction.reactionType} style={styles.reactionItem}>
          <Text style={styles.emoji}>{reaction.emoji}</Text>
          <Text style={styles.count}>{reaction.count}</Text>
        </View>
      ))}
    </View>
  );
}

function createStyles(colors: any, overlap: boolean = false) {
  return StyleSheet.create({
    container: {
      flexDirection: 'row',
      marginTop: overlap ? -12 : 4, // Negative margin only when overlap is true
      marginBottom: 8,
      marginLeft: overlap ? 16 : 0, // Left margin only when overlapping
    },
    reactionItem: {
      flexDirection: 'row',
      alignItems: 'center',
      backgroundColor: colors.cardBackground || colors.background,
      borderRadius: 12,
      paddingHorizontal: 8,
      paddingVertical: 4,
      marginRight: 8,
      borderWidth: 1,
      borderColor: colors.border || '#E5E7EB',
    },
    emoji: {
      fontSize: 14,
      marginRight: 4,
    },
    count: {
      fontSize: 12,
      fontWeight: '600',
      color: colors.textSecondary || '#6B7280',
    },
  });
}
