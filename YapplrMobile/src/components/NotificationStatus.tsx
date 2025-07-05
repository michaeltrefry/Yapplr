import React from 'react';
import { View, Text, TouchableOpacity, StyleSheet, Alert } from 'react-native';
import { useNotifications } from '../contexts/NotificationContext';
import { useThemeColors } from '../hooks/useThemeColors';

interface NotificationStatusProps {
  showDetails?: boolean;
}

export function NotificationStatus({ showDetails = false }: NotificationStatusProps) {
  const { signalRStatus, isSignalRReady, sendTestNotification, refreshSignalRConnection } = useNotifications();
  const colors = useThemeColors();

  const getStatusColor = () => {
    if (isSignalRReady) {
      return colors.success;
    }
    return colors.error;
  };

  const getStatusText = () => {
    if (isSignalRReady) {
      return 'Connected';
    }
    return 'Disconnected';
  };

  const getStatusDescription = () => {
    if (isSignalRReady) {
      return 'Real-time notifications active';
    }
    return 'Real-time notifications unavailable';
  };

  const handleTestNotification = async () => {
    try {
      await sendTestNotification();
      Alert.alert('Success', 'Test notification sent!');
    } catch (error) {
      Alert.alert('Error', 'Failed to send test notification');
    }
  };

  const handleRefreshConnection = async () => {
    try {
      await refreshSignalRConnection();
      Alert.alert('Success', 'Connection refreshed!');
    } catch (error) {
      Alert.alert('Error', 'Failed to refresh connection');
    }
  };

  const styles = StyleSheet.create({
    container: {
      backgroundColor: colors.surface,
      borderRadius: 12,
      padding: 16,
      borderWidth: 1,
      borderColor: colors.border,
    },
    header: {
      flexDirection: 'row',
      alignItems: 'center',
      marginBottom: showDetails ? 12 : 0,
    },
    statusIndicator: {
      width: 12,
      height: 12,
      borderRadius: 6,
      backgroundColor: getStatusColor(),
      marginRight: 8,
    },
    title: {
      fontSize: 16,
      fontWeight: '600',
      color: colors.text,
      flex: 1,
    },
    status: {
      fontSize: 14,
      color: getStatusColor(),
      fontWeight: '500',
    },
    description: {
      fontSize: 14,
      color: colors.textSecondary,
      marginBottom: showDetails ? 16 : 0,
    },
    detailsContainer: {
      marginTop: 8,
    },
    detailRow: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      marginBottom: 4,
    },
    detailLabel: {
      fontSize: 12,
      color: colors.textSecondary,
    },
    detailValue: {
      fontSize: 12,
      color: colors.text,
      fontWeight: '500',
    },
    buttonsContainer: {
      flexDirection: 'row',
      marginTop: 12,
      gap: 8,
    },
    button: {
      flex: 1,
      backgroundColor: colors.primary,
      paddingVertical: 8,
      paddingHorizontal: 12,
      borderRadius: 8,
      alignItems: 'center',
    },
    buttonSecondary: {
      backgroundColor: colors.surface,
      borderWidth: 1,
      borderColor: colors.border,
    },
    buttonText: {
      color: colors.background,
      fontSize: 12,
      fontWeight: '600',
    },
    buttonTextSecondary: {
      color: colors.text,
    },
  });

  if (!showDetails) {
    return (
      <View style={styles.header}>
        <View style={styles.statusIndicator} />
        <Text style={styles.title}>Notifications</Text>
        <Text style={styles.status}>{getStatusText()}</Text>
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <View style={styles.statusIndicator} />
        <Text style={styles.title}>Real-time Notifications</Text>
      </View>
      
      <Text style={styles.description}>
        {getStatusDescription()}
      </Text>

      <View style={styles.detailsContainer}>
        <View style={styles.detailRow}>
          <Text style={styles.detailLabel}>Status:</Text>
          <Text style={[styles.detailValue, { color: getStatusColor() }]}>
            {getStatusText()}
          </Text>
        </View>
        
        <View style={styles.detailRow}>
          <Text style={styles.detailLabel}>Connection State:</Text>
          <Text style={styles.detailValue}>
            {signalRStatus.connectionState}
          </Text>
        </View>
        
        {signalRStatus.lastConnected && (
          <View style={styles.detailRow}>
            <Text style={styles.detailLabel}>Last Connected:</Text>
            <Text style={styles.detailValue}>
              {signalRStatus.lastConnected.toLocaleTimeString()}
            </Text>
          </View>
        )}
        
        {signalRStatus.lastError && (
          <View style={styles.detailRow}>
            <Text style={styles.detailLabel}>Last Error:</Text>
            <Text style={[styles.detailValue, { color: colors.error }]}>
              {signalRStatus.lastError}
            </Text>
          </View>
        )}
      </View>

      <View style={styles.buttonsContainer}>
        <TouchableOpacity 
          style={[styles.button, styles.buttonSecondary]} 
          onPress={handleTestNotification}
          disabled={!isSignalRReady}
        >
          <Text style={[styles.buttonText, styles.buttonTextSecondary]}>
            Test
          </Text>
        </TouchableOpacity>
        
        <TouchableOpacity 
          style={styles.button} 
          onPress={handleRefreshConnection}
        >
          <Text style={styles.buttonText}>
            Refresh
          </Text>
        </TouchableOpacity>
      </View>
    </View>
  );
}

export function NotificationStatusIndicator() {
  const { isSignalRReady } = useNotifications();
  const colors = useThemeColors();

  const styles = StyleSheet.create({
    indicator: {
      position: 'absolute',
      top: 50,
      right: 16,
      width: 12,
      height: 12,
      borderRadius: 6,
      backgroundColor: isSignalRReady ? colors.success : colors.error,
      zIndex: 1000,
    },
  });

  return <View style={styles.indicator} />;
}
