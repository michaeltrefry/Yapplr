# Quote Tweet Feature Testing Guide

## Overview

This guide provides comprehensive testing procedures for the Quote Tweet feature across all platforms and scenarios.

## Prerequisites

- Yapplr API running locally or in staging environment
- Frontend application running (web and/or mobile)
- Test user accounts with various permission levels
- Sample posts with different privacy settings and media types

## Test Scenarios

### 1. Basic Quote Tweet Creation

#### Web Testing
1. **Navigate to timeline or post detail page**
2. **Click the quote tweet button (purple quote icon) on any post**
   - ✅ Quote tweet modal should open
   - ✅ Original post should be displayed in quoted section
   - ✅ Text input should be focused and ready for input

3. **Add comment text and submit**
   - ✅ Quote tweet should be created successfully
   - ✅ Modal should close
   - ✅ Timeline should refresh showing the new quote tweet
   - ✅ Quote tweet count on original post should increment

#### Mobile Testing
1. **Open post in timeline**
2. **Tap the quote tweet button**
   - ✅ Quote tweet modal should slide up from bottom
   - ✅ Keyboard should appear when text input is focused
   - ✅ Original post should be displayed correctly

3. **Add comment and submit**
   - ✅ Quote tweet should be created
   - ✅ Modal should dismiss
   - ✅ Timeline should update

### 2. Quote Tweet with Media

#### Web Testing
1. **Open quote tweet modal**
2. **Add text comment**
3. **Click image/video upload button**
   - ✅ File picker should open
   - ✅ Selected media should appear in preview
   - ✅ Remove buttons should work for each media item

4. **Submit quote tweet with media**
   - ✅ Quote tweet should be created with both text and media
   - ✅ Media should display correctly in timeline

#### Mobile Testing
1. **Open quote tweet modal**
2. **Tap media button**
   - ✅ Native image picker should open
   - ✅ Multiple selection should work
   - ✅ Media preview should show thumbnails

3. **Submit quote tweet**
   - ✅ Upload progress should be shown
   - ✅ Quote tweet should be created successfully

### 3. Quote Tweet with GIF

#### Web Testing
1. **Open quote tweet modal**
2. **Click GIF button**
   - ✅ GIF picker should open
   - ✅ Search functionality should work
   - ✅ Trending GIFs should load

3. **Select a GIF**
   - ✅ GIF should appear in preview
   - ✅ Remove button should work
   - ✅ Quote tweet should submit successfully

#### Mobile Testing
1. **Open quote tweet modal**
2. **Tap GIF button**
   - ✅ GIF picker modal should open
   - ✅ GIFs should load and be selectable
   - ✅ Selected GIF should appear in preview

### 4. Privacy Settings

#### Test Cases
1. **Public quote tweet of public post**
   - ✅ Should work normally
   - ✅ Quote tweet should appear in public timeline

2. **Followers-only quote tweet**
   - ✅ Privacy setting should be respected
   - ✅ Only followers should see the quote tweet

3. **Quote tweet of private post**
   - ✅ Should only work if user has access to original post
   - ✅ Quote tweet should respect original post's privacy constraints

### 5. Timeline Display

#### Web Testing
1. **Check timeline with quote tweets**
   - ✅ Quote tweets should display as regular posts
   - ✅ Quoted post should be shown in bordered container
   - ✅ Original author information should be displayed
   - ✅ Media in quoted posts should display correctly

2. **Interaction with quoted posts**
   - ✅ Clicking quoted post author should navigate to profile
   - ✅ Quoted post media should be viewable
   - ✅ Links in quoted posts should work

#### Mobile Testing
1. **Scroll through timeline**
   - ✅ Quote tweets should render efficiently
   - ✅ Quoted post styling should be consistent
   - ✅ Touch interactions should work properly

### 6. Quote Tweet Metrics

#### Test Cases
1. **Quote tweet count display**
   - ✅ Count should increment when quote tweet is created
   - ✅ Count should be accurate across all views
   - ✅ Count should update in real-time

2. **Quote tweet list**
   - ✅ GET /api/posts/{id}/quote-tweets should return correct list
   - ✅ Pagination should work properly
   - ✅ Quote tweets should be ordered by creation date

### 7. Error Handling

#### Test Cases
1. **Quote tweet non-existent post**
   - ✅ Should return 400 Bad Request
   - ✅ Error message should be clear

