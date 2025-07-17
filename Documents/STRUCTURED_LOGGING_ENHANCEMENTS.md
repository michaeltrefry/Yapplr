# Structured Logging Enhancements

## Overview

This document outlines the comprehensive structured logging enhancements made throughout the Yapplr application. These improvements provide better observability, debugging capabilities, and operational insights.

## Key Enhancements Made

### 1. Enhanced Logging Service (`LoggingEnhancementService`)

**Location**: `Yapplr.Api/Services/LoggingEnhancementService.cs`

**Features**:
- **Logging Scopes**: Automatic context management with user, request, and operation scopes
- **Extension Methods**: Simplified logging for business operations, user actions, security events
- **Performance Timing**: `BeginTimedOperation()` for automatic operation timing
- **Structured Context**: Automatic correlation IDs, user context, and request metadata

**Usage Examples**:
```csharp
// Business operation logging
_logger.LogBusinessOperation("CreatePost", new { userId, mediaCount });

// User action tracking
_logger.LogUserAction(userId, "FollowUser", new { targetUserId });

// Security event logging
_logger.LogSecurityEvent("LoginFailure", userId, details: new { reason });

// Performance timing
using var timer = _logger.BeginTimedOperation("DatabaseQuery");
```

### 2. Request Context Middleware (`LoggingContextMiddleware`)

**Location**: `Yapplr.Api/Middleware/LoggingContextMiddleware.cs`

**Features**:
- **Automatic Context**: Adds request metadata to all logs within the request scope
- **Correlation IDs**: Generates and tracks correlation IDs across requests
- **User Context**: Automatically includes authenticated user information
- **Performance Metrics**: Logs request duration and status codes
- **IP Tracking**: Captures client IP addresses (proxy-aware)

**Context Added**:
- `RequestId` - ASP.NET Core trace identifier
- `CorrelationId` - Custom 8-character correlation ID
- `HttpMethod` - HTTP method (GET, POST, etc.)
- `RequestPath` - Request path
- `UserId` - Authenticated user ID
- `Username` - Authenticated username
- `UserRole` - User role
- `IpAddress` - Client IP address
- `UserAgent` - Browser/client information

### 3. Service-Level Enhancements

#### AuthService
**Enhancements**:
- Registration process tracking with email verification flow
- Login attempt logging with security event tracking
- Failed login attempts with detailed context
- Password reset request tracking

**Security Events**:
- `LoginSuccess` - Successful authentication
- `LoginFailure` - Failed authentication attempts
- `RegistrationAttempt` - User registration attempts

#### PostService
**Enhancements**:
- Post creation with media count and privacy tracking
- Performance timing for post creation operations
- Trust score validation logging
- Video processing request tracking

#### UserService
**Enhancements**:
- FCM token management with action tracking
- User follow/unfollow operations
- Profile update tracking

#### AdminService
**Enhancements**:
- Content moderation actions (hide post, ban user)
- Moderator action tracking with reason logging
- Trust score impact logging
- Audit trail integration

#### NotificationService
**Enhancements**:
- Notification creation with type and recipient tracking
- Mention processing with count tracking
- Notification delivery status

#### MessageService
**Enhancements**:
- System message delivery tracking
- Message conversation context
- User blocking validation logging

#### EmailService
**Enhancements**:
- Email sending with template type tracking
- Delivery success/failure logging
- Email type categorization (verification, password reset, etc.)

#### VideoProcessingService
**Enhancements**:
- Video processing pipeline tracking
- File size and codec information
- Processing duration and success metrics
- Error handling with detailed context

#### TrustScoreService
**Enhancements**:
- Trust score calculation tracking
- Factor analysis logging
- Score change tracking with reasons

### 4. Error Handling Enhancements

**Location**: `Yapplr.Api/Common/ErrorHandlingMiddleware.cs`

**Features**:
- **Contextual Exception Logging**: Includes request context, user information
- **Security Event Detection**: Automatically logs security-related exceptions
- **Exception Classification**: Different log levels based on exception type
- **Stack Trace Capture**: Structured stack trace logging for debugging

### 5. Performance Monitoring

**Features**:
- **Timed Operations**: Automatic timing for critical operations
- **Database Query Timing**: Track slow queries and database performance
- **Request Duration**: End-to-end request timing
- **Operation Metrics**: Business operation performance tracking

## Structured Data Schema

### Common Properties
All logs now include these structured properties where applicable:

```json
{
  "RequestId": "0HN7GLLMVVV9G:00000001",
  "CorrelationId": "a1b2c3d4",
  "UserId": 123,
  "Username": "john_doe",
  "UserRole": "User",
  "Operation": "CreatePost",
  "HttpMethod": "POST",
  "RequestPath": "/api/posts",
  "IpAddress": "192.168.1.100",
  "Duration": 245.67,
  "StatusCode": 200
}
```

### Business Operation Properties
```json
{
  "BusinessOperation": "CreatePost",
  "EntityType": "Post",
  "EntityId": 456,
  "Parameters": {
    "mediaCount": 3,
    "privacy": "Public"
  }
}
```

### Security Event Properties
```json
{
  "SecurityEvent": "LoginFailure",
  "SecurityDetails": {
    "reason": "InvalidPassword",
    "email": "user@example.com"
  }
}
```

## Query Examples

### Find All Failed Login Attempts
```logql
{service="yapplr-api", level="Warning"} | json | SecurityEvent="LoginFailure"
```

### Track User Activity
```logql
{service="yapplr-api"} | json | UserId="123"
```

### Monitor Post Creation Performance
```logql
{service="yapplr-api"} | json | BusinessOperation="CreatePost" | Duration > 1000
```

### Security Events Dashboard
```logql
{service="yapplr-api"} | json | SecurityEvent != ""
```

### Error Rate by Endpoint
```logql
rate({service="yapplr-api", level="Error"}[5m]) by (RequestPath)
```

### Video Processing Metrics
```logql
{service="yapplr-video-processor"} | json | Operation="ProcessVideo"
```

## Benefits

### 1. **Enhanced Debugging**
- Correlation IDs allow tracking requests across services
- Structured context provides immediate insight into user actions
- Performance timing identifies bottlenecks

### 2. **Security Monitoring**
- Automatic security event detection
- Failed authentication tracking
- Suspicious activity pattern detection

### 3. **Business Intelligence**
- User behavior tracking
- Feature usage analytics
- Performance metrics for business operations

### 4. **Operational Excellence**
- Real-time error monitoring
- Performance degradation detection
- Capacity planning insights

### 5. **Compliance & Auditing**
- Complete audit trail for user actions
- Moderation action tracking
- Data access logging

## Best Practices Implemented

1. **Consistent Property Names**: Standardized property names across all services
2. **Structured Context**: Rich context without log message parsing
3. **Performance Awareness**: Minimal overhead logging design
4. **Security Focus**: Automatic security event detection
5. **User Privacy**: Sensitive data exclusion from logs
6. **Correlation**: Request tracking across service boundaries
7. **Actionable Insights**: Logs designed for alerting and dashboards

## Next Steps

1. **Dashboard Creation**: Build operational dashboards in Grafana
2. **Alerting Setup**: Configure alerts for critical events
3. **Log Analysis**: Regular analysis of patterns and trends
4. **Performance Optimization**: Use logs to identify optimization opportunities
5. **Security Monitoring**: Implement automated security event responses
