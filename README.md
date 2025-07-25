# Yapplr - Twitter-like Social Media Platform

A complete Twitter-like social media platform built with modern web technologies. Features a clean, responsive design inspired by 2015 Twitter with comprehensive social features including yaps, reyaps, comments, likes, follow system, and privacy controls.

## 🚀 Features

### Core Social Features
- **Yaps**: Create text yaps (up to 256 characters) with optional images and videos
- **Multi-Media Posts**: Upload up to 10 photos and videos per post with comprehensive media gallery support
- **Photo Library**: Dedicated photo library showing all user images with slideshow navigation and theme-aware full-screen viewing
- **Reyaps**: Reyap content with proper attribution in timeline feeds
- **Comments**: Full-featured commenting system with dedicated comment screens, real-time count updates, auto-scroll to new comments, and reply functionality
- **Emoji Reactions**: Rich reaction system with 6 emoji types (❤️ Heart, 👍 Like, 😂 Laugh, 😮 Surprised, 😢 Sad, 😡 Angry) for posts and comments
- **Interactive Reaction Picker**: Hover-based emoji selection with smooth transitions from grayscale to color, colored SVG icons on hover, and background circle animations
- **Reaction Visual Effects**: Emojis start in grayscale and transition to full color on hover with colored background circles and scale animations
- **Reaction Button Priority**: Reaction button positioned first (leftmost) in action buttons for enhanced user engagement
- **Reaction Counts Display**: Visual reaction count badges positioned between post content and actions to prevent layout shifts
- **Colored Reaction States**: Selected reactions show colored SVG icons instead of filled black versions for clear visual feedback
- **Reaction Analytics**: View detailed reaction breakdowns showing count per emoji type with pill-shaped badges
- **Comment Reactions**: Full emoji reaction support for comments with same rich interaction as posts
- **Legacy Compatibility**: Existing likes automatically converted to heart reactions during migration
- **Consistent Reaction Layout**: Unified reaction experience across PostCard, FullScreenPhotoViewer, and FullScreenVideoViewer
- **Mentions**: @username mention system with clickable links and real-time notifications
- **Hashtags**: Complete #hashtag system with searchable tags, trending topics, clickable links, dedicated trending page with time periods, hashtag suggestions in post creation, and trending widgets
- **Link Previews**: Automatic visual previews for URLs in posts with title, description, images, and error handling for broken links
- **Comment Replies**: Reply to specific comments with automatic @username prefilling and smart reply context
- **Follow System**: Follow/unfollow users with instant UI updates and optional follow approval system
- **Following/Followers Lists**: Tabbed interface on profile pages showing Posts, Following (count), and Followers (count) with detailed user lists and navigation to their profiles
- **User Profiles**: Complete profile management with bio, pronouns, tagline, birthday, and profile images
- **Profile Editing**: Edit profile information including bio, pronouns, tagline, and birthday with real-time updates
- **Yap Sharing**: Share yaps with social media integration and direct link copying
- **Content Management**: Delete your own yaps, comments, and reyaps with confirmation dialogs
- **User Blocking**: Block/unblock users with automatic unfollowing and content filtering
- **Settings Management**: Dedicated settings page with blocklist management
- **Private Messaging**: Send direct messages with text, photo, and video attachments
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
- **Reaction Notifications**: Instant notifications when someone reacts to your posts or comments with emoji type display
- **Repost Notifications**: Real-time alerts when someone reposts your content with direct post navigation
- **Smart Navigation**: Click notifications to navigate directly to mentioned posts, comments, or user profiles with automatic scrolling and highlighting
- **Background Notifications**: Push notifications work even when mobile apps are minimized or closed
- **Real-time Web Notifications**: Instant SignalR WebSocket-based notifications for active web sessions
- **Fallback System**: Automatic fallback between notification providers for reliability
- **Dark Mode**: Complete dark theme support with user preferences and persistent storage

### 👥 Groups & Communities
- **Open Groups**: Create and join public groups where anyone can participate
- **Group Management**: Create groups with custom names, descriptions, and images
- **Group Discovery**: Search and browse all available groups with pagination
- **Member Management**: View group members with join dates and roles (Member, Moderator, Admin)
- **Group Posts**: Post content directly in groups with automatic public visibility
- **Group Media Support**: Full image and video support in group posts with proper URL generation
- **Public Group Content**: All posts in groups are public regardless of user privacy settings
- **Group Ownership**: Group creators become owners with full management permissions
- **Easy Joining**: One-click join/leave functionality for all groups
- **Group Navigation**: Dedicated group pages showing posts, members, and group information
- **User Group Lists**: View all groups a user is a member of
- **Group Statistics**: Real-time member counts and post counts for each group
- **Group Validation**: Automatic validation of group membership before posting
- **Unique Group Names**: Enforced unique group names across the platform
- **RESTful Group API**: Complete REST API with 12 endpoints for all group operations

### 🛡️ Admin & Moderation System
- **Comprehensive Admin Dashboard**: Full-featured admin interface with role-based access control
  - **Admin Roles**: Support for Admin and Moderator roles with different permission levels
  - **Real-time Statistics**: Live dashboard with user counts, content metrics, and moderation activity
  - **Quick Actions**: Fast access to common moderation tasks and system management
