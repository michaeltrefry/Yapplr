# Multiple File Upload API

This document describes the multiple file upload functionality for posts in the Yapplr API.

## Overview

The Yapplr API supports uploading multiple media files (images and videos) for posts. Users can upload up to 10 files per post with proper validation and error handling.

## Endpoints

### 1. Upload Multiple Files

**POST** `/api/uploads/media`

Upload multiple image and video files at once.

#### Request
- **Content-Type**: `multipart/form-data`
- **Authorization**: Bearer token required (ActiveUser)
- **Body**: Form data with `files` field containing multiple files

#### Supported Formats
- **Images**: JPG, JPEG, PNG, GIF, WebP (max 5MB each)
- **Videos**: MP4, AVI, MOV, WMV, FLV, WebM, MKV (max 100MB each)

#### Validation Rules
- Maximum 10 files per request
- Each image file must be ≤ 5MB
- Each video file must be ≤ 100MB
- Only supported file formats are allowed

#### Response
```json
{
  "uploadedFiles": [
    {
      "fileName": "image_123.jpg",
      "fileUrl": "https://api.yapplr.com/api/images/image_123.jpg",
      "mediaType": 0,
      "fileSizeBytes": 1024000,
      "width": 1920,
      "height": 1080
    }
  ],
  "errors": [
    {
      "originalFileName": "invalid.txt",
      "errorMessage": "Unsupported file type",
      "errorCode": "UNSUPPORTED_TYPE"
    }
  ],
  "totalFiles": 2,
  "successfulUploads": 1,
  "failedUploads": 1
}
```

#### Status Codes
- **200**: All files uploaded successfully
- **207**: Partial success (some files uploaded, some failed)
- **400**: Validation failed or all files failed to upload
- **401**: Authentication required

### 2. Get Upload Limits

**GET** `/api/uploads/limits`

Get current upload limits and supported file formats.

#### Response
```json
{
  "maxFiles": 10,
  "maxImageSizeMB": 5,
  "maxVideoSizeMB": 100,
  "supportedImageFormats": ["JPG", "JPEG", "PNG", "GIF", "WebP"],
  "supportedVideoFormats": ["MP4", "AVI", "MOV", "WMV", "FLV", "WebM", "MKV"]
}
```

### 3. Create Post with Multiple Media

**POST** `/api/posts/with-media`

Create a new post with multiple media files.

#### Request
```json
{
  "content": "Check out these amazing photos!",
  "privacy": 0,
  "mediaFiles": [
    {
      "fileName": "image_123.jpg",
      "mediaType": 0,
      "width": 1920,
      "height": 1080,
      "fileSizeBytes": 1024000
    },
    {
      "fileName": "video_456.mp4",
      "mediaType": 1,
      "width": 1920,
      "height": 1080,
      "duration": "PT30S",
      "fileSizeBytes": 5120000
    }
  ]
}
```

#### Media Types
- `0`: Image
- `1`: Video

#### Privacy Levels
- `0`: Public
- `1`: Followers
- `2`: Private

#### Response
Returns a standard `PostDto` with the `mediaItems` array populated.

## Usage Flow

1. **Upload Files**: Use `/api/uploads/media` to upload multiple files
2. **Get File Names**: Extract the `fileName` values from successful uploads
3. **Create Post**: Use `/api/posts/with-media` with the file names and metadata

## Error Handling

The API provides comprehensive error handling:

- **File validation errors**: Returned in the `errors` array with specific error codes
- **Partial success**: HTTP 207 status when some files succeed and others fail
- **Complete failure**: HTTP 400 status when all files fail validation or upload

## Backward Compatibility

The original `/api/posts` endpoint continues to work for single media files. New applications should use `/api/posts/with-media` for multiple media support.

## Rate Limiting

Multiple file uploads are subject to the same rate limiting as other API endpoints. Consider the increased processing time for multiple large files.

## Best Practices

1. **Validate files client-side** before uploading to reduce server load
2. **Show upload progress** for better user experience
3. **Handle partial failures** gracefully by informing users which files failed
4. **Compress large files** when possible to stay within size limits
5. **Use appropriate file formats** for better compatibility and performance
