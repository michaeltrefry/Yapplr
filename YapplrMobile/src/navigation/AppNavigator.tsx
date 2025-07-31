import React, { useRef, useEffect } from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { NavigationContainer, NavigationContainerRef } from '@react-navigation/native';
import { createStackNavigator } from '@react-navigation/stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { Ionicons } from '@expo/vector-icons';
import { useQuery } from '@tanstack/react-query';

import { useAuth } from '../contexts/AuthContext';
import { useThemeColors } from '../hooks/useThemeColors';
import { UserStatus } from '../types';
import LoadingScreen from '../screens/LoadingScreen';
import LoginScreen from '../screens/auth/LoginScreen';
import RegisterScreen from '../screens/auth/RegisterScreen';
import ForgotPasswordScreen from '../screens/auth/ForgotPasswordScreen';
import ResetPasswordScreen from '../screens/auth/ResetPasswordScreen';
import VerifyEmailScreen from '../screens/auth/VerifyEmailScreen';
import ResendVerificationScreen from '../screens/auth/ResendVerificationScreen';
import EmailVerificationRequiredScreen from '../screens/auth/EmailVerificationRequiredScreen';
import HomeScreen from '../screens/main/HomeScreen';
import ProfileScreen from '../screens/main/ProfileScreen';
import UserProfileScreen from '../screens/main/UserProfileScreen';
import EditProfileScreen from '../screens/main/EditProfileScreen';
import FollowingListScreen from '../screens/main/FollowingListScreen';
import FollowersListScreen from '../screens/main/FollowersListScreen';
import ConversationScreen from '../screens/main/ConversationScreen';
import MessagesScreen from '../screens/main/MessagesScreen';
import SearchScreen from '../screens/main/SearchScreen';

import SettingsScreen from '../screens/main/SettingsScreen';
import BlockedUsersScreen from '../screens/main/BlockedUsersScreen';
import HelpSupportScreen from '../screens/main/HelpSupportScreen';
import CreatePostScreen from '../screens/main/CreatePostScreen';
import NotificationDebugScreen from '../screens/main/NotificationDebugScreen';
import NotificationsScreen from '../screens/main/NotificationsScreen';
import NotificationTestScreen from '../screens/NotificationTestScreen';
import GroupsScreen from '../screens/main/GroupsScreen';
import GroupDetailScreen from '../screens/main/GroupDetailScreen';
import CreateGroupScreen from '../screens/main/CreateGroupScreen';
import SinglePostScreen from '../screens/main/SinglePostScreen';
import PrivacyPolicyScreen from '../screens/legal/PrivacyPolicyScreen';
import TermsOfServiceScreen from '../screens/legal/TermsOfServiceScreen';
import SubscriptionScreen from '../screens/SubscriptionScreen';
import { Post, Group } from '../types';
import NotificationNavigationService from '../services/NotificationNavigationService';

export type RootStackParamList = {
  MainTabs: undefined;
  UserProfile: { username: string };
  EditProfile: undefined;
  FollowingList: { userId: number; username: string };
  FollowersList: { userId: number; username: string };
  Settings: undefined;
  Subscription: undefined;
  Messages: undefined;
  BlockedUsers: undefined;
  HelpSupport: undefined;
  NotificationDebug: undefined;
  NotificationTest: undefined;
  Notifications: undefined;
  Groups: undefined;
  GroupDetail: { groupId: number };
  CreateGroup: undefined;
  EditGroup: { group: Group };
  PrivacyPolicy: undefined;
  TermsOfService: undefined;
  Conversation: {
    conversationId: number;
    otherUser: { id: number; username: string }
  };
  SinglePost: {
    postId: number;
    scrollToComment?: number;
    showComments?: boolean;
  };
};

export type AuthStackParamList = {
  Login: undefined;
  Register: undefined;
  ForgotPassword: undefined;
  ResetPassword: { token?: string };
  VerifyEmail: { email?: string };
  ResendVerification: undefined;
  EmailVerificationRequired: { email?: string };
  PrivacyPolicy: undefined;
  TermsOfService: undefined;
  Subscription: undefined;
};