- **🎯 User Trust Score System**: Advanced behavioral scoring system for intelligent moderation
  - **Dynamic Trust Calculation**: Real-time trust scores based on user behavior, activity, and community standing
  - **Trust-Based Moderation**: Automatic content visibility and rate limiting based on user trust levels
  - **Smart Rate Limiting**: Dynamic rate limits (0.25x to 2x) based on user trust scores
  - **Auto-Hide Low Trust Content**: Content from very low-trust users (< 0.1) automatically hidden
  - **Moderation Priority Scoring**: 1-5 priority levels for efficient moderation queue management
  - **Trust-Based Permissions**: Action thresholds for posting, messaging, and reporting based on trust
  - **Comprehensive Analytics**: Trust score statistics, distribution analysis, and trend tracking
  - **Admin Trust Management**: Manual trust score adjustments with full audit trail
  - **Background Maintenance**: Automated trust score recalculation and inactivity decay
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
  - **Comprehensive User Details**: Click any username to view detailed user information including:
    - **Profile Information**: Complete user profile data, email verification status, member since date, last login details
    - **Trust Score Analysis**: Visual trust score display with factors breakdown and recent history
    - **Rate Limiting Management**: Configure user-specific rate limiting overrides and view current status
    - **Activity Statistics**: Comprehensive metrics including posts, comments, likes, followers, reports, and moderation actions
    - **Moderation History**: Recent administrative actions taken against the user with full audit trail
    - **Role Management**: Inline role editing with reason tracking and audit logging
    - **User Actions**: Suspend, ban, shadow ban, and unban users with comprehensive action buttons
  - **Rate Limiting Overrides**: Disable or configure rate limiting settings for individual users
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

### 💳 Subscription System & Payment Gateways
- **Tiered Subscription Model**: Flexible subscription system with multiple tiers and pricing options
  - **Subscription Tiers**: Create and manage multiple subscription tiers with different features and pricing
  - **Feature Flags**: Control access to features like verified badges, ad-free experience, and premium content
  - **Billing Cycles**: Support for monthly, yearly, and custom billing periods
  - **Trial Periods**: Configurable trial periods for new subscribers with automatic conversion
  - **Subscription Management**: Users can upgrade, downgrade, and cancel subscriptions
  - **Grace Periods**: Configurable grace periods for failed payments before suspension
- **Dynamic Payment Gateway Configuration**: Admin-configurable payment providers without code changes
  - **Multiple Providers**: Support for PayPal, Stripe, and extensible architecture for additional providers
  - **Real-time Provider Switching**: Change payment providers instantly without application restarts
  - **Provider Priority**: Configure fallback order for payment provider selection
  - **Environment Management**: Separate sandbox/test and production configurations
  - **Secure Settings Storage**: Encrypted storage of sensitive payment credentials in database
  - **Provider Health Monitoring**: Real-time status monitoring and automatic failover
- **Comprehensive Payment Features**:
  - **Subscription Webhooks**: Automatic processing of payment provider webhooks for real-time updates
  - **Payment Retry Logic**: Intelligent retry mechanisms for failed payments with exponential backoff
  - **Proration Support**: Automatic proration for subscription upgrades and downgrades
  - **Payment Analytics**: Detailed analytics and reporting for subscription metrics and revenue
  - **Admin Payment Dashboard**: Complete payment management interface with transaction history
  - **Subscription Analytics**: Revenue tracking, churn analysis, and subscription lifecycle metrics

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
- **Media Upload**: Server-side storage for images and videos in yaps, messages, and profile pictures
- **Multi-Media Upload**: Upload up to 10 photos and videos per post with drag-and-drop support and file validation
- **Photo Library**: Comprehensive photo gallery showing all user images with slideshow navigation, keyboard controls, and theme-aware backgrounds
- **Video Processing**: Automatic video processing with FFmpeg including format conversion, compression, and thumbnail generation
- **iPhone Photo Library Support**: Unified media picker supporting both photos and videos from iPhone photo library via Safari
- **User Search**: Find users by username or bio content
- **Hashtag Search**: Search and discover hashtags with trending topics and post counts
- **Clean UI**: 2015 Twitter-inspired design with modern touches
- **Individual Yap Pages**: Dedicated URLs for each yap with shareable links
- **Share Modal**: Popup sharing interface with social media integration (Twitter, Facebook, LinkedIn, Reddit)
- **Content Control**: Delete buttons with confirmation dialogs for your own content
- **Optimized Images**: Next.js Image component for better performance and loading
- **Real-time Messaging**: Live message updates with automatic conversation refresh
- **Message Attachments**: Send photos and videos in messages with preview and validation
- **Conversation Management**: Infinite scroll message history with unread count indicators
- **Message Notifications**: Red badge indicators showing unread message counts on tabs and conversation lists
- **Visual Read Status**: Bold text and background highlights for conversations with unread messages
- **Dark Mode**: Complete dark theme with toggle in Settings, synchronized across all platforms
- **Database Performance**: Optimized with strategic indexing for fast timeline, profile, and messaging queries
- **Redis Caching**: High-performance count caching for followers, posts, likes, comments, and notifications with automatic fallback

### 📸 Photo Library & Multi-Media Features
- **Multi-Media Posts**: Upload up to 10 photos and videos per post with comprehensive media management
  - **Drag & Drop Upload**: Intuitive drag-and-drop interface for easy media selection
  - **File Validation**: Automatic format detection and size validation with user-friendly error messages
  - **Mixed Media Support**: Combine photos and videos in a single post
  - **Upload Progress**: Real-time upload progress indicators with cancel functionality
- **Photo Library**: Dedicated photo gallery for browsing all user images
  - **Grid Layout**: Clean 3-column grid displaying all photos from user's posts
  - **Slideshow Navigation**: Full-screen photo viewing with left/right arrow navigation
  - **Keyboard Controls**: Navigate photos using arrow keys (← → for navigation, Escape to close)
  - **Photo Counter**: Shows current position (e.g., "3 of 15") with navigation hints
  - **Theme-Aware Backgrounds**: Proper light/dark mode support for full-screen viewing
  - **Smooth Transitions**: Enhanced hover effects and animations for better user experience
- **Media Gallery**: Enhanced media viewing for posts with multiple images/videos
  - **Thumbnail Grid**: Visual grid layout for posts with multiple media items
  - **Full-Screen Viewer**: Click any image to view in full-screen with navigation controls
  - **Video Support**: Integrated video playback with standard HTML5 controls
  - **Cross-Platform**: Consistent media experience across web and mobile platforms

