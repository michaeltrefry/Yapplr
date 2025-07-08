# Yapplr - Twitter-like Social Media Platform

A complete Twitter-like social media platform built with modern web technologies. Features a clean, responsive design inspired by 2015 Twitter with comprehensive social features including yaps, reyaps, comments, likes, follow system, and privacy controls.

## 🚀 Features

### Core Social Features
- **Yaps**: Create text yaps (up to 256 characters) with optional images
- **Reyaps**: Reyap content with proper attribution in timeline feeds
- **Comments**: Full-featured commenting system with dedicated comment screens, real-time count updates, auto-scroll to new comments, and reply functionality
- **Mentions**: @username mention system with clickable links and real-time notifications
- **Hashtags**: Complete #hashtag system with searchable tags, trending topics, clickable links, dedicated trending page with time periods, hashtag suggestions in post creation, and trending widgets
- **Link Previews**: Automatic visual previews for URLs in posts with title, description, images, and error handling for broken links
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
- **Real-time Notifications**: Optimized notification system with platform-specific providers
  - **Web Frontend**: SignalR WebSocket notifications for instant real-time delivery
  - **Mobile Apps**: Firebase push notifications via API for battery-efficient delivery
  - **Clean Architecture**: Frontend uses SignalR exclusively, API supports both for mobile compatibility
- **Comprehensive Notifications**: Complete notification system for mentions, likes, reposts, follows, and comments with real-time red badge indicators
- **Comment Notifications**: Instant notifications when someone comments on your posts with smart duplicate prevention (no double notifications when mentioned)
- **Follow Notifications**: Get notified when someone starts following you with direct navigation to their profile
- **Follow Request System**: Optional follow approval with request/accept/decline workflow and persistent status tracking
- **Like Notifications**: Instant notifications when someone likes your posts with navigation to the liked post
- **Repost Notifications**: Real-time alerts when someone reposts your content with direct post navigation
- **Smart Navigation**: Click notifications to navigate directly to mentioned posts, comments, or user profiles with automatic scrolling and highlighting
- **Background Notifications**: Push notifications work even when mobile apps are minimized or closed
- **Real-time Web Notifications**: Instant SignalR WebSocket-based notifications for active web sessions
- **Fallback System**: Automatic fallback between notification providers for reliability
- **Dark Mode**: Complete dark theme support with user preferences and persistent storage

### 🛡️ Admin & Moderation System
- **Comprehensive Admin Dashboard**: Full-featured admin interface with role-based access control
  - **Admin Roles**: Support for Admin and Moderator roles with different permission levels
  - **Real-time Statistics**: Live dashboard with user counts, content metrics, and moderation activity
  - **Quick Actions**: Fast access to common moderation tasks and system management
- **AI-Powered Content Moderation**: Intelligent automated content analysis and moderation
  - **Real-time Content Analysis**: Automatic analysis of all posts and comments using AI sentiment analysis
  - **Pattern-Based Detection**: Advanced pattern matching for NSFW content, violence, harassment, hate speech, and misinformation
  - **Risk Assessment**: Automated risk scoring with configurable thresholds for review and auto-hiding
  - **Smart Tag Suggestions**: AI-generated system tag recommendations with confidence scores
  - **Content Warning Detection**: Automatic detection of sensitive content requiring warnings (NSFW, violence, spoilers)
  - **Violation Classification**: Intelligent categorization of policy violations (harassment, hate speech, misinformation)
  - **Sentiment Analysis**: Real-time sentiment scoring for content tone and emotional context
  - **Moderation Queue Integration**: Flagged content automatically appears in admin moderation queue
  - **Configurable Automation**: Adjustable settings for auto-apply tags, review thresholds, and auto-hiding
- **User Management**: Complete user administration and moderation tools
  - **User Overview**: Paginated user list with filtering by status (Active, Suspended, Banned, Shadow Banned) and role
  - **User Actions**: Suspend, ban, shadow ban, and unban users with reason tracking
  - **Role Management**: Promote users to Moderator or Admin roles with audit logging
  - **Account Status**: View user details including login history, IP addresses, and account metrics
