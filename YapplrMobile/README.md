# Yapplr Mobile App

React Native mobile application for the Yapplr social media platform.

## 🏗️ **Architecture**

### **Shared Code Structure**
- **yapplr-shared**: Common package containing API clients, TypeScript types, and business logic
- **Code Reuse**: 70-80% code sharing between web and mobile apps
- **Type Safety**: Full TypeScript support with shared interfaces

### **Tech Stack**
- **React Native**: Cross-platform mobile development
- **Expo**: Development platform and tooling
- **React Navigation**: Navigation library
- **Axios**: HTTP client for API communication
- **AsyncStorage**: Local data persistence
- **TypeScript**: Type safety and better development experience
- **Expo Image Picker**: Camera and gallery integration for photos and videos

## 🚀 **Getting Started**

### **Prerequisites**
- Node.js 18+ 
- Expo CLI: `npm install -g @expo/cli`
- iOS Simulator (for iOS development)
- Android Studio (for Android development)
- Expo Go app on your phone (for testing)

### **Installation**

1. **Install dependencies:**
   ```bash
   cd YapplrMobile
   npm install --legacy-peer-deps
   ```

2. **Build shared package:**
   ```bash
   cd ../yapplr-shared
   npm run build
   ```

3. **Start the development server:**
   ```bash
   cd ../YapplrMobile
   npx expo start
   ```

4. **Run on device/simulator:**
   - **iOS Simulator**: Press `i` in the terminal
   - **Android Emulator**: Press `a` in the terminal
   - **Physical Device**: Scan QR code with Expo Go app

## 📱 **Features Implemented**

### **Authentication**
- ✅ Login/Register screens
- ✅ JWT token management
- ✅ Persistent authentication
- ✅ Auto-logout on token expiry

### **Core Screens**
- ✅ **Home**: Timeline with posts, reposts, and images
- ✅ **Search**: Enhanced user search with navigation to profiles
- ✅ **Create Post**: Integrated tab bar button with modal interface
- ✅ **Messages**: Conversation list with unread counts and navigation
- ✅ **Conversations**: Full messaging interface with real-time chat
- ✅ **Profile**: User profile with logout and profile image display
- ✅ **Edit Profile**: Profile editing with image upload functionality
- ✅ **User Profiles**: View other users' profiles and posts with profile images
- ✅ **Following/Followers**: User lists with profile image display
- ✅ **Settings**: Privacy and safety settings management
- ✅ **Blocked Users**: View and manage blocked users list

### **Post Management**
- ✅ **Create Posts**: Text, image, and video post creation with mention detection
- ✅ **Media Upload**: Unified gallery picker for both images and videos from iPhone photo library
- ✅ **Video Support**: Upload videos up to 100MB in multiple formats (MP4, MOV, AVI, WMV, FLV, WebM, MKV)
- ✅ **File Validation**: Automatic format detection and size validation with user-friendly error messages
- ✅ **Timeline Display**: Posts with images, videos, and interactions
- ✅ **Emoji Reactions**: Rich reaction system with 6 emoji types (❤️👍😂😮😢😡) for posts and comments
- ✅ **Reaction Picker**: Modal-based emoji selection with real-time counts and haptic feedback
- ✅ **Like/Repost**: Social interaction features (legacy like support maintained)
- ✅ **Comments System**: Full commenting functionality with dedicated screens
- ✅ **Comment Replies**: Reply to specific comments with automatic @username prefilling
- ✅ **Mentions**: @username mention system with clickable links and notifications

### **Navigation & User Interaction**
- ✅ **User Profile Navigation**: Tap avatars/usernames to view profiles
- ✅ **Profile Timeline**: View user's posts and reposts
- ✅ **Cross-Profile Navigation**: Navigate between different user profiles
- ✅ **User Search**: Search users by username/bio with instant navigation
- ✅ **Message Users**: Start private conversations from user profiles
- ✅ **Conversation Navigation**: Access conversations from Messages tab
- ✅ **Create Post Navigation**: Elevated tab bar button opens create post modal
- ✅ **Back Navigation**: Proper navigation stack management
- ✅ **Public Profile View**: Tap username/avatar/post count to see public profile view
- ✅ **Settings Navigation**: Access settings from profile menu

