import React, { useState } from 'react';
import {
  View,
  Text,
  StyleSheet,
  TextInput,
  TouchableOpacity,
  Alert,
  ScrollView,
  KeyboardAvoidingView,
  Platform,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Ionicons } from '@expo/vector-icons';
import { useThemeColors } from '../../hooks/useThemeColors';
import { CreateGroup } from '../../types';
import { useAuth } from '../../contexts/AuthContext';

interface CreateGroupScreenProps {
  navigation: any;
}

export default function CreateGroupScreen({ navigation }: CreateGroupScreenProps) {
  const colors = useThemeColors();
  const { api } = useAuth();
  const [formData, setFormData] = useState<CreateGroup>({
    name: '',
    description: '',
    imageFileName: undefined,
  });
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleInputChange = (field: keyof CreateGroup, value: string) => {
    setFormData(prev => ({
      ...prev,
      [field]: value,
    }));
  };

  const handleSubmit = async () => {
    if (!formData.name.trim()) {
      Alert.alert('Error', 'Group name is required.');
      return;
    }

    if (formData.name.length < 3) {
      Alert.alert('Error', 'Group name must be at least 3 characters long.');
      return;
    }

    setIsSubmitting(true);
    try {
      const newGroup = await api.groups.createGroup(formData);
      Alert.alert(
        'Success',
        'Group created successfully!',
        [
          {
            text: 'OK',
            onPress: () => {
              navigation.navigate('GroupDetail', { groupId: newGroup.id });
            },
          },
        ]
      );
    } catch (error: any) {
      console.error('Failed to create group:', error);
      Alert.alert(
        'Error',
        error.response?.data?.error || 'Failed to create group. Please try again.'
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  const isFormValid = formData.name.trim().length >= 3;

  return (
    <SafeAreaView style={[styles.container, { backgroundColor: colors.background }]}>
      <KeyboardAvoidingView
        style={styles.keyboardAvoid}
        behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
      >
        {/* Header */}
        <View style={[styles.header, { backgroundColor: colors.surface }]}>
          <TouchableOpacity
            style={styles.cancelButton}
            onPress={() => navigation.goBack()}
          >
            <Text style={[styles.cancelButtonText, { color: colors.onSurfaceVariant }]}>
              Cancel
            </Text>
          </TouchableOpacity>
          <Text style={[styles.headerTitle, { color: colors.onSurface }]}>
            Create Group
          </Text>
          <TouchableOpacity
            style={[
              styles.createButton,
              {
                backgroundColor: isFormValid ? colors.primary : colors.surfaceVariant,
              },
            ]}
            onPress={handleSubmit}
            disabled={!isFormValid || isSubmitting}
          >
            <Text
              style={[
                styles.createButtonText,
                {
                  color: isFormValid ? colors.onPrimary : colors.onSurfaceVariant,
                },
              ]}
            >
              {isSubmitting ? 'Creating...' : 'Create'}
            </Text>
          </TouchableOpacity>
        </View>

        <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
          {/* Group Avatar Placeholder */}
          <View style={styles.avatarSection}>
            <View style={[styles.avatarPlaceholder, { backgroundColor: colors.surfaceVariant }]}>
              <Ionicons name="camera-outline" size={32} color={colors.onSurfaceVariant} />
            </View>
            <TouchableOpacity style={styles.changeAvatarButton}>
              <Text style={[styles.changeAvatarText, { color: colors.primary }]}>
                Add Group Photo
              </Text>
            </TouchableOpacity>
          </View>

          {/* Group Name */}
          <View style={styles.inputSection}>
            <Text style={[styles.inputLabel, { color: colors.onSurface }]}>
              Group Name *
            </Text>
            <TextInput
              style={[
                styles.textInput,
                {
                  backgroundColor: colors.surface,
                  color: colors.onSurface,
                  borderColor: colors.outline,
                },
              ]}
              placeholder="Enter group name"
              placeholderTextColor={colors.onSurfaceVariant}
              value={formData.name}
              onChangeText={(text) => handleInputChange('name', text)}
              maxLength={100}
              autoCapitalize="words"
            />
            <View style={styles.inputMeta}>
              <Text style={[styles.inputHint, { color: colors.onSurfaceVariant }]}>
                Choose a name that describes your group
              </Text>
              <Text style={[styles.characterCount, { color: colors.onSurfaceVariant }]}>
                {formData.name.length}/100
              </Text>
            </View>
          </View>

          {/* Group Description */}
          <View style={styles.inputSection}>
            <Text style={[styles.inputLabel, { color: colors.onSurface }]}>
              Description (Optional)
            </Text>
            <TextInput
              style={[
                styles.textInput,
                styles.textArea,
                {
                  backgroundColor: colors.surface,
                  color: colors.onSurface,
                  borderColor: colors.outline,
                },
              ]}
              placeholder="Describe your group..."
              placeholderTextColor={colors.onSurfaceVariant}
              value={formData.description}
              onChangeText={(text) => handleInputChange('description', text)}
              maxLength={500}
              multiline
              numberOfLines={4}
              textAlignVertical="top"
            />
            <View style={styles.inputMeta}>
              <Text style={[styles.inputHint, { color: colors.onSurfaceVariant }]}>
                Help people understand what your group is about
              </Text>
              <Text style={[styles.characterCount, { color: colors.onSurfaceVariant }]}>
                {formData.description.length}/500
              </Text>
            </View>
          </View>

          {/* Group Settings Info */}
          <View style={[styles.infoSection, { backgroundColor: colors.surfaceVariant }]}>
            <View style={styles.infoHeader}>
              <Ionicons name="information-circle-outline" size={20} color={colors.primary} />
              <Text style={[styles.infoTitle, { color: colors.onSurfaceVariant }]}>
                Group Settings
              </Text>
            </View>
            <Text style={[styles.infoText, { color: colors.onSurfaceVariant }]}>
              • Your group will be public and open to everyone{'\n'}
              • Anyone can join without approval{'\n'}
              • You'll be the group admin and can manage settings later
            </Text>
          </View>

          {/* Guidelines */}
          <View style={[styles.guidelinesSection, { backgroundColor: colors.surface }]}>
            <Text style={[styles.guidelinesTitle, { color: colors.onSurface }]}>
              Community Guidelines
            </Text>
            <Text style={[styles.guidelinesText, { color: colors.onSurfaceVariant }]}>
              By creating a group, you agree to follow our community guidelines and ensure your group provides a safe and welcoming environment for all members.
            </Text>
          </View>
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
  },
  keyboardAvoid: {
    flex: 1,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 16,
    borderBottomWidth: 1,
    borderBottomColor: 'rgba(0,0,0,0.1)',
  },
  cancelButton: {
    paddingVertical: 8,
  },
  cancelButtonText: {
    fontSize: 16,
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: '600',
  },
  createButton: {
    paddingHorizontal: 16,
    paddingVertical: 8,
    borderRadius: 16,
  },
  createButtonText: {
    fontSize: 14,
    fontWeight: '600',
  },
  content: {
    flex: 1,
    padding: 16,
  },
  avatarSection: {
    alignItems: 'center',
    marginBottom: 32,
  },
  avatarPlaceholder: {
    width: 80,
    height: 80,
    borderRadius: 40,
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 12,
  },
  changeAvatarButton: {
    paddingVertical: 8,
  },
  changeAvatarText: {
    fontSize: 14,
    fontWeight: '500',
  },
  inputSection: {
    marginBottom: 24,
  },
  inputLabel: {
    fontSize: 16,
    fontWeight: '500',
    marginBottom: 8,
  },
  textInput: {
    borderWidth: 1,
    borderRadius: 8,
    paddingHorizontal: 16,
    paddingVertical: 12,
    fontSize: 16,
  },
  textArea: {
    height: 100,
    paddingTop: 12,
  },
  inputMeta: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginTop: 8,
  },
  inputHint: {
    fontSize: 12,
    flex: 1,
  },
  characterCount: {
    fontSize: 12,
    marginLeft: 8,
  },
  infoSection: {
    padding: 16,
    borderRadius: 8,
    marginBottom: 24,
  },
  infoHeader: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 8,
  },
  infoTitle: {
    fontSize: 14,
    fontWeight: '500',
    marginLeft: 8,
  },
  infoText: {
    fontSize: 12,
    lineHeight: 18,
  },
  guidelinesSection: {
    padding: 16,
    borderRadius: 8,
    marginBottom: 24,
  },
  guidelinesTitle: {
    fontSize: 14,
    fontWeight: '500',
    marginBottom: 8,
  },
  guidelinesText: {
    fontSize: 12,
    lineHeight: 18,
  },
});
