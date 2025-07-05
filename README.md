# Yapplr - Twitter-like Social Media Platform

A complete Twitter-like social media platform built with modern web technologies. Features a clean, responsive design inspired by 2015 Twitter with comprehensive social features including yaps, reyaps, comments, likes, follow system, and privacy controls.

## 🚀 Features

### Core Social Features
- **Yaps**: Create text yaps (up to 256 characters) with optional images
- **Reyaps**: Reyap content with proper attribution in timeline feeds
- **Comments**: Full-featured commenting system with dedicated comment screens, real-time count updates, auto-scroll to new comments, and reply functionality
- **Mentions**: @username mention system with clickable links and real-time notifications
- **Comment Replies**: Reply to specific comments with automatic @username prefilling and smart reply context
- **Likes**: Like and unlike yaps with real-time counts
- **Follow System**: Follow/unfollow users with instant UI updates and optional follow approval system
- **Following/Followers Lists**: Tabbed interface on profile pages showing Posts, Following (count), and Followers (count) with detailed user lists and navigation to their profiles
- **User Profiles**: Complete profile management with bio, pronouns, tagline, birthday, and profile images
- **Profile Editing**: Edit profile information including bio, pronouns, tagline, and birthday with real-time updates
- **Yap Sharing**: Share yaps with social media integration and direct link copying
- **Content Management**: Delete your own yaps, comments, and reyaps with confirmation dialogs
- **User Blocking**: Block/unblock users with automatic unfollowing and content filtering
- **Settings Management**: Dedicated settings page with blocklist management
- **Private Messaging**: Send direct messages with text and photo attachments
- **Message Conversations**: Organized conversation threads with read status tracking and visual unread indicators
- **Messaging Privacy**: Blocked users cannot send messages to each other
- **Message Notifications**: Real-time unread message badges on Messages tab and conversation list
- **Enhanced Conversation UI**: Bold text and background highlights for unread conversations
- **Platform-Optimized Notifications**: Intelligent notification system that uses the best provider for each platform
  - **Web/Desktop**: SignalR real-time WebSocket notifications for instant delivery while browsing
  - **Mobile**: Firebase push notifications for battery-efficient delivery even when app is closed
  - **Automatic Detection**: Platform detection automatically selects the optimal notification provider
- **Comprehensive Notifications**: Complete notification system for mentions, likes, reposts, follows, and comments with real-time red badge indicators
- **Comment Notifications**: Instant notifications when someone comments on your posts with smart duplicate prevention (no double notifications when mentioned)
- **Follow Notifications**: Get notified when someone starts following you with direct navigation to their profile
- **Follow Request System**: Optional follow approval with request/accept/decline workflow and persistent status tracking
- **Like Notifications**: Instant notifications when someone likes your posts with navigation to the liked post
- **Repost Notifications**: Real-time alerts when someone reposts your content with direct post navigation
- **Smart Navigation**: Click notifications to navigate directly to mentioned posts, comments, or user profiles with automatic scrolling and highlighting
- **Background Notifications**: Push notifications work even when the app is minimized or closed (mobile)
- **Real-time Web Notifications**: Instant WebSocket-based notifications for active web sessions
- **Configurable Providers**: Easy switching between Firebase, SignalR, or both for testing and deployment
- **Dark Mode**: Complete dark theme support with user preferences and persistent storage

### Privacy & Security
- **Yap Privacy**: Three levels - Public (everyone), Followers (followers only), Private (author only)
- **Privacy-Aware Timeline**: Smart filtering based on user relationships and yap privacy
- **JWT Authentication**: Secure token-based authentication
- **Password Reset**: Email-based password recovery with 6-digit codes and AWS SES integration