const Stack = createStackNavigator<RootStackParamList>();
const AuthStack = createStackNavigator<AuthStackParamList>();
const Tab = createBottomTabNavigator();



function AuthStackNavigator() {
  return (
    <AuthStack.Navigator screenOptions={{ headerShown: false }}>
      <AuthStack.Screen name="Login" component={LoginScreen} />
      <AuthStack.Screen name="Register" component={RegisterScreen} />
      <AuthStack.Screen name="ForgotPassword" component={ForgotPasswordScreen} />
      <AuthStack.Screen name="ResetPassword" component={ResetPasswordScreen} />
      <AuthStack.Screen name="VerifyEmail" component={VerifyEmailScreen} />
      <AuthStack.Screen name="ResendVerification" component={ResendVerificationScreen} />
      <AuthStack.Screen name="EmailVerificationRequired" component={EmailVerificationRequiredScreen} />
      <AuthStack.Screen name="PrivacyPolicy" component={PrivacyPolicyScreen} />
      <AuthStack.Screen name="TermsOfService" component={TermsOfServiceScreen} />
    </AuthStack.Navigator>
  );
}

function MainTabs() {
  const { api, user } = useAuth();
  const colors = useThemeColors();

  // Check if user is suspended
  const isSuspended = user?.status === UserStatus.Suspended;

  // Fetch unread message count
  const { data: unreadData } = useQuery({
    queryKey: ['unreadMessageCount'],
    queryFn: () => api.messages.getUnreadCount(),
    enabled: !!user,
    refetchInterval: 30000, // Refetch every 30 seconds
    retry: 2,
  });

  const unreadCount = unreadData?.unreadCount || 0;
  console.log('Unread count data:', unreadData, 'Final count:', unreadCount);

  return (
    <Tab.Navigator
      screenOptions={({ route }) => ({
        tabBarIcon: ({ focused, color, size }) => {
          let iconName: keyof typeof Ionicons.glyphMap;

          if (route.name === 'Home') {
            iconName = focused ? 'home' : 'home-outline';
          } else if (route.name === 'Groups') {
            iconName = focused ? 'people' : 'people-outline';
          } else if (route.name === 'CreatePost') {
            return (
              <View style={{
                width: 50,
                height: 50,
                borderRadius: 25,
                backgroundColor: isSuspended ? colors.textMuted : colors.primary,
                justifyContent: 'center',
                alignItems: 'center',
                marginBottom: 2,
                shadowColor: isSuspended ? colors.textMuted : colors.primary,
                shadowOffset: { width: 0, height: 2 },
                shadowOpacity: isSuspended ? 0.1 : 0.3,
                shadowRadius: 4,
                elevation: isSuspended ? 2 : 5,
              }}>
                <Ionicons
                  name={isSuspended ? "ban" : "add"}
                  size={28}
                  color={isSuspended ? colors.background : colors.primaryText}
                />
              </View>
            );
          } else if (route.name === 'Messages') {
            iconName = focused ? 'chatbubbles' : 'chatbubbles-outline';
          } else if (route.name === 'Profile') {
            iconName = focused ? 'person' : 'person-outline';
          } else {
            iconName = 'help-outline';
          }

          // For Messages tab, show badge if there are unread messages
          if (route.name === 'Messages' && unreadCount > 0) {
            return (
              <View style={styles.tabIconContainer}>
                <Ionicons name={iconName} size={size} color={color} />
                <View style={styles.badge}>
                  <Text style={styles.badgeText}>
                    {unreadCount > 99 ? '99+' : unreadCount.toString()}
                  </Text>
                </View>
              </View>
            );
          }

          return <Ionicons name={iconName} size={size} color={color} />;
        },
        tabBarActiveTintColor: colors.primary,
        tabBarInactiveTintColor: colors.textMuted,
        headerShown: false,
        tabBarStyle: {
          height: 90,
          paddingBottom: 20,
          paddingTop: 10,
          backgroundColor: colors.card,
          borderTopColor: colors.border,
        },
      })}
    >
      <Tab.Screen name="Home" component={HomeScreen} />
      <Tab.Screen name="Groups" component={GroupsScreen} />
      <Tab.Screen
        name="CreatePost"
        component={CreatePostScreen}
        options={{ tabBarLabel: '' }}
      />
      <Tab.Screen name="Messages" component={MessagesScreen} />
      <Tab.Screen name="Profile" component={ProfileScreen} />
    </Tab.Navigator>
  );
}

