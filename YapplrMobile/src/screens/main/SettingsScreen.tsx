import React from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  SafeAreaView,
  Switch,
} from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useNavigation } from '@react-navigation/native';
import { StackNavigationProp } from '@react-navigation/stack';
import { RootStackParamList } from '../../navigation/AppNavigator';
import { useTheme } from '../../contexts/ThemeContext';
import { useThemeColors } from '../../hooks/useThemeColors';

type SettingsScreenNavigationProp = StackNavigationProp<RootStackParamList, 'Settings'>;

export default function SettingsScreen() {
  const navigation = useNavigation<SettingsScreenNavigationProp>();
  const { isDarkMode, toggleDarkMode } = useTheme();
  const colors = useThemeColors();

  const styles = createStyles(colors);

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <TouchableOpacity onPress={() => navigation.goBack()}>
          <Ionicons name="arrow-back" size={24} color={colors.text} />
        </TouchableOpacity>
        <Text style={styles.headerTitle}>Settings</Text>
        <View style={{ width: 24 }} />
      </View>

      <View style={styles.content}>
        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Appearance</Text>

          <View style={styles.menuItem}>
            <View style={styles.menuItemLeft}>
              <View style={[styles.iconContainer, styles.blueIconContainer]}>
                <Ionicons name={isDarkMode ? "moon" : "sunny"} size={20} color={colors.primary} />
              </View>
              <View style={styles.menuItemText}>
                <Text style={styles.menuItemTitle}>Dark Mode</Text>
                <Text style={styles.menuItemSubtitle}>Switch between light and dark themes</Text>
              </View>
            </View>
            <Switch
              value={isDarkMode}
              onValueChange={toggleDarkMode}
              trackColor={{ false: colors.inputBorder, true: colors.primary }}
              thumbColor={colors.background}
            />
          </View>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Notifications</Text>

          {/* Debug/Test screens - only available in development */}
          {__DEV__ && (
            <>
              <TouchableOpacity
                style={styles.menuItem}
                onPress={() => navigation.navigate('NotificationDebug')}
              >
                <View style={styles.menuItemLeft}>
                  <View style={[styles.iconContainer, styles.orangeIconContainer]}>
                    <Ionicons name="bug-outline" size={20} color="#F97316" />
                  </View>
                  <View style={styles.menuItemText}>
                    <Text style={styles.menuItemTitle}>Debug Center</Text>
                    <Text style={styles.menuItemSubtitle}>Test notification functionality</Text>
                  </View>
                </View>
                <Ionicons name="chevron-forward" size={20} color={colors.textSecondary} />
              </TouchableOpacity>

              <TouchableOpacity
                style={styles.menuItem}
                onPress={() => navigation.navigate('NotificationTest')}
              >
                <View style={styles.menuItemLeft}>
                  <View style={[styles.iconContainer, styles.blueIconContainer]}>
                    <Ionicons name="notifications-outline" size={20} color={colors.primary} />
                  </View>
                  <View style={styles.menuItemText}>
                    <Text style={styles.menuItemTitle}>Expo Push Test</Text>
                    <Text style={styles.menuItemSubtitle}>Test Expo push notifications</Text>
                  </View>
                </View>
                <Ionicons name="chevron-forward" size={20} color={colors.textSecondary} />
              </TouchableOpacity>
            </>
          )}
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Account</Text>

          <TouchableOpacity
            style={styles.menuItem}
            onPress={() => navigation.navigate('Subscription')}
          >
            <View style={styles.menuItemLeft}>
              <View style={[styles.iconContainer, styles.greenIconContainer]}>
                <Ionicons name="card-outline" size={20} color="#10B981" />
              </View>
              <View style={styles.menuItemText}>
                <Text style={styles.menuItemTitle}>Subscription</Text>
                <Text style={styles.menuItemSubtitle}>Manage your subscription tier</Text>
              </View>
            </View>
            <Ionicons name="chevron-forward" size={20} color={colors.textSecondary} />
          </TouchableOpacity>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Privacy & Safety</Text>

          <TouchableOpacity
            style={styles.menuItem}
            onPress={() => navigation.navigate('BlockedUsers')}
          >
            <View style={styles.menuItemLeft}>
              <View style={[styles.iconContainer, styles.redIconContainer]}>
                <Ionicons name="person-remove-outline" size={20} color={colors.error} />
              </View>
              <View style={styles.menuItemText}>
                <Text style={styles.menuItemTitle}>Blocklist</Text>
                <Text style={styles.menuItemSubtitle}>Manage users you've blocked</Text>
              </View>
            </View>
            <Ionicons name="chevron-forward" size={20} color={colors.textSecondary} />
          </TouchableOpacity>
        </View>

        <View style={styles.section}>
          <Text style={styles.sectionTitle}>Support</Text>

          <TouchableOpacity
            style={styles.menuItem}
            onPress={() => navigation.navigate('HelpSupport')}
          >
            <View style={styles.menuItemLeft}>
              <View style={[styles.iconContainer, styles.blueIconContainer]}>
                <Ionicons name="help-circle-outline" size={20} color={colors.primary} />
              </View>
              <View style={styles.menuItemText}>
                <Text style={styles.menuItemTitle}>Help & Support</Text>
                <Text style={styles.menuItemSubtitle}>Get help and view legal documents</Text>
              </View>
            </View>
            <Ionicons name="chevron-forward" size={20} color={colors.textSecondary} />
          </TouchableOpacity>
        </View>
      </View>
    </SafeAreaView>
  );
}

const createStyles = (colors: any) => StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: colors.background,
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: 'bold',
    color: colors.text,
  },
  content: {
    flex: 1,
    padding: 16,
  },
  section: {
    marginBottom: 32,
  },
  sectionTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: colors.text,
    marginBottom: 12,
    paddingHorizontal: 4,
  },
  menuItem: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingVertical: 16,
    paddingHorizontal: 16,
    backgroundColor: colors.card,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    marginBottom: 8,
  },
  menuItemLeft: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  iconContainer: {
    width: 40,
    height: 40,
    borderRadius: 20,
    justifyContent: 'center',
    alignItems: 'center',
    marginRight: 12,
  },
  redIconContainer: {
    backgroundColor: colors.background === '#FFFFFF' ? '#FEF2F2' : '#7F1D1D',
  },
  blueIconContainer: {
    backgroundColor: colors.background === '#FFFFFF' ? '#EFF6FF' : '#1E3A8A',
  },
  orangeIconContainer: {
    backgroundColor: colors.background === '#FFFFFF' ? '#FFF7ED' : '#9A3412',
  },
  greenIconContainer: {
    backgroundColor: colors.background === '#FFFFFF' ? '#F0FDF4' : '#14532D',
  },
  menuItemText: {
    flex: 1,
  },
  menuItemTitle: {
    fontSize: 16,
    fontWeight: '600',
    color: colors.text,
    marginBottom: 2,
  },
  menuItemSubtitle: {
    fontSize: 14,
    color: colors.textSecondary,
  },
});
