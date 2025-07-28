# Simple Video Processing Service

## Overview

The `SimpleVideoProcessingService` is a direct FFmpeg implementation that replaces the problematic FFMpegCore-based video processor. This implementation addresses rotation detection and scaling issues that were causing problems with portrait videos, particularly those with -90° rotation metadata.

## Key Features

- **Direct FFmpeg Execution**: Uses `Process.Start()` to execute FFmpeg directly, avoiding FFMpegCore wrapper issues
- **Reliable Rotation Handling**: Properly detects and corrects video rotation using proven FFmpeg commands
- **Accurate Scaling**: Maintains aspect ratios and ensures even dimensions for video encoding
- **Battle-tested Commands**: Uses FFmpeg patterns proven in production systems like PeerTube
- **Comprehensive Logging**: Detailed logging for debugging and monitoring
- **Feature Flag Support**: Can be enabled/disabled via configuration

## Configuration

### Feature Flag

The service is controlled by the `UseSimpleProcessor` configuration flag:

```json
{
  "VideoProcessing": {
    "UseSimpleProcessor": true,  // Enable SimpleVideoProcessingService
    // ... other video processing settings
  }
}
```

### Environment-Specific Settings

- **Development**: `UseSimpleProcessor: true` (enabled for testing)
- **Staging**: `UseSimpleProcessor: true` (enabled for testing)
- **Production**: `UseSimpleProcessor: false` (disabled by default for safety)

### FFmpeg Configuration

```json
{
  "FFmpeg": {
    "BinaryPath": "/usr/bin/ffmpeg",     // Path to ffmpeg binary
    "ProbePath": "/usr/bin/ffprobe",     // Path to ffprobe binary
    "WorkingDirectory": "/tmp"           // Working directory for temp files
  }
}
```

## How It Works

### 1. Video Information Detection

The service uses `ffprobe` to extract video metadata:

```bash
ffprobe -v quiet -print_format json -show_format -show_streams "input.mp4"
```

This provides:
- Video dimensions (width, height)
- Duration
- Rotation metadata from tags or side_data_list
- Codec information
- Pixel format

### 2. Rotation Detection

The service checks multiple sources for rotation information:

1. **Tags**: `stream.tags.rotate`
2. **Side Data**: `stream.side_data_list[].rotation` (newer FFmpeg versions)

Rotation values are normalized to 0°, 90°, 180°, or 270°.

### 3. Dimension Calculation

For rotated videos, display dimensions are calculated:

```csharp
// For 90° and 270° rotations, swap width and height
if (normalizedRotation == 90 || normalizedRotation == 270)
{
    return (height, width);  // Swap dimensions
}
return (width, height);      // Keep original dimensions
```

### 4. Video Processing

The service builds FFmpeg commands using proven patterns:

```bash
ffmpeg -i "input.mp4" \
  -c:v libx264 \
  -c:a aac \
  -b:v 2000k \
  -vf "transpose=1,scale=1920:1080" \
  -movflags +faststart \
  -metadata:s:v:0 rotate=0 \
  -y "output.mp4"
```

### 5. Rotation Correction

Different rotation values use different transpose filters:

- **0°**: No rotation - `scale=W:H`
- **90°**: Clockwise - `transpose=1,scale=W:H`
- **180°**: Upside down - `transpose=1,transpose=1,scale=W:H`
- **270°**: Counter-clockwise - `transpose=2,scale=W:H`

### 6. Thumbnail Generation

Thumbnails are generated with the same rotation correction:

```bash
ffmpeg -i "input.mp4" \
  -vf "transpose=1,scale=320:240" \
  -vframes 1 \
  -ss 1.0 \
  -y "thumbnail.jpg"
```

## Problem Solved

### Original Issue

Portrait videos (1920x1080 with -90° rotation) were being processed incorrectly:
- FFMpegCore wrapper was producing wrong dimensions (606x1080 instead of proper portrait)
- Complex rotation detection logic was unreliable
- Scaling calculations were inconsistent

### Solution

The SimpleVideoProcessingService:
1. **Detects rotation correctly** using direct ffprobe JSON parsing
2. **Calculates proper display dimensions** by swapping width/height for 90°/270° rotations
3. **Uses proven FFmpeg commands** that work reliably across different video types
4. **Ensures consistent behavior** with battle-tested patterns

## Testing

### Unit Tests

Comprehensive unit tests cover:
- Rotation normalization (handles negative rotations, >360°, etc.)
- Display dimension calculation for all rotation scenarios
- Target dimension calculation with aspect ratio preservation
- FFmpeg command building
- Filter string generation

### Integration Tests

Integration tests verify:
- Complete video processing workflow
- Metadata extraction accuracy
- File output verification
- Error handling for invalid inputs

### Manual Testing

Run the video processor to see the feature flag in action:

```bash
dotnet run --project Yapplr.VideoProcessor
```

Look for the log message:
```
Using SimpleVideoProcessingService (direct FFmpeg implementation)
```

## Deployment Strategy

### Phase 1: Testing (Current)
- Enable in Development and Staging environments
- Test with various video types and rotations
- Monitor processing success rates

### Phase 2: Production Rollout
- Enable feature flag in Production
- Monitor performance and error rates
- Compare results with old implementation

### Phase 3: Cleanup
- Remove FFMpegCore dependency
- Delete old VideoProcessingService
- Update all configurations to use new service

## Rollback Plan

If issues arise:

1. **Immediate**: Set `UseSimpleProcessor: false` in configuration
2. **Redeploy**: The old FFMpegCore service will be used
3. **Monitor**: Verify processing returns to previous behavior

## Performance Considerations

### Advantages
- **Simpler execution path**: Direct process execution vs. wrapper overhead
- **Better error handling**: Direct access to FFmpeg stderr output
- **More reliable**: Fewer abstraction layers to fail

### Resource Usage
- **Memory**: Similar to FFMpegCore (FFmpeg process overhead)
- **CPU**: Identical (same FFmpeg operations)
- **Disk**: Same temporary file usage

## Monitoring

Key metrics to monitor:
- Processing success rate
- Processing duration
- Error types and frequencies
- Output file quality
- Rotation correction accuracy

## Troubleshooting

### Common Issues

1. **FFmpeg not found**
   - Verify `FFmpeg:BinaryPath` configuration
   - Ensure FFmpeg is installed and accessible

2. **Permission errors**
   - Check file system permissions for input/output directories
   - Verify FFmpeg binary execution permissions

3. **Codec errors**
   - Run codec compatibility tests on startup
   - Check available codecs with `ffmpeg -codecs`

4. **Rotation not detected**
   - Verify video has rotation metadata
   - Check ffprobe output for rotation information

### Debug Logging

Enable debug logging to see detailed FFmpeg commands:

```json
{
  "Logging": {
    "LogLevel": {
      "Yapplr.VideoProcessor.Services.SimpleVideoProcessingService": "Debug"
    }
  }
}
```

This will log:
- FFmpeg commands being executed
- ffprobe JSON output
- Processing steps and decisions
- Error details from FFmpeg stderr

## Future Enhancements

Potential improvements:
- **Parallel processing**: Multiple videos simultaneously
- **Progress reporting**: Real-time processing progress
- **Quality presets**: Different quality/speed tradeoffs
- **Format detection**: Automatic input format handling
- **Batch processing**: Multiple files in single operation
