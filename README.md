# Yapplr - Twitter-like Social Media Platform

A complete Twitter-like social media platform built with modern web technologies. Features a clean, responsive design inspired by 2015 Twitter with comprehensive social features including yaps, reyaps, comments, likes, follow system, and privacy controls.

## 🚀 Features

### Core Social Features
- **Yaps**: Create text yaps (up to 256 characters) with optional images
- **Reyaps**: Reyap content with proper attribution in timeline feeds
- **Comments**: Comment on yaps with expandable comment sections
- **Likes**: Like and unlike yaps with real-time counts
- **Follow System**: Follow/unfollow users with instant UI updates
- **User Profiles**: Complete profile management with bio, pronouns, tagline, and profile images
- **Yap Sharing**: Share yaps with social media integration and direct link copying
- **Content Management**: Delete your own yaps, comments, and reyaps with confirmation dialogs

### Privacy & Security
- **Yap Privacy**: Three levels - Public (everyone), Followers (followers only), Private (author only)
- **Privacy-Aware Timeline**: Smart filtering based on user relationships and yap privacy
- **JWT Authentication**: Secure token-based authentication
- **Password Reset**: Email-based password recovery with AWS SES

### User Experience
- **Responsive Design**: Mobile-first approach with collapsible sidebar
- **Real-time Updates**: Live timeline with instant social interaction feedback
- **Image Upload**: Server-side image storage for yaps and profile pictures
- **User Search**: Find users by username or bio content
- **Clean UI**: 2015 Twitter-inspired design with modern touches
- **Individual Yap Pages**: Dedicated URLs for each yap with shareable links
- **Share Modal**: Popup sharing interface with social media integration (Twitter, Facebook, LinkedIn, Reddit)
- **Content Control**: Delete buttons with confirmation dialogs for your own content
- **Optimized Images**: Next.js Image component for better performance and loading

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

## 📱 Key Features in Detail

### Timeline & Reyaps
- **Mixed Timeline**: Shows both original yaps and reyaps chronologically
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
- **Privacy Integration**: Following relationships affect content visibility
- **Profile Integration**: Follow/unfollow buttons on user profiles
- **Timeline Impact**: Following users affects your timeline content

### Image Management
- **Yap Images**: Upload images with yaps (server-side storage)
- **Profile Images**: Upload and manage profile pictures
- **Image Serving**: Optimized image serving with proper content types
- **File Management**: Secure image upload with validation

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
- `GET /api/posts/timeline` - Get timeline with yaps and reyaps
- `GET /api/posts/user/{userId}/timeline` - Get user timeline (yaps + reyaps)
- `POST /api/posts/{id}/repost` - Reyap a yap
- `DELETE /api/posts/{id}/repost` - Remove reyap
- `POST /api/posts/{id}/like` - Like a yap
- `POST /api/posts/{id}/comments` - Add comment
- `DELETE /api/posts/{id}` - Delete your own yap
- `DELETE /api/posts/comments/{commentId}` - Delete your own comment

### User Management
- `GET /api/users/{username}` - Get user profile
- `POST /api/users/{userId}/follow` - Follow user
- `DELETE /api/users/{userId}/follow` - Unfollow user
- `POST /api/users/me/profile-image` - Upload profile image

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
