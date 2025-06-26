import React from 'react';
import {
  View,
  Text,
  TouchableOpacity,
  StyleSheet,
  SafeAreaView,
  Alert,
  ActivityIndicator,
} from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { Ionicons } from '@expo/vector-icons';
import { useNavigation } from '@react-navigation/native';
import { StackNavigationProp } from '@react-navigation/stack';
import { useAuth } from '../../contexts/AuthContext';
import { RootStackParamList } from '../../navigation/AppNavigator';

type ProfileScreenNavigationProp = StackNavigationProp<RootStackParamList>;

export default function ProfileScreen() {
  const { user, logout, api } = useAuth();
  const navigation = useNavigation<ProfileScreenNavigationProp>();

  // Fetch full profile data for current user
  const { data: profile, isLoading, error } = useQuery({
    queryKey: ['userProfile', user?.username],
    queryFn: () => api.users.getUserProfile(user!.username),
    enabled: !!user?.username,
    retry: 2,
  });

  const handleLogout = () => {
    Alert.alert(
      'Sign Out',
      'Are you sure you want to sign out?',
      [
        { text: 'Cancel', style: 'cancel' },
        { text: 'Sign Out', style: 'destructive', onPress: logout },
      ]
    );
  };

  if (!user) {
    return null;
  }

  if (isLoading) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.headerTitle}>Profile</Text>
          <TouchableOpacity onPress={handleLogout}>
            <Ionicons name="log-out-outline" size={24} color="#EF4444" />
          </TouchableOpacity>
        </View>
        <View style={styles.loadingContainer}>
          <ActivityIndicator size="large" color="#3B82F6" />
          <Text style={styles.loadingText}>Loading profile...</Text>
        </View>
      </SafeAreaView>
    );
  }

  if (error || !profile) {
    return (
      <SafeAreaView style={styles.container}>
        <View style={styles.header}>
          <Text style={styles.headerTitle}>Profile</Text>
          <TouchableOpacity onPress={handleLogout}>
            <Ionicons name="log-out-outline" size={24} color="#EF4444" />
          </TouchableOpacity>
        </View>
        <View style={styles.errorContainer}>
          <Text style={styles.errorText}>Failed to load profile</Text>
        </View>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.headerTitle}>Profile</Text>
        <TouchableOpacity onPress={handleLogout}>
          <Ionicons name="log-out-outline" size={24} color="#EF4444" />
        </TouchableOpacity>
      </View>

      <View style={styles.profileSection}>
        <View style={styles.avatar}>
          <Text style={styles.avatarText}>
            {profile.username.charAt(0).toUpperCase()}
          </Text>
        </View>

        <View style={styles.usernameContainer}>
          <Text style={styles.username}>@{profile.username}</Text>
          {profile.pronouns && (
            <Text style={styles.pronouns}> ({profile.pronouns})</Text>
          )}
        </View>
        <Text style={styles.email}>{user.email}</Text>

        {profile.bio && (
          <Text style={styles.bio}>{profile.bio}</Text>
        )}

        {profile.tagline && (
          <Text style={styles.tagline}>"{profile.tagline}"</Text>
        )}

        {profile.birthday && (
          <Text style={styles.birthday}>🎂 Born {new Date(profile.birthday).toLocaleDateString()}</Text>
        )}

        <View style={styles.statsContainer}>
          <View style={styles.statItem}>
            <Text style={styles.statNumber}>{profile.postCount}</Text>
            <Text style={styles.statLabel}>Posts</Text>
          </View>
          <TouchableOpacity
            style={styles.statItem}
            onPress={() => navigation.navigate('FollowingList')}
            activeOpacity={0.7}
          >
            <Text style={styles.statNumber}>{profile.followingCount}</Text>
            <Text style={styles.statLabel}>Following</Text>
          </TouchableOpacity>
          <TouchableOpacity
            style={styles.statItem}
            onPress={() => navigation.navigate('FollowersList')}
            activeOpacity={0.7}
          >
            <Text style={styles.statNumber}>{profile.followerCount}</Text>
            <Text style={styles.statLabel}>Followers</Text>
          </TouchableOpacity>
        </View>
      </View>

      <View style={styles.menuSection}>
        <TouchableOpacity
          style={styles.menuItem}
          onPress={() => navigation.navigate('EditProfile')}
        >
          <Ionicons name="person-outline" size={20} color="#6B7280" />
          <Text style={styles.menuText}>Edit Profile</Text>
          <Ionicons name="chevron-forward" size={20} color="#6B7280" />
        </TouchableOpacity>

        <TouchableOpacity style={styles.menuItem}>
          <Ionicons name="settings-outline" size={20} color="#6B7280" />
          <Text style={styles.menuText}>Settings</Text>
          <Ionicons name="chevron-forward" size={20} color="#6B7280" />
        </TouchableOpacity>

        <TouchableOpacity style={styles.menuItem}>
          <Ionicons name="help-circle-outline" size={20} color="#6B7280" />
          <Text style={styles.menuText}>Help & Support</Text>
          <Ionicons name="chevron-forward" size={20} color="#6B7280" />
        </TouchableOpacity>
      </View>
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
  headerTitle: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#1F2937',
  },
  profileSection: {
    alignItems: 'center',
    paddingVertical: 32,
    paddingHorizontal: 16,
  },
  avatar: {
    width: 80,
    height: 80,
    borderRadius: 40,
    backgroundColor: '#3B82F6',
    justifyContent: 'center',
    alignItems: 'center',
    marginBottom: 16,
  },
  avatarText: {
    color: '#fff',
    fontWeight: 'bold',
    fontSize: 32,
  },
  usernameContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    marginBottom: 4,
  },
  username: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#1F2937',
  },
  email: {
    fontSize: 16,
    color: '#6B7280',
    marginBottom: 12,
  },
  bio: {
    fontSize: 16,
    color: '#1F2937',
    textAlign: 'center',
    lineHeight: 24,
    marginBottom: 12,
  },
  pronouns: {
    fontSize: 18,
    color: '#6B7280',
    fontWeight: 'normal',
  },
  tagline: {
    fontSize: 14,
    color: '#6B7280',
    textAlign: 'center',
    fontStyle: 'italic',
    marginBottom: 8,
  },
  birthday: {
    fontSize: 14,
    color: '#6B7280',
    textAlign: 'center',
    marginBottom: 16,
  },
  statsContainer: {
    flexDirection: 'row',
    justifyContent: 'space-around',
    width: '100%',
    paddingTop: 16,
    borderTopWidth: 1,
    borderTopColor: '#E5E7EB',
  },
  statItem: {
    alignItems: 'center',
  },
  statNumber: {
    fontSize: 20,
    fontWeight: 'bold',
    color: '#1F2937',
  },
  statLabel: {
    fontSize: 14,
    color: '#6B7280',
    marginTop: 4,
  },
  menuSection: {
    paddingHorizontal: 16,
    paddingTop: 16,
  },
  menuItem: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 16,
    borderBottomWidth: 1,
    borderBottomColor: '#E5E7EB',
  },
  menuText: {
    flex: 1,
    fontSize: 16,
    color: '#1F2937',
    marginLeft: 12,
  },
  loadingContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 32,
  },
  loadingText: {
    marginTop: 16,
    fontSize: 16,
    color: '#6B7280',
  },
  errorContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: 32,
  },
  errorText: {
    fontSize: 16,
    color: '#EF4444',
    textAlign: 'center',
  },
});
