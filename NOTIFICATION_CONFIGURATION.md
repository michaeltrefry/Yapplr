# Notification Provider Configuration

This document explains how to configure and test different notification providers (Firebase and SignalR) in the Yapplr application.

## Overview

The notification system now supports configurable providers that can be enabled or disabled individually:

- **Firebase**: Push notifications via Firebase Cloud Messaging (FCM)
- **SignalR**: Real-time notifications via WebSocket connections
- **Polling**: Fallback method when real-time providers are unavailable

## Configuration Options

### Frontend Configuration (yapplr-frontend/.env.local)

```env
# Notification Provider Configuration
NEXT_PUBLIC_ENABLE_FIREBASE=true
NEXT_PUBLIC_ENABLE_SIGNALR=true
```

### Backend Configuration (Yapplr.Api/appsettings.Development.json)

```json
{
  "NotificationProviders": {
    "Firebase": {
      "Enabled": true,
      "ProjectId": "yapplr-bd41a",
      "ServiceAccountKeyFile": "firebase-service-account.json"
    },
    "SignalR": {
      "Enabled": true,
      "MaxConnectionsPerUser": 10,
      "MaxTotalConnections": 10000,
      "CleanupIntervalMinutes": 30,
      "InactivityThresholdHours": 2,
      "EnableDetailedErrors": false
    }
  }
}
```

## Quick Configuration Script

Use the provided configuration script to quickly switch between different setups:

```bash
# Show current configuration
node configure-notifications.js status

# Enable Firebase only
node configure-notifications.js firebase-only

# Enable SignalR only
node configure-notifications.js signalr-only

# Enable both providers
node configure-notifications.js both

# Disable both (polling only)
node configure-notifications.js none

# Show help
node configure-notifications.js --help
```

## Testing Configurations

### 1. Firebase Only
```bash
node configure-notifications.js firebase-only
```
- Firebase will handle all notifications
- SignalR is disabled
- Fallback to polling if Firebase fails

### 2. SignalR Only
```bash
node configure-notifications.js signalr-only
```
- SignalR will handle all notifications
- Firebase is disabled
- Fallback to polling if SignalR fails

### 3. Both Providers (Recommended)
```bash
node configure-notifications.js both
```
- Firebase is tried first
- SignalR is used as fallback if Firebase fails
- Polling as final fallback

### 4. Polling Only
```bash
node configure-notifications.js none
```
- Both real-time providers disabled
- Only polling for notifications
- Useful for testing baseline functionality

## Test Page

Visit `/notification-test` to:
- View current configuration status
- Test notification delivery
- See provider availability
- Configure settings (requires restart)

## Manual Configuration

### Frontend Environment Variables

Edit `yapplr-frontend/.env.local`:

```env
# Enable/disable providers
NEXT_PUBLIC_ENABLE_FIREBASE=true|false
NEXT_PUBLIC_ENABLE_SIGNALR=true|false
```

### Backend Configuration

Edit `Yapplr.Api/appsettings.Development.json`:

```json
{
  "NotificationProviders": {
    "Firebase": {
      "Enabled": true|false
    },
    "SignalR": {
      "Enabled": true|false
    }
  }
}
```

## Provider Behavior

### Initialization Order
1. **Frontend**: Checks environment variables to determine which providers to initialize
2. **Firebase**: Attempts initialization if enabled and permissions allow
3. **SignalR**: Attempts initialization if enabled (or as fallback if Firebase fails)
4. **Polling**: Used if no real-time providers are available

### Backend Provider Registration
- Services are only registered if enabled in configuration
- Disabled providers return `false` for `IsAvailableAsync()`
- Composite service skips disabled providers

### Fallback Strategy
1. Try Firebase (if enabled and permissions granted)
2. Try SignalR (if enabled)
3. Fall back to polling

## API Endpoints

### Get Configuration
```
GET /api/notification-config
```
Returns current backend configuration for notification providers.

### Get Provider Status
```
GET /api/notification-config/status
```
Returns availability status of each configured provider.

## Development Workflow

1. **Choose Configuration**: Use the script or manually edit config files
2. **Restart Services**: Both frontend and backend need restart for changes to take effect
3. **Test**: Visit `/notification-test` to verify configuration
4. **Send Test**: Use the test button to send a notification
5. **Monitor Logs**: Check browser console and server logs for provider behavior

## Troubleshooting

### Firebase Issues
- Check Firebase project configuration
- Verify service account key file exists
- Ensure notification permissions are granted
- Check browser console for Firebase errors

### SignalR Issues
- Verify SignalR hub is mapped (only when enabled)
- Check WebSocket connection in browser dev tools
- Monitor server logs for connection issues

### Configuration Issues
- Ensure both frontend and backend are configured consistently
- Restart both services after configuration changes
- Check `/notification-test` page for configuration status

## Production Considerations

- Use environment variables for production configuration
- Consider load balancing implications for SignalR
- Monitor notification delivery metrics
- Set up proper Firebase service account for production
- Configure appropriate connection limits for SignalR

## Examples

### Testing Firebase Reliability
```bash
# Test Firebase only
node configure-notifications.js firebase-only
# Restart services and test

# Test with both (Firebase should be primary)
node configure-notifications.js both
# Restart services and test
```

### Testing SignalR Performance
```bash
# Test SignalR only
node configure-notifications.js signalr-only
# Restart services and test real-time behavior
```

### Testing Fallback Behavior
```bash
# Start with both enabled
node configure-notifications.js both
# Disable Firebase in browser (deny permissions)
# Verify SignalR takes over
```
