# Yapplr API

A Twitter-like social media API built with .NET 9, PostgreSQL, and JWT authentication.

## Features

- **User Management**: Registration, login, profile management with profile images
- **Yaps**: Create text yaps (up to 256 characters) with optional images
- **Yap Privacy**: Three privacy levels - Public, Followers-only, and Private yaps
- **Social Features**: Like, comment, and reyap functionality with full delete capabilities
- **Mentions System**: @username mention detection with automatic notification creation
- **Notifications**: Comprehensive notification system for mentions, likes, reposts, follows, and comments
- **Comment Replies**: Reply to specific comments with automatic mention detection
- **Follow System**: Users can follow/unfollow each other with follower/following counts
- **User Profiles**: Username, bio, birthday, pronouns, tagline, and profile images
- **Timeline**: Smart timeline with yaps and reyaps, privacy-aware filtering
- **Image Upload**: Server-side image storage and serving
- **Password Reset**: Email-based password reset with AWS SES integration
- **Email Verification**: Email verification system for new user registrations
- **Authentication**: JWT-based authentication with secure password hashing and automatic token expiration handling
- **Trust Score System**: Advanced behavioral scoring system for intelligent moderation and content management
- **Trust-Based Moderation**: Automatic content visibility and rate limiting based on user trust levels
- **Admin Dashboard**: Comprehensive admin interface with user management, content moderation, and analytics

## Tech Stack

- **.NET 9** - Minimal Web API
- **PostgreSQL** - Database
- **Entity Framework Core** - ORM
- **JWT Bearer** - Authentication
- **BCrypt** - Password hashing
- **SendGrid/AWS SES** - Email service for password reset and email verification
- **File System Storage** - Image upload and serving

## Getting Started

### Prerequisites

- .NET 9 SDK
- PostgreSQL 12+

### Setup

1. **Clone and navigate to the project:**
   ```bash
   cd Yapplr.Api
   ```

2. **Install dependencies:**
   ```bash
   dotnet restore
   ```

3. **Configure database connection:**
   Update the connection string in `appsettings.json` if needed:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=yapplr_db;Username=postgres;Password=postgres"
   }
   ```

4. **Setup database:**
   ```bash
   ./setup-db.sh
   ```

   **For existing users migrating from yapplr_db:**
   ```bash
   ./migrate-database.sh
   ```

   Or manually:
   ```bash
   createdb yapplr_db
   dotnet ef database update
   ```

5. **Run the API:**
   ```bash
   dotnet run
   ```

The API will be available at:
- HTTPS: `https://localhost:7000`
- HTTP: `http://localhost:5000`
- OpenAPI/Swagger: `https://localhost:7000/openapi/v1.json`

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user (requires email verification)
- `POST /api/auth/login` - Login user
- `POST /api/auth/forgot-password` - Request password reset
- `POST /api/auth/reset-password` - Reset password with token
- `POST /api/auth/send-verification` - Send email verification token
- `POST /api/auth/verify-email` - Verify email with token

### Users
- `GET /api/users/me` - Get current user profile (authenticated)
- `PUT /api/users/me` - Update current user profile (authenticated)
- `GET /api/users/{username}` - Get user profile by username (authenticated)
- `GET /api/users/search/{query}` - Search users
- `POST /api/users/me/profile-image` - Upload profile image (authenticated)
- `DELETE /api/users/me/profile-image` - Remove profile image (authenticated)

### Follow System
- `GET /api/users/me/following` - Get users that current user is following (authenticated)
- `POST /api/users/{userId}/follow` - Follow a user (authenticated)
- `DELETE /api/users/{userId}/follow` - Unfollow a user (authenticated)

### Yaps
- `POST /api/posts` - Create new yap with privacy settings (authenticated)
- `GET /api/posts/{id}` - Get yap by ID
- `GET /api/posts/timeline` - Get timeline with yaps and reyaps (authenticated)
- `GET /api/posts/user/{userId}` - Get user's yaps (privacy-filtered)
- `GET /api/posts/user/{userId}/timeline` - Get user timeline with yaps and reyaps
- `DELETE /api/posts/{id}` - Delete yap (authenticated, own yaps only)

### Social Features
- `POST /api/posts/{id}/like` - Like a yap (authenticated)
- `DELETE /api/posts/{id}/like` - Unlike a yap (authenticated)
- `POST /api/posts/{id}/repost` - Reyap a yap (authenticated)
- `DELETE /api/posts/{id}/repost` - Remove reyap (authenticated)

### Comments
- `POST /api/posts/{id}/comments` - Add comment to yap with automatic mention detection (authenticated)
- `GET /api/posts/{id}/comments` - Get yap comments
- `PUT /api/posts/comments/{commentId}` - Update comment (authenticated, own comments only)
- `DELETE /api/posts/comments/{commentId}` - Delete comment (authenticated, own comments only)

### Notifications
- `GET /api/notifications` - Get user notifications (paginated, 25 per page)
- `GET /api/notifications/unread-count` - Get count of unread notifications
- `PUT /api/notifications/{id}/read` - Mark specific notification as read
- `PUT /api/notifications/read-all` - Mark all notifications as read for user

### Images
- `POST /api/images/upload` - Upload image file (authenticated)
- `GET /api/images/{fileName}` - Serve uploaded image
- `DELETE /api/images/{fileName}` - Delete image file (authenticated)

