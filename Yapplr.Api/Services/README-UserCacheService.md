# UserCacheService

The `UserCacheService` provides an in-memory cache for User entities to improve performance by reducing database queries for frequently accessed users.

## Features

- **Dual Key Caching**: Users are cached by both ID and username for flexible lookups
- **Configurable Expiration**: Cache entries expire after a configurable time period (default: 30 seconds)
- **Size Limits**: Configurable maximum cache size to prevent memory issues (default: 1000 entries)
- **Statistics Tracking**: Built-in metrics for monitoring cache performance
- **Thread-Safe**: Uses concurrent collections for thread-safe operations
- **Automatic Cleanup**: Memory cache handles eviction and cleanup automatically

## Configuration

Add the following section to your `appsettings.json`:

```json
{
  "UserCache": {
    "ExpirationSeconds": 30,
    "MaxSize": 1000
  }
}
```

### Configuration Options

- `ExpirationSeconds`: How long users stay in cache before expiring (default: 30 seconds)
- `MaxSize`: Maximum number of users to cache (default: 1000)

## Usage

### Basic Usage

```csharp
// Inject the service
public class MyService
{
    private readonly IUserCacheService _userCache;
    
    public MyService(IUserCacheService userCache)
    {
        _userCache = userCache;
    }
    
    // Get user by ID (checks cache first, then database)
    public async Task<User?> GetUserAsync(int userId)
    {
        return await _userCache.GetUserByIdAsync(userId);
    }
    
    // Get user by username (checks cache first, then database)
    public async Task<User?> GetUserAsync(string username)
    {
        return await _userCache.GetUserByUsernameAsync(username);
    }
}
```

### Cache Management

```csharp
// Invalidate a specific user when they're updated
_userCache.InvalidateUser(userId);
_userCache.InvalidateUser(username);

// Clear all cached users
_userCache.ClearCache();

// Get cache statistics
var stats = _userCache.GetCacheStatistics();
Console.WriteLine($"Hit Rate: {stats.HitRate:P2}");
Console.WriteLine($"Cached Entries: {stats.CachedEntries}");
```

## API Endpoints

The service includes several endpoints for testing and monitoring:

### Cache Statistics
```
GET /api/users/cache/stats
```
Returns cache performance statistics including hit rate, miss rate, and entry count.

### Cache Management
```
POST /api/users/cache/clear
DELETE /api/users/cache/{userId}
DELETE /api/users/cache/username/{username}
```

### Cached User Lookups
```
GET /api/users/cached/{userId}
GET /api/users/cached/username/{username}
```
These endpoints demonstrate direct cache usage and can be used for testing.

## Performance Considerations

### When to Use
- High-frequency user lookups (e.g., authentication, authorization)
- User profile displays
- Social features (followers, following)
- Any scenario where the same users are accessed repeatedly

### When NOT to Use
- User updates (always invalidate cache after updates)
- Admin operations requiring fresh data
- One-time user lookups

### Best Practices

1. **Invalidate on Updates**: Always invalidate cache when user data changes
```csharp
// After updating a user
await _context.SaveChangesAsync();
_userCache.InvalidateUser(userId);
```

2. **Monitor Performance**: Use the statistics endpoint to monitor cache effectiveness
```csharp
var stats = _userCache.GetCacheStatistics();
if (stats.HitRate < 0.5) // Less than 50% hit rate
{
    // Consider adjusting cache size or expiration
}
```

3. **Handle Null Results**: Cache service returns null for non-existent users
```csharp
var user = await _userCache.GetUserByIdAsync(userId);
if (user == null)
{
    return Results.NotFound();
}
```

## Integration with Existing Services

The cache service is designed to be a drop-in replacement for direct database queries:

```csharp
// Before (direct database query)
var user = await _context.Users.FindAsync(userId);

// After (with caching)
var user = await _userCache.GetUserByIdAsync(userId);
```

## Monitoring and Debugging

Use the cache statistics to monitor performance:

- **Hit Rate**: Percentage of requests served from cache (higher is better)
- **Miss Rate**: Percentage of requests that required database queries
- **Cached Entries**: Current number of users in cache
- **Total Requests**: Total number of cache requests

A healthy cache typically has:
- Hit rate > 70%
- Cached entries below max size limit
- Reasonable memory usage

## Thread Safety

The service is fully thread-safe and can be used concurrently from multiple threads without additional synchronization.