### **Messaging System**
- ✅ **Private Conversations**: One-on-one messaging between users
- ✅ **Message Composition**: Real-time message sending with validation
- ✅ **Conversation History**: View all messages in chronological order
- ✅ **Message Bubbles**: Distinct styling for sent vs received messages
- ✅ **Keyboard Handling**: Proper keyboard avoidance and input positioning
- ✅ **Auto-scroll**: Automatic scrolling to latest messages
- ✅ **Permission Checking**: Verify messaging permissions before allowing contact

### **Notifications System**
- ✅ **Real-time Notifications**: Instant notifications for mentions, likes, reposts, follows, and comments
- ✅ **Notification Center**: Comprehensive notification list with smart navigation
- ✅ **Smart Navigation**: Click notifications to navigate directly to mentioned posts or comments
- ✅ **Notification Badges**: Red badge indicators showing unread notification count
- ✅ **Auto-scroll & Highlighting**: Automatic scrolling to specific comments with visual highlighting
- ✅ **Read Status Management**: Mark notifications as read with proper state management

### **Enhanced Notification Banners** 🆕
- ✅ **Navigation on Tap**: Tap notification banners to navigate to relevant screens
  - **Messages**: Navigate directly to conversation with sender
  - **Mentions**: Navigate to post comments where you were mentioned
  - **Follows**: Navigate to the follower's profile
- ✅ **Banner Queue System**: Multiple notifications display sequentially with smooth transitions
  - **Smart Queuing**: Notifications queue when multiple arrive quickly
  - **Sequential Display**: Shows one banner at a time with 300ms delays
  - **Auto-processing**: Automatically shows next notification after dismissal
- ✅ **Custom Swipe Actions**: Interactive swipe gestures for quick actions
  - **Swipe Right**: Mark notification as read (with success haptic feedback)
  - **Swipe Left**: Reply to message notifications (with medium haptic feedback)
  - **Gesture Recognition**: Smooth swipe detection with spring-back animations
- ✅ **Sound & Haptic Feedback**: Rich feedback patterns for different notification types
  - **Messages**: Medium haptic + double vibration pattern + sound
  - **Mentions**: Heavy haptic + strong single vibration + sound
  - **Likes**: Light haptic + light vibration (no sound for less urgency)
  - **Comments**: Medium haptic + standard vibration + sound
  - **Follows**: Success haptic + triple vibration pattern + sound
  - **Reposts**: Light haptic + light vibration (no sound)
- ✅ **Visual Design**: Modern banner design with emoji icons and smooth animations
- ✅ **Testing Suite**: Comprehensive test buttons for all banner features

### **Post Creation & Privacy**
- ✅ **Tab Bar Integration**: Prominent create post button in center of tab bar
- ✅ **Three Privacy Levels**: Public, Followers, and Private post options
- ✅ **Top Controls Layout**: Privacy, image, and character count controls above keyboard
- ✅ **Privacy Cycling**: Tap to cycle through Public → Followers → Private
- ✅ **Visual Privacy Indicators**: Icons and text clearly show current privacy setting
- ✅ **Keyboard-Friendly Design**: Controls remain visible when typing

### **User Safety & Privacy**
- ✅ **User Blocking**: Block/unblock users from their profiles
- ✅ **Block Confirmation**: Confirmation modal explaining blocking consequences
- ✅ **Blocked Users Management**: View and manage list of blocked users
- ✅ **Settings Screen**: Centralized privacy and safety settings
- ✅ **Block Status Checking**: Real-time block status updates
- ✅ **Automatic Unfollowing**: Blocked users are automatically unfollowed

### **Image Functionality**
- ✅ **Image Upload**: Select from device gallery
- ✅ **Image Display**: Optimized loading in timeline
- ✅ **Full-Screen Viewer**: Tap to expand with zoom
- ✅ **Pinch to Zoom**: Native zoom gestures
- ✅ **Loading States**: Smooth image loading experience
- ✅ **Profile Images**: Upload and display user profile pictures
- ✅ **Avatar Display**: Profile images shown in timeline posts and user lists
- ✅ **Profile Image Upload**: Camera icon overlay for easy profile picture changes

