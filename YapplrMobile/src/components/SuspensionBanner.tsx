import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useThemeColors } from '../hooks/useThemeColors';
import { User } from '../types';

interface SuspensionBannerProps {
  user: User;
}

export default function SuspensionBanner({ user }: SuspensionBannerProps) {
  const colors = useThemeColors();
  const styles = createStyles(colors);

  const suspensionEndDate = user.suspendedUntil ? new Date(user.suspendedUntil) : null;
  const suspensionReason = user.suspensionReason;

  return (
    <View style={styles.container}>
      <View style={styles.content}>
        <View style={styles.iconContainer}>
          <Ionicons name="warning" size={20} color={colors.error} />
        </View>
        
        <View style={styles.textContainer}>
          <Text style={styles.title}>Account Suspended</Text>
          
          <Text style={styles.description}>
            You cannot create posts or interact with content.
          </Text>
          
          {suspensionEndDate && (
            <Text style={styles.detail}>
              <Text style={styles.label}>Ends: </Text>
              {suspensionEndDate.toLocaleDateString()} at {suspensionEndDate.toLocaleTimeString()}
            </Text>
          )}
          
          {!suspensionEndDate && (
            <Text style={styles.detail}>
              <Text style={styles.label}>Duration: </Text>
              Indefinite
            </Text>
          )}
          
          {suspensionReason && (
            <Text style={styles.detail}>
              <Text style={styles.label}>Reason: </Text>
              {suspensionReason}
            </Text>
          )}
        </View>
      </View>
    </View>
  );
}

const createStyles = (colors: any) => StyleSheet.create({
  container: {
    backgroundColor: colors.errorBackground || '#FEF2F2',
    borderLeftWidth: 4,
    borderLeftColor: colors.error,
    marginHorizontal: 16,
    marginTop: 12,
    marginBottom: 8,
    borderRadius: 8,
    shadowColor: colors.error,
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.1,
    shadowRadius: 2,
    elevation: 2,
  },
  content: {
    flexDirection: 'row',
    padding: 12,
    alignItems: 'flex-start',
  },
  iconContainer: {
    marginRight: 12,
    marginTop: 2,
  },
  textContainer: {
    flex: 1,
  },
  title: {
    fontSize: 16,
    fontWeight: '600',
    color: colors.error,
    marginBottom: 4,
  },
  description: {
    fontSize: 14,
    color: colors.text,
    marginBottom: 8,
    lineHeight: 18,
  },
  detail: {
    fontSize: 13,
    color: colors.text,
    marginBottom: 4,
    lineHeight: 16,
  },
  label: {
    fontWeight: '600',
  },
});
