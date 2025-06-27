# Yapplr - Twitter-like Social Media Platform

A complete Twitter-like social media platform built with modern web technologies. Features a clean, responsive design inspired by 2015 Twitter with comprehensive social features including yaps, reyaps, comments, likes, follow system, and privacy controls.

## 🚀 Features

### Core Social Features
- **Yaps**: Create text yaps (up to 256 characters) with optional images
- **Reyaps**: Reyap content with proper attribution in timeline feeds
- **Comments**: Comment on yaps with expandable comment sections
- **Likes**: Like and unlike yaps with real-time counts
- **Follow System**: Follow/unfollow users with instant UI updates
- **Following/Followers Lists**: View and navigate to profiles of users you follow and users who follow you
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

### Privacy & Security
- **Yap Privacy**: Three levels - Public (everyone), Followers (followers only), Private (author only)
- **Privacy-Aware Timeline**: Smart filtering based on user relationships and yap privacy
- **JWT Authentication**: Secure token-based authentication
- **Password Reset**: Email-based password recovery with AWS SES

### User Experience
- **Responsive Design**: Mobile-first approach with collapsible sidebar
- **Real-time Updates**: Live timeline with instant social interaction feedback
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

## 🛠 Tech Stack

### Backend (.NET 9 API)
- **.NET 9** - Minimal Web API
- **PostgreSQL** - Database with Entity Framework Core
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
- **date-fns** - Date formatting and manipulation

### Mobile App (React Native + Expo)
- **React Native** - Cross-platform mobile development
- **Expo** - Development platform and tooling
- **TypeScript** - Full type safety
- **React Navigation** - Navigation library with stack navigation
- **TanStack Query** - Data fetching (shared with web)
- **AsyncStorage** - Local data persistence
- **Expo Image Picker** - Camera and gallery integration for profile images and message attachments
- **Profile Management** - Complete profile editing with image upload functionality
- **Profile Images** - Display and upload user profile pictures across all screens
- **Message Notifications** - Real-time unread message badges and visual indicators
- **Enhanced Messaging UI** - Image attachments, read status, and conversation management
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
- **Following/Followers Lists**: Tap Following or Followers count to view and navigate to user profiles with images
- **Enhanced Messaging**: Send text and photo messages with real-time delivery and read status
- **Message Notifications**: Red badge on Messages tab showing total unread message count
- **Visual Conversation Indicators**: Bold text and background highlights for conversations with unread messages
- **Image Message Support**: Send photos in messages with gallery picker and preview functionality
- **Automatic Read Marking**: Conversations automatically marked as read when opened
- **Navigation**: Stack-based navigation with proper screen transitions
- **API Integration**: Full integration with backend API for profile updates and image uploads
- **Image Fallbacks**: Graceful fallback to user initials when no profile image is available

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

### Follow System
- **Real-time Counts**: Instant follower/following count updates
- **Following/Followers Lists**: Clickable counts that show lists of users you follow and users who follow you
- **Profile Navigation**: Tap any user in Following/Followers lists to navigate to their profile
- **Privacy Integration**: Following relationships affect content visibility
- **Profile Integration**: Follow/unfollow buttons on user profiles
- **Timeline Impact**: Following users affects your timeline content
- **Online Status**: Green circle indicators showing when followed users are currently online (active within 5 minutes)
- **Mobile Support**: Full Following/Followers list functionality available in mobile app

### User Management & Privacy
- **User Blocking**: Block users to prevent interactions and hide content
- **Automatic Unfollowing**: Blocking automatically removes follow relationships
- **Settings Page**: Dedicated settings interface with organized sections
- **Blocklist Management**: View and unblock previously blocked users
- **Privacy Controls**: Comprehensive privacy system with relationship-based filtering
- **Profile Editing**: Complete profile management with bio, pronouns, tagline, and birthday
- **Profile Display**: Enhanced profile information display with pronouns shown inline with username

### Image Management
- **Yap Images**: Upload images with yaps (server-side storage)
- **Profile Images**: Upload and manage profile pictures
- **Message Images**: Send photo attachments in private messages
- **Image Serving**: Optimized image serving with proper content types
- **File Management**: Secure image upload with validation

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

## 🔧 Development

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
- `POST /api/posts/{id}/repost` - Reyap a yap
- `DELETE /api/posts/{id}/repost` - Remove reyap
- `POST /api/posts/{id}/like` - Like a yap
- `POST /api/posts/{id}/comments` - Add comment
- `DELETE /api/posts/{id}` - Delete your own yap
- `DELETE /api/posts/comments/{commentId}` - Delete your own comment

### User Management
- `GET /api/users/{username}` - Get user profile
- `PUT /api/users/me` - Update current user's profile (bio, pronouns, tagline, birthday)
- `POST /api/users/{userId}/follow` - Follow user
- `DELETE /api/users/{userId}/follow` - Unfollow user
- `GET /api/users/me/following` - Get users that current user is following
- `GET /api/users/me/followers` - Get users that are following the current user
- `GET /api/users/me/following/online-status` - Get following users with their online status
- `POST /api/users/me/profile-image` - Upload profile image

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

## 🔒 Security Features

- **Password Hashing**: BCrypt with salt rounds
- **JWT Tokens**: Secure authentication with 60-minute expiration
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