### 🎥 Video Features
- **Video Upload Support**: Upload videos in posts and messages with comprehensive format support
  - **Supported Formats**: MP4, MOV, AVI, WMV, FLV, WebM, MKV
  - **File Size Limits**: Up to 100MB for videos, 5MB for images
  - **iPhone Compatibility**: Native support for iPhone video formats (MOV, MP4)
- **Unified Media Picker**: Single interface for selecting both photos and videos
  - **Web Frontend**: iPhone Safari optimized file picker with photo library access
  - **Mobile App**: Native photo library integration with expo-image-picker
  - **File Validation**: Automatic format detection and size validation with user-friendly error messages
- **Video Processing Pipeline**: Automatic server-side video processing with FFmpeg
  - **Format Conversion**: Standardize videos to web-compatible formats
  - **Compression**: Optimize file sizes while maintaining quality
  - **Thumbnail Generation**: Automatic video thumbnail creation for previews
  - **Processing Status**: Real-time status updates (Pending → Processing → Completed/Failed)
- **Video Display**: Optimized video playback with controls and thumbnails
  - **Video Player**: HTML5 video player with standard controls
  - **Thumbnail Previews**: Show video thumbnails while processing
  - **Processing Indicators**: Visual feedback during video processing
  - **Error Handling**: Graceful handling of processing failures with retry options
- **Cross-Platform Consistency**: Same video features across web and mobile platforms

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
- **File System Storage** - Image and video upload and serving with multi-media support
- **Multi-Media Processing** - Support for up to 10 photos/videos per post with PostMedia entity relationships
- **Groups & Communities** - Complete group system with membership management and public group posts
- **Video Processing Service** - FFmpeg-based video processing with RabbitMQ messaging
- **RabbitMQ** - Message queue for asynchronous video processing tasks
- **Redis** - High-performance caching for count operations and real-time data
- **AI Content Moderation** - Python-based sentiment analysis and content moderation service
- **Payment Gateway System** - Multi-provider payment processing with dynamic configuration
  - **PayPal Integration** - Complete PayPal subscriptions API with webhook processing
  - **Stripe Integration** - Full Stripe payment processing with advanced features
  - **Dynamic Configuration** - Database-driven payment provider configuration with admin UI
  - **Subscription Management** - Complete subscription lifecycle management with trials and grace periods
  - **Payment Analytics** - Revenue tracking, subscription metrics, and payment analytics
  - **Webhook Processing** - Secure webhook handling with signature verification and retry logic

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
- **Multi-Media Components** - PhotoGrid, MediaGallery, and FullScreenPhotoViewer for comprehensive media handling
- **Subscription System UI** - SubscriptionTierBadge, payment configuration interface, and subscription management components
- **Admin Payment Interface** - Complete payment provider configuration and subscription tier management UI

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
- **Content Reporting**: Report objectionable posts and comments with system tag categorization and detailed reasons
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

## 📸 Photo Library & Multi-Media API

Yapplr provides comprehensive API endpoints for multi-media post creation and photo library management.

### Core Features
- **Multi-Media Posts**: Support for up to 10 photos and videos per post
- **Photo Library**: Dedicated endpoints for retrieving user's photo collections
- **Media Management**: Complete CRUD operations for post media items
- **File Upload**: Secure file upload with validation and processing

### API Endpoints

#### Multi-Media Post Creation
- `POST /api/posts` - Create post with multiple media items (up to 10 photos/videos)
- `POST /api/posts/upload-media` - Upload media files with validation and processing
- `GET /api/posts/{id}/media` - Get all media items for a specific post
- `DELETE /api/posts/{id}/media/{mediaId}` - Remove specific media item from post

#### Photo Library
- `GET /api/posts/user/{userId}/photos` - Get user's photo library with pagination
- `GET /api/posts/user/{userId}/photos?page={page}&limit={limit}` - Paginated photo retrieval
- Photo library automatically filters for image media types and respects privacy settings

#### Media Processing
- `GET /api/media/{id}/status` - Check media processing status (Pending, Processing, Completed, Failed)
- `GET /api/media/{id}/thumbnail` - Get video thumbnail or image preview
- Media files are automatically processed and optimized for web delivery

### Technical Implementation
- **PostMedia Entity**: One-to-many relationship between Post and PostMedia for flexible media handling
- **Media Types**: Support for Image and Video media types with extensible enum structure
- **File Validation**: Automatic format detection, size limits (5MB images, 100MB videos)
- **Privacy Aware**: All photo library queries respect post privacy settings and user relationships
- **Performance Optimized**: Efficient database queries with proper indexing for fast photo retrieval

## 👥 Groups & Communities API

Yapplr provides comprehensive group functionality allowing users to create, join, and participate in public communities.

### Core Features
- **Open Groups**: All groups are public and open for anyone to join
- **Group Management**: Create, update, and delete groups with proper ownership controls
- **Member Management**: Join/leave groups with automatic membership tracking
- **Group Posts**: Post content directly in groups with automatic public visibility
- **Search & Discovery**: Full-text search across group names and descriptions
- **Member Roles**: Support for Member, Moderator, and Admin roles (extensible for future features)

### API Endpoints

#### Group Management
- `POST /api/groups` - Create a new group (requires authentication)
- `GET /api/groups` - Get all groups (paginated)
- `GET /api/groups/search?query={query}` - Search groups by name or description
- `GET /api/groups/{id}` - Get group details by ID
- `GET /api/groups/name/{name}` - Get group details by name
- `PUT /api/groups/{id}` - Update group (owner only)
- `DELETE /api/groups/{id}` - Delete group (owner only)

