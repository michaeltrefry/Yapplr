# Yapplr Frontend

A clean, responsive Twitter-like social media frontend built with Next.js 15, TypeScript, and Tailwind CSS.

## Features

- **Clean 2015 Twitter-inspired Design**: Simple, focused interface
- **Responsive Layout**: Works seamlessly on desktop and mobile
- **Real-time Updates**: Live timeline with social interactions
- **Infinite Scroll**: Smooth pagination with 25 posts per page for optimal performance
- **Return to Top**: Convenient navigation button when reaching the end of timelines
- **Authentication**: Secure login, registration, and password reset
- **User Profiles**: Complete profile management with profile images
- **Follow System**: Follow/unfollow users with real-time counts
- **User Blocking**: Block/unblock users with automatic unfollowing and content filtering
- **Settings Management**: Dedicated settings page with blocklist management
- **Yap Privacy**: Create yaps with Public, Followers, or Private visibility
- **Enhanced Emoji Reaction System**: Rich reactions with 6 emoji types (❤️👍😂😮😢😡) replacing simple likes
- **Interactive Reaction Picker**: Advanced hover-based emoji selection with smooth visual transitions:
  - **Grayscale to Color Transition**: Emojis start in grayscale and transition to full color on hover
  - **Colored SVG Icons**: Hover reveals colored SVG icons that replace emojis with smooth fade transitions
  - **Background Circle Animations**: Reaction-specific colored background circles appear on hover
  - **Scale Effects**: Subtle scale animations (110%) for enhanced visual feedback
- **Optimized Reaction Layout**: Improved user experience with strategic positioning:
  - **Reaction Button Priority**: Positioned first (leftmost) in action buttons for primary engagement
  - **Reaction Counts Display**: Visual pill-shaped badges positioned between content and actions
  - **Layout Stability**: Reaction counts don't affect button alignment, preventing layout shifts
- **Colored Reaction States**: Selected reactions display colored SVG icons instead of filled black versions
- **Reaction Analytics**: Visual breakdown of reaction counts per emoji type with pill-shaped badges
- **Consistent Experience**: Unified reaction system across PostCard, FullScreenPhotoViewer, and FullScreenVideoViewer
- **Social Features**: Yaps, reactions, comments, reyaps with timeline integration
- **Mentions System**: @username mentions with clickable links and real-time notifications
- **Real-time Notifications**: Firebase-powered instant push notifications for all social interactions
- **Comprehensive Notifications**: Complete notification system for mentions, reactions, reposts, follows, and comments
- **Follow Notifications**: Instant alerts when someone follows you with navigation to their profile
- **Reaction Notifications**: Real-time notifications when someone reacts to your posts with emoji display
- **Repost Notifications**: Immediate alerts when someone reposts your content with navigation to the post
- **Smart Navigation**: Click notifications to navigate directly to relevant content with automatic scrolling
- **Background Notifications**: Push notifications work even when the app is minimized or closed
- **Comment Replies**: Reply to specific comments with automatic @username prefilling and reply context
- **Content Management**: Delete your own yaps, comments, and reyaps with confirmation dialogs
- **Media Upload**: Upload and display images and videos in yaps and profiles with Next.js optimization
- **Video Support**: Upload videos up to 100MB in multiple formats (MP4, MOV, AVI, WMV, FLV, WebM, MKV)
- **iPhone Photo Library**: Unified media picker optimized for iPhone Safari with direct photo library access
- **File Validation**: Automatic format detection and size validation with user-friendly error messages
- **Search**: Find users by username or bio
- **Private Messaging**: Send direct messages with text, photo, and video attachments with real-time notifications
- **Message Conversations**: Organized conversation threads with read status tracking
- **Admin Interface**: Comprehensive admin dashboard with user management, content moderation, and analytics
- **User Details Pages**: Detailed admin user profiles with trust score analysis, rate limiting controls, and moderation history

## Tech Stack

- **Next.js 15** - React framework with App Router
- **TypeScript** - Type safety
- **Tailwind CSS** - Utility-first styling
- **TanStack Query** - Data fetching and caching
- **Axios** - HTTP client
- **Lucide React** - Beautiful icons
- **SignalR Client** - Real-time WebSocket notifications

## Getting Started

### Prerequisites

- Node.js 18+
- npm or yarn
- Yapplr API running on `http://localhost:5161`

### Installation

1. **Install dependencies:**
   ```bash
   npm install
   ```

2. **Set up environment variables:**
   Create a `.env.local` file:
   ```env
   NEXT_PUBLIC_API_URL=http://localhost:5161

   # Notification Configuration
   NEXT_PUBLIC_ENABLE_SIGNALR=true
   ```

3. **Start the development server:**
   ```bash
   npm run dev
   ```

4. **Open your browser:**
   Navigate to `http://localhost:3000`

## Key Features

### Authentication
- JWT-based authentication
- Persistent login state
- Secure token management

