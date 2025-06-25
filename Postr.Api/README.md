# Postr API

A Twitter-like social media API built with .NET 9, PostgreSQL, and JWT authentication.

## Features

- **User Management**: Registration, login, profile management
- **Posts**: Create text posts (up to 256 characters) with optional images
- **Social Features**: Like, comment, and repost functionality
- **User Profiles**: Username, bio, birthday, pronouns, and tagline
- **Timeline**: View posts from all users (timeline feed)
- **Authentication**: JWT-based authentication with secure password hashing

## Tech Stack

- **.NET 9** - Minimal Web API
- **PostgreSQL** - Database
- **Entity Framework Core** - ORM
- **JWT Bearer** - Authentication
- **BCrypt** - Password hashing

## Getting Started

### Prerequisites

- .NET 9 SDK
- PostgreSQL 12+

### Setup

1. **Clone and navigate to the project:**
   ```bash
   cd Postr.Api
   ```

2. **Install dependencies:**
   ```bash
   dotnet restore
   ```

3. **Configure database connection:**
   Update the connection string in `appsettings.json` if needed:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=postr_db;Username=postgres;Password=postgres"
   }
   ```

4. **Setup database:**
   ```bash
   ./setup-db.sh
   ```
   
   Or manually:
   ```bash
   createdb postr_db
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
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login user

### Users
- `GET /api/users/me` - Get current user profile (authenticated)
- `PUT /api/users/me` - Update current user profile (authenticated)
- `GET /api/users/{username}` - Get user profile by username
- `GET /api/users/search/{query}` - Search users

### Posts
- `POST /api/posts` - Create new post (authenticated)
- `GET /api/posts/{id}` - Get post by ID
- `GET /api/posts/timeline` - Get timeline feed (authenticated)
- `GET /api/posts/user/{userId}` - Get user's posts
- `DELETE /api/posts/{id}` - Delete post (authenticated, own posts only)

### Social Features
- `POST /api/posts/{id}/like` - Like a post (authenticated)
- `DELETE /api/posts/{id}/like` - Unlike a post (authenticated)
- `POST /api/posts/{id}/repost` - Repost a post (authenticated)
- `DELETE /api/posts/{id}/repost` - Remove repost (authenticated)

### Comments
- `POST /api/posts/{id}/comments` - Add comment to post (authenticated)
- `GET /api/posts/{id}/comments` - Get post comments
- `DELETE /api/posts/comments/{commentId}` - Delete comment (authenticated, own comments only)

## Authentication

The API uses JWT Bearer tokens. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## Data Models

### User
- Email (unique)
- Username (unique, 3-50 characters)
- Bio (up to 500 characters)
- Birthday (optional)
- Pronouns (up to 100 characters)
- Tagline (up to 200 characters)

### Post
- Content (1-256 characters)
- Image URL (optional)
- User (author)
- Like/Comment/Repost counts

### Comment
- Content (1-256 characters)
- User (author)
- Post (parent post)

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

### Environment Variables

For production, set these environment variables instead of using appsettings.json:
- `ConnectionStrings__DefaultConnection`
- `JwtSettings__SecretKey`
- `JwtSettings__Issuer`
- `JwtSettings__Audience`

## Security Notes

- Passwords are hashed using BCrypt
- JWT tokens expire after 60 minutes (configurable)
- CORS is configured for frontend development
- All sensitive endpoints require authentication