#### Group Membership
- `POST /api/groups/{id}/join` - Join a group
- `POST /api/groups/{id}/leave` - Leave a group (owners cannot leave)
- `GET /api/groups/{id}/members` - Get group members (paginated)
- `GET /api/groups/me` - Get current user's groups
- `GET /api/groups/user/{userId}` - Get groups for a specific user

#### Group Content
- `GET /api/groups/{id}/posts` - Get posts in a group (paginated)
- `POST /api/posts` - Create post (with optional `groupId` for group posts)

### Technical Implementation
- **Database Schema**: Groups, GroupMembers, and Posts tables with proper foreign key relationships
- **Unique Constraints**: Group names must be unique across the platform
- **Cascade Deletes**: Proper cleanup when groups or users are deleted
- **Performance Indexes**: Optimized queries for group discovery, membership, and content retrieval
- **Public Content**: All group posts are automatically public regardless of user privacy settings
- **Membership Validation**: Users must be group members to post in groups
- **Role-Based Permissions**: Group owners have full management rights

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

## 🎯 User Trust Score System

Yapplr features an advanced user trust score system that provides intelligent, behavior-based moderation and content management. The system automatically calculates trust scores for all users based on their activity, behavior, and community standing.

### Core Features
- **Dynamic Trust Calculation**: Real-time trust scores (0.0-1.0) based on user behavior and activity patterns
- **Behavioral Analysis**: Considers profile completeness, posting activity, community engagement, and moderation history
- **Trust-Based Moderation**: Automatic content visibility adjustments and rate limiting based on user trust levels
- **Smart API Rate Limiting**: Dynamic API rate limits from 0.25x (low trust) to 2x (high trust) normal limits across all endpoints
- **Auto-Hide Protection**: Content from very low-trust users (< 0.1) automatically hidden from feeds
- **Moderation Priority**: 1-5 priority levels for efficient moderation queue management

### Trust Score Factors
#### Positive Factors (Increase Trust)
- **Profile Completeness**: Bio, profile image, email verification (+0.1 to +0.3)
- **Positive Activity**: Creating posts, engaging with content (+0.05 to +0.15 per action)
- **Community Standing**: Receiving likes, follows, positive interactions (+0.02 to +0.1)
- **Account Age**: Established accounts with consistent activity (+0.05 to +0.2)

#### Negative Factors (Decrease Trust)
- **Moderation Actions**: Content hidden, user suspended, policy violations (-0.1 to -0.5)
- **Reported Content**: Posts/comments reported by other users (-0.05 to -0.2)
- **Spam Behavior**: Excessive posting, repetitive content (-0.1 to -0.3)
- **Inactivity Decay**: Gradual score reduction for inactive accounts (-0.01 per week)

### Trust-Based Features
#### Content Visibility Levels
- **Hidden** (< 0.1): Content hidden from all feeds and searches
- **Limited** (0.1-0.3): Reduced visibility, requires user action to view
- **Reduced** (0.3-0.5): Lower priority in feeds, limited reach
- **Normal** (0.5-0.8): Standard visibility and engagement
- **Full** (0.8+): Maximum visibility and engagement opportunities

#### Action Thresholds
- **Create Posts**: 0.1 minimum trust score
- **Create Comments**: 0.1 minimum trust score
- **Like Content**: 0.05 minimum trust score
- **Report Content**: 0.2 minimum trust score
- **Send Messages**: 0.3 minimum trust score

#### Rate Limiting Multipliers
- **Very Low Trust** (< 0.2): 0.25x normal rate limits
- **Low Trust** (0.2-0.4): 0.5x normal rate limits
- **Medium Trust** (0.4-0.6): 1.0x normal rate limits
- **High Trust** (0.6-0.8): 1.5x normal rate limits
- **Very High Trust** (0.8+): 2.0x normal rate limits

### Technical Implementation
- **Background Service**: Automated trust score recalculation and maintenance
- **Real-time Updates**: Trust scores update immediately based on user actions
- **Audit Trail**: Complete history of trust score changes with detailed metadata
- **Safe Defaults**: System defaults to allowing actions if trust score calculation fails
- **Performance Optimized**: Efficient database queries with proper indexing
- **Admin Override**: Administrators can manually adjust trust scores with reason tracking

### Admin Features
- **Trust Score Dashboard**: View platform-wide trust score statistics and distribution
- **User Trust Management**: View individual user trust scores and adjustment history
- **Manual Adjustments**: Manually increase or decrease user trust scores with reason tracking
- **Trust Score Analytics**: Detailed breakdown of factors affecting user trust scores
- **Moderation Integration**: Trust scores automatically factor into moderation decisions

### Configuration Options
```json
{
  "TrustScore": {
    "EnableBackgroundService": true,
    "RecalculationIntervalMinutes": 60,
    "InactivityDecayDays": 7,
    "DefaultNewUserScore": 1.0,
    "MinimumActionThresholds": {
      "CreatePost": 0.1,
      "CreateComment": 0.1,
      "LikeContent": 0.05,
      "ReportContent": 0.2,
      "SendMessage": 0.3
    }
  }
}
```

### API Endpoints
- `GET /api/admin/trust-scores/` - Get all user trust scores with filtering
- `GET /api/admin/trust-scores/{userId}` - Get specific user trust score
- `GET /api/admin/trust-scores/{userId}/history` - Get trust score change history
- `GET /api/admin/trust-scores/{userId}/factors` - Get trust score factor breakdown
- `PUT /api/admin/trust-scores/{userId}` - Manually adjust user trust score
- `GET /api/admin/trust-scores/statistics` - Get platform trust score statistics

## 💳 Payment Gateway Configuration & Subscription System

Yapplr features a comprehensive payment system with dynamic gateway configuration and flexible subscription management.

