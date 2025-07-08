#!/usr/bin/env python3

import requests
import json
import time

def test_content_moderation_integration():
    """Test the complete AI content moderation integration"""
    print("🤖 Testing AI Content Moderation Integration")
    print("=" * 60)
    
    # Test 1: Content Moderation Service Health
    print("\n1. Testing Content Moderation Service...")
    try:
        response = requests.get("http://localhost:8000/health", timeout=5)
        if response.status_code == 200:
            result = response.json()
            print(f"✅ Content Moderation Service: {result['status']}")
            print(f"   Model: {result['model']}")
        else:
            print(f"❌ Content Moderation Service: HTTP {response.status_code}")
            return False
    except Exception as e:
        print(f"❌ Content Moderation Service: {e}")
        return False
    
    # Test 2: Normal Content Analysis
    print("\n2. Testing Normal Content Analysis...")
    try:
        normal_content = "I love spending time with my family and friends. Great weather today!"
        response = requests.post(
            "http://localhost:8000/moderate",
            json={"text": normal_content, "include_sentiment": True},
            timeout=10
        )
        if response.status_code == 200:
            result = response.json()
            print(f"✅ Normal Content Analysis:")
            print(f"   Risk Level: {result['risk_assessment']['level']}")
            print(f"   Risk Score: {result['risk_assessment']['score']}")
            print(f"   Requires Review: {result['requires_review']}")
            print(f"   Suggested Tags: {result['suggested_tags']}")
            if result['sentiment']:
                print(f"   Sentiment: {result['sentiment']['label']} ({result['sentiment']['confidence']:.2f})")
        else:
            print(f"❌ Normal Content Analysis: HTTP {response.status_code}")
    except Exception as e:
        print(f"❌ Normal Content Analysis: {e}")
    
    # Test 3: Problematic Content Analysis
    print("\n3. Testing Problematic Content Analysis...")
    try:
        problematic_content = "This is harassment and contains violence you idiot, I hate everyone"
        response = requests.post(
            "http://localhost:8000/moderate",
            json={"text": problematic_content, "include_sentiment": True},
            timeout=10
        )
        if response.status_code == 200:
            result = response.json()
            print(f"✅ Problematic Content Analysis:")
            print(f"   Risk Level: {result['risk_assessment']['level']}")
            print(f"   Risk Score: {result['risk_assessment']['score']}")
            print(f"   Requires Review: {result['requires_review']}")
            print(f"   Suggested Tags: {result['suggested_tags']}")
            if result['sentiment']:
                print(f"   Sentiment: {result['sentiment']['label']} ({result['sentiment']['confidence']:.2f})")
        else:
            print(f"❌ Problematic Content Analysis: HTTP {response.status_code}")
    except Exception as e:
        print(f"❌ Problematic Content Analysis: {e}")
    
    # Test 4: Yapplr API Health Check
    print("\n4. Testing Yapplr API...")
    try:
        response = requests.get("http://localhost:5000/health", timeout=5)
        if response.status_code == 200:
            print("✅ Yapplr API: Running")
        else:
            print(f"⚠️  Yapplr API: HTTP {response.status_code}")
            print("   Note: Start the API with 'dotnet run --project Yapplr.Api' to test full integration")
    except Exception as e:
        print(f"⚠️  Yapplr API: {e}")
        print("   Note: Start the API with 'dotnet run --project Yapplr.Api' to test full integration")
    
    print("\n" + "=" * 60)
    print("🎉 Content Moderation Integration Test Complete!")
    print("\nNext Steps:")
    print("1. Start the Yapplr API: dotnet run --project Yapplr.Api")
    print("2. Apply database migrations: dotnet ef database update")
    print("3. Create a post with problematic content")
    print("4. Check the admin interface at /admin/queue for AI suggestions")
    print("5. Approve or reject AI-suggested system tags")
    
    return True

def test_batch_analysis():
    """Test batch content analysis"""
    print("\n🔄 Testing Batch Content Analysis...")
    
    test_contents = [
        "I love this community!",
        "This is harassment and violence",
        "Great post, thanks for sharing!",
        "You're an idiot and I hate you",
        "Beautiful sunset today"
    ]
    
    try:
        response = requests.post(
            "http://localhost:8000/batch-moderate",
            json={"texts": test_contents, "include_sentiment": True},
            timeout=30
        )
        if response.status_code == 200:
            results = response.json()
            print(f"✅ Batch Analysis: Processed {len(results.get('results', []))} items")
            for i, result in enumerate(results.get('results', [])):
                content = test_contents[i][:30] + "..." if len(test_contents[i]) > 30 else test_contents[i]
                print(f"   {i+1}. '{content}' -> {result['risk_assessment']['level']} risk")
        else:
            print(f"❌ Batch Analysis: HTTP {response.status_code}")
    except Exception as e:
        print(f"❌ Batch Analysis: {e}")

if __name__ == "__main__":
    success = test_content_moderation_integration()
    if success:
        test_batch_analysis()
