# Video URL Debugging Summary

## Issue Description
The mobile app was showing empty `videoUrl` values, causing video players to fail with the error:
```
🎥 VideoPlayer status change: {"error": {"message": "Failed to load the player item: Could not connect to the server."}, "oldStatus": "loading", "status": "error"}
```

## Root Cause Analysis

### 1. **Missing Environment Variable**
The API backend uses the `API_BASE_URL` environment variable to generate complete URLs for media files (images, videos, thumbnails). This variable was:
- ✅ **Present** in local development (`docker-compose.local.yml`)
- ❌ **Missing** in staging environment (`docker-compose.stage.yml`)
- ❌ **Missing** in production environment (`docker-compose.prod.yml`)

### 2. **URL Generation Logic**
The API generates video URLs in `Yapplr.Api/Common/MappingUtilities.cs`:
```csharp
public static string? GenerateVideoUrl(string? fileName)
{
    if (string.IsNullOrEmpty(fileName) || BaseUrl == null)
        return null;

    return $"{BaseUrl}/api/videos/processed/{fileName}";
}
```

When `BaseUrl` (from `API_BASE_URL` env var) is null, the function returns null, resulting in empty video URLs.

### 3. **Impact on Mobile App**
The mobile app receives timeline data with:
- `media.videoUrl` = null/empty
- `media.videoThumbnailUrl` = null/empty
- Video player attempts to load empty URL and fails

## Debugging Enhancements Added

### 1. **Mobile App Debugging**
Added comprehensive logging to track video URL flow:

**VideoPlayer.tsx:**
```typescript
player.addListener('statusChange', (status: any) => {
  console.log('🎥 VideoPlayer status change:', {
    ...status,
    videoUrl: videoUrl
  });
});
```

**PostCard.tsx:**
```typescript
// Debug logging for video media
console.log('🎥 PostCard: Video media debug:', {
  mediaId: media.id,
  videoUrl: media.videoUrl,
  videoThumbnailUrl: media.videoThumbnailUrl,
  videoProcessingStatus: media.videoProcessingStatus,
  hasVideoUrl: !!media.videoUrl,
  videoUrlLength: media.videoUrl?.length || 0
});
```

**API Client (client.ts):**
```typescript
// Debug logging for video URLs in timeline
response.data.forEach((item: TimelineItem) => {
  const videoItems = item.post.mediaItems?.filter(media => media.mediaType === 1);
  if (videoItems?.length > 0) {
    console.log(`🎥 API Timeline: Post ${item.post.id} video media:`, videoItems);
  }
});
```

### 2. **Backend API Debugging**
Added logging to track URL generation:

**MappingUtilities.cs:**
```csharp
static MappingUtilities()
{
    BaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL");
    Console.WriteLine($"🔧 MappingUtilities: API_BASE_URL = '{BaseUrl}'");
    if (string.IsNullOrEmpty(BaseUrl))
    {
        Console.WriteLine("⚠️ API_BASE_URL environment variable is not set!");
    }
}

public static string? GenerateVideoUrl(string? fileName)
{
    Console.WriteLine($"🎥 GenerateVideoUrl: fileName='{fileName}', BaseUrl='{BaseUrl}'");
    // ... rest of function
}
```

## Solution Implemented

### 1. **Added Missing Environment Variables**

**Staging (docker-compose.stage.yml):**
```yaml
environment:
  - API_BASE_URL=${STAGE_API_BASE_URL}
```

**Production (docker-compose.prod.yml):**
```yaml
environment:
  - API_BASE_URL=${PROD_API_BASE_URL}
```

### 2. **Updated GitHub Secrets Documentation**
Added required secrets to `Documents/GITHUB_SECRETS_REQUIRED.md`:
- `STAGE_API_BASE_URL` - Full API base URL for staging (e.g., https://stg-api.yapplr.com)
- `PROD_API_BASE_URL` - Full API base URL for production (e.g., https://api.yapplr.com)

### 3. **Local Development**
Local development already had the correct configuration:
```yaml
environment:
  - API_BASE_URL=http://localhost:8080
```

## Next Steps

### 1. **Set GitHub Secrets**
Configure the following GitHub repository secrets:
- `STAGE_API_BASE_URL` = `https://stg-api.yapplr.com` (or your staging domain)
- `PROD_API_BASE_URL` = `https://api.yapplr.com` (or your production domain)

### 2. **Deploy Updates**
Deploy the updated Docker Compose configurations to staging and production environments.

### 3. **Verify Fix**
After deployment, check the logs for:
- ✅ `🔧 MappingUtilities: API_BASE_URL = 'https://...'`
- ✅ `🎥 GenerateVideoUrl: Generated URL: 'https://...'`
- ✅ Video URLs in mobile app timeline logs

### 4. **Remove Debug Logging**
Once the issue is confirmed fixed, consider removing the extensive debug logging to reduce log noise.

## Prevention

### 1. **Environment Variable Validation**
Consider adding startup validation to ensure required environment variables are set.

### 2. **Health Checks**
Add health check endpoints that verify media URL generation is working correctly.

### 3. **Integration Tests**
Add tests that verify complete media URLs are returned in API responses.

## Files Modified

### Mobile App
- `YapplrMobile/src/components/VideoPlayer.tsx` - Added URL to status logs
- `YapplrMobile/src/components/FullScreenVideoViewer.tsx` - Added URL to status logs
- `YapplrMobile/src/components/PostCard.tsx` - Added video media debugging
- `YapplrMobile/src/api/client.ts` - Added timeline video URL debugging

### Backend API
- `Yapplr.Api/Common/MappingUtilities.cs` - Added environment variable and URL generation logging

### Infrastructure
- `docker-compose.stage.yml` - Added API_BASE_URL environment variable
- `docker-compose.prod.yml` - Added API_BASE_URL environment variable
- `Documents/GITHUB_SECRETS_REQUIRED.md` - Added new required secrets

This comprehensive debugging and fix should resolve the empty video URL issue across all environments.
