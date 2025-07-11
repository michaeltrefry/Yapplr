import React from 'react';
import {
  View,
  Text,
  ScrollView,
  StyleSheet,
  TouchableOpacity,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';

interface TermsOfServiceScreenProps {
  navigation: any;
}

export default function TermsOfServiceScreen({ navigation }: TermsOfServiceScreenProps) {
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

      <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
        <Text style={styles.lastUpdated}>
          Last updated: {new Date().toLocaleDateString()}
        </Text>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>1. Acceptance of Terms</Text>
          <Text style={styles.paragraph}>
            By creating an account or using Yapplr, you agree to be bound by these Terms of Service and our Privacy Policy. If you do not agree to these terms, please do not use our service.
          </Text>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>2. Description of Service</Text>
          <Text style={styles.paragraph}>
            Yapplr is a social media platform that allows users to share short messages ("yaps"), follow other users, and engage with content through likes, comments, and reposts.
          </Text>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>3. User Accounts</Text>
          <Text style={styles.paragraph}>
            To use Yapplr, you must:
          </Text>
          <Text style={styles.bulletPoint}>• Be at least 13 years old</Text>
          <Text style={styles.bulletPoint}>• Provide accurate and complete information</Text>
          <Text style={styles.bulletPoint}>• Maintain the security of your account credentials</Text>
          <Text style={styles.bulletPoint}>• Verify your email address</Text>
          <Text style={styles.bulletPoint}>• Accept responsibility for all activity under your account</Text>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>4. Content Guidelines</Text>
          <Text style={styles.paragraph}>
            You are responsible for the content you post. You agree not to post content that:
          </Text>
          <Text style={styles.bulletPoint}>• Is illegal, harmful, threatening, or abusive</Text>
          <Text style={styles.bulletPoint}>• Harasses, bullies, or intimidates others</Text>
          <Text style={styles.bulletPoint}>• Contains hate speech or discriminatory language</Text>
          <Text style={styles.bulletPoint}>• Violates intellectual property rights</Text>
          <Text style={styles.bulletPoint}>• Contains spam, malware, or phishing attempts</Text>
          <Text style={styles.bulletPoint}>• Impersonates another person or entity</Text>
          <Text style={styles.bulletPoint}>• Contains explicit sexual content involving minors</Text>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>5. Privacy and Content Visibility</Text>
          <Text style={styles.paragraph}>
            Yapplr offers three privacy levels for your content:
          </Text>
          <Text style={styles.bulletPoint}>• <Text style={styles.bold}>Public:</Text> Visible to everyone on the platform</Text>
          <Text style={styles.bulletPoint}>• <Text style={styles.bold}>Followers:</Text> Visible only to your approved followers</Text>
          <Text style={styles.bulletPoint}>• <Text style={styles.bold}>Private:</Text> Visible only to you</Text>
          <Text style={styles.paragraph}>
            You are responsible for setting appropriate privacy levels for your content.
          </Text>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>6. Intellectual Property</Text>
          <Text style={styles.paragraph}>
            You retain ownership of content you create and post on Yapplr. By posting content, you grant Yapplr a non-exclusive, royalty-free license to use, display, and distribute your content on the platform.
          </Text>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>7. Prohibited Activities</Text>
          <Text style={styles.paragraph}>
            You agree not to:
          </Text>
          <Text style={styles.bulletPoint}>• Use automated tools to access or interact with the service</Text>
          <Text style={styles.bulletPoint}>• Attempt to gain unauthorized access to other accounts</Text>
          <Text style={styles.bulletPoint}>• Interfere with the proper functioning of the service</Text>
          <Text style={styles.bulletPoint}>• Create multiple accounts to evade restrictions</Text>
          <Text style={styles.bulletPoint}>• Sell or transfer your account to others</Text>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>8. Moderation and Enforcement</Text>
          <Text style={styles.paragraph}>
            We reserve the right to:
          </Text>
          <Text style={styles.bulletPoint}>• Remove content that violates these terms</Text>
          <Text style={styles.bulletPoint}>• Suspend or terminate accounts for violations</Text>
          <Text style={styles.bulletPoint}>• Investigate reported content and user behavior</Text>
          <Text style={styles.bulletPoint}>• Cooperate with law enforcement when required</Text>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>9. Disclaimers</Text>
          <Text style={styles.paragraph}>
            Yapplr is provided "as is" without warranties of any kind. We do not guarantee uninterrupted service or the accuracy of user-generated content.
          </Text>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>10. Changes to Terms</Text>
          <Text style={styles.paragraph}>
            We may update these Terms of Service from time to time. Continued use of the service after changes constitutes acceptance of the new terms.
          </Text>
        </View>

        <View style={[styles.section, { marginBottom: 40 }]}>
          <Text style={styles.sectionTitle}>11. Contact Information</Text>
          <Text style={styles.paragraph}>
            If you have questions about these Terms of Service, please contact us at:
          </Text>
          <Text style={styles.paragraph}>
            Email: legal@yapplr.com
          </Text>
        </View>
      </ScrollView>
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
});
