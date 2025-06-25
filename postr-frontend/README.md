# Postr Frontend

A clean, responsive Twitter-like social media frontend built with Next.js 15, TypeScript, and Tailwind CSS.

## Features

- **Clean 2015 Twitter-inspired Design**: Simple, focused interface
- **Responsive Layout**: Works seamlessly on desktop and mobile
- **Real-time Updates**: Live timeline with social interactions
- **Authentication**: Secure login, registration, and password reset
- **User Profiles**: Complete profile management with profile images
- **Follow System**: Follow/unfollow users with real-time counts
- **Post Privacy**: Create posts with Public, Followers, or Private visibility
- **Social Features**: Posts, likes, comments, reposts with real-time updates
- **Image Upload**: Upload and display images in posts and profiles
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
- Postr API running on `http://localhost:5161`

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

### Post Privacy
- Three privacy levels: Public, Followers, Private
- Visual privacy indicators on posts
- Smart timeline filtering based on user relationships
- Privacy selector in post creation form

### Profile Management
- Upload and manage profile images
- Edit profile information (bio, pronouns, tagline, birthday)
- View follower/following statistics
- Dedicated profile pages with user posts

### Social Features
- Create posts (256 character limit) with privacy settings
- Upload images to posts and profiles
- Like and unlike posts with real-time counts
- Comment on posts with expandable comment sections
- Repost functionality with visual indicators
- Follow/unfollow users with instant UI updates
- Privacy-aware timeline filtering
- Real-time interaction counts and follower statistics

## Pages & Components

### Main Pages
- **Home** (`/`) - Timeline with post creation and privacy selector
- **Profile** (`/profile/[username]`) - User profiles with follow buttons and post filtering
- **Profile Edit** (`/profile/edit`) - Profile management with image upload
- **Login/Register** - Authentication with password reset functionality
- **Forgot/Reset Password** - Email-based password recovery

### Key Components
- **CreatePost** - Post creation with privacy selector and image upload
- **PostCard** - Post display with privacy indicators and social actions
- **UserAvatar** - Consistent user avatar display throughout the app
- **Timeline** - Privacy-filtered post feed
- **CommentList** - Expandable comment sections
- **Sidebar** - Navigation with responsive design

## API Integration

The frontend communicates with the Postr API for all data operations including authentication, posts, users, social interactions, follow relationships, and image management.

## Development

Run the development server:

```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) to view the application.
