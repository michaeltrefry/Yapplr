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
  [ReactionType.Heart]: { emoji: '❤️', color: '#EF4444' },
  [ReactionType.ThumbsUp]: { emoji: '👍', color: '#3B82F6' },
  [ReactionType.Laugh]: { emoji: '😂', color: '#F59E0B' },
  [ReactionType.Surprised]: { emoji: '😮', color: '#8B5CF6' },
  [ReactionType.Sad]: { emoji: '😢', color: '#60A5FA' },
  [ReactionType.Angry]: { emoji: '😡', color: '#DC2626' }
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

  return (
    <View>
      <TouchableOpacity
        style={styles.actionButton}
        onPress={handleMainButtonPress}
        disabled={disabled}
      >
        {currentUserReaction ? (
          <Text style={styles.selectedEmoji}>
            {currentReactionConfig?.emoji || '❤️'}
          </Text>
        ) : (
          <Ionicons
            name="heart-outline"
            size={20}
            color="#6B7280"
          />
        )}
        <Text style={[styles.actionText, currentUserReaction && { color: currentReactionConfig?.color }]}>
          {totalReactionCount}
        </Text>
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
    </View>
  );
}

function createStyles(colors: any) {
  return StyleSheet.create({
    actionButton: {
      flexDirection: 'row',
      alignItems: 'center',
      paddingHorizontal: 12,
      paddingVertical: 8,
    },
    actionText: {
      marginLeft: 4,
      fontSize: 14,
      color: colors.text,
    },
    selectedEmoji: {
      fontSize: 20,
      lineHeight: 20,
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
  });
}
