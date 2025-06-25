# Password Reset Feature

## Overview

The Postr API includes a comprehensive password reset feature that allows users to securely reset their passwords via email.

## Features

- **Secure Token Generation**: Uses cryptographically secure random tokens
- **Token Expiration**: Reset tokens expire after 1 hour for security
- **Email Templates**: Professional HTML and text email templates
- **Security**: Doesn't reveal if email exists in the system
- **Token Invalidation**: Previous tokens are invalidated when new ones are requested

## API Endpoints

### Request Password Reset
```
POST /api/auth/forgot-password
Content-Type: application/json

{
  "email": "user@example.com"
}
```

**Response:**
```json
{
  "message": "If an account with that email exists, a password reset link has been sent."
}
```

### Reset Password
```
POST /api/auth/reset-password
Content-Type: application/json

{
  "token": "secure-reset-token",
  "newPassword": "newpassword123"
}
```

**Response (Success):**
```json
{
  "message": "Password has been reset successfully"
}
```

**Response (Error):**
```json
{
  "message": "Invalid or expired reset token"
}
```

## Database Schema

### PasswordReset Table
- `Id` (int, primary key)
- `Token` (string, unique, indexed)
- `Email` (string, indexed)
- `UserId` (int, foreign key to Users)
- `CreatedAt` (datetime)
- `ExpiresAt` (datetime)
- `IsUsed` (boolean)
- `UsedAt` (datetime, nullable)

## Email Configuration

Configure SMTP settings in `appsettings.json`:

```json
{
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "your-email@gmail.com",
    "FromName": "Postr"
  }
}
```

### Gmail Setup
1. Enable 2-factor authentication
2. Generate an App Password
3. Use the App Password in the configuration

### Other SMTP Providers
- **SendGrid**: Use API key as password
- **Mailgun**: Use domain and API key
- **AWS SES**: Use IAM credentials

## Security Features

1. **Token Security**: 256-bit random tokens, URL-safe encoding
2. **Time-based Expiration**: Tokens expire after 1 hour
3. **Single Use**: Tokens are invalidated after use
4. **Email Privacy**: Doesn't reveal if email exists
5. **Token Invalidation**: Previous tokens are invalidated on new requests

## Frontend Integration

### Forgot Password Page
- Located at `/forgot-password`
- Simple email input form
- Success/error messaging
- Link back to login

### Reset Password Page
- Located at `/reset-password?token=<token>`
- Password and confirm password fields
- Show/hide password toggles
- Token validation
- Automatic redirect to login on success

## Testing

### Manual Testing
1. Request password reset for existing user
2. Check email for reset link
3. Click link or copy URL to reset page
4. Enter new password
5. Verify login with new password

### API Testing
```bash
# Request reset
curl -X POST "http://localhost:5161/api/auth/forgot-password" \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com"}'

# Reset password (with valid token)
curl -X POST "http://localhost:5161/api/auth/reset-password" \
  -H "Content-Type: application/json" \
  -d '{"token": "valid-token", "newPassword": "newpass123"}'
```

## Production Considerations

1. **Rate Limiting**: Implement rate limiting on reset requests
2. **Email Delivery**: Use reliable email service (SendGrid, Mailgun)
3. **Monitoring**: Log reset attempts and failures
4. **Cleanup**: Periodically clean up expired tokens
5. **Security Headers**: Ensure proper CORS and security headers

## Troubleshooting

### Email Not Sending
- Check SMTP configuration
- Verify credentials
- Check firewall/network settings
- Review application logs

### Invalid Token Errors
- Check token expiration (1 hour limit)
- Verify token hasn't been used
- Ensure token is URL-encoded properly

### Database Issues
- Verify PasswordReset table exists
- Check foreign key constraints
- Review migration status
