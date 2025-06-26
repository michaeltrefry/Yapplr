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
- **Image Upload**: Upload and display images in yaps and profiles
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
- Upload images to yaps and profiles
- Like and unlike yaps with real-time counts
- Comment on yaps with expandable comment sections
- Reyap functionality with timeline integration and attribution
- Follow/unfollow users with instant UI updates
- Privacy-aware timeline filtering
- Mixed timeline showing both yaps and reyaps chronologically
- Real-time interaction counts and follower statistics
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
- **PostCard** - Yap display with privacy indicators and social actions
- **TimelineItemCard** - Unified display for yaps and reyaps with attribution
- **UserAvatar** - Consistent user avatar display throughout the app
- **Timeline** - Mixed timeline with yaps and reyaps, privacy-filtered
- **CommentList** - Expandable comment sections
- **ShareModal** - Yap sharing modal with social media integration and link copying
- **Sidebar** - Navigation with responsive design

## API Integration

The frontend communicates with the Yapplr API for all data operations including authentication, yaps, users, social interactions, follow relationships, and image management.

## Development

Run the development server:

```bash
npm run dev
```

Open [http://localhost:3000](http://localhost:3000) to view the application.