### Core Features
- **Dynamic Payment Provider Configuration**: Configure payment providers through admin interface without code changes
- **Multi-Provider Support**: PayPal, Stripe, and extensible architecture for additional providers
- **Real-time Configuration Updates**: Change payment settings instantly without application restarts
- **Secure Credential Storage**: Encrypted storage of sensitive payment provider credentials
- **Subscription Lifecycle Management**: Complete subscription creation, management, and cancellation workflows
- **Trial Period Support**: Configurable trial periods with automatic conversion to paid subscriptions
- **Payment Analytics**: Comprehensive analytics and reporting for subscription metrics and revenue tracking

### Subscription Tiers
- **Flexible Tier Management**: Create unlimited subscription tiers with custom pricing and features
- **Feature Flags**: Control access to premium features like verified badges and ad-free experience
- **Billing Cycles**: Support for monthly, yearly, and custom billing periods
- **Tier Comparison**: Visual tier comparison interface for users to choose the best plan
- **Automatic Tier Assignment**: Seamless tier assignment upon successful payment

### Payment Provider Configuration
#### Admin Interface Features
- **Provider Management**: Enable/disable payment providers with priority ordering
- **Environment Configuration**: Separate sandbox/test and production environment settings
- **Credential Management**: Secure storage and management of API keys, secrets, and webhooks
- **Health Monitoring**: Real-time provider status monitoring with automatic failover
- **Test Connectivity**: Built-in provider connectivity testing and validation

#### Supported Providers
- **PayPal**: Complete PayPal subscriptions API integration with webhook processing
- **Stripe**: Full Stripe payment processing with advanced subscription features
- **Extensible Architecture**: Easy integration of additional payment providers

### API Endpoints

#### Subscription Management
- `GET /api/subscriptions/tiers` - Get all active subscription tiers
- `GET /api/subscriptions/tiers/{id}` - Get specific subscription tier details
- `GET /api/subscriptions/my-subscription` - Get current user's subscription
- `POST /api/subscriptions/assign-tier` - Assign subscription tier to user
- `DELETE /api/subscriptions/remove-subscription` - Remove user's subscription

#### Payment Processing
- `GET /api/payments/providers` - Get available payment providers
- `POST /api/payments/subscriptions` - Create new subscription with payment
- `GET /api/payments/subscriptions/current` - Get current user's subscription details
- `POST /api/payments/subscriptions/cancel` - Cancel current subscription
- `POST /api/payments/webhooks/{provider}` - Process payment provider webhooks

#### Admin Configuration
- `GET /api/admin/payment-configuration/summary` - Get payment configuration overview
- `GET /api/admin/payment-configuration/providers` - Get all payment provider configurations
- `POST /api/admin/payment-configuration/providers` - Create new payment provider configuration
- `PUT /api/admin/payment-configuration/providers/{id}` - Update payment provider configuration
- `DELETE /api/admin/payment-configuration/providers/{id}` - Delete payment provider configuration
- `POST /api/admin/payment-configuration/providers/{id}/test` - Test payment provider connectivity
- `GET /api/admin/payment-configuration/global` - Get global payment configuration
- `PUT /api/admin/payment-configuration/global` - Update global payment configuration

#### Admin Subscription Management
- `GET /api/admin/subscriptions/tiers` - Get all subscription tiers (including inactive)
- `POST /api/admin/subscriptions/tiers` - Create new subscription tier
- `PUT /api/admin/subscriptions/tiers/{id}` - Update subscription tier
- `DELETE /api/admin/subscriptions/tiers/{id}` - Delete subscription tier
- `GET /api/admin/subscriptions/users` - Get user subscriptions with filtering
- `GET /api/admin/subscriptions/analytics` - Get subscription analytics and metrics

### Technical Implementation
- **Database-Driven Configuration**: All payment settings stored in database with encryption for sensitive data
- **Provider Abstraction**: Clean abstraction layer allowing easy addition of new payment providers
- **Webhook Security**: Automatic signature verification and replay attack prevention
- **Retry Logic**: Intelligent retry mechanisms for failed payments with exponential backoff
- **Audit Trail**: Complete audit logging for all payment configuration changes and transactions
- **Performance Optimized**: Efficient database queries with proper indexing for payment operations

### Configuration Options
```json
{
  "PaymentSystem": {
    "EnableSubscriptions": true,
    "DefaultTrialDays": 14,
    "GracePeriodDays": 7,
    "MaxRetryAttempts": 3,
    "RetryIntervalDays": 3,
    "EnableProration": true,
    "WebhookTimeoutSeconds": 10,
    "VerifyWebhookSignatures": true
  }
}
```

## 🚦 Smart API Rate Limiting

Yapplr implements intelligent API rate limiting that dynamically adjusts based on user trust scores, providing a fair and secure experience for all users while preventing abuse.

### Core Features
- **Trust-Based Rate Limiting**: Dynamic rate limits that scale with user trust scores (0.25x to 2x normal limits)
- **Operation-Specific Limits**: Different rate limits for different types of API operations
- **Burst Protection**: Short-term burst detection to prevent rapid-fire requests
- **Automatic Blocking**: Users with excessive violations are temporarily blocked
- **Comprehensive Monitoring**: Real-time statistics and violation tracking

### Rate Limit Configuration

#### Base Rate Limits (per operation type)
- **Create Post**: 5/minute, 50/hour, 500/day (burst: 3)
- **Create Comment**: 10/minute, 100/hour, 1000/day (burst: 5)
- **Like/Unlike Post**: 30/minute, 300/hour, 3000/day (burst: 15)
- **Follow/Unfollow User**: 5-10/minute, 50-100/hour, 500-1000/day (burst: 3-5)
- **Report Content**: 2/minute, 20/hour, 100/day (burst: 1)
- **Send Message**: 10/minute, 100/hour, 1000/day (burst: 5)
- **Upload Media**: 3/minute, 30/hour, 200/day (burst: 2)
- **Search**: 20/minute, 200/hour, 2000/day (burst: 10)
- **General Operations**: 60/minute, 600/hour, 6000/day (burst: 30)

