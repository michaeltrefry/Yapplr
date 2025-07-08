#!/usr/bin/env python3

import requests
import json

def test_health():
    """Test the health endpoint"""
    try:
        response = requests.get("http://localhost:8000/health")
        print(f"Health check: {response.status_code}")
        print(f"Response: {response.text}")
        return response.status_code == 200
    except Exception as e:
        print(f"Health check failed: {e}")
        return False

def test_moderation():
    """Test the moderation endpoint"""
    try:
        data = {"text": "This is a normal post"}
        response = requests.post(
            "http://localhost:8000/moderate",
            json=data,
            headers={"Content-Type": "application/json"},
            timeout=30
        )
        print(f"Moderation test: {response.status_code}")
        print(f"Response: {response.text}")
        return response.status_code == 200
    except Exception as e:
        print(f"Moderation test failed: {e}")
        return False

def test_problematic_content():
    """Test with problematic content"""
    try:
        data = {"text": "This content contains violence and harassment"}
        response = requests.post(
            "http://localhost:8000/moderate",
            json=data,
            headers={"Content-Type": "application/json"},
            timeout=30
        )
        print(f"Problematic content test: {response.status_code}")
        print(f"Response: {response.text}")
        if response.status_code == 200:
            result = response.json()
            print(f"Suggested tags: {result.get('suggested_tags', {})}")
            print(f"Risk level: {result.get('risk_assessment', {}).get('level', 'UNKNOWN')}")
        return response.status_code == 200
    except Exception as e:
        print(f"Problematic content test failed: {e}")
        return False

if __name__ == "__main__":
    print("Testing Content Moderation Service")
    print("=" * 40)
    
    print("\n1. Testing health endpoint...")
    health_ok = test_health()
    
    if health_ok:
        print("\n2. Testing moderation with normal content...")
        test_moderation()
        
        print("\n3. Testing moderation with problematic content...")
        test_problematic_content()
    else:
        print("Health check failed, skipping other tests")