- **Content Moderation**: Advanced content management and moderation capabilities
  - **Post Management**: View, hide, unhide, and delete posts with reason tracking and bulk actions
  - **Comment Management**: Moderate comments with hide/unhide/delete functionality and bulk operations
  - **AI-Suggested Tags**: Review and approve AI-generated system tag recommendations
  - **System Tags**: Apply and manage system tags for content categorization and violation tracking
  - **Content Queue**: Review flagged content with priority-based moderation workflow and AI insights
  - **Bulk Actions**: Efficiently moderate multiple posts or comments simultaneously
- **System Tags**: Flexible content labeling and violation tracking system
  - **Tag Categories**: Organize tags by type (Violation, Warning, Information, etc.)
  - **Content Tagging**: Apply system tags to posts and comments for tracking and filtering
  - **Tag Management**: Create, edit, and manage system tags with descriptions and visibility settings
  - **AI Integration**: Automatic tag suggestions based on content analysis
- **Audit Logging**: Comprehensive activity tracking and accountability
  - **Action Logging**: Track all admin and moderator actions with timestamps and reasons
  - **User Activity**: Monitor user behavior and moderation history
  - **System Events**: Log important system events and administrative changes
  - **AI Moderation Logs**: Track AI analysis results and tag application history
- **Appeals System**: User appeal workflow for moderation decisions
  - **Appeal Submission**: Users can appeal suspensions, bans, and content removals
  - **Appeal Review**: Admins and moderators can review and respond to user appeals
  - **Appeal Tracking**: Track appeal status and resolution history
- **Analytics & Reporting**: Detailed insights into platform health and moderation effectiveness
  - **User Growth**: Track user registration, activity, and retention metrics
  - **Content Trends**: Monitor post and comment creation, engagement, and moderation patterns
  - **Moderation Stats**: Analyze moderation activity, response times, and effectiveness
  - **AI Performance**: Monitor AI moderation accuracy and effectiveness metrics
  - **System Health**: Monitor platform performance and identify potential issues

### Privacy & Security
- **Email Verification**: Required email verification at signup to prevent bot registrations
  - **6-digit verification codes** sent via SendGrid with beautiful email templates
  - **Direct verification links** for one-click email confirmation
  - **Login protection** - unverified users cannot log in
  - **Dedicated verification pages** with resend functionality for better UX
  - **Smart error handling** - redirects to verification page when unverified users try to log in
- **Yap Privacy**: Three levels - Public (everyone), Followers (followers only), Private (author only)
- **Privacy-Aware Timeline**: Smart filtering based on user relationships and yap privacy
- **JWT Authentication**: Secure token-based authentication
- **Password Reset**: Email-based password recovery with 6-digit codes and email integration

### User Experience
- **Responsive Design**: Mobile-first approach with collapsible sidebar
- **Real-time Updates**: Live timeline with instant social interaction feedback and immediate sidebar following list updates
- **Infinite Scroll**: Smooth pagination with 25 posts per page for optimal performance
- **Return to Top**: Convenient navigation button when reaching the end of timelines
- **Image Upload**: Server-side image storage for yaps and profile pictures
- **User Search**: Find users by username or bio content
- **Hashtag Search**: Search and discover hashtags with trending topics and post counts
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
- **Composite Notification System** - Multi-provider notification system with automatic fallback
- **JWT Bearer** - Authentication
- **BCrypt** - Password hashing
- **SendGrid** - Email service for verification and password reset (with AWS SES fallback)
- **File System Storage** - Image upload and serving
- **AI Content Moderation** - Python-based sentiment analysis and content moderation service

### Content Moderation Service (Python)
- **Python 3.11** - Modern Python runtime
- **Flask** - Lightweight web framework for API endpoints
- **Gunicorn** - Production WSGI server with worker processes
- **Pattern Matching** - Advanced regex-based content analysis
- **Docker** - Containerized deployment with health checks
- **RESTful API** - HTTP-based communication with main API