#### Trust Score Multipliers
- **Very Low Trust** (< 0.2): 0.25x rate limits (quarter normal limits)
- **Low Trust** (0.2-0.4): 0.5x rate limits (half normal limits)
- **Medium Trust** (0.4-0.6): 1.0x rate limits (normal limits)
- **High Trust** (0.6-0.8): 1.5x rate limits (50% more requests)
- **Very High Trust** (0.8+): 2.0x rate limits (double normal limits)

### Violation Handling
- **Burst Violations**: 10-second cooldown period
- **Rate Limit Violations**: Retry-after headers with appropriate wait times
- **Auto-Blocking**: Users with 15+ violations in 24 hours are blocked for 2 hours
- **Progressive Penalties**: Repeated violations result in longer blocking periods

### Exempt Endpoints
The following endpoints are exempt from rate limiting:
- Authentication endpoints (`/api/auth/*`)
- Health checks (`/api/health`)
- User profile retrieval (`/api/users/me`)
- Rate limit status endpoints (`/api/security/rate-limits/*`)

### Response Headers
Rate-limited responses include helpful headers:
- `X-RateLimit-Remaining`: Number of requests remaining in current window
- `X-RateLimit-Reset`: Unix timestamp when rate limit resets
- `X-RateLimit-Type`: Indicates "trust-based" rate limiting
- `Retry-After`: Seconds to wait before retrying (when rate limited)

### Configuration Options

#### System-Wide Configuration
Rate limiting can be configured globally via `appsettings.json`:

```json
{
  "RateLimiting": {
    "Enabled": true,
    "TrustBasedEnabled": true,
    "BurstProtectionEnabled": true,
    "AutoBlockingEnabled": true,
    "AutoBlockViolationThreshold": 15,
    "AutoBlockDurationHours": 2,
    "ApplyToAdmins": false,
    "ApplyToModerators": false,
    "FallbackMultiplier": 1.0
  }
}
```

**Configuration Options:**
- `Enabled`: Global enable/disable flag for all rate limiting
- `TrustBasedEnabled`: Enable/disable trust score multipliers
- `BurstProtectionEnabled`: Enable/disable burst protection
- `AutoBlockingEnabled`: Enable/disable automatic user blocking
- `AutoBlockViolationThreshold`: Number of violations before auto-blocking (default: 15)
- `AutoBlockDurationHours`: Duration in hours for auto-blocking (default: 2)
- `ApplyToAdmins`: Whether to apply rate limits to admin users (default: false)
- `ApplyToModerators`: Whether to apply rate limits to moderators (default: false)
- `FallbackMultiplier`: Rate limit multiplier when trust-based is disabled (default: 1.0)

#### Per-User Configuration
Individual users can have rate limiting overrides:
- **Rate Limiting Enabled**: Override global setting for specific user
- **Trust-Based Enabled**: Override trust-based rate limiting for specific user
- **Manual Blocking**: Admins can manually block/unblock users from API access

#### Admin Controls
Administrators can manage rate limiting through the admin interface:
- View and modify system-wide rate limiting configuration
- View per-user rate limiting settings and violations
- Enable/disable rate limiting for individual users
- Manually block/unblock users from API access
- Reset user rate limits and violation history
- View comprehensive rate limiting statistics

### Technical Implementation
- **Middleware-Based**: Transparent rate limiting applied at the middleware level
- **Trust Integration**: Seamlessly integrates with the existing trust score system
- **Memory-Efficient**: In-memory tracking with automatic cleanup of old data
- **Production-Ready**: Designed for Redis or distributed cache in production environments
- **Highly Configurable**: System-wide and per-user configuration options

### API Endpoints

#### User Endpoints
- `GET /api/security/rate-limits/violations` - Get current user's rate limit violations
- `GET /api/security/rate-limits/stats` - Get platform-wide rate limiting statistics

#### Admin Configuration Endpoints
- `GET /api/security/admin/rate-limits/config` - Get current rate limiting configuration
- `PUT /api/security/admin/rate-limits/config` - Update rate limiting configuration
- `GET /api/security/admin/rate-limits/stats` - Get comprehensive rate limiting statistics

#### Admin User Management Endpoints
- `GET /api/security/admin/rate-limits/users/{userId}` - Get user's rate limiting settings
- `PUT /api/security/admin/rate-limits/users/{userId}` - Update user's rate limiting settings
- `DELETE /api/security/admin/rate-limits/users/{userId}/reset` - Reset user's rate limits and violations
- `POST /api/security/admin/rate-limits/users/{userId}/block` - Block user from API access
- `DELETE /api/security/admin/rate-limits/users/{userId}/block` - Unblock user from API access

## 💳 Payment Gateway Configuration & Subscription System

Yapplr features a comprehensive payment system with dynamic gateway configuration and flexible subscription management.

### Core Features
- **Dynamic Payment Provider Configuration**: Configure payment providers through admin interface without code changes
- **Multi-Provider Support**: PayPal, Stripe, and extensible architecture for additional providers
- **Real-time Configuration Updates**: Change payment settings instantly without application restarts
- **Secure Credential Storage**: Encrypted storage of sensitive payment provider credentials
- **Subscription Lifecycle Management**: Complete subscription creation, management, and cancellation workflows
- **Trial Period Support**: Configurable trial periods with automatic conversion to paid subscriptions
- **Payment Analytics**: Comprehensive analytics and reporting for subscription metrics and revenue tracking

### Subscription Tiers
- **Flexible Tier Management**: Create unlimited subscription tiers with custom pricing and features
- **Feature Flags**: Control access to premium features like verified badges and ad-free experience
- **Billing Cycles**: Support for monthly, yearly, and custom billing periods
- **Tier Comparison**: Visual tier comparison interface for users to choose the best plan
- **Automatic Tier Assignment**: Seamless tier assignment upon successful payment

