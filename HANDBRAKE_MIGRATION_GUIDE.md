# HandBrake Migration Guide

This document outlines the migration from FFmpeg to HandBrake for video processing in the Yapplr video processor service.

## Overview

We've implemented a **hybrid approach** that leverages the strengths of both tools:
- **HandBrake CLI**: Primary video transcoding and encoding
- **FFmpeg**: Metadata extraction and thumbnail generation

This approach provides superior video quality from HandBrake while maintaining compatibility with existing metadata and thumbnail functionality.

## Architecture Changes

### New Service Implementation

- **`HandBrakeVideoProcessingService`**: New implementation of `IVideoProcessingService`
- **`HandBrakeCodecTestService`**: New codec testing service for HandBrake
- **Hybrid Processing**: Uses HandBrake for video transcoding, FFmpeg for metadata/thumbnails

### Configuration Structure

```json
{
  "VideoProcessing": {
    "UseSimpleProcessor": false,
    "UseHandBrakeProcessor": false,  // NEW: Enable HandBrake processing
    // ... existing settings
  },
  "HandBrake": {                     // NEW: HandBrake-specific configuration
    "BinaryPath": "/usr/bin/HandBrakeCLI",
    "WorkingDirectory": "/tmp",
    "TimeoutSeconds": 300,
    "LogLevel": "error",
    "DefaultPreset": "Fast 1080p30",
    "QualityMode": "constant_quality",
    "ConstantQuality": 22.0,
    "EncoderPreset": "medium",
    "EncoderProfile": "baseline",
    "EncoderLevel": "3.1"
  }
}
```

## Migration Steps

### 1. Enable HandBrake in Configuration

Update your environment-specific configuration files:

**Development/Staging (Testing):**
```json
{
  "VideoProcessing": {
    "UseHandBrakeProcessor": true
  }
}
```

**Production (After Testing):**
```json
{
  "VideoProcessing": {
    "UseHandBrakeProcessor": true
  }
}
```

### 2. Docker Infrastructure

The Docker configuration has been updated to install both HandBrake CLI and FFmpeg:

```dockerfile
# Install HandBrake CLI and FFmpeg with comprehensive codec support
RUN apt-get update && apt-get install -y \
    handbrake-cli \
    ffmpeg \
    # ... other dependencies
```

### 3. Environment Variables

Add HandBrake configuration to your Docker Compose files:

```yaml
environment:
  # HandBrake configuration
  - HandBrake__BinaryPath=/usr/bin/HandBrakeCLI
  - HandBrake__WorkingDirectory=/tmp
  - HandBrake__TimeoutSeconds=300
  - HandBrake__LogLevel=error
```

## Key Differences

### Video Processing Quality

| Aspect | FFmpeg | HandBrake |
|--------|--------|-----------|
| **Quality** | Good | Superior (optimized presets) |
| **Compression** | Standard | Better (advanced algorithms) |
| **Mobile Compatibility** | Manual tuning required | Built-in optimizations |
| **Web Streaming** | Manual optimization | Automatic fast-start |

### Feature Comparison

| Feature | FFmpeg | HandBrake | Implementation |
|---------|--------|-----------|----------------|
| **Video Transcoding** | ✅ | ✅ | HandBrake (primary) |
| **Thumbnail Generation** | ✅ | ❌ | FFmpeg (fallback) |
| **Metadata Extraction** | ✅ | ❌ | FFmpeg (fallback) |
| **Rotation Correction** | ✅ | ✅ | HandBrake (primary) |
| **Format Support** | Extensive | Good | Combined coverage |

### Command Line Differences

**FFmpeg Example:**
```bash
ffmpeg -i input.mp4 -c:v libx264 -b:v 2000k -vf "scale=1920:1080,transpose=1" output.mp4
```

**HandBrake Example:**
```bash
HandBrakeCLI -i input.mp4 -o output.mp4 -e x264 -q 22 -w 1920 -l 1080 --rotate angle=90
```

## Configuration Options

### HandBrake Quality Settings

- **Constant Quality Mode**: Uses `-q` parameter (recommended: 18-28)
- **Bitrate Mode**: Uses `-b` parameter (fallback option)
- **Encoder Presets**: `ultrafast`, `fast`, `medium`, `slow`, `veryslow`

### Codec Mapping

| FFmpeg Codec | HandBrake Codec | Notes |
|--------------|-----------------|-------|
| `libx264` | `x264` | H.264 encoding |
| `libx265` | `x265` | H.265/HEVC encoding |
| `libvpx` | `VP8` | VP8 encoding |
| `libvpx-vp9` | `VP9` | VP9 encoding |
| `aac` | `av_aac` | AAC audio |
| `libmp3lame` | `mp3` | MP3 audio |

## Testing and Validation

### Codec Test Service

The new `HandBrakeCodecTestService` tests:
- HandBrake CLI installation
- Available video encoders
- FFmpeg installation (for metadata/thumbnails)
- Basic processing functionality

### Validation Steps

1. **Build and Deploy**: Ensure Docker builds successfully
2. **Codec Tests**: Verify all required codecs are available
3. **Processing Tests**: Test video transcoding with various formats
4. **Quality Comparison**: Compare output quality with previous FFmpeg results
5. **Performance Monitoring**: Monitor processing times and resource usage

## Rollback Plan

If issues arise, you can quickly rollback by setting:

```json
{
  "VideoProcessing": {
    "UseHandBrakeProcessor": false,
    "UseSimpleProcessor": true  // or false for FFMpegCore
  }
}
```

## Performance Considerations

### Expected Improvements

- **Better Compression**: 10-30% smaller file sizes at same quality
- **Improved Quality**: Better visual quality, especially for mobile devices
- **Web Optimization**: Automatic fast-start for streaming

### Potential Trade-offs

- **Processing Time**: May be slightly slower due to quality optimizations
- **Resource Usage**: Similar CPU usage, potentially higher memory usage
- **Complexity**: Additional tool dependency

## Monitoring and Logging

### Key Metrics to Monitor

- Processing success rate
- Average processing time
- Output file sizes
- Quality metrics (if available)
- Error rates

### Log Messages

HandBrake processing includes detailed logging:
```
[INFO] Starting HandBrake video processing: input.mp4 -> output.mp4
[INFO] HandBrake command: HandBrakeCLI -i "input.mp4" -o "output.mp4" ...
[INFO] HandBrake video processing completed successfully
```

## Troubleshooting

### Common Issues

1. **HandBrake Not Found**: Verify `HandBrake__BinaryPath` configuration
2. **Codec Errors**: Check encoder availability with codec test service
3. **Permission Issues**: Ensure proper file permissions in Docker container
4. **Memory Issues**: Monitor container memory usage during processing

### Debug Steps

1. Enable verbose logging: `HandBrake__LogLevel=info`
2. Run codec tests manually
3. Check Docker container logs
4. Verify file permissions and paths

## Next Steps

1. **Deploy to Staging**: Test with real video uploads
2. **Monitor Performance**: Track processing metrics
3. **Quality Assessment**: Compare output quality
4. **Production Deployment**: Roll out after successful staging tests
5. **Cleanup**: Remove unused FFmpeg dependencies (optional, after full migration)

## Support

For issues or questions regarding the HandBrake migration:
1. Check the codec test results in application logs
2. Verify configuration settings
3. Test with sample videos
4. Monitor processing metrics