### User Experience
- **Responsive Design**: Mobile-first approach with collapsible sidebar
- **Real-time Updates**: Live timeline with instant social interaction feedback and immediate sidebar following list updates
- **Infinite Scroll**: Smooth pagination with 25 posts per page for optimal performance
- **Return to Top**: Convenient navigation button when reaching the end of timelines
- **Image Upload**: Server-side image storage for yaps and profile pictures
- **User Search**: Find users by username or bio content
- **Clean UI**: 2015 Twitter-inspired design with modern touches
- **Individual Yap Pages**: Dedicated URLs for each yap with shareable links
- **Share Modal**: Popup sharing interface with social media integration (Twitter, Facebook, LinkedIn, Reddit)
- **Content Control**: Delete buttons with confirmation dialogs for your own content
- **Optimized Images**: Next.js Image component for better performance and loading
- **Real-time Messaging**: Live message updates with automatic conversation refresh
- **Message Attachments**: Send photos in messages with preview and validation
- **Conversation Management**: Infinite scroll message history with unread count indicators
- **Message Notifications**: Red badge indicators showing unread message counts on tabs and conversation lists
- **Visual Read Status**: Bold text and background highlights for conversations with unread messages
- **Dark Mode**: Complete dark theme with toggle in Settings, synchronized across all platforms
- **Database Performance**: Optimized with strategic indexing for fast timeline, profile, and messaging queries

## 🛠 Tech Stack

### Backend (.NET 9 API)
- **.NET 9** - Minimal Web API
- **PostgreSQL** - Database with Entity Framework Core and performance-optimized indexing
- **SignalR** - Real-time WebSocket notifications for web clients
- **Firebase Admin SDK** - Push notifications for mobile clients
- **Platform-Aware Notifications** - Configurable notification providers with automatic platform detection
- **JWT Bearer** - Authentication
- **BCrypt** - Password hashing
- **AWS SES** - Email service for password reset
- **File System Storage** - Image upload and serving

### Frontend (Next.js 15)
- **Next.js 15** - React framework with App Router
- **TypeScript** - Type safety
- **Tailwind CSS** - Utility-first styling
- **TanStack Query** - Data fetching and caching
- **Axios** - HTTP client
- **Lucide React** - Beautiful icons
- **SignalR Client** - Real-time WebSocket notifications for web
- **Firebase SDK** - Push notifications for mobile web
- **Platform Detection** - Automatic notification provider selection
- **Service Workers** - Background notification handling
- **date-fns** - Date formatting and manipulation

### Mobile App (React Native + Expo)
- **React Native** - Cross-platform mobile development
- **Expo** - Development platform and tooling
- **TypeScript** - Full type safety
- **React Navigation** - Navigation library with stack navigation
- **TanStack Query** - Data fetching (shared with web)
- **AsyncStorage** - Local data persistence
- **Expo Image Picker** - Camera and gallery integration for profile images, post images, and message attachments
- **Image Upload Support** - Full image upload functionality for posts and messages with automatic HEIC to JPEG conversion
- **Profile Management** - Complete profile editing with image upload functionality
- **Profile Images** - Display and upload user profile pictures across all screens
- **Message Notifications** - Real-time unread message badges and visual indicators
- **Enhanced Messaging UI** - Image attachments, read status, and conversation management
- **Comments System** - Full commenting functionality with dedicated screens, real-time updates, reply functionality, and optimized performance
- **Mentions & Notifications**: @username mention system with comprehensive notification support and smart navigation
- **Dark Mode**: Complete dark theme support with toggle in Settings and persistent user preferences
- **Shared Package** - 70-80% code reuse with web app

## 🏃‍♂️ Quick Start

### Prerequisites
- .NET 9 SDK
- Node.js 18+
- PostgreSQL 12+

### 1. Backend Setup

```bash
# Navigate to API directory
cd Yapplr.Api

# Install dependencies
dotnet restore

# Setup database (creates yapplr_db and runs migrations)
./setup-db.sh

# Start the API
dotnet run
```

The API will be available at `http://localhost:5161`

### 2. Frontend Setup

```bash
# Navigate to frontend directory
cd yapplr-frontend

# Install dependencies
npm install

# Create environment file
echo "NEXT_PUBLIC_API_URL=http://localhost:5161" > .env.local

# Start the frontend
npm run dev
```

The frontend will be available at `http://localhost:3000`

### 3. Mobile App Setup (Optional)

```bash
# Navigate to mobile app directory
cd YapplrMobile

# Install dependencies
npm install --legacy-peer-deps

# Build shared package
cd ../yapplr-shared
npm install && npm run build

# Start mobile development server
cd ../YapplrMobile
npx expo start
```

Use Expo Go app on your phone to scan the QR code, or press `i` for iOS simulator / `a` for Android emulator.