### Admin - Trust Scores (Admin Only)
- `GET /api/admin/trust-scores/` - Get all user trust scores with filtering
- `GET /api/admin/trust-scores/{userId}` - Get specific user trust score
- `GET /api/admin/trust-scores/{userId}/history` - Get trust score change history
- `GET /api/admin/trust-scores/{userId}/factors` - Get trust score factor breakdown
- `PUT /api/admin/trust-scores/{userId}` - Manually adjust user trust score
- `GET /api/admin/trust-scores/statistics` - Get platform trust score statistics

### Admin - User Management (Admin/Moderator Only)
- `GET /api/admin/users` - Get all users with filtering and pagination
- `GET /api/admin/users/{userId}` - Get detailed user information for admin
- `PUT /api/admin/users/{userId}/suspend` - Suspend user with reason
- `PUT /api/admin/users/{userId}/ban` - Ban user with reason
- `PUT /api/admin/users/{userId}/unban` - Unban user
- `PUT /api/admin/users/{userId}/role` - Change user role (Admin only)

## Authentication

The API uses JWT Bearer tokens. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## Data Models

### User
- Email (unique, requires verification)
- Username (unique, 3-50 characters)
- Bio (up to 500 characters)
- Birthday (optional)
- Pronouns (up to 100 characters)
- Tagline (up to 200 characters)
- Profile image (optional)
- Email verification status
- Follower/Following counts

### Yap
- Content (1-256 characters)
- Image file (optional)
- Privacy level (Public, Followers, Private)
- User (author)
- Like/Comment/Reyap counts

### Comment
- Content (1-256 characters)
- User (author)
- Yap (parent yap)
- Edit tracking (isEdited flag)

### Notification
- Type (Mention, Like, Repost, Follow, Comment)
- Actor user (who performed the action)
- Target user (who receives the notification)
- Related post/comment (if applicable)
- Read status and timestamp

### Mention
- Mentioned user
- Mentioning user
- Related post or comment
- Username mentioned

### Follow
- Follower (user who follows)
- Following (user being followed)
- Created timestamp

### Yap Privacy Levels
- **Public**: Visible to everyone
- **Followers**: Only visible to followers and the author
- **Private**: Only visible to the author

## Trust Score System

The API includes an advanced user trust score system that provides intelligent, behavior-based moderation and content management.

### Core Features
- **Dynamic Trust Calculation**: Real-time trust scores (0.0-1.0) based on user behavior
- **Trust-Based Rate Limiting**: Dynamic rate limits from 0.25x to 2x based on trust levels
- **Auto-Hide Protection**: Content from very low-trust users automatically hidden
- **Moderation Priority**: 1-5 priority levels for efficient moderation queue management
- **Background Maintenance**: Automated trust score recalculation and inactivity decay

### Trust Score Factors
#### Positive Factors
- Profile completeness (bio, image, email verification)
- Positive activity (creating posts, engaging with content)
- Community standing (receiving likes, follows)
- Account age and consistent activity

#### Negative Factors
- Moderation actions (content hidden, user suspended)
- Reported content (posts/comments reported by users)
- Spam behavior (excessive posting, repetitive content)
- Inactivity decay (gradual score reduction for inactive accounts)

### Action Thresholds
- **Create Posts**: 0.1 minimum trust score
- **Create Comments**: 0.1 minimum trust score
- **Like Content**: 0.05 minimum trust score
- **Report Content**: 0.2 minimum trust score
- **Send Messages**: 0.3 minimum trust score

### Content Visibility Levels
- **Hidden** (< 0.1): Content hidden from feeds and searches
- **Limited** (0.1-0.3): Reduced visibility, requires user action to view
- **Reduced** (0.3-0.5): Lower priority in feeds
- **Normal** (0.5-0.8): Standard visibility
- **Full** (0.8+): Maximum visibility and engagement

### Configuration
```json
{
  "TrustScore": {
    "EnableBackgroundService": true,
    "RecalculationIntervalMinutes": 60,
    "InactivityDecayDays": 7,
    "DefaultNewUserScore": 1.0
  }
}
```

## Database Migration from Yapplr

If you're upgrading from a previous version that used `yapplr_db`, you can migrate your database:

```bash
./migrate-database.sh
```

This script will:
- Detect your existing `yapplr_db` database
- Offer to rename it to `yapplr_db` (preserving all data)
- Or create a fresh `yapplr_db` database

## Development

### Database Migrations

Create new migration:
```bash
dotnet ef migrations add MigrationName
```

Update database:
```bash
dotnet ef database update
```

Apply migrations to production:
```bash
./run-migrations.sh
```

### Environment Variables

For production, set these environment variables instead of using appsettings.json:
- `ConnectionStrings__DefaultConnection`
- `JwtSettings__SecretKey`
- `JwtSettings__Issuer`
- `JwtSettings__Audience`

## Security Notes

- Passwords are hashed using BCrypt
- JWT tokens expire after 60 minutes (configurable)
- Email verification required for new registrations
- Automatic token expiration handling with login redirection
- CORS is configured for frontend development
- All sensitive endpoints require authentication

## Email Verification

New users must verify their email addresses before they can log in:

1. User registers with email and password
2. System sends verification email with token
3. User clicks verification link or enters token
4. Email is marked as verified
5. User can now log in

Unverified users will be redirected to email verification when attempting to log in.

## Token Expiration Handling

The application automatically handles token expiration:

- **Frontend Web**: Automatically redirects to login page when tokens expire
- **Mobile App**: Clears session and returns to authentication flow
- **API**: Returns 401 Unauthorized for expired tokens

This provides a seamless user experience without confusing error messages.