2. **Quote tweet without permission**
   - ✅ Should return 403 Forbidden
   - ✅ UI should handle error gracefully

3. **Network errors during creation**
   - ✅ Should show appropriate error message
   - ✅ Should not leave UI in broken state

4. **Media upload failures**
   - ✅ Should show specific error for failed uploads
   - ✅ Should allow retry or removal of failed media

### 8. Performance Testing

#### Load Testing
1. **Create multiple quote tweets rapidly**
   - ✅ API should handle concurrent requests
   - ✅ Database should maintain consistency
   - ✅ UI should remain responsive

2. **Timeline with many quote tweets**
   - ✅ Should load efficiently
   - ✅ Scrolling should be smooth
   - ✅ Memory usage should be reasonable

### 9. Cross-Platform Consistency

#### Test Cases
1. **Create quote tweet on web, view on mobile**
   - ✅ Quote tweet should display correctly on mobile
   - ✅ All content should be preserved

2. **Create quote tweet on mobile, view on web**
   - ✅ Quote tweet should display correctly on web
   - ✅ Media should load properly

### 10. Edge Cases

#### Test Cases
1. **Quote tweet of deleted post**
   - ✅ Should handle gracefully
   - ✅ Should show appropriate message

2. **Quote tweet with maximum content length**
   - ✅ Should enforce character limits
   - ✅ Should show character count

3. **Quote tweet with maximum media files**
   - ✅ Should enforce file limits (10 files)
   - ✅ Should show appropriate error if exceeded

4. **Nested quote tweets (quote tweet of quote tweet)**
   - ✅ Should work properly
   - ✅ Should display nested structure correctly

## API Testing

### Endpoint Tests

#### POST /api/posts/quote-tweet
```bash
curl -X POST "http://localhost:5000/api/posts/quote-tweet" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "This is my quote tweet comment",
    "quotedPostId": 123,
    "privacy": 0
  }'
```

#### POST /api/posts/quote-tweet-with-media
```bash
curl -X POST "http://localhost:5000/api/posts/quote-tweet-with-media" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Quote tweet with media",
    "quotedPostId": 123,
    "privacy": 0,
    "mediaFiles": [
      {
        "fileName": "test.jpg",
        "mediaType": 0,
        "width": 800,
        "height": 600,
        "fileSizeBytes": 1024000
      }
    ]
  }'
```

#### GET /api/posts/{id}/quote-tweets
```bash
curl -X GET "http://localhost:5000/api/posts/123/quote-tweets?page=1&pageSize=10" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

## Automated Testing

### Unit Tests
Run the quote tweet unit tests:
```bash
cd Yapplr.Api.Tests
dotnet test --filter "QuoteTweetTests"
```

### Integration Tests
Run full integration test suite:
```bash
dotnet test --filter "Category=Integration"
```

## Performance Benchmarks

### Expected Performance
- Quote tweet creation: < 500ms
- Timeline loading with quote tweets: < 1s
- Quote tweet list retrieval: < 300ms
- Mobile app rendering: 60fps maintained

### Monitoring
- Check application logs for errors
- Monitor database query performance
- Track API response times
- Monitor memory usage on mobile devices

## Troubleshooting

### Common Issues

1. **Quote tweet button not appearing**
   - Check user permissions
   - Verify post is not already a quote tweet of the same post
   - Check if original post still exists

2. **Media upload failures**
   - Check file size limits
   - Verify supported file formats
   - Check network connectivity

3. **Timeline not updating**
   - Check React Query cache invalidation
   - Verify WebSocket connections for real-time updates
   - Check for JavaScript errors in console

4. **Mobile app crashes**
   - Check device memory usage
   - Verify React Native version compatibility
   - Check for native module conflicts

## Test Data Cleanup

After testing, clean up test data:
```sql
-- Remove test quote tweets
DELETE FROM Posts WHERE PostType = 2 AND Content LIKE '%test%';

-- Reset quote tweet counts
UPDATE Posts SET QuoteTweetCount = (
  SELECT COUNT(*) FROM Posts qt WHERE qt.QuotedPostId = Posts.Id
);
```

## Conclusion

This testing guide ensures comprehensive coverage of the Quote Tweet feature across all platforms and scenarios. Regular execution of these tests will help maintain feature quality and catch regressions early in the development cycle.
