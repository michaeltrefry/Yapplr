import React from 'react';
import {
  View,
  Text,
  StyleSheet,
  TouchableOpacity,
  ScrollView,
  Alert,
} from 'react-native';
import { useNotifications } from '../contexts/NotificationContext';
import { useAuth } from '../contexts/AuthContext';

const NotificationTestScreen: React.FC = () => {
  const { api } = useAuth();
  const {
    signalRStatus,
    isSignalRReady,
    isExpoNotificationReady,
    expoPushToken,
    sendTestNotification,
    sendTestExpoNotification,
    refreshSignalRConnection,
    refreshExpoNotifications,
  } = useNotifications();

  const handleSendTestSignalR = async () => {
    try {
      await sendTestNotification();
      Alert.alert('Success', 'SignalR test notification sent!');
    } catch (error) {
      Alert.alert('Error', 'Failed to send SignalR test notification');
      console.error(error);
    }
  };

  const handleSendTestExpo = async () => {
    try {
      await sendTestExpoNotification();
      Alert.alert('Success', 'Expo test notification sent!');
    } catch (error) {
      Alert.alert('Error', 'Failed to send Expo test notification');
      console.error(error);
    }
  };

  const handleRefreshSignalR = async () => {
    try {
      await refreshSignalRConnection();
      Alert.alert('Success', 'SignalR connection refreshed!');
    } catch (error) {
      Alert.alert('Error', 'Failed to refresh SignalR connection');
      console.error(error);
    }
  };

  const handleRefreshExpo = async () => {
    try {
      await refreshExpoNotifications();
      Alert.alert('Success', 'Expo notifications refreshed!');
    } catch (error) {
      Alert.alert('Error', 'Failed to refresh Expo notifications');
      console.error(error);
    }
  };

  const copyTokenToClipboard = () => {
    if (expoPushToken) {
      // Note: In a real app, you might want to use @react-native-clipboard/clipboard
      console.log('📱🔔 Expo Push Token:', expoPushToken);
      Alert.alert(
        'Token Copied',
        'Expo push token has been logged to console. You can use it with the Expo push notification tool at https://expo.dev/notifications'
      );
    }
  };

  const handleRegisterToken = async () => {
    if (!expoPushToken) {
      Alert.alert('Error', 'No Expo push token available');
      return;
    }

    try {
      await api.users.updateExpoPushToken({ token: expoPushToken });
      Alert.alert('Success', 'Expo push token registered with backend!');
    } catch (error) {
      Alert.alert('Error', 'Failed to register Expo push token with backend');
      console.error(error);
    }
  };

  const handleTestBackendNotification = async () => {
    try {
      // Call the backend test endpoint
      const response = await fetch(`${api.baseURL}/api/notifications/test-expo`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${api.getToken()}`,
          'Content-Type': 'application/json',
        },
      });

      if (response.ok) {
        const result = await response.json();
        Alert.alert('Success', `Backend test notification sent!\n\nToken: ${result.expoPushTokenStart || 'N/A'}`);
      } else {
        const errorData = await response.json();
        Alert.alert('Error', errorData.error || 'Failed to send backend test notification');
      }
    } catch (error) {
      Alert.alert('Error', 'Failed to send backend test notification');
      console.error(error);
    }
  };



  return (
    <ScrollView style={styles.container}>
      <Text style={styles.title}>Notification Test Screen</Text>
      
      {/* SignalR Status */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>SignalR Status</Text>
        <View style={styles.statusRow}>
          <Text style={styles.label}>Connected:</Text>
          <Text style={[styles.status, { color: isSignalRReady ? '#4CAF50' : '#F44336' }]}>
            {isSignalRReady ? 'Yes' : 'No'}
          </Text>
        </View>
        <View style={styles.statusRow}>
          <Text style={styles.label}>Connection State:</Text>
          <Text style={styles.value}>{signalRStatus.connectionState}</Text>
        </View>
        
        <View style={styles.buttonRow}>
          <TouchableOpacity
            style={[styles.button, styles.testButton]}
            onPress={handleSendTestSignalR}
            disabled={!isSignalRReady}
          >
            <Text style={styles.buttonText}>Test SignalR</Text>
          </TouchableOpacity>
          
          <TouchableOpacity
            style={[styles.button, styles.refreshButton]}
            onPress={handleRefreshSignalR}
          >
            <Text style={styles.buttonText}>Refresh SignalR</Text>
          </TouchableOpacity>
        </View>
      </View>

      {/* Expo Notifications Status */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Expo Notifications Status</Text>
        <View style={styles.statusRow}>
          <Text style={styles.label}>Ready:</Text>
          <Text style={[styles.status, { color: isExpoNotificationReady ? '#4CAF50' : '#F44336' }]}>
            {isExpoNotificationReady ? 'Yes' : 'No'}
          </Text>
        </View>
        
        {expoPushToken && (
          <View style={styles.tokenContainer}>
            <Text style={styles.label}>Push Token:</Text>
            <TouchableOpacity onPress={copyTokenToClipboard} style={styles.tokenButton}>
              <Text style={styles.tokenText} numberOfLines={3}>
                {expoPushToken}
              </Text>
              <Text style={styles.tokenHint}>Tap to copy</Text>
            </TouchableOpacity>
          </View>
        )}
        
        <View style={styles.buttonRow}>
          <TouchableOpacity
            style={[styles.button, styles.testButton]}
            onPress={handleSendTestExpo}
            disabled={!isExpoNotificationReady}
          >
            <Text style={styles.buttonText}>Test Expo</Text>
          </TouchableOpacity>

          <TouchableOpacity
            style={[styles.button, styles.refreshButton]}
            onPress={handleRefreshExpo}
          >
            <Text style={styles.buttonText}>Refresh Expo</Text>
          </TouchableOpacity>
        </View>

        <View style={styles.buttonRow}>
          <TouchableOpacity
            style={[styles.button, styles.registerButton]}
            onPress={handleRegisterToken}
            disabled={!expoPushToken}
          >
            <Text style={styles.buttonText}>Register Token</Text>
          </TouchableOpacity>

          <TouchableOpacity
            style={[styles.button, styles.backendTestButton]}
            onPress={handleTestBackendNotification}
            disabled={!expoPushToken}
          >
            <Text style={styles.buttonText}>Test Backend</Text>
          </TouchableOpacity>
        </View>
      </View>

      {/* Instructions */}
      <View style={styles.section}>
        <Text style={styles.sectionTitle}>Instructions</Text>
        <Text style={styles.instruction}>
          1. Make sure you're running on a physical device (notifications don't work in simulators)
        </Text>
        <Text style={styles.instruction}>
          2. Grant notification permissions when prompted
        </Text>
        <Text style={styles.instruction}>
          3. Use the "Test Expo" button to send a test notification
        </Text>
        <Text style={styles.instruction}>
          4. Copy the push token and use it with the Expo push notification tool at:
        </Text>
        <Text style={styles.link}>https://expo.dev/notifications</Text>
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
    padding: 16,
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    textAlign: 'center',
    marginBottom: 24,
    color: '#333',
  },
  section: {
    backgroundColor: '#fff',
    borderRadius: 8,
    padding: 16,
    marginBottom: 16,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    marginBottom: 12,
    color: '#333',
  },
  statusRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
  },
  label: {
    fontSize: 16,
    color: '#666',
    fontWeight: '500',
  },
  status: {
    fontSize: 16,
    fontWeight: 'bold',
  },
  value: {
    fontSize: 16,
    color: '#333',
  },
  buttonRow: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    marginTop: 16,
  },
  button: {
    paddingHorizontal: 20,
    paddingVertical: 12,
    borderRadius: 6,
    minWidth: 120,
    alignItems: 'center',
  },
  testButton: {
    backgroundColor: '#2196F3',
  },
  refreshButton: {
    backgroundColor: '#FF9800',
  },
  registerButton: {
    backgroundColor: '#4CAF50',
  },
  backendTestButton: {
    backgroundColor: '#9C27B0',
  },
  buttonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  tokenContainer: {
    marginVertical: 12,
  },
  tokenButton: {
    backgroundColor: '#f0f0f0',
    padding: 12,
    borderRadius: 6,
    marginTop: 8,
  },
  tokenText: {
    fontSize: 12,
    color: '#333',
    fontFamily: 'monospace',
  },
  tokenHint: {
    fontSize: 12,
    color: '#666',
    fontStyle: 'italic',
    marginTop: 4,
  },
  instruction: {
    fontSize: 14,
    color: '#666',
    marginBottom: 8,
    lineHeight: 20,
  },
  link: {
    fontSize: 14,
    color: '#2196F3',
    textDecorationLine: 'underline',
  },
});

export default NotificationTestScreen;
