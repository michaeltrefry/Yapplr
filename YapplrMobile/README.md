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
- **TanStack Query**: Data fetching and caching
- **AsyncStorage**: Local data persistence
- **TypeScript**: Type safety and better development experience

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
- ✅ **Home**: Timeline with posts and reposts
- ✅ **Search**: User search functionality
- ✅ **Messages**: Conversation list with unread counts
- ✅ **Profile**: User profile with logout

### **API Integration**
- ✅ Shared API client with web app
- ✅ Automatic token injection
- ✅ Error handling and retry logic
- ✅ Real-time data updates

## 🔧 **Configuration**

### **API Base URL**
Update the API URL in `src/contexts/AuthContext.tsx`:
```typescript
const API_BASE_URL = 'http://localhost:5161'; // Change to your API URL
```

For production, use your deployed API URL.

### **Development vs Production**
- **Development**: Uses localhost API
- **Production**: Update to production API URL before building

## 📂 **Project Structure**

```
YapplrMobile/
├── src/
│   ├── contexts/
│   │   └── AuthContext.tsx          # Authentication state management
│   ├── navigation/
│   │   └── AppNavigator.tsx         # Navigation configuration
│   ├── screens/
│   │   ├── auth/
│   │   │   ├── LoginScreen.tsx      # Login interface
│   │   │   └── RegisterScreen.tsx   # Registration interface
│   │   └── main/
│   │       ├── HomeScreen.tsx       # Timeline/feed
│   │       ├── SearchScreen.tsx     # User search
│   │       ├── MessagesScreen.tsx   # Conversations
│   │       └── ProfileScreen.tsx    # User profile
│   └── LoadingScreen.tsx            # Loading state
├── App.tsx                          # Root component
└── package.json
```

## 🔄 **Shared Package Integration**

The mobile app uses the `yapplr-shared` package for:
- **API Clients**: Consistent API calls across platforms
- **TypeScript Types**: Shared interfaces and enums
- **Business Logic**: Common utilities and helpers

### **Updating Shared Code**
When making changes to shared code:
```bash
cd yapplr-shared
npm run build
cd ../YapplrMobile
# Restart the development server
```

## 🎯 **Next Steps**

### **Immediate Enhancements**
1. **Post Creation**: Add camera integration and post composer
2. **Push Notifications**: Real-time message and interaction alerts
3. **Image Handling**: Photo upload and display optimization
4. **Offline Support**: Cache posts for offline viewing

### **Advanced Features**
1. **Real-time Updates**: WebSocket integration for live features
2. **Deep Linking**: Direct links to posts and profiles
3. **Share Extension**: Share to Yapplr from other apps
4. **Haptic Feedback**: Enhanced user interactions

### **Performance Optimizations**
1. **Infinite Scroll**: Optimized FlatList implementation
2. **Image Caching**: Fast image loading and caching
3. **Memory Management**: Proper cleanup and optimization
4. **Bundle Size**: Code splitting and optimization

## 🧪 **Testing**

### **Development Testing**
- Use Expo Go app for quick testing
- iOS Simulator for iOS-specific testing
- Android Emulator for Android testing

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

1. Make changes to shared code in `yapplr-shared/`
2. Build shared package: `npm run build`
3. Test changes in mobile app
4. Ensure web app compatibility
5. Submit pull request

## 📞 **Support**

For issues or questions:
- Check Expo documentation
- Review React Native guides
- Check shared package integration