### **API Integration**
- ✅ Custom API client with error handling
- ✅ Automatic token injection
- ✅ Network error recovery
- ✅ Real-time data updates
- ✅ Image upload with progress tracking
- ✅ Profile image upload endpoint integration
- ✅ Multipart form data handling for image uploads
- ✅ Block/unblock user endpoints
- ✅ Block status checking endpoints
- ✅ Blocked users list retrieval

## 🔧 **Configuration**

### **API Base URL**
Update the API URL in `src/api/client.ts`:
```typescript
const API_BASE_URL = 'http://192.168.254.181:5161'; // Change to your API URL
```

For production, use your deployed API URL. For development, use your local network IP address to allow mobile device access.

### **Development vs Production**
- **Development**: Uses localhost API
- **Production**: Update to production API URL before building

## 📂 **Project Structure**

```
YapplrMobile/
├── src/
│   ├── api/
│   │   └── client.ts                # API client configuration
│   ├── components/
│   │   ├── CreatePostModal.tsx      # Post creation with image upload
│   │   ├── ImageViewer.tsx          # Full-screen image viewer
│   │   ├── PostCard.tsx             # Timeline post display with user navigation
│   │   ├── NotificationBanner.tsx   # Enhanced notification banner with swipe actions
│   │   └── NotificationBannerManager.tsx # Banner queue and lifecycle management
│   ├── services/
│   │   ├── NotificationNavigationService.ts # Smart navigation from notifications
│   │   └── NotificationFeedbackService.ts   # Haptic and sound feedback patterns
│   ├── contexts/
│   │   └── AuthContext.tsx          # Authentication state management
│   ├── navigation/
│   │   └── AppNavigator.tsx         # Navigation configuration
│   ├── screens/
│   │   ├── auth/
│   │   │   ├── LoginScreen.tsx      # Login interface
│   │   │   └── RegisterScreen.tsx   # Registration interface
│   │   └── main/
│   │       ├── HomeScreen.tsx       # Timeline/feed with posts
│   │       ├── SearchScreen.tsx     # User search
│   │       ├── MessagesScreen.tsx   # Conversation list
│   │       ├── ConversationScreen.tsx # Individual conversation interface
│   │       ├── ProfileScreen.tsx    # Current user profile with image display
│   │       ├── EditProfileScreen.tsx # Profile editing with image upload
│   │       ├── CreatePostScreen.tsx # Create post tab screen with modal
│   │       ├── UserProfileScreen.tsx # Other users' profiles with images and blocking
│   │       ├── FollowingListScreen.tsx # Following list with profile images
│   │       ├── FollowersListScreen.tsx # Followers list with profile images
│   │       ├── SettingsScreen.tsx   # Privacy and safety settings
│   │       └── BlockedUsersScreen.tsx # Blocked users management
│   ├── types/
│   │   └── index.ts                 # TypeScript type definitions
│   ├── utils/
│   │   └── networkTest.ts           # Network connectivity utilities
│   └── LoadingScreen.tsx            # Loading state
├── App.tsx                          # Root component
└── package.json
```

## 🔄 **API Integration**

The mobile app uses a custom API client for:
- **HTTP Requests**: Axios-based client with interceptors
- **Authentication**: Automatic token injection
- **Error Handling**: Network error recovery and retry logic
- **Image Upload**: Multipart form data support

### **Network Configuration**
For development with physical devices:
1. **Find your local IP**: Use `ipconfig` (Windows) or `ifconfig` (Mac/Linux)
2. **Update API URL**: Change `localhost` to your network IP
3. **Use tunnel mode**: Run `npx expo start --tunnel` for external access

## 🎯 **Next Steps**

### **Immediate Enhancements**
1. **Camera Integration**: Add camera capture for posts (gallery picker implemented)
2. **Push Notifications**: Real-time push notification delivery (notifications system implemented, push delivery pending)
3. **Follow/Unfollow**: Implement follow functionality in user profiles

