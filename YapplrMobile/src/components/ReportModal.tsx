import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  TextInput,
  TouchableOpacity,
  Modal,
  StyleSheet,
  SafeAreaView,
  Alert,
  KeyboardAvoidingView,
  Platform,
  ScrollView,
  ActivityIndicator,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useAuth } from '../contexts/AuthContext';
import { useThemeColors } from '../hooks/useThemeColors';
import { CreateUserReportDto, SystemTag } from '../types';

interface ReportModalProps {
  visible: boolean;
  onClose: () => void;
  postId?: number;
  commentId?: number;
  contentType: 'post' | 'comment';
  contentPreview: string;
}

export default function ReportModal({
  visible,
  onClose,
  postId,
  commentId,
  contentType,
  contentPreview
}: ReportModalProps) {
  const { api } = useAuth();
  const colors = useThemeColors();
  const styles = createStyles(colors);

  const [reason, setReason] = useState('');
  const [selectedTagIds, setSelectedTagIds] = useState<number[]>([]);
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [systemTags, setSystemTags] = useState<SystemTag[]>([]);
  const [isLoadingTags, setIsLoadingTags] = useState(false);

  // Load system tags when modal opens
  useEffect(() => {
    if (visible && !isSubmitted) {
      loadSystemTags();
    }
  }, [visible]);

  // Reset form when modal closes
  useEffect(() => {
    if (!visible) {
      setReason('');
      setSelectedTagIds([]);
      setIsSubmitted(false);
      setIsSubmitting(false);
    }
  }, [visible]);

  const loadSystemTags = async () => {
    try {
      setIsLoadingTags(true);
      const tags = await api.userReports.getSystemTags();
      // Filter tags that are appropriate for user reporting
      const reportingTags = tags.filter(tag =>
        tag.isActive && (
          tag.category === 1 || // Violation
          tag.category === 5 || // Safety
          tag.category === 0 || // ContentWarning
          tag.category === 3    // Quality (includes Spam)
        )
      );
      setSystemTags(reportingTags);
    } catch (error) {
      console.error('Failed to load system tags:', error);
      Alert.alert('Error', 'Failed to load reporting categories. Please try again.');
    } finally {
      setIsLoadingTags(false);
    }
  };

  const handleSubmit = async () => {
    if (!reason.trim()) {
      Alert.alert('Error', 'Please provide a reason for reporting this content.');
      return;
    }

    try {
      setIsSubmitting(true);
      
      const reportData: CreateUserReportDto = {
        postId,
        commentId,
        reason: reason.trim(),
        systemTagIds: selectedTagIds,
      };

      await api.userReports.createReport(reportData);
      setIsSubmitted(true);
    } catch (error) {
      console.error('Failed to submit report:', error);
      Alert.alert('Error', 'Failed to submit report. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleTagToggle = (tagId: number) => {
    setSelectedTagIds(prev => 
      prev.includes(tagId) 
        ? prev.filter(id => id !== tagId)
        : [...prev, tagId]
    );
  };

  const handleClose = () => {
    if (!isSubmitting) {
      onClose();
    }
  };

  return (
    <Modal
      visible={visible}
      animationType="slide"
      presentationStyle="pageSheet"
      onRequestClose={handleClose}
    >
      <SafeAreaView style={styles.container}>
        <KeyboardAvoidingView
          style={styles.keyboardAvoid}
          behavior={Platform.OS === 'ios' ? 'padding' : 'height'}
        >
          {/* Header */}
          <View style={styles.header}>
            <TouchableOpacity 
              onPress={handleClose} 
              style={styles.headerButton}
              disabled={isSubmitting}
            >
              <Text style={[styles.cancelText, isSubmitting && styles.disabledText]}>
                Cancel
              </Text>
            </TouchableOpacity>
            
            <View style={styles.headerTitle}>
              <Ionicons name="flag" size={20} color="#EF4444" />
              <Text style={styles.titleText}>Report {contentType}</Text>
            </View>
            
            <View style={styles.headerButton} />
          </View>

          <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
            {isSubmitted ? (
              <View style={styles.successContainer}>
                <Ionicons name="checkmark-circle" size={64} color="#10B981" />
                <Text style={styles.successTitle}>Report Submitted</Text>
                <Text style={styles.successMessage}>
                  Thank you for reporting this content. Our moderation team will review it shortly.
                </Text>
                <TouchableOpacity style={styles.doneButton} onPress={handleClose}>
                  <Text style={styles.doneButtonText}>Done</Text>
                </TouchableOpacity>
              </View>
            ) : (
              <>
                {/* Content Preview */}
                <View style={styles.previewContainer}>
                  <Text style={styles.previewLabel}>Reporting this {contentType}:</Text>
                  <Text style={styles.previewText} numberOfLines={3}>
                    {contentPreview}
                  </Text>
                </View>

                {/* System Tags Selection */}
                {isLoadingTags ? (
                  <View style={styles.loadingContainer}>
                    <ActivityIndicator size="small" color="#3B82F6" />
                    <Text style={styles.loadingText}>Loading categories...</Text>
                  </View>
                ) : (
                  <View style={styles.tagsContainer}>
                    <Text style={styles.tagsLabel}>
                      What type of issue are you reporting? (Select all that apply)
                    </Text>
                    <View style={styles.tagsList}>
                      {systemTags.map((tag) => (
                        <TouchableOpacity
                          key={tag.id}
                          style={[
                            styles.tagItem,
                            selectedTagIds.includes(tag.id) && styles.tagItemSelected
                          ]}
                          onPress={() => handleTagToggle(tag.id)}
                          disabled={isSubmitting}
                        >
                          <View style={[
                            styles.checkbox,
                            selectedTagIds.includes(tag.id) && styles.checkboxSelected
                          ]}>
                            {selectedTagIds.includes(tag.id) && (
                              <Ionicons name="checkmark" size={16} color="#fff" />
                            )}
                          </View>
                          <View style={styles.tagContent}>
                            <Text style={styles.tagName}>{tag.name}</Text>
                            {tag.description && (
                              <Text style={styles.tagDescription}>{tag.description}</Text>
                            )}
                          </View>
                        </TouchableOpacity>
                      ))}
                    </View>
                  </View>
                )}

                {/* Reason Input */}
                <View style={styles.reasonContainer}>
                  <Text style={styles.reasonLabel}>
                    Please provide additional details about why you're reporting this content:
                  </Text>
                  <TextInput
                    style={styles.reasonInput}
                    value={reason}
                    onChangeText={setReason}
                    placeholder="Describe the issue..."
                    placeholderTextColor="#9CA3AF"
                    multiline
                    numberOfLines={4}
                    textAlignVertical="top"
                    editable={!isSubmitting}
                  />
                </View>

                {/* Submit Button */}
                <TouchableOpacity
                  style={[
                    styles.submitButton,
                    (!reason.trim() || isSubmitting) && styles.submitButtonDisabled
                  ]}
                  onPress={handleSubmit}
                  disabled={!reason.trim() || isSubmitting}
                >
                  {isSubmitting ? (
                    <ActivityIndicator size="small" color="#fff" />
                  ) : (
                    <Text style={styles.submitButtonText}>Submit Report</Text>
                  )}
                </TouchableOpacity>
              </>
            )}
          </ScrollView>
        </KeyboardAvoidingView>
      </SafeAreaView>
    </Modal>
  );
}

const createStyles = (colors: any) => StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  keyboardAvoid: {
    flex: 1,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  headerButton: {
    minWidth: 60,
  },
  cancelText: {
    fontSize: 16,
    color: '#3B82F6',
  },
  disabledText: {
    opacity: 0.5,
  },
  headerTitle: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
  },
  titleText: {
    fontSize: 18,
    fontWeight: '600',
    color: colors.text,
  },
  content: {
    flex: 1,
    padding: 16,
  },
  successContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingVertical: 40,
  },
  successTitle: {
    fontSize: 24,
    fontWeight: '600',
    color: colors.text,
    marginTop: 16,
    marginBottom: 8,
  },
  successMessage: {
    fontSize: 16,
    color: '#6B7280',
    textAlign: 'center',
    lineHeight: 24,
    marginBottom: 32,
  },
  doneButton: {
    backgroundColor: '#3B82F6',
    paddingHorizontal: 32,
    paddingVertical: 12,
    borderRadius: 8,
  },
  doneButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
  previewContainer: {
    backgroundColor: '#F9FAFB',
    padding: 12,
    borderRadius: 8,
    marginBottom: 24,
  },
  previewLabel: {
    fontSize: 14,
    color: '#6B7280',
    marginBottom: 8,
  },
  previewText: {
    fontSize: 14,
    color: colors.text,
    lineHeight: 20,
  },
  loadingContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    paddingVertical: 20,
    gap: 8,
  },
  loadingText: {
    fontSize: 14,
    color: '#6B7280',
  },
  tagsContainer: {
    marginBottom: 24,
  },
  tagsLabel: {
    fontSize: 16,
    fontWeight: '500',
    color: colors.text,
    marginBottom: 12,
  },
  tagsList: {
    gap: 8,
  },
  tagItem: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    padding: 12,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: '#E5E7EB',
    backgroundColor: colors.background,
    gap: 12,
  },
  tagItemSelected: {
    borderColor: '#3B82F6',
    backgroundColor: '#EFF6FF',
  },
  checkbox: {
    width: 20,
    height: 20,
    borderRadius: 4,
    borderWidth: 2,
    borderColor: '#D1D5DB',
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: 2,
  },
  checkboxSelected: {
    backgroundColor: '#3B82F6',
    borderColor: '#3B82F6',
  },
  tagContent: {
    flex: 1,
  },
  tagName: {
    fontSize: 14,
    fontWeight: '500',
    color: colors.text,
    marginBottom: 2,
  },
  tagDescription: {
    fontSize: 12,
    color: '#6B7280',
    lineHeight: 16,
  },
  reasonContainer: {
    marginBottom: 24,
  },
  reasonLabel: {
    fontSize: 16,
    fontWeight: '500',
    color: colors.text,
    marginBottom: 12,
  },
  reasonInput: {
    borderWidth: 1,
    borderColor: '#D1D5DB',
    borderRadius: 8,
    padding: 12,
    fontSize: 16,
    color: colors.text,
    backgroundColor: colors.background,
    minHeight: 100,
  },
  submitButton: {
    backgroundColor: '#EF4444',
    paddingVertical: 16,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  submitButtonDisabled: {
    backgroundColor: '#9CA3AF',
  },
  submitButtonText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: '600',
  },
});