### Frontend (Next.js 15)
- **Next.js 15** - React framework with App Router
- **TypeScript** - Type safety
- **Tailwind CSS** - Utility-first styling
- **TanStack Query** - Data fetching and caching
- **Axios** - HTTP client
- **Lucide React** - Beautiful icons
- **SignalR Client** - Real-time WebSocket notifications
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
- **Hashtag Support**: #hashtag tagging with search, trending topics, and clickable navigation
- **Dark Mode**: Complete dark theme support with toggle in Settings and persistent user preferences
- **Shared Package** - 70-80% code reuse with web app

## 🏷️ Hashtag System

Yapplr features a comprehensive hashtag system that enables content discovery and trending topic tracking:

### Core Features
- **Automatic Hashtag Detection**: Posts automatically parse and extract #hashtag syntax
- **Clickable Hashtag Links**: All hashtags are clickable throughout the application
- **Hashtag Search**: Dedicated search functionality with post counts and trending indicators
- **Trending Algorithm**: Real-time trending hashtag calculation based on recent activity
- **Privacy-Aware**: Hashtag counts respect user privacy settings and blocking relationships

### User Interface
- **Dedicated Trending Page**: `/trending` with time period filters (Now, Today, This Week)
- **Hashtag Pages**: Individual pages for each hashtag showing all related posts
- **Search Integration**: Hashtag search tab in the main search interface
- **Trending Widget**: Sidebar widget on homepage showing top trending hashtags
- **Post Creation Suggestions**: Smart hashtag suggestions when creating posts

### Technical Implementation
- **Database Optimization**: Efficient many-to-many relationship with performance indexes
- **Real-time Updates**: Live trending calculations with configurable refresh intervals
- **Cross-Platform**: Consistent hashtag functionality across web and mobile apps
- **Analytics Ready**: Built-in analytics service for hashtag metrics and usage tracking

### API Endpoints
- `GET /api/tags/trending` - Get trending hashtags
- `GET /api/tags/search/{query}` - Search hashtags
- `GET /api/tags/tag/{tagName}` - Get specific hashtag details
- `GET /api/tags/tag/{tagName}/posts` - Get posts by hashtag
- Analytics endpoints for detailed hashtag metrics

## 🤖 AI Content Moderation System

Yapplr features an advanced AI-powered content moderation system that automatically analyzes all posts and comments for policy violations, inappropriate content, and safety concerns.

### Core Features
- **Real-time Analysis**: Every post and comment is automatically analyzed upon creation
- **Pattern-Based Detection**: Advanced regex patterns detect NSFW content, violence, harassment, hate speech, misinformation, and sensitive topics
- **Risk Assessment**: Automated risk scoring (MINIMAL, LOW, MEDIUM, HIGH) with configurable thresholds
- **Smart Tag Suggestions**: AI generates system tag recommendations with confidence scores
- **Content Classification**: Automatic categorization into Content Warnings and Violations

### Content Categories
#### Content Warnings
- **NSFW**: Adult content, explicit material, mature themes
- **Violence**: Violent content, weapons, gore, threats
- **Sensitive**: Mental health topics, trauma, self-harm discussions
- **Spoiler**: Plot spoilers, ending reveals, surprise content

#### Violations
- **Harassment**: Bullying, intimidation, personal attacks
- **Hate Speech**: Discriminatory language, slurs, prejudice
- **Misinformation**: False information, conspiracy theories, hoaxes
- **Spam**: Repetitive content, promotional spam, bot behavior

### Technical Implementation
- **Microservice Architecture**: Dedicated Python service for content analysis
- **Docker Deployment**: Containerized sentiment analysis service with health checks
- **Scalable Design**: Independent scaling of moderation service with resource limits
- **Fallback System**: Graceful degradation when moderation service is unavailable
- **Performance Optimized**: Efficient pattern matching with minimal latency impact

### Configuration Options
```json
{
  "ContentModeration": {
    "ServiceUrl": "http://content-moderation:8000",
    "Enabled": true,
    "AutoApplyTags": false,
    "RequireReviewThreshold": 0.5,
    "AutoHideThreshold": 0.8
  }
}
```

