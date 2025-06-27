import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { NavigationContainer } from '@react-navigation/native';
import { createStackNavigator } from '@react-navigation/stack';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { Ionicons } from '@expo/vector-icons';
import { useQuery } from '@tanstack/react-query';

import { useAuth } from '../contexts/AuthContext';
import LoadingScreen from '../screens/LoadingScreen';
import LoginScreen from '../screens/auth/LoginScreen';
import RegisterScreen from '../screens/auth/RegisterScreen';
import HomeScreen from '../screens/main/HomeScreen';
import ProfileScreen from '../screens/main/ProfileScreen';
import UserProfileScreen from '../screens/main/UserProfileScreen';
import EditProfileScreen from '../screens/main/EditProfileScreen';
import FollowingListScreen from '../screens/main/FollowingListScreen';
import FollowersListScreen from '../screens/main/FollowersListScreen';
import ConversationScreen from '../screens/main/ConversationScreen';
import MessagesScreen from '../screens/main/MessagesScreen';
import SearchScreen from '../screens/main/SearchScreen';
import CommentsScreen from '../screens/main/CommentsScreen';
import SettingsScreen from '../screens/main/SettingsScreen';
import BlockedUsersScreen from '../screens/main/BlockedUsersScreen';
import { Post } from '../types';

export type RootStackParamList = {
  MainTabs: undefined;
  UserProfile: { username: string };
  EditProfile: undefined;
  FollowingList: undefined;
  FollowersList: undefined;
  Settings: undefined;
  BlockedUsers: undefined;
  Conversation: {
    conversationId: number;
    otherUser: { id: number; username: string }
  };
  Comments: {
    post: Post;
    onCommentCountUpdate?: (postId: number, newCount: number) => void;
  };
};

export type AuthStackParamList = {
  Login: undefined;
  Register: undefined;
};

const Stack = createStackNavigator<RootStackParamList>();
const AuthStack = createStackNavigator<AuthStackParamList>();
const Tab = createBottomTabNavigator();



function AuthStackNavigator() {
  return (
    <AuthStack.Navigator screenOptions={{ headerShown: false }}>
      <AuthStack.Screen name="Login" component={LoginScreen} />
      <AuthStack.Screen name="Register" component={RegisterScreen} />
    </AuthStack.Navigator>
  );
}

function MainTabs() {
  const { api, user } = useAuth();

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
          } else if (route.name === 'Search') {
            iconName = focused ? 'search' : 'search-outline';
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
        tabBarActiveTintColor: '#3B82F6',
        tabBarInactiveTintColor: 'gray',
        headerShown: false,
      })}
    >
      <Tab.Screen name="Home" component={HomeScreen} />
      <Tab.Screen name="Search" component={SearchScreen} />
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
        name="BlockedUsers"
        component={BlockedUsersScreen}
        options={{ headerShown: false }}
      />
      <Stack.Screen
        name="Conversation"
        component={ConversationScreen}
        options={{ headerShown: false }}
      />
      <Stack.Screen
        name="Comments"
        component={CommentsScreen}
        options={{ headerShown: false }}
      />
    </Stack.Navigator>
  );
}

export default function AppNavigator() {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <LoadingScreen />;
  }

  return (
    <NavigationContainer>
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
