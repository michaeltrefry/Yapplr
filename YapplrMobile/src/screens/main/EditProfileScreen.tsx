import React, { useState } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  StyleSheet,
  SafeAreaView,
  ScrollView,
  Alert,
  ActivityIndicator,
  KeyboardAvoidingView,
  Platform,
  Image,
} from 'react-native';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Ionicons } from '@expo/vector-icons';
import { StackScreenProps } from '@react-navigation/stack';
import * as ImagePicker from 'expo-image-picker';
import { useAuth } from '../../contexts/AuthContext';
import { RootStackParamList } from '../../navigation/AppNavigator';

type EditProfileScreenProps = StackScreenProps<RootStackParamList, 'EditProfile'>;

export default function EditProfileScreen({ navigation }: EditProfileScreenProps) {
  const { user, updateUser, api } = useAuth();
  const queryClient = useQueryClient();

  const [formData, setFormData] = useState({
    bio: user?.bio || '',
    pronouns: user?.pronouns || '',
    tagline: user?.tagline || '',
    birthday: user?.birthday ? user.birthday.split('T')[0] : '', // Convert ISO to YYYY-MM-DD
  });

  const [profileImageUri, setProfileImageUri] = useState<string | null>(null);
  const [isUploadingImage, setIsUploadingImage] = useState(false);

  // Helper function to generate image URL
  const getImageUrl = (fileName: string) => {
    if (!fileName) return '';
    return `http://192.168.254.181:5161/api/images/${fileName}`;
  };

  const updateMutation = useMutation({
    mutationFn: (data: { bio?: string; pronouns?: string; tagline?: string; birthday?: string }) =>
      api.users.updateProfile(data),
    onSuccess: (updatedUser) => {
      // Update the auth context if updateUser function is available
      if (updateUser) {
        updateUser(updatedUser);
      }

      // Invalidate and refetch profile queries
      queryClient.invalidateQueries({ queryKey: ['userProfile'] });

      Alert.alert('Success', 'Profile updated successfully!', [
        { text: 'OK', onPress: () => navigation.goBack() }
      ]);
    },
    onError: (error) => {
      console.error('Failed to update profile:', error);
      Alert.alert('Error', 'Failed to update profile. Please try again.');
    },
  });

  const handleChange = (field: string, value: string) => {
    setFormData(prev => ({
      ...prev,
      [field]: value,
    }));
  };

  const handleSubmit = () => {
    // Prepare the data for submission, only including non-empty values
    const submitData: { bio?: string; pronouns?: string; tagline?: string; birthday?: string } = {};
    
    if (formData.bio.trim()) {
      submitData.bio = formData.bio.trim();
    }
    
    if (formData.pronouns.trim()) {
      submitData.pronouns = formData.pronouns.trim();
    }
    
    if (formData.tagline.trim()) {
      submitData.tagline = formData.tagline.trim();
    }
    
    if (formData.birthday.trim()) {
      // Convert to ISO format with UTC timezone
      const date = new Date(formData.birthday + 'T00:00:00.000Z');
      submitData.birthday = date.toISOString();
    }
    
    updateMutation.mutate(submitData);
  };

  const handleCancel = () => {
    navigation.goBack();
  };

  const pickImage = async () => {
    try {
      console.log('Starting image picker...');

      // Request permission
      const permissionResult = await ImagePicker.requestMediaLibraryPermissionsAsync();
      console.log('Permission result:', permissionResult);

      if (permissionResult.granted === false) {
        Alert.alert('Permission Required', 'Permission to access camera roll is required!');
        return;
      }

      // Launch image picker
      console.log('Launching image library...');
      const result = await ImagePicker.launchImageLibraryAsync({
        mediaTypes: ['images'],
        allowsEditing: true,
        aspect: [1, 1],
        quality: 0.8,
      });

      console.log('Image picker result:', result);

      if (!result.canceled && result.assets && result.assets[0]) {
        const asset = result.assets[0];
        console.log('Selected image asset:', asset);
        setProfileImageUri(asset.uri);
        await uploadProfileImage(asset.uri, asset.fileName || 'profile.jpg');
      } else {
        console.log('Image picker was canceled or no assets');
      }
    } catch (error) {
      console.error('Error picking image:', error);
      Alert.alert('Error', `Failed to pick image: ${(error as Error).message}`);
    }
  };

  const uploadProfileImage = async (uri: string, fileName: string) => {
    try {
      setIsUploadingImage(true);
      console.log('Starting profile image upload...', { uri, fileName });

      // Upload the profile image directly using the API client
      const updatedUser = await api.users.uploadProfileImage(uri, fileName, 'image/jpeg');
      console.log('Profile image upload successful:', updatedUser);

      if (updateUser) {
        updateUser(updatedUser);
      }

      // Invalidate profile queries
      queryClient.invalidateQueries({ queryKey: ['userProfile'] });

      Alert.alert('Success', 'Profile image updated successfully!');
    } catch (error) {
      console.error('Error uploading profile image:', error);
      console.error('Error details:', (error as any).response?.data || (error as Error).message);
      Alert.alert('Error', `Failed to upload profile image: ${(error as Error).message}`);
    } finally {
      setIsUploadingImage(false);
    }
  };

  if (!user) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.errorContainer}>
          <Text style={styles.errorText}>Please log in to edit your profile</Text>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      <KeyboardAvoidingView 
        style={styles.container} 
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
      >
        {/* Header */}
        <View style={styles.header}>
          <TouchableOpacity onPress={handleCancel} style={styles.headerButton}>
            <Ionicons name="arrow-back" size={24} color="#1F2937" />
          </TouchableOpacity>
          <Text style={styles.headerTitle}>Edit Profile</Text>
          <TouchableOpacity 
            onPress={handleSubmit} 
            style={[styles.headerButton, styles.saveButton]}
            disabled={updateMutation.isPending}
          >
            {updateMutation.isPending ? (
              <ActivityIndicator size="small" color="#3B82F6" />
            ) : (
              <Text style={styles.saveButtonText}>Save</Text>
            )}
          </TouchableOpacity>
        </View>

        <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
          {/* Profile Picture Section */}
          <View style={styles.profileSection}>
            <TouchableOpacity
              style={styles.avatarContainer}
              onPress={pickImage}
              disabled={isUploadingImage}
              activeOpacity={0.7}
            >
              <View style={styles.avatar}>
                {profileImageUri ? (
                  <Image source={{ uri: profileImageUri }} style={styles.profileImage} />
                ) : user.profileImageFileName ? (
                  <Image
                    source={{ uri: getImageUrl(user.profileImageFileName) }}
                    style={styles.profileImage}
                  />
                ) : (
                  <Text style={styles.avatarText}>
                    {user.username.charAt(0).toUpperCase()}
                  </Text>
                )}
              </View>

              {/* Camera overlay - moved outside avatar */}
              <View style={styles.cameraOverlay}>
                {isUploadingImage ? (
                  <ActivityIndicator size="small" color="#fff" />
                ) : (
                  <Ionicons name="camera" size={18} color="#fff" />
                )}
              </View>
            </TouchableOpacity>

            <Text style={styles.username}>@{user.username}</Text>
            <Text style={styles.email}>{user.email}</Text>
            <Text style={styles.changePhotoText}>Tap to change photo</Text>
          </View>

          {/* Form Fields */}
          <View style={styles.formSection}>
            <View style={styles.inputGroup}>
              <Text style={styles.label}>Bio</Text>
              <TextInput
                style={[styles.textInput, styles.textArea]}
                value={formData.bio}
                onChangeText={(value) => handleChange('bio', value)}
                placeholder="Tell us about yourself..."
                multiline
                numberOfLines={4}
                maxLength={500}
                textAlignVertical="top"
              />
              <Text style={styles.charCount}>{formData.bio.length}/500</Text>
            </View>

            <View style={styles.inputGroup}>
              <Text style={styles.label}>Pronouns</Text>
              <TextInput
                style={styles.textInput}
                value={formData.pronouns}
                onChangeText={(value) => handleChange('pronouns', value)}
                placeholder="e.g., they/them, she/her, he/him"
                maxLength={100}
              />
            </View>

            <View style={styles.inputGroup}>
              <Text style={styles.label}>Tagline</Text>
              <TextInput
                style={styles.textInput}
                value={formData.tagline}
                onChangeText={(value) => handleChange('tagline', value)}
                placeholder="A short tagline or motto"
                maxLength={200}
              />
            </View>

            <View style={styles.inputGroup}>
              <Text style={styles.label}>Birthday</Text>
              <TextInput
                style={styles.textInput}
                value={formData.birthday}
                onChangeText={(value) => handleChange('birthday', value)}
                placeholder="YYYY-MM-DD"
                maxLength={10}
              />
              <Text style={styles.helpText}>Format: YYYY-MM-DD (e.g., 1990-01-15)</Text>
            </View>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#fff',
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  headerButton: {
    padding: 8,
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: '#1F2937',
  },
  saveButton: {
    backgroundColor: '#3B82F6',
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 20,
  },
  saveButtonText: {
    color: '#fff',
    fontWeight: '600',
  },
  content: {
    flex: 1,
  },
  profileSection: {
    alignItems: 'center',
    paddingVertical: 24,
    paddingHorizontal: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  avatarContainer: {
    position: 'relative',
    marginBottom: 12,
    width: 90, // Slightly larger to accommodate camera icon
    height: 90,
    justifyContent: 'center',
    alignItems: 'center',
  },
  avatar: {
    width: 80,
    height: 80,
    borderRadius: 40,
    backgroundColor: '#3B82F6',
    justifyContent: 'center',
    alignItems: 'center',
    overflow: 'hidden',
  },
  profileImage: {
    width: 80,
    height: 80,
    borderRadius: 40,
  },
  cameraOverlay: {
    position: 'absolute',
    bottom: -5,
    right: -5,
    width: 32,
    height: 32,
    borderRadius: 16,
    backgroundColor: 'rgba(0, 0, 0, 0.8)',
    justifyContent: 'center',
    alignItems: 'center',
    borderWidth: 2,
    borderColor: '#fff',
    zIndex: 10,
    elevation: 5, // For Android shadow/elevation
    shadowColor: '#000',
    shadowOffset: {
      width: 0,
      height: 2,
    },
    shadowOpacity: 0.25,
    shadowRadius: 3.84,
  },
  avatarText: {
    color: '#fff',
    fontWeight: 'bold',
    fontSize: 32,
  },
  username: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#1F2937',
    marginBottom: 4,
  },
  email: {
    fontSize: 14,
    color: '#6B7280',
  },
  changePhotoText: {
    fontSize: 12,
    color: '#6B7280',
    marginTop: 4,
    fontStyle: 'italic',
  },
  formSection: {
    padding: 16,
  },
  inputGroup: {
    marginBottom: 24,
  },
  label: {
    fontSize: 16,
    fontWeight: '600',
    color: '#1F2937',
    marginBottom: 8,
  },
  textInput: {
    borderWidth: 1,
    borderColor: '#D1D5DB',
    borderRadius: 8,
    paddingHorizontal: 12,
    paddingVertical: 12,
    fontSize: 16,
    color: '#1F2937',
    backgroundColor: '#fff',
  },
  textArea: {
    height: 100,
    textAlignVertical: 'top',
  },
  charCount: {
    fontSize: 12,
    color: '#6B7280',
    textAlign: 'right',
    marginTop: 4,
  },
  helpText: {
    fontSize: 12,
    color: '#6B7280',
    marginTop: 4,
  },
  errorContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 16,
  },
  errorText: {
    fontSize: 16,
    color: '#EF4444',
    textAlign: 'center',
  },
});
