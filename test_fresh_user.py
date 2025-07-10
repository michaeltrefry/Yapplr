#!/usr/bin/env python3

import requests
import json

def test_fresh_user():
    """Test system message with a fresh user who has unhidden posts"""
    
    base_url = "http://localhost:5161/api"
    
    # Login as admin
    print("🔐 Logging in as admin...")
    admin_login_response = requests.post(f"{base_url}/auth/login", json={
        "email": "admin@yapplr.com",
        "password": "P@$$w0rd!"
    })
    
    if admin_login_response.status_code != 200:
        print(f"❌ Failed to login as admin: {admin_login_response.status_code}")
        return False
    
    admin_token = admin_login_response.json()["token"]
    admin_headers = {"Authorization": f"Bearer {admin_token}"}
    
    print("✅ Admin login successful")
    
    # Get all posts to find a user with unhidden posts
    print("📋 Getting all posts...")
    posts_response = requests.get(f"{base_url}/admin/posts", headers=admin_headers)
    if posts_response.status_code != 200:
        print(f"❌ Failed to get posts: {posts_response.status_code}")
        return False
    
    posts = posts_response.json()
    print(f"✅ Found {len(posts)} total posts")
    
    # Find a user with unhidden posts
    unhidden_posts = [p for p in posts if not p.get('isHidden', False)]
    print(f"📋 Found {len(unhidden_posts)} unhidden posts")
    
    if not unhidden_posts:
        print("⚠️ No unhidden posts found")
        return False
    
    # Pick the first unhidden post
    test_post = unhidden_posts[0]
    test_username = test_post['user']['username']
    test_user_id = test_post['user']['id']
    
    print(f"🎯 Testing with user {test_username} (ID: {test_user_id})")
    print(f"   Post ID: {test_post['id']}")
    print(f"   Content: {test_post['content'][:50]}...")
    
    # Login as the test user
    print(f"🔐 Logging in as {test_username}...")
    user_login_response = requests.post(f"{base_url}/auth/login", json={
        "email": f"{test_username}@example.com",
        "password": "P@$$w0rd!"
    })
    
    if user_login_response.status_code != 200:
        print(f"❌ Failed to login as {test_username}: {user_login_response.status_code}")
        return False
    
    user_token = user_login_response.json()["token"]
    user_headers = {"Authorization": f"Bearer {user_token}"}
    
    print(f"✅ User {test_username} login successful")
    
    # Check conversations before hiding post
    print("📬 Checking conversations before hiding post...")
    conversations_before = requests.get(f"{base_url}/messages/conversations", headers=user_headers)
    
    if conversations_before.status_code == 200:
        conv_before = conversations_before.json()
        print(f"📋 Conversations before: {len(conv_before)}")
        
        for i, conv in enumerate(conv_before):
            print(f"  Conversation {i+1}: {conv['otherParticipant']['username']}")
    else:
        print(f"❌ Failed to get conversations before: {conversations_before.status_code}")
        conv_before = []
    
    # Hide the post
    print(f"🚫 Hiding post {test_post['id']}...")
    hide_response = requests.post(
        f"{base_url}/admin/posts/{test_post['id']}/hide",
        headers=admin_headers,
        json={"reason": "Fresh user system message test"}
    )
    
    if hide_response.status_code != 200:
        print(f"❌ Failed to hide post: {hide_response.status_code}")
        print(f"Response: {hide_response.text}")
        return False
    
    print("✅ Post hidden successfully")
    
    # Wait a moment for processing
    import time
    time.sleep(3)
    
    # Check conversations after hiding post
    print("📬 Checking conversations after hiding post...")
    conversations_after = requests.get(f"{base_url}/messages/conversations", headers=user_headers)
    
    if conversations_after.status_code == 200:
        conv_after = conversations_after.json()
        print(f"📋 Conversations after: {len(conv_after)}")
        
        for i, conv in enumerate(conv_after):
            print(f"  Conversation {i+1}: {conv['otherParticipant']['username']}")
            if conv.get('lastMessage'):
                print(f"    Last message: {conv['lastMessage']['content'][:50]}...")
        
        # Check if we have a new conversation
        if len(conv_after) > len(conv_before):
            print("✅ New conversation detected!")
            
            # Find the system conversation
            for conv in conv_after:
                if conv['otherParticipant']['username'] == 'yapplr_system':
                    print("✅ Found system conversation!")
                    print(f"   Conversation ID: {conv['id']}")
                    print(f"   Unread count: {conv.get('unreadCount', 0)}")
                    
                    # Get messages in the system conversation
                    messages_response = requests.get(
                        f"{base_url}/messages/conversations/{conv['id']}/messages",
                        headers=user_headers
                    )
                    
                    if messages_response.status_code == 200:
                        messages = messages_response.json()
                        print(f"   Messages count: {len(messages)}")
                        
                        for msg in messages:
                            sender = msg.get('sender', {})
                            print(f"     From {sender.get('username', 'unknown')}: {msg['content'][:100]}...")
                    else:
                        print(f"   Failed to get messages: {messages_response.status_code}")
                    
                    return True
            
            print("❌ New conversation found but it's not a system conversation")
            return False
        else:
            print("❌ No new conversations found")
            return False
    else:
        print(f"❌ Failed to get conversations after: {conversations_after.status_code}")
        return False

if __name__ == "__main__":
    print("🧪 Testing System Message with Fresh User")
    print("=" * 50)
    
    success = test_fresh_user()
    
    if success:
        print("\n✅ System message test successful!")
    else:
        print("\n❌ System message test failed")
        print("🔍 Check the API implementation and logs")
