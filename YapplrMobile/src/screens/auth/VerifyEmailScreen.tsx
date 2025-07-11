import React, { useState } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  Alert,
  ActivityIndicator,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
} from 'react-native';
import { useAuth } from '../../contexts/AuthContext';
import { StackNavigationProp } from '@react-navigation/stack';
import { AuthStackParamList } from '../../navigation/AppNavigator';

type VerifyEmailScreenNavigationProp = StackNavigationProp<AuthStackParamList, 'VerifyEmail'>;

interface Props {
  navigation: VerifyEmailScreenNavigationProp;
  route: {
    params?: {
      email?: string;
    };
  };
}

export default function VerifyEmailScreen({ navigation, route }: Props) {
  const [token, setToken] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const { api } = useAuth();
  const email = route.params?.email;

  const handleVerifyEmail = async () => {
    if (!token.trim()) {
      Alert.alert('Error', 'Please enter the verification code');
      return;
    }

    setIsLoading(true);
    try {
      await api.auth.verifyEmail(token);
      Alert.alert(
        'Success',
        'Email verified successfully! You can now log in.',
        [
          {
            text: 'OK',
            onPress: () => navigation.navigate('Login'),
          },
        ]
      );
    } catch (error: any) {
      Alert.alert('Error', error.response?.data?.message || 'Verification failed');
    } finally {
      setIsLoading(false);
    }
  };

  const handleResendVerification = async () => {
    if (!email) {
      Alert.alert('Error', 'Email address not available');
      return;
    }

    setIsLoading(true);
    try {
      await api.auth.resendVerification(email);
      Alert.alert('Success', 'Verification email sent! Please check your inbox.');
    } catch (error: any) {
      Alert.alert('Error', error.response?.data?.message || 'Failed to resend verification email');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <KeyboardAvoidingView
      style={styles.container}
      behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
    >
      <ScrollView contentContainerStyle={styles.scrollContainer}>
        <View style={styles.content}>
          <Text style={styles.title}>Verify Your Email</Text>
          <Text style={styles.subtitle}>
            Registration successful! Please check your email for a verification code and enter it below to complete your account setup.
          </Text>
          {email && (
            <Text style={styles.emailText}>
              Code sent to: {email}
            </Text>
          )}

          <View style={styles.form}>
            <TextInput
              style={styles.input}
              placeholder="Enter 6-digit code"
              value={token}
              onChangeText={setToken}
              keyboardType="number-pad"
              maxLength={6}
              autoCapitalize="none"
              autoCorrect={false}
            />

            <TouchableOpacity
              style={[styles.button, isLoading && styles.buttonDisabled]}
              onPress={handleVerifyEmail}
              disabled={isLoading}
            >
              {isLoading ? (
                <ActivityIndicator color="#fff" />
              ) : (
                <Text style={styles.buttonText}>Verify Email</Text>
              )}
            </TouchableOpacity>

            {email && (
              <TouchableOpacity
                style={styles.linkButton}
                onPress={handleResendVerification}
                disabled={isLoading}
              >
                <Text style={styles.linkText}>Didn't receive the code? Resend</Text>
              </TouchableOpacity>
            )}

            <TouchableOpacity
              style={styles.linkButton}
              onPress={() => navigation.navigate('Login')}
            >
              <Text style={styles.linkText}>Back to Login</Text>
            </TouchableOpacity>
          </View>
        </View>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
  },
  scrollContainer: {
    flexGrow: 1,
    justifyContent: 'center',
  },
  content: {
    flex: 1,
    justifyContent: 'center',
    paddingHorizontal: 20,
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    textAlign: 'center',
    marginBottom: 10,
    color: '#333',
  },
  subtitle: {
    fontSize: 16,
    textAlign: 'center',
    marginBottom: 15,
    color: '#666',
    lineHeight: 22,
  },
  emailText: {
    fontSize: 14,
    textAlign: 'center',
    marginBottom: 30,
    color: '#2563eb',
    fontWeight: '500',
  },
  form: {
    width: '100%',
  },
  input: {
    backgroundColor: '#fff',
    borderWidth: 1,
    borderColor: '#ddd',
    borderRadius: 8,
    paddingHorizontal: 15,
    paddingVertical: 12,
    fontSize: 16,
    marginBottom: 15,
    textAlign: 'center',
    letterSpacing: 2,
  },
  button: {
    backgroundColor: '#1d9bf0',
    borderRadius: 8,
    paddingVertical: 12,
    alignItems: 'center',
    marginBottom: 15,
  },
  buttonDisabled: {
    opacity: 0.6,
  },
  buttonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  linkButton: {
    alignItems: 'center',
    paddingVertical: 10,
  },
  linkText: {
    color: '#1d9bf0',
    fontSize: 14,
    fontWeight: '500',
  },
});