### Responsive Design
- Mobile-first approach
- Collapsible sidebar on smaller screens
- Touch-friendly interactions

### Follow System
- Follow/unfollow buttons on user profiles
- Real-time follower and following counts
- Follow status indicators
- Privacy-aware content filtering based on relationships

### Yap Privacy
- Three privacy levels: Public, Followers, Private
- Visual privacy indicators on yaps
- Smart timeline filtering based on user relationships
- Privacy selector in yap creation form

### Profile Management
- Upload and manage profile images
- Edit profile information (bio, pronouns, tagline, birthday)
- View follower/following statistics
- Dedicated profile pages with user yaps

### Social Features
- Create yaps (256 character limit) with privacy settings
- Upload images to yaps and profiles with Next.js Image optimization
- Like and unlike yaps with real-time counts
- Comment on yaps with expandable comment sections and reply functionality
- @Username Mentions: Mention users in posts and comments with automatic detection and clickable links
- Real-time Notifications: Comprehensive Firebase push notifications for mentions, likes, reposts, follows, and comments with red badge indicators
- Follow Notifications: Get notified when someone follows you with direct navigation to their profile
- Like & Repost Notifications: Instant alerts when someone likes or reposts your content with navigation to the specific post
- Smart Navigation: Click notifications to navigate directly to mentioned posts, comments, or user profiles with automatic scrolling
- Reyap functionality with timeline integration and attribution
- Follow/unfollow users with instant UI updates
- Privacy-aware timeline filtering
- Mixed timeline showing both yaps and reyaps chronologically
- Real-time interaction counts and follower statistics
- **Content Management**: Delete your own content with confirmation dialogs:
  - Delete yaps with trash icon (only visible on your posts)
  - Delete comments with confirmation modal
  - Delete reyaps (unrepost) with confirmation dialog
  - All deletions include loading states and cache invalidation
- Share yaps with popup modal featuring:
  - Copy direct link to yap functionality
  - Social media sharing to Twitter, Facebook, LinkedIn, and Reddit
  - Yap preview with user attribution

## Pages & Components

### Main Pages
- **Home** (`/`) - Timeline with yap creation, privacy selector, and infinite scroll
- **Notifications** (`/notifications`) - Comprehensive notification center with red badge indicators and smart navigation
- **Profile** (`/profile/[username]`) - User profiles with follow/block buttons and infinite scroll timeline
- **Profile Edit** (`/profile/edit`) - Profile management with image upload
- **Settings** (`/settings`) - Settings hub with organized sections
- **Blocklist** (`/settings/blocklist`) - Manage blocked users with unblock functionality
- **Yap Detail** (`/yap/[id]`) - Individual yap pages with comments, replies, and sharing
- **Login/Register** - Authentication with password reset functionality
- **Forgot/Reset Password** - Email-based password recovery
- **Admin Dashboard** (`/admin`) - Admin interface with platform statistics and quick actions
- **Admin Users** (`/admin/users`) - User management with filtering and moderation tools
- **Admin User Details** (`/admin/users/[id]`) - Comprehensive user details with trust score analysis, rate limiting controls, role management, and moderation history

### Key Components
- **CreatePost** - Yap creation with privacy selector, image upload, and mention detection
- **PostCard** - Yap display with privacy indicators, social actions, reply functionality, and delete functionality
- **TimelineItemCard** - Unified display for yaps and reyaps with attribution and delete options
- **UserAvatar** - Consistent user avatar display throughout the app
- **Timeline** - Mixed timeline with infinite scroll, yaps and reyaps, privacy-filtered
- **PublicTimeline** - Public timeline with infinite scroll for unauthenticated users
- **UserTimeline** - User-specific timeline with infinite scroll for profile pages
- **BlockedUsersList** - Blocklist management with unblock functionality
- **CommentList** - Expandable comment sections with reply buttons and delete functionality for own comments
- **NotificationsPage** - Comprehensive notification center with smart navigation and read status management
- **MentionHighlight** - Automatic @username detection and clickable link conversion
- **ShareModal** - Yap sharing modal with social media integration and link copying
- **Sidebar** - Navigation with responsive design, notification badges, and settings link

## Code Quality

### Linting & Standards
- **ESLint**: Configured with Next.js and TypeScript rules
- **Zero Errors**: All linting errors resolved
- **Clean Code**: Unused imports and variables removed
- **Type Safety**: Full TypeScript coverage

### Performance Optimizations
- **Infinite Scroll**: Pagination with 25 posts per page reduces initial load time and memory usage
- **Intersection Observer**: Efficient scroll detection for automatic loading
- **Next.js Image**: Optimized image loading with proper configuration
- **React Query**: Efficient data fetching and caching with useInfiniteQuery
- **Code Splitting**: Automatic route-based code splitting
- **Optimistic Updates**: Immediate UI feedback for user actions

## API Integration

The frontend communicates with the Yapplr API for all data operations including authentication, yaps, users, social interactions, follow relationships, and image management.

## Development

Run the development server:

```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) to view the application.