### **Advanced Features**
1. **Real-time Updates**: WebSocket integration for live features
2. **Deep Linking**: Direct links to posts and profiles
3. **Share Extension**: Share to Yapplr from other apps
4. **Haptic Feedback**: Enhanced user interactions
5. **Offline Support**: Cache posts for offline viewing

### **Performance Optimizations**
1. **Infinite Scroll**: Optimized FlatList implementation (✅ Implemented)
2. **Image Caching**: Enhanced image loading and caching
3. **Memory Management**: Proper cleanup and optimization
4. **Bundle Size**: Code splitting and optimization

## 🧪 **Testing**

### **Development Testing**
- Use Expo Go app for quick testing
- iOS Simulator for iOS-specific testing
- Android Emulator for Android testing

### **Enhanced Notification Banner Testing**
The app includes a comprehensive test suite for notification banner features:

1. **Access Test Screen**: Navigate to "Notification Test Screen" in the app
2. **Available Tests**:
   - **Test Queue**: Sends multiple notifications rapidly to test queuing system
   - **Test Swipe**: Sends a message notification to test swipe gestures
   - **Test Mention**: Sends a mention notification to test navigation to post comments
3. **Test Scenarios**:
   - **Navigation**: Tap banners to test navigation to conversations, posts, and profiles
   - **Swipe Actions**: Swipe right (mark as read) and left (reply) on message notifications
   - **Queue Behavior**: Send multiple notifications to verify sequential display
   - **Haptic Feedback**: Feel different vibration patterns for different notification types
   - **Sound Feedback**: Hear notification sounds (varies by notification type)

### **Testing Haptic Feedback**
- **iOS**: Full haptic feedback support with different patterns
- **Android**: Vibration patterns with fallback support
- **Expo Go**: Limited haptic support, use development build for full testing

### **Production Testing**
- Build standalone apps for app store testing
- Test with production API endpoints
- Performance testing on various devices

## 📦 **Building for Production**

### **iOS Build**
```bash
npx expo build:ios
```

### **Android Build**
```bash
npx expo build:android
```

### **App Store Deployment**
Follow Expo's documentation for app store submission.

## 🤝 **Contributing**

1. Make changes to mobile app code
2. Test on both iOS and Android platforms
3. Ensure API compatibility with backend
4. Test image functionality thoroughly
5. Submit pull request with detailed description

## 📞 **Support**

For issues or questions:
- Check Expo documentation
- Review React Native guides
- Test API connectivity with network tools
- Verify image upload permissions and formats

## 🎉 **Current Status**

The Yapplr mobile app now has **full feature parity** with the web frontend for core functionality, plus **enhanced mobile-specific features**:

### **Core Features**
- ✅ **Authentication**: Complete login/register flow
- ✅ **Timeline**: Posts with images, likes, reposts, and user profile images
- ✅ **Post Creation**: Text, image, and video posts with unified gallery picker
- ✅ **Image Viewing**: Full-screen viewer with pinch-to-zoom
- ✅ **Social Features**: Emoji reactions, repost, and user interactions
- ✅ **User Profiles**: Navigate to user profiles by tapping avatars/names
- ✅ **Profile Images**: Upload, display, and manage user profile pictures
- ✅ **Profile Timeline**: View any user's posts and profile information with images
- ✅ **User Search**: Search and navigate to user profiles instantly
- ✅ **Private Messaging**: Complete messaging system with conversation management
- ✅ **Message Composition**: Send and receive text, image, and video messages in real-time
- ✅ **Conversation Navigation**: Access conversations from multiple entry points
- ✅ **Real-time Updates**: Live timeline refresh
- ✅ **Profile Management**: Edit profile information and upload profile pictures

### **Mobile-Enhanced Features** 🆕
- ✅ **Interactive Notification Banners**: Tap to navigate, swipe for actions
- ✅ **Haptic Feedback System**: Rich tactile feedback for different notification types
- ✅ **Gesture-Based Interactions**: Swipe actions for quick notification management
- ✅ **Smart Notification Queuing**: Elegant handling of multiple simultaneous notifications
- ✅ **Context-Aware Navigation**: Intelligent routing from notifications to relevant content

The app is ready for production use and provides a **superior mobile experience** with native mobile interactions!
