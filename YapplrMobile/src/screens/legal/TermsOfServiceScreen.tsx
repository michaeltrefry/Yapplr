import React, { useState, useEffect } from 'react';
import {
  View,
  Text,
  ScrollView,
  StyleSheet,
  TouchableOpacity,
  ActivityIndicator,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useAuth } from '../../contexts/AuthContext';
import { ContentPageVersion } from '../../types';
import { parseMarkdown, formatLastUpdated, MarkdownElement } from '../../utils/markdownParser';

interface TermsOfServiceScreenProps {
  navigation: any;
}

export default function TermsOfServiceScreen({ navigation }: TermsOfServiceScreenProps) {
  const { api } = useAuth();
  const [content, setContent] = useState<ContentPageVersion | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchTermsContent();
  }, []);

  const fetchTermsContent = async () => {
    try {
      const termsContent = await api.content.getTermsOfService();
      setContent(termsContent);
    } catch (err) {
      console.error('Failed to fetch Terms of Service:', err);
      setError('Failed to load Terms of Service');
    } finally {
      setLoading(false);
    }
  };

  const renderMarkdownElement = (element: MarkdownElement, index: number) => {
    switch (element.type) {
      case 'heading1':
        return (
          <Text key={index} style={styles.mainTitle}>
            {element.content}
          </Text>
        );
      case 'heading2':
        return (
          <Text key={index} style={styles.sectionTitle}>
            {element.content}
          </Text>
        );
      case 'heading3':
        return (
          <Text key={index} style={styles.subSectionTitle}>
            {element.content}
          </Text>
        );
      case 'bulletPoint':
        return (
          <Text key={index} style={styles.bulletPoint}>
            • {element.content}
          </Text>
        );
      case 'break':
        return <View key={index} style={styles.break} />;
      case 'paragraph':
      default:
        return (
          <Text key={index} style={styles.paragraph}>
            {element.content}
          </Text>
        );
    }
  };

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity
          style={styles.backButton}
          onPress={() => navigation.goBack()}
        >
          <Ionicons name="arrow-back" size={24} color="#374151" />
        </TouchableOpacity>
        <Text style={styles.headerTitle}>Terms of Service</Text>
      </View>

      {loading ? (
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color="#3B82F6" />
          <Text style={styles.loadingText}>Loading Terms of Service...</Text>
        </View>
      ) : error ? (
        <View style={styles.errorContainer}>
          <Ionicons name="alert-circle" size={48} color="#EF4444" />
          <Text style={styles.errorText}>{error}</Text>
          <TouchableOpacity style={styles.retryButton} onPress={fetchTermsContent}>
            <Text style={styles.retryButtonText}>Retry</Text>
          </TouchableOpacity>
        </View>
      ) : content ? (
        <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
          <Text style={styles.lastUpdated}>
            Last updated: {formatLastUpdated(content.publishedAt || content.createdAt)}
          </Text>

          <View style={styles.section}>
            {parseMarkdown(content.content).map((element, index) =>
              renderMarkdownElement(element, index)
            )}
          </View>
        </ScrollView>
      ) : (
        <View style={styles.errorContainer}>
          <Ionicons name="document-text" size={48} color="#9CA3AF" />
          <Text style={styles.errorText}>Terms of Service not available</Text>
        </View>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f9fafb',
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    backgroundColor: '#ffffff',
    borderBottomWidth: 1,
    borderBottomColor: '#e5e7eb',
    paddingTop: 50,
  },
  backButton: {
    padding: 8,
    marginRight: 8,
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#111827',
  },
  content: {
    flex: 1,
    paddingHorizontal: 16,
  },
  lastUpdated: {
    fontSize: 14,
    color: '#6b7280',
    marginTop: 16,
    marginBottom: 24,
    fontStyle: 'italic',
  },
  section: {
    marginBottom: 24,
  },
  sectionTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#111827',
    marginBottom: 12,
  },
  paragraph: {
    fontSize: 16,
    lineHeight: 24,
    color: '#374151',
    marginBottom: 8,
  },
  bulletPoint: {
    fontSize: 16,
    lineHeight: 24,
    color: '#374151',
    marginBottom: 4,
    paddingLeft: 8,
  },
  bold: {
    fontWeight: '600',
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 32,
  },
  loadingText: {
    fontSize: 16,
    color: '#6B7280',
    marginTop: 16,
    textAlign: 'center',
  },
  errorContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 32,
  },
  errorText: {
    fontSize: 16,
    color: '#6B7280',
    marginTop: 16,
    textAlign: 'center',
  },
  retryButton: {
    backgroundColor: '#3B82F6',
    paddingHorizontal: 24,
    paddingVertical: 12,
    borderRadius: 8,
    marginTop: 16,
  },
  retryButtonText: {
    color: '#FFFFFF',
    fontSize: 16,
    fontWeight: '600',
  },
  mainTitle: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#111827',
    marginBottom: 16,
    marginTop: 8,
  },
  subSectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: '#111827',
    marginBottom: 8,
    marginTop: 12,
  },
  break: {
    height: 8,
  },
});
