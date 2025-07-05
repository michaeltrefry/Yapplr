import React, { useState } from 'react';
import {
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  StyleSheet,
  Alert,
  RefreshControl,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { useNotifications } from '../../contexts/NotificationContext';
import { NotificationStatus } from '../../components/NotificationStatus';
import { useThemeColors } from '../../hooks/useThemeColors';

interface TestResult {
  name: string;
  status: 'pending' | 'success' | 'error';
  message: string;
  timestamp: Date;
}

export default function NotificationDebugScreen() {
  const { 
    signalRStatus, 
    isSignalRReady, 
    sendTestNotification, 
    refreshSignalRConnection 
  } = useNotifications();
  const colors = useThemeColors();
  const [testResults, setTestResults] = useState<TestResult[]>([]);
  const [isRunningTests, setIsRunningTests] = useState(false);
  const [refreshing, setRefreshing] = useState(false);

  const addTestResult = (result: Omit<TestResult, 'timestamp'>) => {
    setTestResults(prev => [...prev, { ...result, timestamp: new Date() }]);
  };

  const clearTestResults = () => {
    setTestResults([]);
  };

  const runComprehensiveTests = async () => {
    setIsRunningTests(true);
    clearTestResults();

    // Test 1: Check SignalR connection
    addTestResult({
      name: 'SignalR Connection',
      status: 'pending',
      message: 'Checking SignalR connection status...'
    });

    if (isSignalRReady) {
      addTestResult({
        name: 'SignalR Connection',
        status: 'success',
        message: 'SignalR is connected and ready'
      });
    } else {
      addTestResult({
        name: 'SignalR Connection',
        status: 'error',
        message: `SignalR is not connected. State: ${signalRStatus.connectionState}`
      });
    }

    // Test 2: Send test notification
    addTestResult({
      name: 'Test Notification',
      status: 'pending',
      message: 'Sending test notification...'
    });

    try {
      await sendTestNotification();
      addTestResult({
        name: 'Test Notification',
        status: 'success',
        message: 'Test notification sent successfully'
      });
    } catch (error) {
      addTestResult({
        name: 'Test Notification',
        status: 'error',
        message: `Test notification failed: ${error instanceof Error ? error.message : 'Unknown error'}`
      });
    }

    // Test 3: Connection refresh
    addTestResult({
      name: 'Connection Refresh',
      status: 'pending',
      message: 'Testing connection refresh...'
    });

    try {
      await refreshSignalRConnection();
      addTestResult({
        name: 'Connection Refresh',
        status: 'success',
        message: 'Connection refreshed successfully'
      });
    } catch (error) {
      addTestResult({
        name: 'Connection Refresh',
        status: 'error',
        message: `Connection refresh failed: ${error instanceof Error ? error.message : 'Unknown error'}`
      });
    }

    setIsRunningTests(false);
  };

  const onRefresh = async () => {
    setRefreshing(true);
    try {
      await refreshSignalRConnection();
    } catch (error) {
      console.error('Refresh failed:', error);
    }
    setRefreshing(false);
  };

  const getStatusIcon = (status: TestResult['status']) => {
    switch (status) {
      case 'success':
        return '✅';
      case 'error':
        return '❌';
      case 'pending':
        return '⏳';
      default:
        return '⚪';
    }
  };

  const getStatusColor = (status: TestResult['status']) => {
    switch (status) {
      case 'success':
        return colors.success;
      case 'error':
        return colors.error;
      case 'pending':
        return colors.primary;
      default:
        return colors.textSecondary;
    }
  };

  const styles = StyleSheet.create({
    container: {
      flex: 1,
      backgroundColor: colors.background,
    },
    scrollContainer: {
      padding: 16,
    },
    header: {
      fontSize: 24,
      fontWeight: 'bold',
      color: colors.text,
      marginBottom: 20,
      textAlign: 'center',
    },
    section: {
      marginBottom: 24,
    },
    sectionTitle: {
      fontSize: 18,
      fontWeight: '600',
      color: colors.text,
      marginBottom: 12,
    },
    buttonContainer: {
      flexDirection: 'row',
      gap: 12,
      marginBottom: 16,
    },
    button: {
      flex: 1,
      backgroundColor: colors.primary,
      paddingVertical: 12,
      paddingHorizontal: 16,
      borderRadius: 8,
      alignItems: 'center',
    },
    buttonSecondary: {
      backgroundColor: colors.surface,
      borderWidth: 1,
      borderColor: colors.border,
    },
    buttonDisabled: {
      opacity: 0.5,
    },
    buttonText: {
      color: colors.background,
      fontSize: 14,
      fontWeight: '600',
    },
    buttonTextSecondary: {
      color: colors.text,
    },
    testResultsContainer: {
      gap: 8,
    },
    testResult: {
      backgroundColor: colors.surface,
      padding: 12,
      borderRadius: 8,
      borderWidth: 1,
      borderColor: colors.border,
    },
    testResultHeader: {
      flexDirection: 'row',
      alignItems: 'center',
      marginBottom: 4,
    },
    testResultIcon: {
      fontSize: 16,
      marginRight: 8,
    },
    testResultName: {
      fontSize: 14,
      fontWeight: '600',
      color: colors.text,
      flex: 1,
    },
    testResultMessage: {
      fontSize: 12,
      color: colors.textSecondary,
      marginBottom: 4,
    },
    testResultTime: {
      fontSize: 10,
      color: colors.textSecondary,
    },
    connectionDetails: {
      backgroundColor: colors.surface,
      padding: 16,
      borderRadius: 8,
      borderWidth: 1,
      borderColor: colors.border,
    },
    detailRow: {
      flexDirection: 'row',
      justifyContent: 'space-between',
      marginBottom: 8,
    },
    detailLabel: {
      fontSize: 14,
      color: colors.textSecondary,
    },
    detailValue: {
      fontSize: 14,
      color: colors.text,
      fontWeight: '500',
    },
    instructions: {
      backgroundColor: colors.surface,
      padding: 16,
      borderRadius: 8,
      borderWidth: 1,
      borderColor: colors.border,
    },
    instructionTitle: {
      fontSize: 16,
      fontWeight: '600',
      color: colors.text,
      marginBottom: 8,
    },
    instructionText: {
      fontSize: 14,
      color: colors.textSecondary,
      lineHeight: 20,
    },
  });

  return (
    <SafeAreaView style={styles.container}>
      <ScrollView 
        style={styles.scrollContainer}
        refreshControl={
          <RefreshControl refreshing={refreshing} onRefresh={onRefresh} />
        }
      >
        <Text style={styles.header}>Notification Debug Center</Text>

        {/* Current Status */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Current Status</Text>
          <NotificationStatus showDetails={true} />
        </View>

        {/* Connection Details */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Connection Details</Text>
          <View style={styles.connectionDetails}>
            <View style={styles.detailRow}>
              <Text style={styles.detailLabel}>SignalR Status:</Text>
              <Text style={[styles.detailValue, { color: isSignalRReady ? colors.success : colors.error }]}>
                {isSignalRReady ? 'Connected' : 'Disconnected'}
              </Text>
            </View>
            <View style={styles.detailRow}>
              <Text style={styles.detailLabel}>Connection State:</Text>
              <Text style={styles.detailValue}>{signalRStatus.connectionState}</Text>
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
        </View>

        {/* Test Controls */}
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Tests</Text>
          <View style={styles.buttonContainer}>
            <TouchableOpacity
              style={[styles.button, isRunningTests && styles.buttonDisabled]}
              onPress={runComprehensiveTests}
              disabled={isRunningTests}
            >
              <Text style={styles.buttonText}>
                {isRunningTests ? 'Running...' : 'Run All Tests'}
              </Text>
            </TouchableOpacity>
            <TouchableOpacity
              style={[styles.button, styles.buttonSecondary]}
              onPress={clearTestResults}
            >
              <Text style={[styles.buttonText, styles.buttonTextSecondary]}>
                Clear Results
              </Text>
            </TouchableOpacity>
          </View>
        </View>

        {/* Test Results */}
        {testResults.length > 0 && (
          <View style={styles.section}>
            <Text style={styles.sectionTitle}>Test Results</Text>
            <View style={styles.testResultsContainer}>
              {testResults.map((result, index) => (
                <View key={index} style={styles.testResult}>
                  <View style={styles.testResultHeader}>
                    <Text style={styles.testResultIcon}>
                      {getStatusIcon(result.status)}
                    </Text>
                    <Text style={styles.testResultName}>{result.name}</Text>
                  </View>
                  <Text style={styles.testResultMessage}>{result.message}</Text>
                  <Text style={styles.testResultTime}>
                    {result.timestamp.toLocaleTimeString()}
                  </Text>
                </View>
              ))}
            </View>
          </View>
        )}

        {/* Instructions */}
        <View style={styles.section}>
          <View style={styles.instructions}>
            <Text style={styles.instructionTitle}>Testing Instructions</Text>
            <Text style={styles.instructionText}>
              • Pull down to refresh the connection{'\n'}
              • Run "All Tests" to perform comprehensive testing{'\n'}
              • Check the connection details for troubleshooting{'\n'}
              • Monitor the status indicator for real-time updates{'\n'}
              • Test notifications will appear as system notifications
            </Text>
          </View>
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}
