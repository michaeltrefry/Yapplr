import React, { useState } from 'react';
import { View, Text, TouchableOpacity, Modal, StyleSheet } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useThemeColors } from '../hooks/useThemeColors';

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
  [ReactionType.Heart]: { emoji: '❤️', icon: 'heart', color: '#EF4444' },
  [ReactionType.ThumbsUp]: { emoji: '👍', icon: 'thumbs-up', color: '#3B82F6' },
  [ReactionType.Laugh]: { emoji: '😂', icon: 'happy', color: '#F59E0B' },
  [ReactionType.Surprised]: { emoji: '😮', icon: 'alert-circle', color: '#8B5CF6' },
  [ReactionType.Sad]: { emoji: '😢', icon: 'sad', color: '#60A5FA' },
  [ReactionType.Angry]: { emoji: '😡', icon: 'flame', color: '#DC2626' }
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
  const colors = useThemeColors();

  const styles = createStyles(colors);

  const handleReactionPress = (reactionType: ReactionType) => {
    if (currentUserReaction === reactionType) {
      onRemoveReaction();
    } else {
      onReact(reactionType);
    }
    setShowPicker(false);
  };

  const handleMainButtonPress = () => {
    if (currentUserReaction) {
      onRemoveReaction();
    } else {
      setShowPicker(true);
    }
  };

  const currentReactionConfig = currentUserReaction ? reactionConfig[currentUserReaction] : null;
  const iconName = currentUserReaction 
    ? (currentReactionConfig?.icon as any) || 'heart'
    : 'heart-outline';
  const iconColor = currentUserReaction 
    ? currentReactionConfig?.color || '#EF4444'
    : '#6B7280';

  return (
    <View>
      <TouchableOpacity
        style={styles.actionButton}
        onPress={handleMainButtonPress}
        disabled={disabled}
      >
        <Ionicons
          name={iconName}
          size={20}
          color={iconColor}
        />
        <Text style={styles.actionText}>{totalReactionCount}</Text>
      </TouchableOpacity>

      <Modal
        visible={showPicker}
        transparent={true}
        animationType="fade"
        onRequestClose={() => setShowPicker(false)}
      >
        <TouchableOpacity
          style={styles.modalOverlay}
          activeOpacity={1}
          onPress={() => setShowPicker(false)}
        >
          <View style={styles.pickerContainer}>
            {Object.entries(reactionConfig).map(([type, config]) => {
              const reactionType = parseInt(type) as ReactionType;
              const count = reactionCounts.find(r => r.reactionType === reactionType)?.count || 0;
              
              return (
                <TouchableOpacity
                  key={type}
                  style={styles.reactionButton}
                  onPress={() => handleReactionPress(reactionType)}
                >
                  <Text style={styles.reactionEmoji}>{config.emoji}</Text>
                  {count > 0 && (
                    <Text style={styles.reactionCount}>{count}</Text>
                  )}
                </TouchableOpacity>
              );
            })}
          </View>
        </TouchableOpacity>
      </Modal>

      {/* Reaction counts display */}
      {reactionCounts.length > 0 && (
        <View style={styles.reactionCountsContainer}>
          {reactionCounts
            .filter(r => r.count > 0)
            .slice(0, 3) // Show top 3 reactions
            .map(reaction => (
              <View key={reaction.reactionType} style={styles.reactionCountItem}>
                <Text style={styles.reactionCountEmoji}>{reaction.emoji}</Text>
                <Text style={styles.reactionCountText}>{reaction.count}</Text>
              </View>
            ))}
        </View>
      )}
    </View>
  );
}

function createStyles(colors: any) {
  return StyleSheet.create({
    actionButton: {
      flexDirection: 'row',
      alignItems: 'center',
      paddingHorizontal: 8,
      paddingVertical: 4,
    },
    actionText: {
      marginLeft: 4,
      fontSize: 14,
      color: colors.text,
    },
    modalOverlay: {
      flex: 1,
      backgroundColor: 'rgba(0, 0, 0, 0.5)',
      justifyContent: 'center',
      alignItems: 'center',
    },
    pickerContainer: {
      backgroundColor: colors.background,
      borderRadius: 25,
      flexDirection: 'row',
      paddingHorizontal: 16,
      paddingVertical: 12,
      shadowColor: '#000',
      shadowOffset: {
        width: 0,
        height: 2,
      },
      shadowOpacity: 0.25,
      shadowRadius: 3.84,
      elevation: 5,
    },
    reactionButton: {
      alignItems: 'center',
      justifyContent: 'center',
      paddingHorizontal: 12,
      paddingVertical: 8,
      marginHorizontal: 4,
      borderRadius: 20,
      position: 'relative',
    },
    reactionEmoji: {
      fontSize: 24,
    },
    reactionCount: {
      position: 'absolute',
      top: -4,
      right: -4,
      backgroundColor: colors.text,
      color: colors.background,
      fontSize: 10,
      paddingHorizontal: 4,
      paddingVertical: 1,
      borderRadius: 8,
      minWidth: 16,
      textAlign: 'center',
    },
    reactionCountsContainer: {
      flexDirection: 'row',
      marginTop: 4,
      marginLeft: 8,
    },
    reactionCountItem: {
      flexDirection: 'row',
      alignItems: 'center',
      marginRight: 8,
    },
    reactionCountEmoji: {
      fontSize: 12,
      marginRight: 2,
    },
    reactionCountText: {
      fontSize: 10,
      color: colors.textSecondary,
    },
  });
}