#### Mobile App Features
- **Complete Profile Management**: Edit bio, pronouns, tagline, birthday, and profile images
- **Profile Image Upload**: Camera icon overlay for easy profile picture changes with gallery picker
- **Profile Information Display**: View all profile information including pronouns next to username
- **Profile Images Everywhere**: User avatars displayed in timeline posts, user lists, and profiles
- **Following/Followers Lists**: Tap Following or Followers count to view detailed lists and navigate to user profiles with images
- **Enhanced Profile Layout**: Consolidated profile design with image on left, name/pronouns aligned to top, and tagline positioned directly underneath
- **Enhanced Messaging**: Send text and photo messages with real-time delivery and read status
- **Message Notifications**: Red badge on Messages tab showing total unread message count
- **Visual Conversation Indicators**: Bold text and background highlights for conversations with unread messages
- **Image Message Support**: Send photos in messages with gallery picker and preview functionality
- **Post Image Upload**: Create posts with images using gallery picker with automatic compression for large iPhone photos
- **HEIC Format Support**: Automatic conversion of iPhone HEIC images to JPEG format for compatibility
- **Image Compression**: Smart compression to handle large iPhone photos within 5MB API limits while maintaining quality
- **Automatic Read Marking**: Conversations automatically marked as read when opened
- **Comments System**: Dedicated comment screens with post context, real-time comment count updates, and auto-scroll to new comments
- **Optimized Performance**: Memoized components prevent image flashing and unnecessary re-renders during typing
- **Navigation**: Stack-based navigation with proper screen transitions
- **API Integration**: Full integration with backend API for profile updates and image uploads
- **Image Fallbacks**: Graceful fallback to user initials when no profile image is available
- **Password Recovery**: Complete password reset flow with 6-digit email codes and automatic navigation
- **Dark Mode**: Complete dark theme with toggle in Settings, synchronized with web app preferences

### 4. Firebase Setup (Required for Real-time Notifications)

Firebase provides real-time push notifications for all social interactions. The system supports both development and production environments with automatic fallback.

#### Development Setup (Application Default Credentials)
```bash
# Install Google Cloud CLI
# macOS: brew install google-cloud-sdk
# Windows: Download from https://cloud.google.com/sdk/docs/install

# Authenticate with your Google account
gcloud auth application-default login
```