### Admin Integration
- **Moderation Queue**: Flagged content automatically appears in admin dashboard
- **AI Tag Review**: Admins can approve or reject AI-suggested tags
- **Bulk Operations**: Efficient review of multiple flagged items
- **Analytics**: Track AI performance and moderation effectiveness
- **Manual Override**: Admins can always override AI decisions

### API Endpoints
- `POST /moderate` - Analyze content for moderation (internal service)
- `GET /health` - Content moderation service health check
- Admin endpoints for reviewing AI-suggested tags and moderation decisions

## 🏃‍♂️ Quick Start

### Prerequisites
- .NET 9 SDK
- Node.js 18+
- PostgreSQL 12+
- Docker (for content moderation service)

### 1. Backend Setup

```bash
# Navigate to API directory
cd Yapplr.Api

# Install dependencies
dotnet restore

# Setup database (creates yapplr_db)
./setup-db.sh

# Start the API (migrations run automatically at startup)
dotnet run
```

The API will be available at `http://localhost:5161`

**Note**: Database migrations now run automatically when the API starts, ensuring your database is always up-to-date.

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

### 3. Content Moderation Service Setup

```bash
# Navigate to sentiment analysis directory
cd sentiment-analysis

# Build and run the Docker container
docker build -t sentiment-analysis .
docker run -d -p 8000:8000 --name sentiment-analysis-container sentiment-analysis

# Verify the service is running
curl http://localhost:8000/health
```

The content moderation service will be available at `http://localhost:8000`

**Note**: The API automatically connects to the content moderation service. If the service is unavailable, content moderation will be disabled gracefully without affecting other functionality.

### 4. Mobile App Setup (Optional)

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

### 5. Firebase Setup (Required for Real-time Notifications)

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

#### Frontend Configuration
The frontend now uses SignalR-only for notifications. Add to `yapplr-frontend/.env.local`:
```bash
NEXT_PUBLIC_API_URL=http://localhost:5161
NEXT_PUBLIC_ENABLE_SIGNALR=true
```

### 6. SendGrid Setup (Required for Email Verification & Password Reset)

SendGrid provides reliable email delivery for email verification and password reset functionality.

