import React, { useState, useEffect } from 'react';
import { View, StyleSheet, Alert } from 'react-native';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import CreatePostModal from '../../components/CreatePostModal';
import { useAuth } from '../../contexts/AuthContext';
import { UserStatus } from '../../types';

export default function CreatePostScreen() {
  const navigation = useNavigation();
  const { user } = useAuth();
  const [showModal, setShowModal] = useState(false);

  // Check if user is suspended
  const isSuspended = user?.status === UserStatus.Suspended;
  const suspensionEndDate = user?.suspendedUntil ? new Date(user.suspendedUntil) : null;
  const suspensionReason = user?.suspensionReason;

  // Show modal when screen is focused, but check for suspension first
  useFocusEffect(
    React.useCallback(() => {
      if (isSuspended) {
        // Show suspension alert and navigate back
        let message = 'Your account has been suspended and you cannot create posts.';

        if (suspensionEndDate) {
          message += `\n\nSuspension ends: ${suspensionEndDate.toLocaleDateString()} at ${suspensionEndDate.toLocaleTimeString()}`;
        } else {
          message += '\n\nDuration: Indefinite';
        }

        if (suspensionReason) {
          message += `\n\nReason: ${suspensionReason}`;
        }

        Alert.alert(
          'Account Suspended',
          message,
          [
            {
              text: 'OK',
              onPress: () => navigation.navigate('Home' as never)
            }
          ]
        );
      } else {
        setShowModal(true);
      }

      return () => {
        setShowModal(false);
      };
    }, [isSuspended, suspensionEndDate, suspensionReason, navigation])
  );

  useEffect(() => {
    // When the modal is closed, navigate back to Home
    if (!showModal) {
      navigation.navigate('Home' as never);
    }
  }, [showModal, navigation]);

  const handleClose = () => {
    setShowModal(false);
  };

  return (
    <View style={styles.container}>
      <CreatePostModal
        visible={showModal}
        onClose={handleClose}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: 'transparent',
  },
});