#### Production Setup (Service Account Key)
1. **Create Firebase Project**: Go to [Firebase Console](https://console.firebase.google.com/) and create a new project
2. **Generate Service Account Key**:
   - Go to Project Settings → Service Accounts
   - Click "Generate new private key"
   - Download the JSON file
3. **Configure Environment Variables**:
   ```bash
   # For production deployment, set these environment variables:
   Firebase__ProjectId=your-firebase-project-id
   Firebase__ServiceAccountKey={"type":"service_account","project_id":"your-project-id",...}
   ```

#### Frontend Firebase Configuration
Add these environment variables to `yapplr-frontend/.env.local`:
```bash
NEXT_PUBLIC_FIREBASE_API_KEY=your-api-key
NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN=your-project.firebaseapp.com
NEXT_PUBLIC_FIREBASE_PROJECT_ID=your-project-id
NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET=your-project.appspot.com
NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID=your-sender-id
NEXT_PUBLIC_FIREBASE_APP_ID=your-app-id
NEXT_PUBLIC_FIREBASE_VAPID_KEY=your-vapid-key
```

#### Features
- **Dual Authentication**: Automatic fallback from Service Account Key to Application Default Credentials
- **Production Ready**: Service Account Key authentication for reliable deployment
- **Real-time Messaging**: Instant notifications for comments, mentions, likes, reposts, and follows
- **Smart Notifications**: Prevents duplicate notifications (e.g., no double notifications when post owner is mentioned in comments)
- **Cross-Platform**: Works on web browsers with push notification support
- **Background Notifications**: Notifications work even when the app is closed

## 📱 Key Features in Detail

### Timeline & Reyaps
- **Mixed Timeline**: Shows both original yaps and reyaps chronologically
- **Infinite Scroll**: Automatic loading of 25 posts per page with smooth pagination
- **Return to Top**: Quick navigation button when reaching the end of content
- **Reyap Attribution**: Clear "User reyapped" headers with original content
- **Privacy Respect**: Only shows reyaps of content you're allowed to see
- **User Profiles**: Both original yaps and reyaps appear on user profiles

### Yap Privacy System
- **Public Yaps**: Visible to everyone, can be reyapped by anyone
- **Followers Yaps**: Only visible to followers and author
- **Private Yaps**: Only visible to the author
- **Smart Filtering**: Timeline automatically filters based on relationships

### Mentions & Notifications System
- **@Username Mentions**: Mention users in posts and comments using @username syntax with automatic detection
- **Clickable Mentions**: All @username mentions are automatically converted to clickable profile links
- **Real-time Notifications**: Instant Firebase push notifications for mentions, likes, reposts, follows, and comments
- **Follow Notifications**: Get notified immediately when someone follows you with navigation to their profile
- **Like Notifications**: Real-time alerts when someone likes your posts with direct navigation to the liked post
- **Repost Notifications**: Instant notifications when someone reposts your content with navigation to the original post
- **Mention Notifications**: Immediate alerts when mentioned in posts or comments with smart navigation to context
- **Comment Notifications**: Real-time notifications when someone comments on your posts
- **Smart Navigation**: Click notifications to navigate directly to relevant content (posts, comments, profiles)
- **Comment Scrolling**: Automatic scrolling and highlighting when navigating to specific comments from notifications
- **Notification Badges**: Red badge indicators showing unread notification count in sidebar
- **Privacy Respect**: Blocked users don't receive notifications from users who blocked them
- **Notification Management**: Mark individual notifications or all notifications as read
- **Notification History**: Paginated notification list with timestamps and context
- **Background Push**: Notifications work even when app is closed or minimized
- **Cross-Platform**: Consistent notification experience across web and mobile platforms

### Comments System
- **Dedicated Comment Screens**: Full-screen comment interface showing post context and all comments
- **Real-time Updates**: Comment counts update immediately across all screens when comments are added
- **Auto-scroll**: Automatically scroll to show newly added comments for better user experience
- **Reply Functionality**: Reply to specific comments with automatic @username prefilling and reply context UI
- **Smart Reply Protection**: Prevents accidental removal of @username when replying to comments
- **Reply Cancellation**: Cancel reply mode to return to normal commenting with clear UI indicators
- **Post Context**: Display original post content at the top of comment screens for context
- **User Information**: Show commenter avatars, usernames, and timestamps for each comment
- **Performance Optimized**: Memoized components prevent image flashing and unnecessary re-renders
- **Cross-Platform**: Consistent commenting experience across web and mobile platforms
- **Edit Indicators**: Visual indicators for edited comments with "(edited)" labels
- **Mobile Optimized**: Touch-friendly interface with proper keyboard handling and scroll behavior

### Follow System
- **Real-time Counts**: Instant follower/following count updates
- **Immediate Sidebar Updates**: Following list in sidebar updates instantly when following/unfollowing users
- **Tabbed Profile Interface**: Clean tabbed navigation on profile pages with Posts, Following (count), and Followers (count) tabs
- **Following/Followers Lists**: Detailed user lists accessible through profile tabs showing profile images, usernames, pronouns, and bio snippets
- **Profile Navigation**: Click any user in Following/Followers lists to navigate to their profile
- **User-Specific Lists**: View following/followers for any user profile, not just your own
- **Consistent Username Display**: Clean @username format throughout the application eliminating redundant username displays
- **Privacy Integration**: Following relationships affect content visibility
- **Profile Integration**: Follow/unfollow buttons on user profiles
- **Timeline Impact**: Following users affects your timeline content
- **Online Status**: Green circle indicators showing when followed users are currently online (active within 5 minutes)
- **Mobile Support**: Full Following/Followers list functionality available in mobile app with enhanced profile layout

### Follow Request Approval System
- **Optional Follow Approval**: Users can require approval for follow requests in their preferences
- **Request Workflow**: Send follow requests that show "Request Pending" until processed
- **Accept/Decline Actions**: Notification-based approval system with Accept/Decline buttons
- **Persistent Status Tracking**: Complete history of all follow requests with status (pending/approved/denied)
- **Smart Button States**: Profile buttons dynamically show "Follow", "Request to Follow", "Request Pending", or "Following"
- **Re-request Capability**: Users can send new follow requests after being denied
- **Notification Integration**: Follow requests appear in notifications with actionable buttons
- **Status Persistence**: Follow request status survives page refreshes and app restarts
- **Audit Trail**: Complete history of follow request interactions for transparency
- **Automatic Unfollowing**: Blocking users automatically removes follow relationships

### User Management & Privacy
- **User Blocking**: Block users to prevent interactions and hide content
- **Automatic Unfollowing**: Blocking automatically removes follow relationships
- **Settings Page**: Dedicated settings interface with organized sections
- **Blocklist Management**: View and unblock previously blocked users
- **Privacy Controls**: Comprehensive privacy system with relationship-based filtering
- **Profile Editing**: Complete profile management with bio, pronouns, tagline, and birthday
- **Profile Display**: Enhanced profile information display with pronouns shown inline with username
- **Dark Mode**: System-wide dark theme with user preferences stored in database and synchronized across platforms

### Image Management
- **Yap Images**: Upload images with yaps (server-side storage) with support for JPG, PNG, GIF, WebP formats
- **Profile Images**: Upload and manage profile pictures across web and mobile platforms
- **Message Images**: Send photo attachments in private messages with gallery picker integration
- **Mobile Image Support**: Full image upload functionality in React Native with automatic HEIC to JPEG conversion
- **Smart Compression**: Automatic image compression for large iPhone photos to meet 5MB API limits
- **Format Validation**: Server-side validation of image formats, file sizes, and security signatures
- **Image Serving**: Optimized image serving with proper content types and secure file access
- **File Management**: Secure image upload with comprehensive validation and error handling

### Private Messaging System
- **Direct Messages**: Send private messages between users with text and photo attachments
- **Conversation Threads**: Organized message conversations with participant management
- **Real-time Updates**: Live message updates with automatic refresh (5-second intervals)
- **Read Status Tracking**: Mark conversations as read with timestamp tracking and automatic read marking
- **Infinite Scroll**: Load message history with pagination (25 messages per page)
- **Blocking Integration**: Blocked users cannot send messages to each other
- **Message Composer**: Rich message input with photo upload and character limits
- **Unread Indicators**: Visual unread message counts on conversation list with bold text and background highlights
- **Notification Badges**: Red badge on Messages tab/link showing total count of conversations with unread messages
- **Mobile Image Support**: Send photos in mobile messages with gallery picker, preview, and validation
- **Enhanced Mobile UI**: Profile images in conversation list, automatic read marking, and visual unread indicators
- **Cross-Platform Consistency**: Unified messaging experience across web and mobile platforms

### Dark Mode System
- **Complete Theme Support**: Light and dark themes across all screens and components
- **User Preferences**: Toggle dark mode in Settings with persistent storage in database
- **Synchronized Experience**: Dark mode preference shared between web and mobile platforms
- **Tailwind CSS Integration**: Class-based dark mode with proper color schemes
- **Theme Context**: React context providers for consistent theme management
- **Automatic Persistence**: User preferences automatically saved and restored on app launch
- **Professional Design**: Carefully crafted dark color palette with proper contrast ratios
- **Smooth Transitions**: Instant theme switching with optimistic updates

### Password Recovery System
- **6-Digit Codes**: User-friendly numeric codes instead of long cryptographic tokens
- **Email Integration**: Professional HTML and text email templates with prominent code display
- **Mobile-First Design**: Optimized for easy code entry on mobile devices with numeric keypad
- **Automatic Navigation**: Seamless flow from forgot password to reset screen in mobile app
- **Secure Token Generation**: Cryptographically secure random number generation
- **Time-Limited Codes**: 1-hour expiration for security with automatic cleanup
- **Token Invalidation**: Previous codes automatically invalidated when new ones are requested
- **Cross-Platform Support**: Works on both web and mobile with consistent user experience
- **AWS SES Integration**: Reliable email delivery with professional branding
- **Error Handling**: Comprehensive error handling with user-friendly messages

## ⚡ Performance Optimizations

### Database Performance
The application includes comprehensive database performance optimizations:

- **Strategic Indexing**: Performance-optimized indexes for all critical query patterns
- **Timeline Queries**: Composite indexes for efficient post retrieval with date ordering
- **User Profiles**: Optimized indexes for user post queries with privacy filtering
- **Comment System**: Fast comment loading with PostId+CreatedAt indexing
- **Message History**: Efficient conversation message retrieval with proper indexing
- **Notification System**: Optimized notification queries with user+date composite indexes
- **Online Status**: Indexed LastSeenAt for fast online user queries
- **Following Lists**: Efficient following relationship queries with proper indexing

### Query Performance Improvements
- **10-100x faster timeline loading** with optimized date ordering
- **Efficient user profile queries** combining user filtering and date sorting
- **Optimized comment loading** for post discussions
- **Faster message pagination** in conversations
- **Improved notification performance** for real-time updates

See [Database Performance Analysis](Yapplr.Api/Database-Performance-Analysis.md) for detailed technical information.

## 🔧 Development

### Configuration Management

The project includes powerful configuration management tools for easy testing and deployment:

```bash
# Quick notification provider switching
npm run config:platform-optimized  # SignalR web + Firebase mobile (recommended)
npm run config:signalr-only        # SignalR only (great for web testing)
npm run config:firebase-only       # Firebase only (mobile-focused)
npm run config:both                # Both providers (maximum coverage)
npm run config:none                # Polling only (baseline testing)

# Check current configuration
npm run config:status

# Manual configuration script
node configure-notifications.js [option]
```

The configuration system automatically updates both frontend and backend settings, making it easy to test different notification strategies without manual file editing.

### Database Migrations
```bash
# Create new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update
```

### Environment Configuration

#### Backend (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=yapplr_db;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key",
    "Issuer": "Yapplr.Api",
    "Audience": "Yapplr.Frontend"
  }
}
```

#### Frontend (.env.local)
```env
NEXT_PUBLIC_API_URL=http://localhost:5161
```

## 🔔 Notification Configuration

Yapplr features a sophisticated platform-aware notification system that automatically selects the best notification provider for each platform.

### Platform-Optimized Strategy
- **Web/Desktop**: SignalR WebSocket notifications for instant real-time delivery
- **Mobile**: Firebase push notifications for battery-efficient delivery even when app is closed
- **Automatic Detection**: Platform detection automatically selects the optimal provider

### Quick Configuration
```bash
# Platform-optimized (recommended)
npm run config:platform-optimized

