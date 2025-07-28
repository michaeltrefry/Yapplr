#!/usr/bin/env python3
"""
Test script to verify rotation detection logic for iPhone videos
"""
import json
import subprocess
import sys

def get_video_metadata(video_path):
    """Get video metadata using ffprobe"""
    cmd = [
        'ffprobe', '-v', 'quiet', '-print_format', 'json', 
        '-show_format', '-show_streams', video_path
    ]
    
    try:
        result = subprocess.run(cmd, capture_output=True, text=True, check=True)
        return json.loads(result.stdout)
    except subprocess.CalledProcessError as e:
        print(f"Error running ffprobe: {e}")
        return None
    except json.JSONDecodeError as e:
        print(f"Error parsing JSON: {e}")
        return None

def extract_rotation_info(metadata):
    """Extract rotation information from metadata"""
    if not metadata or 'streams' not in metadata:
        return None
    
    # Find video stream
    video_stream = None
    for stream in metadata['streams']:
        if stream.get('codec_type') == 'video':
            video_stream = stream
            break
    
    if not video_stream:
        return None
    
    rotation_info = {
        'width': video_stream.get('width'),
        'height': video_stream.get('height'),
        'rotate_tag': None,
        'display_matrix_rotation': None,
        'has_conflict': False
    }
    
    # Check for rotate tag
    tags = video_stream.get('tags', {})
    if 'rotate' in tags:
        try:
            rotation_info['rotate_tag'] = int(tags['rotate'])
        except ValueError:
            pass
    
    # Check for display matrix
    side_data_list = video_stream.get('side_data_list', [])
    for side_data in side_data_list:
        if side_data.get('side_data_type') == 'Display Matrix':
            if 'rotation' in side_data:
                try:
                    rotation_info['display_matrix_rotation'] = float(side_data['rotation'])
                except ValueError:
                    pass
            break
    
    # Check for conflict
    if (rotation_info['rotate_tag'] is not None and 
        rotation_info['display_matrix_rotation'] is not None):
        rotation_info['has_conflict'] = True
    
    return rotation_info

def resolve_rotation_conflict(rotation_info):
    """Apply the same logic as our C# implementation"""
    if not rotation_info:
        return 0
    
    rotate_tag = rotation_info['rotate_tag']
    display_matrix = rotation_info['display_matrix_rotation']
    
    if rotate_tag is not None and display_matrix is not None:
        # Conflict case - prioritize display matrix with conversion
        print(f"⚠️  CONFLICT DETECTED: Rotate tag={rotate_tag}°, Display matrix={display_matrix}°")
        
        if display_matrix == -90:
            resolved = 270
        elif display_matrix == -180:
            resolved = 180
        elif display_matrix == -270:
            resolved = 90
        else:
            resolved = int(display_matrix) % 360
            if resolved < 0:
                resolved += 360
        
        print(f"✅ RESOLVED: Using {resolved}° (converted from display matrix {display_matrix}°)")
        return resolved
        
    elif display_matrix is not None:
        # Only display matrix
        resolved = int(display_matrix) % 360
        if resolved < 0:
            resolved += 360
        print(f"📐 Using display matrix: {resolved}°")
        return resolved
        
    elif rotate_tag is not None:
        # Only rotate tag
        resolved = rotate_tag % 360
        print(f"🏷️  Using rotate tag: {resolved}°")
        return resolved
    
    else:
        print("❌ No rotation metadata found")
        return 0

def main():
    if len(sys.argv) != 2:
        print("Usage: python3 test_rotation_detection.py <video_path>")
        sys.exit(1)
    
    video_path = sys.argv[1]
    print(f"🎬 Analyzing video: {video_path}")
    print("=" * 60)
    
    # Get metadata
    metadata = get_video_metadata(video_path)
    if not metadata:
        print("❌ Failed to get video metadata")
        sys.exit(1)
    
    # Extract rotation info
    rotation_info = extract_rotation_info(metadata)
    if not rotation_info:
        print("❌ No video stream found")
        sys.exit(1)
    
    # Display findings
    print(f"📏 Video dimensions: {rotation_info['width']}x{rotation_info['height']}")
    print(f"🏷️  Rotate tag: {rotation_info['rotate_tag']}°" if rotation_info['rotate_tag'] is not None else "🏷️  Rotate tag: None")
    print(f"📐 Display matrix: {rotation_info['display_matrix_rotation']}°" if rotation_info['display_matrix_rotation'] is not None else "📐 Display matrix: None")
    
    if rotation_info['has_conflict']:
        print("⚠️  ROTATION CONFLICT DETECTED!")
    
    print("\n" + "=" * 60)
    
    # Resolve rotation
    final_rotation = resolve_rotation_conflict(rotation_info)
    print(f"🎯 FINAL ROTATION: {final_rotation}°")
    
    # Determine expected display dimensions
    width, height = rotation_info['width'], rotation_info['height']
    if final_rotation in [90, 270]:
        display_width, display_height = height, width
    else:
        display_width, display_height = width, height
    
    print(f"📺 Expected display dimensions: {display_width}x{display_height}")

if __name__ == "__main__":
    main()
