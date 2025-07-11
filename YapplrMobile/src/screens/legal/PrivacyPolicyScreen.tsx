import React from 'react';
import {
  View,
  Text,
  ScrollView,
  StyleSheet,
  TouchableOpacity,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';

interface PrivacyPolicyScreenProps {
  navigation: any;
}

export default function PrivacyPolicyScreen({ navigation }: PrivacyPolicyScreenProps) {
  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity
          style={styles.backButton}
          onPress={() => navigation.goBack()}
        >
          <Ionicons name="arrow-back" size={24} color="#374151" />
        </TouchableOpacity>
        <Text style={styles.headerTitle}>Privacy Policy</Text>
      </View>

      <ScrollView style={styles.content} showsVerticalScrollIndicator={false}>
        <Text style={styles.lastUpdated}>
          Last updated: {new Date().toLocaleDateString()}
        </Text>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>1. Information We Collect</Text>
          <Text style={styles.paragraph}>
            When you create an account on Yapplr, we collect:
          </Text>
          <Text style={styles.bulletPoint}>• Email address (required for account creation and verification)</Text>
          <Text style={styles.bulletPoint}>• Username (your unique identifier on the platform)</Text>
          <Text style={styles.bulletPoint}>• Password (stored securely using industry-standard encryption)</Text>
          <Text style={styles.bulletPoint}>• Profile information (bio, pronouns, tagline, birthday - all optional)</Text>
          <Text style={styles.bulletPoint}>• Posts, comments, and other content you create</Text>
          <Text style={styles.bulletPoint}>• Usage data and interactions with other users</Text>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>2. How We Use Your Information</Text>
          <Text style={styles.paragraph}>
            We use your information to:
          </Text>
          <Text style={styles.bulletPoint}>• Provide and maintain the Yapplr service</Text>
          <Text style={styles.bulletPoint}>• Verify your identity and secure your account</Text>
          <Text style={styles.bulletPoint}>• Enable you to connect and communicate with other users</Text>
          <Text style={styles.bulletPoint}>• Send important account notifications and updates</Text>
          <Text style={styles.bulletPoint}>• Improve our service and develop new features</Text>
          <Text style={styles.bulletPoint}>• Ensure compliance with our Terms of Service</Text>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>3. Information Sharing</Text>
          <Text style={styles.paragraph}>
            We do not sell, trade, or rent your personal information to third parties. We may share your information only in the following circumstances:
          </Text>
          <Text style={styles.bulletPoint}>• With your explicit consent</Text>
          <Text style={styles.bulletPoint}>• To comply with legal obligations or court orders</Text>
          <Text style={styles.bulletPoint}>• To protect the rights, property, or safety of Yapplr, our users, or others</Text>
          <Text style={styles.bulletPoint}>• In connection with a business transfer or acquisition</Text>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>4. Data Security</Text>
          <Text style={styles.paragraph}>
            We implement appropriate technical and organizational measures to protect your personal information against unauthorized access, alteration, disclosure, or destruction.
          </Text>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>5. Your Rights</Text>
          <Text style={styles.paragraph}>
            You have the right to:
          </Text>
          <Text style={styles.bulletPoint}>• Access and update your personal information</Text>
          <Text style={styles.bulletPoint}>• Delete your account and associated data</Text>
          <Text style={styles.bulletPoint}>• Control your privacy settings and content visibility</Text>
          <Text style={styles.bulletPoint}>• Opt out of non-essential communications</Text>
          <Text style={styles.bulletPoint}>• Request a copy of your data</Text>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>6. Children's Privacy</Text>
          <Text style={styles.paragraph}>
            Yapplr is not intended for children under 13 years of age. We do not knowingly collect personal information from children under 13.
          </Text>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>7. Changes to This Policy</Text>
          <Text style={styles.paragraph}>
            We may update this Privacy Policy from time to time. We will notify you of any material changes by posting the new policy and updating the "Last updated" date.
          </Text>
        </View>

        <View style={[styles.section, { marginBottom: 40 }]}>
          <Text style={styles.sectionTitle}>8. Contact Us</Text>
          <Text style={styles.paragraph}>
            If you have any questions about this Privacy Policy, please contact us at:
          </Text>
          <Text style={styles.paragraph}>
            Email: privacy@yapplr.com
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
});
