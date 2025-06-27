import React, { useState, useEffect } from 'react';
import { View, StyleSheet } from 'react-native';
import { useNavigation, useFocusEffect } from '@react-navigation/native';
import CreatePostModal from '../../components/CreatePostModal';

export default function CreatePostScreen() {
  const navigation = useNavigation();
  const [showModal, setShowModal] = useState(false);

  // Show modal when screen is focused
  useFocusEffect(
    React.useCallback(() => {
      setShowModal(true);
      return () => {
        setShowModal(false);
      };
    }, [])
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
