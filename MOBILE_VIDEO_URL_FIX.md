# Mobile Video URL Fix - Local Development

## Problem Identified

The issue is a **network configuration mismatch** between the mobile app and the API in local development:

### Current Situation
- **Frontend (Web)**: Connects to `http://localhost:8080` ✅ Works
- **Mobile App**: Connects to `http://192.168.254.181:8080` ❌ Gets URLs with `localhost`
- **API Docker**: Configured with `API_BASE_URL=http://localhost:8080`

### The Issue
1. Mobile app connects to API at `http://192.168.254.181:8080`
2. API generates video URLs using `API_BASE_URL=http://localhost:8080`
3. Mobile app receives video URLs like `http://localhost:8080/api/videos/processed/video.mp4`
4. Mobile device **cannot reach** `localhost:8080` (localhost refers to the mobile device, not your computer)
5. Video loading fails with "Could not connect to the server"

## Fix Applied

### 1. Updated Docker Configuration
Changed `docker-compose.local.yml`:
```yaml
# Before
- API_BASE_URL=http://localhost:8080

# After  
- API_BASE_URL=http://192.168.254.181:8080
```

### 2. Added Enhanced Debugging
Added logging to track:
- What base URL the mobile app is connecting to
- What video URLs the API is returning
- First 30 characters of video URLs to verify they start with the correct IP

## Testing Steps

### 1. Restart Docker Services
```bash
docker-compose -f docker-compose.local.yml down
docker-compose -f docker-compose.local.yml up -d
```

### 2. Check API Logs
Look for these log messages:
```
🔧 MappingUtilities: API_BASE_URL environment variable = 'http://192.168.254.181:8080'
🎥 GenerateVideoUrl: Generated URL: 'http://192.168.254.181:8080/api/videos/processed/...'
```

### 3. Check Mobile App Logs
Look for these log messages:
```
🎥 API Timeline: Mobile app connecting to: http://192.168.254.181:8080
🎥 API Timeline: Post X video media: [{"videoUrl": "http://192.168.254.181:8080/api/videos/processed/..."}]
🎥 VideoPlayer status change: {"videoUrl": "http://192.168.254.181:8080/api/videos/processed/..."}
```

### 4. Verify Frontend Still Works
The frontend should continue working because:
- Browser runs on host machine
- Can reach `localhost:8080` directly
- Video URLs with `192.168.254.181:8080` are also reachable from browser

## Expected Results

After the fix:
- ✅ **Mobile App**: Gets video URLs with `http://192.168.254.181:8080` - can reach these
- ✅ **Frontend**: Gets video URLs with `http://192.168.254.181:8080` - can also reach these
- ✅ **Videos load successfully** in both mobile and web

## Alternative Solutions (if needed)

If the IP address `192.168.254.181` changes or causes issues:

### Option 1: Use host.docker.internal
```yaml
- API_BASE_URL=http://host.docker.internal:8080
```

### Option 2: Dynamic IP Detection
Create a script to detect your current IP and update the configuration automatically.

### Option 3: Use Different URLs for Different Clients
Modify the API to return different base URLs based on the client type (web vs mobile).

## Verification Commands

### Check your current IP
```bash
# macOS/Linux
ifconfig | grep "inet " | grep -v 127.0.0.1

# Windows
ipconfig | findstr "IPv4"
```

### Test API connectivity from mobile network
```bash
# From your mobile device's browser, visit:
http://192.168.254.181:8080/api/auth/login
# Should return a 405 Method Not Allowed (means API is reachable)
```

## Files Modified

1. `docker-compose.local.yml` - Updated API_BASE_URL
2. `YapplrMobile/src/api/client.ts` - Added connection and URL debugging
3. `Yapplr.Api/Common/MappingUtilities.cs` - Added URL generation debugging

## Cleanup

Once confirmed working, you can remove the debug logging by:
1. Removing console.log statements from `client.ts`
2. Removing Console.WriteLine statements from `MappingUtilities.cs`
3. Removing debug logs from `PostCard.tsx` and `VideoPlayer.tsx`
