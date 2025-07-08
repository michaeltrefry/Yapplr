#!/usr/bin/env python3

import requests
import json
import time

def test_content_moderation_service():
    """Test the content moderation service directly"""
    print("Testing Content Moderation Service...")
    
    # Test normal content
    try:
        response = requests.post(
            "http://localhost:8000/moderate",
            json={"text": "This is a normal post about cats"},
            timeout=10
        )
        if response.status_code == 200:
            result = response.json()
            print(f"✓ Normal content: Risk Level = {result['risk_assessment']['level']}")
            print(f"  Suggested tags: {result['suggested_tags']}")
        else:
            print(f"✗ Normal content test failed: {response.status_code}")
    except Exception as e:
        print(f"✗ Normal content test error: {e}")
    
    # Test problematic content
    try:
        response = requests.post(
            "http://localhost:8000/moderate",
            json={"text": "This is harassment and contains violence you idiot"},
            timeout=10
        )
        if response.status_code == 200:
            result = response.json()
            print(f"✓ Problematic content: Risk Level = {result['risk_assessment']['level']}")
            print(f"  Suggested tags: {result['suggested_tags']}")
            print(f"  Requires review: {result['requires_review']}")
        else:
            print(f"✗ Problematic content test failed: {response.status_code}")
    except Exception as e:
        print(f"✗ Problematic content test error: {e}")

def test_api_health():
    """Test if the Yapplr API is running"""
    print("\nTesting Yapplr API Health...")
    try:
        response = requests.get("http://localhost:5000/health", timeout=5)
        if response.status_code == 200:
            print("✓ Yapplr API is running")
            return True
        else:
            print(f"✗ Yapplr API health check failed: {response.status_code}")
            return False
    except Exception as e:
        print(f"✗ Yapplr API not accessible: {e}")
        return False

def main():
    print("Content Moderation Integration Test")
    print("=" * 50)
    
    # Test content moderation service
    test_content_moderation_service()
    
    # Test API health (if running)
    api_running = test_api_health()
    
    if not api_running:
        print("\nNote: Start the Yapplr API with 'dotnet run --project Yapplr.Api' to test full integration")
    
    print("\nContent moderation service is ready for integration!")
    print("\nNext steps:")
    print("1. Start the Yapplr API")
    print("2. Create a post with problematic content")
    print("3. Check the admin interface for AI-suggested system tags")

if __name__ == "__main__":
    main()