### Payment Provider Configuration
#### Admin Interface Features
- **Provider Management**: Enable/disable payment providers with priority ordering
- **Environment Configuration**: Separate sandbox/test and production environment settings
- **Credential Management**: Secure storage and management of API keys, secrets, and webhooks
- **Health Monitoring**: Real-time provider status monitoring with automatic failover
- **Test Connectivity**: Built-in provider connectivity testing and validation

#### Supported Providers
- **PayPal**: Complete PayPal subscriptions API integration with webhook processing
- **Stripe**: Full Stripe payment processing with advanced subscription features
- **Extensible Architecture**: Easy integration of additional payment providers

### API Endpoints

#### Subscription Management
- `GET /api/subscriptions/tiers` - Get all active subscription tiers
- `GET /api/subscriptions/tiers/{id}` - Get specific subscription tier details
- `GET /api/subscriptions/my-subscription` - Get current user's subscription
- `POST /api/subscriptions/assign-tier` - Assign subscription tier to user
- `DELETE /api/subscriptions/remove-subscription` - Remove user's subscription

#### Payment Processing
- `GET /api/payments/providers` - Get available payment providers
- `POST /api/payments/subscriptions` - Create new subscription with payment
- `GET /api/payments/subscriptions/current` - Get current user's subscription details
- `POST /api/payments/subscriptions/cancel` - Cancel current subscription
- `POST /api/payments/webhooks/{provider}` - Process payment provider webhooks

#### Admin Configuration
- `GET /api/admin/payment-configuration/summary` - Get payment configuration overview
- `GET /api/admin/payment-configuration/providers` - Get all payment provider configurations
- `POST /api/admin/payment-configuration/providers` - Create new payment provider configuration
- `PUT /api/admin/payment-configuration/providers/{id}` - Update payment provider configuration
- `DELETE /api/admin/payment-configuration/providers/{id}` - Delete payment provider configuration
- `POST /api/admin/payment-configuration/providers/{id}/test` - Test payment provider connectivity
- `GET /api/admin/payment-configuration/global` - Get global payment configuration
- `PUT /api/admin/payment-configuration/global` - Update global payment configuration

#### Admin Subscription Management
- `GET /api/admin/subscriptions/tiers` - Get all subscription tiers (including inactive)
- `POST /api/admin/subscriptions/tiers` - Create new subscription tier
- `PUT /api/admin/subscriptions/tiers/{id}` - Update subscription tier
- `DELETE /api/admin/subscriptions/tiers/{id}` - Delete subscription tier
- `GET /api/admin/subscriptions/users` - Get user subscriptions with filtering
- `GET /api/admin/subscriptions/analytics` - Get subscription analytics and metrics

### Technical Implementation
- **Database-Driven Configuration**: All payment settings stored in database with encryption for sensitive data
- **Provider Abstraction**: Clean abstraction layer allowing easy addition of new payment providers
- **Webhook Security**: Automatic signature verification and replay attack prevention
- **Retry Logic**: Intelligent retry mechanisms for failed payments with exponential backoff
- **Audit Trail**: Complete audit logging for all payment configuration changes and transactions
- **Performance Optimized**: Efficient database queries with proper indexing for payment operations

## 👤 Admin User Management API

Comprehensive user management endpoints for admin interface:

### User Details Endpoints
- `GET /api/admin/users/{id}` - Get comprehensive user details including profile, trust score, rate limiting settings, activity statistics, and moderation history
- `PUT /api/admin/users/{id}/rate-limiting` - Update user-specific rate limiting settings with audit trail
- `POST /api/admin/users/{id}/change-role` - Change user role with reason tracking and audit logging

## 🏃‍♂️ Quick Start

### Prerequisites
- .NET 9 SDK
- Node.js 18+
- PostgreSQL 12+
- Redis 7+ (or Docker for Redis container)
- Docker (for content moderation service and Redis)

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

### 4. Redis Setup (Recommended for Performance)

Redis provides high-performance caching for count operations, dramatically improving response times.

#### Option A: Docker Redis (Recommended)
```bash
# Using Docker Compose (includes Redis)
docker compose -f docker-compose.local.yml up --build -d

# Verify Redis is running
docker compose -f docker-compose.local.yml exec redis redis-cli ping
# Should return: PONG
```

#### Option B: Local Redis Installation
```bash
# macOS
brew install redis
brew services start redis

# Ubuntu/Debian
sudo apt update && sudo apt install redis-server
sudo systemctl start redis-server

# Windows
# Download from https://redis.io/download
```

#### Configuration
Add to `Yapplr.Api/appsettings.Development.json`:
```json
{
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}
```

#### Verify Redis Caching
```bash
# Check if caching is working
curl http://localhost:5161/health

# Monitor Redis keys (should see count:* keys)
redis-cli keys "count:*"
```

**Note**: If Redis is unavailable, the application automatically falls back to memory caching without affecting functionality.

### 5. Mobile App Setup (Optional)

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
- **Content Reporting**: Report inappropriate posts and comments with system tag categorization (Violation, Safety, Content Warning, Quality/Spam) and detailed reason submission
- **Dark Mode**: Complete dark theme with toggle in Settings, synchronized with web app preferences

### 6. Firebase Setup (Required for Real-time Notifications)

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

### 7. Payment System Setup (Optional - For Subscription Features)

The payment system enables subscription tiers and monetization features. This is optional for basic social media functionality.