function MainStack() {
  return (
    <Stack.Navigator>
      <Stack.Screen
        name="MainTabs"
        component={MainTabs}
        options={{ headerShown: false }}
      />
      <Stack.Screen
        name="UserProfile"
        component={UserProfileScreen}
        options={{ headerShown: false }}
      />
      <Stack.Screen
        name="EditProfile"
        component={EditProfileScreen}
        options={{ headerShown: false }}
      />
      <Stack.Screen
        name="FollowingList"
        component={FollowingListScreen}
        options={{ headerShown: false }}
      />
      <Stack.Screen
        name="FollowersList"
        component={FollowersListScreen}
        options={{ headerShown: false }}
      />
      <Stack.Screen
        name="Settings"
        component={SettingsScreen}
        options={{ headerShown: false }}
      />
      <Stack.Screen
        name="Subscription"
        component={SubscriptionScreen}
        options={{ headerShown: false }}
      />
      <Stack.Screen
        name="Messages"
        component={MessagesScreen}
        options={{ headerShown: false }}
      />
      <Stack.Screen
        name="BlockedUsers"
        component={BlockedUsersScreen}
        options={{ headerShown: false }}
      />
      <Stack.Screen
        name="HelpSupport"
        component={HelpSupportScreen}
        options={{ headerShown: false }}
      />
      {/* Debug/Test screens - only available in development */}
      {__DEV__ && (
        <>
          <Stack.Screen
            name="NotificationDebug"
            component={NotificationDebugScreen}
            options={{ headerShown: false }}
          />
          <Stack.Screen
            name="NotificationTest"
            component={NotificationTestScreen}
            options={{ headerShown: false }}
          />
        </>
      )}
      <Stack.Screen
        name="Notifications"
        component={NotificationsScreen}
        options={{ headerShown: false }}
      />
      <Stack.Screen
        name="GroupDetail"
        component={GroupDetailScreen}
        options={{ headerShown: false }}
      />
      <Stack.Screen
        name="CreateGroup"
        component={CreateGroupScreen}
        options={{ headerShown: false }}
      />
      <Stack.Screen
        name="PrivacyPolicy"
        component={PrivacyPolicyScreen}
        options={{ headerShown: false }}
      />
      <Stack.Screen
        name="TermsOfService"
        component={TermsOfServiceScreen}
        options={{ headerShown: false }}
      />
      <Stack.Screen
        name="Conversation"
        component={ConversationScreen}
        options={{ headerShown: false }}
      />
      <Stack.Screen
        name="SinglePost"
        component={SinglePostScreen}
        options={{ headerShown: false }}
      />

    </Stack.Navigator>
  );
}

export default function AppNavigator() {
  const { isAuthenticated, isLoading } = useAuth();
  const navigationRef = useRef<NavigationContainerRef<RootStackParamList>>(null);

  // Register navigation ref with the notification navigation service
  useEffect(() => {
    if (navigationRef.current) {
      NotificationNavigationService.setNavigationRef(navigationRef.current);
    }
  }, []);

  if (isLoading) {
    return <LoadingScreen />;
  }

  return (
    <NavigationContainer ref={navigationRef}>
      {isAuthenticated ? <MainStack /> : <AuthStackNavigator />}
    </NavigationContainer>
  );
}

const styles = StyleSheet.create({
  tabIconContainer: {
    position: 'relative',
    alignItems: 'center',
    justifyContent: 'center',
  },
  badge: {
    position: 'absolute',
    top: -6,
    right: -6,
    backgroundColor: '#EF4444',
    borderRadius: 10,
    minWidth: 20,
    height: 20,
    justifyContent: 'center',
    alignItems: 'center',
    paddingHorizontal: 4,
  },
  badgeText: {
    color: '#fff',
    fontSize: 12,
    fontWeight: 'bold',
  },
});