#### Development Setup
1. **Create SendGrid Account**: Go to [SendGrid](https://sendgrid.com/) and sign up for a free account
2. **Get API Key**:
   - Go to Settings → API Keys
   - Click "Create API Key"
   - Choose "Restricted Access" and give it a name
   - Under "Mail Send", select "Full Access"
   - Copy the API key (you won't see it again!)
3. **Verify Sender Identity**:
   - Go to Settings → Sender Authentication
   - Click "Verify a Single Sender"
   - Fill out the form with your details
   - Verify your email address

#### Configuration
Add to `Yapplr.Api/appsettings.Development.json`:
```json
{
  "SendGridSettings": {
    "ApiKey": "SG.your-api-key-here",
    "FromEmail": "your-verified-email@domain.com",
    "FromName": "Your App Name"
  },
  "EmailProvider": "SendGrid"
}
```

#### Production Deployment
For production, use GitHub secrets:
- `PROD_SENDGRID_API_KEY`: Your SendGrid API key
- `PROD_SENDGRID_FROM_EMAIL`: Your verified sender email
- `PROD_SENDGRID_FROM_NAME`: Your app name
- `PROD_EMAIL_PROVIDER`: `SendGrid`

#### Features
- **Dual Authentication**: Automatic fallback from Service Account Key to Application Default Credentials
- **Production Ready**: Service Account Key authentication for reliable deployment
- **Real-time Messaging**: Instant notifications for comments, mentions, likes, reposts, and follows
- **Smart Notifications**: Prevents duplicate notifications (e.g., no double notifications when post owner is mentioned in comments)
- **Cross-Platform**: Works on web browsers with push notification support
- **Background Notifications**: Notifications work even when the app is closed

### 7. Admin Setup (Creating Admin Users)

The platform includes comprehensive admin and moderation tools. To access the admin interface, you need to create admin users using command-line tools.

#### Creating the First Admin User
```bash
# Navigate to API directory
cd Yapplr.Api

# Create an admin user (requires API to be running)
dotnet run create-admin <username> <email> <password>

# Example:
dotnet run create-admin admin admin@yapplr.com SecurePassword123!
```

#### Promoting Existing Users
```bash
# Promote an existing user to Admin or Moderator
dotnet run promote-user <username-or-email> <role>

# Examples:
dotnet run promote-user john@example.com Admin
dotnet run promote-user moderator1 Moderator
```

#### Admin Interface Access
Once you have admin users created:
1. **Login**: Use your admin credentials to log into the web app
2. **Access Admin Panel**: Navigate to `/admin` or use the admin navigation
3. **Admin Dashboard**: View platform statistics, user metrics, and moderation queue
4. **Available Admin Pages**:
   - **Dashboard** (`/admin`) - Overview and quick actions
   - **Users** (`/admin/users`) - User management and moderation
   - **Posts** (`/admin/posts`) - Post moderation and management
   - **Comments** (`/admin/comments`) - Comment moderation
   - **Content Queue** (`/admin/queue`) - Flagged content review
   - **System Tags** (`/admin/system-tags`) - Content labeling system
   - **Appeals** (`/admin/appeals`) - User appeal management
   - **Audit Logs** (`/admin/audit-logs`) - Action tracking and accountability
   - **Analytics** (`/admin/analytics`) - Platform insights and reporting

#### Admin Features
- **Role-Based Access**: Different permissions for Admin vs Moderator roles
- **User Management**: Suspend, ban, shadow ban users with reason tracking
- **Content Moderation**: Hide, delete, and tag posts and comments
- **Bulk Actions**: Efficiently moderate multiple items simultaneously
- **Audit Logging**: Complete tracking of all administrative actions
- **Real-time Stats**: Live dashboard with platform metrics and activity

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

### Hashtag System
- **#Hashtag Tagging**: Tag posts with hashtags using #hashtag syntax with automatic detection and validation
- **Clickable Hashtags**: All #hashtag tags are automatically converted to clickable links leading to hashtag pages
- **Hashtag Search**: Search for hashtags with autocomplete and real-time results showing post counts
- **Trending Hashtags**: Algorithm-based trending hashtag detection based on recent activity and usage patterns
- **Hashtag Pages**: Dedicated pages for each hashtag showing all posts containing that tag with infinite scroll
- **Tag Analytics**: Comprehensive hashtag metrics including total posts, recent activity, and usage statistics
- **Cross-Platform Support**: Consistent hashtag experience across web and mobile platforms
- **Privacy Aware**: Hashtag searches respect post privacy settings and user blocking relationships
- **Performance Optimized**: Database indexes and efficient queries for fast hashtag operations
- **Tag Validation**: Robust hashtag validation (1-50 characters, starts with letter, alphanumeric + underscore/hyphen)
- **Case Insensitive**: All hashtags normalized to lowercase for consistent behavior
- **Real-time Updates**: Hashtag post counts update automatically when posts are created or deleted
- **Search Integration**: Hashtag search integrated into main search page with tabbed interface
- **Mobile Optimized**: Touch-friendly hashtag interaction with proper navigation in mobile apps

### Link Preview System
- **Automatic URL Detection**: Posts automatically detect and parse HTTP/HTTPS URLs with comprehensive regex patterns
- **Visual Link Previews**: Rich preview cards showing title, description, images, and site information from Open Graph metadata
- **Error Handling**: Clear warning messages for inaccessible links (404, 401, 403, timeouts, network errors)
- **Status Tracking**: Comprehensive status system (Pending, Success, NotFound, Unauthorized, Forbidden, Timeout, etc.)
- **Performance Optimization**: Link previews are cached to avoid duplicate fetching of the same URLs
- **Security Features**: URL validation, content type checking, size limits, and timeout protection
- **Cross-Platform Support**: Consistent link preview experience across web and mobile applications
- **External Image Support**: Properly configured Next.js image optimization for external preview images
- **Smart Parsing**: Extracts metadata using Open Graph tags with fallback to Twitter Card and standard HTML meta tags
- **Loading States**: Shows "Loading preview..." indicators while fetching link metadata
- **Clickable Previews**: Preview cards are clickable and open links in new tabs with proper security attributes
- **Mobile Touch Support**: Touch-friendly preview cards with proper React Native styling and Linking integration
- **Database Optimization**: Efficient link preview storage with proper indexing and relationship management
- **API Integration**: RESTful endpoints for link preview management and processing

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

### Email System (Verification & Password Recovery)
- **6-Digit Codes**: User-friendly numeric codes for both email verification and password reset
- **Email Verification**: Required at signup with 24-hour expiration and beautiful templates
- **Password Recovery**: Secure password reset with 1-hour expiration codes
- **SendGrid Integration**: Professional email delivery with beautiful HTML templates and branding
- **Mobile-First Design**: Optimized for easy code entry on mobile devices with numeric keypad
- **Automatic Navigation**: Seamless flow between verification and authentication screens
- **Secure Token Generation**: Cryptographically secure random number generation
- **Token Invalidation**: Previous codes automatically invalidated when new ones are requested
- **Cross-Platform Support**: Works on both web and mobile with consistent user experience
- **Dedicated UI Pages**: User-friendly verification required pages with resend functionality
- **Error Handling**: Comprehensive error handling with helpful user guidance

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

# Migrations run automatically at application startup
# No manual database update needed - just restart the API
dotnet run
```

**Automatic Migration System**: Database migrations now run automatically when the API starts, eliminating deployment issues and ensuring consistency across environments.

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

### Hashtag System
- `GET /api/tags/search/{query}` - Search for hashtags with optional limit parameter
- `GET /api/tags/trending` - Get trending hashtags based on recent activity
- `GET /api/tags/{tagName}` - Get specific hashtag information and post count
- `GET /api/tags/{tagName}/posts` - Get all posts containing a specific hashtag (paginated)
- `GET /api/tags/analytics/trending` - Get trending hashtags with analytics (days and limit parameters)
- `GET /api/tags/analytics/top` - Get top hashtags by total post count
- `GET /api/tags/{tagName}/analytics` - Get detailed analytics for a specific hashtag
- `GET /api/tags/{tagName}/usage` - Get hashtag usage over time (daily breakdown)

### Link Preview System
- `GET /api/link-previews?url={url}` - Get existing link preview by URL
- `POST /api/link-previews` - Create or get link preview for a specific URL
- `POST /api/link-previews/process` - Process and create link previews for URLs found in post content

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
- **Token Expiration Handling**: Automatic detection and seamless login redirection when tokens expire
- **Email Verification**: Required email verification to prevent bot registrations
- **Password Recovery**: Secure 6-digit code system with 1-hour expiration and token invalidation
- **Email Security**: SendGrid integration with professional email templates and reliable delivery
- **CORS Configuration**: Properly configured for frontend development
- **Input Validation**: Comprehensive validation on all endpoints
- **Privacy Controls**: Robust privacy system with relationship-based filtering

## 🚀 Deployment

The platform includes automated deployment scripts that handle all services including the AI content moderation system.

### Staging Deployment
```bash
# Deploy to staging environment
./deploy-stage.sh
```

### Production Deployment
```bash
# Deploy to production environment
./deploy-prod.sh
```

Both deployment scripts automatically:
- Build and deploy the .NET API
- Build and deploy the Next.js frontend
- Build and deploy the AI content moderation service
- Set up nginx with SSL termination
- Configure PostgreSQL database
- Run database migrations
- Perform health checks on all services

### Service Architecture
The deployment includes these containerized services:
- **yapplr-api**: Main .NET API service
- **yapplr-frontend**: Next.js web application
- **content-moderation**: Python AI moderation service
- **postgres**: PostgreSQL database
- **nginx**: Reverse proxy with SSL termination

See individual README files for detailed deployment instructions:
- [Backend Deployment Guide](Yapplr.Api/Production-Deployment-Guide.md)
- [SendGrid Email Setup](#6-sendgrid-setup-required-for-email-verification--password-reset)

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