#### Payment Provider Setup
1. **PayPal Setup** (Optional):
   - Create a [PayPal Developer Account](https://developer.paypal.com/)
   - Create an application and get Client ID and Secret
   - Set up products and billing plans for subscriptions
   - Configure webhook endpoints for real-time updates

2. **Stripe Setup** (Optional):
   - Create a [Stripe Account](https://dashboard.stripe.com/)
   - Get API keys (Publishable and Secret keys)
   - Set up products and prices for subscription tiers
   - Configure webhook endpoints for payment events

#### Admin Configuration
1. **Access Payment Configuration**:
   - Log in as an admin user
   - Navigate to `/admin/payment-configuration`
   - Configure payment providers with your credentials

2. **Create Subscription Tiers**:
   - Navigate to `/admin/subscriptions`
   - Create subscription tiers with pricing and features
   - Set up trial periods and billing cycles

#### Configuration Example
Add to `Yapplr.Api/appsettings.Development.json`:
```json
{
  "PaymentSystem": {
    "EnableSubscriptions": true,
    "DefaultTrialDays": 14,
    "GracePeriodDays": 7
  }
}
```

**Note**: Payment providers can be configured entirely through the admin interface. No code changes required for adding or modifying payment settings.

For detailed payment gateway setup instructions, see [PAYMENT_GATEWAY_CONFIGURATION.md](./PAYMENT_GATEWAY_CONFIGURATION.md).

### 8. SendGrid Setup (Required for Email Verification & Password Reset)

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

### 8. Admin Setup (Creating Admin Users)

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
   - **User Details** (`/admin/users/{id}`) - Comprehensive user information and management
   - **Posts** (`/admin/posts`) - Post moderation and management
   - **Comments** (`/admin/comments`) - Comment moderation
   - **Content Queue** (`/admin/queue`) - Flagged content review
   - **System Tags** (`/admin/system-tags`) - Content labeling system
   - **Appeals** (`/admin/appeals`) - User appeal management
   - **Audit Logs** (`/admin/audit-logs`) - Action tracking and accountability
   - **Analytics** (`/admin/analytics`) - Platform insights and reporting

#### Admin Features
- **Role-Based Access**: Different permissions for Admin vs Moderator roles
- **Comprehensive User Management**: Detailed user profiles with trust score analysis, rate limiting controls, and complete moderation history
- **User Details Pages**: Click any username to access comprehensive user information and management tools
- **Rate Limiting Controls**: Override rate limiting settings for individual users with audit trail
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

## 🚀 Redis Caching System

Yapplr features a comprehensive Redis-based caching system that dramatically improves performance for count operations and frequently accessed data.

### Core Features
- **Count Caching**: High-performance caching for followers, following, posts, likes, comments, reposts, and notifications
- **Smart Expiration**: Optimized cache expiration times based on data volatility (2-15 minutes)
- **Automatic Invalidation**: Cache automatically invalidated when data changes (posts, likes, follows, etc.)
- **Graceful Fallback**: Automatic fallback to memory caching if Redis is unavailable
- **Type-Safe Operations**: Strongly-typed caching with JSON serialization for complex data structures

### Performance Benefits
- **50-80% faster** user profile loads with cached follower/following counts
- **40-60% faster** post feed rendering with cached like/comment counts
- **70-90% faster** notification count queries
- **60-80% reduction** in database queries for count operations
- **Sub-millisecond** cache response times vs 10-100ms+ database queries

### Technical Implementation
- **Redis 7.2**: Latest Redis with optimized memory management and persistence
- **LRU Eviction**: Automatic eviction of least recently used keys when memory limits reached
- **Data Persistence**: Redis data survives container restarts with configurable save intervals
- **Health Monitoring**: Comprehensive health checks and error logging
- **Memory Limits**: Configurable memory limits (256MB staging, 512MB production)

### Cache Categories
#### User Counts (10-minute expiration)
- Follower count: `count:followers:{userId}`
- Following count: `count:following:{userId}`
- Post count: `count:posts:{userId}`

#### Post Counts (5-minute expiration)
- Like count: `count:likes:{postId}`
- Comment count: `count:comments:{postId}`
- Repost count: `count:reposts:{postId}`

#### Notification Counts (2-minute expiration)
- Unread notifications: `count:notifications:unread:{userId}`
- Unread messages: `count:messages:unread:{userId}`

#### Tag Counts (15-minute expiration)
- Tag post count: `count:tag:posts:{tagId}`
- Tag post count by name: `count:tag:posts:name:{tagName}`

### Configuration
```json
{
  "Redis": {
    "ConnectionString": "redis:6379",
    "DefaultExpiration": "00:05:00"
  }
}
```

### Deployment
- **Development**: Redis on localhost:6379
- **Docker Local**: Redis container on port 6380
- **Staging/Production**: Redis containers with persistent volumes
- **External Redis**: Easy switch to managed Redis services (AWS ElastiCache, Azure Redis)

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

### User Reporting System
- `POST /api/reports` - Create user report for objectionable content (posts or comments)
- `GET /api/reports/my-reports` - Get current user's submitted reports (paginated)
- `GET /api/admin/system-tags` - Get system tags for report categorization

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
- **Environment-Based Security**: Test and debug endpoints are conditionally registered based on environment
  - **Development-Only Endpoints**: Debug, test, and cache management endpoints only available in development
  - **Production Security**: Test endpoints (`/api/notifications/test-*`, `/api/cqrs-test/*`, debug endpoints) are automatically disabled in production
  - **Swagger/OpenAPI**: API documentation only accessible in development and staging environments
  - **Mobile App Security**: Debug and test screens in mobile app only accessible in development builds

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

## 🆕 Recent Updates

### Emoji Reaction System (Latest)
- **Rich Reactions**: Replaced simple likes with 6 emoji reaction types (❤️👍😂😮😢😡)
- **Interactive UI**: New reaction picker with hover effects and real-time counts
- **Database Migration**: Automatic conversion of existing likes to heart reactions
- **Full Coverage**: Reactions available for both posts and comments
- **Mobile Support**: Complete React Native implementation with modal picker
- **Admin Interface**: Updated admin views to show reaction breakdowns
- **Backward Compatibility**: Maintains existing like functionality during transition

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
