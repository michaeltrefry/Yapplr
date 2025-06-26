# Yapplr Frontend

A clean, responsive Twitter-like social media frontend built with Next.js 15, TypeScript, and Tailwind CSS.

## Features

- **Clean 2015 Twitter-inspired Design**: Simple, focused interface
- **Responsive Layout**: Works seamlessly on desktop and mobile
- **Real-time Updates**: Live timeline with social interactions
- **Authentication**: Secure login, registration, and password reset
- **User Profiles**: Complete profile management with profile images
- **Follow System**: Follow/unfollow users with real-time counts
- **Yap Privacy**: Create yaps with Public, Followers, or Private visibility
- **Social Features**: Yaps, likes, comments, reyaps with timeline integration
- **Content Management**: Delete your own yaps, comments, and reyaps with confirmation dialogs
- **Image Upload**: Upload and display images in yaps and profiles with Next.js optimization
- **Search**: Find users by username or bio

## Tech Stack

- **Next.js 15** - React framework with App Router
- **TypeScript** - Type safety
- **Tailwind CSS** - Utility-first styling
- **TanStack Query** - Data fetching and caching
- **Axios** - HTTP client
- **Lucide React** - Beautiful icons

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
- Comment on yaps with expandable comment sections
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
- **Home** (`/`) - Timeline with yap creation and privacy selector
- **Profile** (`/profile/[username]`) - User profiles with follow buttons and yap filtering
- **Profile Edit** (`/profile/edit`) - Profile management with image upload
- **Yap Detail** (`/yap/[id]`) - Individual yap pages with comments and sharing
- **Login/Register** - Authentication with password reset functionality
- **Forgot/Reset Password** - Email-based password recovery

### Key Components
- **CreatePost** - Yap creation with privacy selector and image upload
- **PostCard** - Yap display with privacy indicators, social actions, and delete functionality
- **TimelineItemCard** - Unified display for yaps and reyaps with attribution and delete options
- **UserAvatar** - Consistent user avatar display throughout the app
- **Timeline** - Mixed timeline with yaps and reyaps, privacy-filtered
- **CommentList** - Expandable comment sections with delete functionality for own comments
- **ShareModal** - Yap sharing modal with social media integration and link copying
- **Sidebar** - Navigation with responsive design

## Code Quality

### Linting & Standards
- **ESLint**: Configured with Next.js and TypeScript rules
- **Zero Errors**: All linting errors resolved
- **Clean Code**: Unused imports and variables removed
- **Type Safety**: Full TypeScript coverage

### Performance Optimizations
- **Next.js Image**: Optimized image loading with proper configuration
- **React Query**: Efficient data fetching and caching
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