# Test individual providers
npm run config:signalr-only     # SignalR only
npm run config:firebase-only    # Firebase only
npm run config:both            # Both providers
npm run config:none            # Polling only

# Check current configuration
npm run config:status
```

### Environment Variables
```env
# Platform-Specific Configuration
NEXT_PUBLIC_ENABLE_FIREBASE_WEB=false      # Firebase for web
NEXT_PUBLIC_ENABLE_SIGNALR=true            # SignalR for web
NEXT_PUBLIC_ENABLE_FIREBASE=true           # Firebase for mobile
NEXT_PUBLIC_ENABLE_SIGNALR_MOBILE=false    # SignalR for mobile
```

### Backend Configuration
```json
{
  "NotificationProviders": {
    "Firebase": {
      "Enabled": true,
      "ProjectId": "your-firebase-project-id"
    },
    "SignalR": {
      "Enabled": true,
      "MaxConnectionsPerUser": 10
    }
  }
}
```

### Testing
Visit `/notification-test` to:
- View platform detection results
- Check active notification providers
- Send test notifications
- Monitor provider behavior

For detailed configuration options, see [PLATFORM_SPECIFIC_NOTIFICATIONS.md](PLATFORM_SPECIFIC_NOTIFICATIONS.md).

## 📚 API Documentation

### Authentication Endpoints
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login user
- `POST /api/auth/forgot-password` - Request password reset
- `POST /api/auth/reset-password` - Reset password with token

### Social Features
- `GET /api/posts/{id}` - Get individual yap by ID (for sharing)
- `GET /api/posts/timeline` - Get timeline with yaps and reyaps (paginated, 25 per page)
- `GET /api/posts/public` - Get public timeline (paginated, 25 per page)
- `GET /api/posts/user/{userId}/timeline` - Get user timeline (yaps + reyaps, paginated)
- `POST /api/posts` - Create new yap with optional image attachment
- `POST /api/posts/{id}/repost` - Reyap a yap
- `DELETE /api/posts/{id}/repost` - Remove reyap
- `POST /api/posts/{id}/like` - Like a yap
- `DELETE /api/posts/{id}` - Delete your own yap

### Image Management
- `POST /api/images/upload` - Upload image file (JPG, PNG, GIF, WebP, max 5MB)
- `GET /api/images/{fileName}` - Serve uploaded image with proper content type
- `DELETE /api/images/{fileName}` - Delete uploaded image (authorized users only)

### Comments System
- `GET /api/posts/{id}/comments` - Get all comments for a specific yap
- `POST /api/posts/{id}/comments` - Add a new comment to a yap
- `PUT /api/posts/comments/{commentId}` - Update your own comment
- `DELETE /api/posts/comments/{commentId}` - Delete your own comment

### User Management
- `GET /api/users/{username}` - Get user profile
- `PUT /api/users/me` - Update current user's profile (bio, pronouns, tagline, birthday)
- `POST /api/users/{userId}/follow` - Follow user (creates follow request if approval required)
- `DELETE /api/users/{userId}/follow` - Unfollow user
- `GET /api/users/me/following` - Get users that current user is following
- `GET /api/users/me/followers` - Get users that are following the current user
- `GET /api/users/{userId}/following` - Get users that a specific user is following
- `GET /api/users/{userId}/followers` - Get users that are following a specific user
- `GET /api/users/me/following/online-status` - Get following users with their online status
- `POST /api/users/me/profile-image` - Upload profile image

### Follow Request System
- `GET /api/users/follow-requests` - Get pending follow requests for current user
- `POST /api/users/follow-requests/{requestId}/approve` - Approve a follow request
- `POST /api/users/follow-requests/{requestId}/deny` - Deny a follow request
- `POST /api/users/follow-requests/approve-by-user/{requesterId}` - Approve follow request by requester user ID
- `POST /api/users/follow-requests/deny-by-user/{requesterId}` - Deny follow request by requester user ID

### Blocking System
- `POST /api/blocks/users/{userId}` - Block a user
- `DELETE /api/blocks/users/{userId}` - Unblock a user
- `GET /api/blocks/users/{userId}/status` - Check if user is blocked
- `GET /api/blocks` - Get list of blocked users

### Private Messaging
- `POST /api/messages` - Send new message (creates conversation if needed)
- `POST /api/messages/conversation` - Send message to existing conversation
- `GET /api/messages/conversations` - Get user's conversations (paginated, 25 per page)
- `GET /api/messages/conversations/{id}` - Get specific conversation details
- `GET /api/messages/conversations/{id}/messages` - Get messages in conversation (paginated, 25 per page)
- `POST /api/messages/conversations/{id}/read` - Mark conversation as read
- `GET /api/messages/can-message/{userId}` - Check if current user can message another user
- `POST /api/messages/conversations/with/{userId}` - Get or create conversation with user
- `GET /api/messages/unread-count` - Get total count of unread messages across all conversations

### Notifications System
- `GET /api/notifications` - Get user notifications (paginated, 25 per page)
- `GET /api/notifications/unread-count` - Get count of unread notifications
- `PUT /api/notifications/{id}/read` - Mark specific notification as read
- `PUT /api/notifications/read-all` - Mark all notifications as read for user

### User Preferences
- `GET /api/preferences` - Get current user's preferences (dark mode, etc.)
- `PUT /api/preferences` - Update user preferences with partial updates

## 🔒 Security Features

- **Password Hashing**: BCrypt with salt rounds
- **JWT Tokens**: Secure authentication with 60-minute expiration
- **Password Recovery**: Secure 6-digit code system with 1-hour expiration and token invalidation
- **Email Security**: AWS SES integration with professional email templates
- **CORS Configuration**: Properly configured for frontend development
- **Input Validation**: Comprehensive validation on all endpoints
- **Privacy Controls**: Robust privacy system with relationship-based filtering

## 🚀 Deployment

See individual README files for detailed deployment instructions:
- [Backend Deployment Guide](Yapplr.Api/Production-Deployment-Guide.md)
- [AWS SES Setup](Yapplr.Api/AWS-SES-Setup.md)

## 📄 License

This project is open source and available under the MIT License.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📞 Support

For questions or issues, please open a GitHub issue or contact the development team.
