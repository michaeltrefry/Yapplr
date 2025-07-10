#!/usr/bin/env python3

import requests
import json

def test_direct_system_message():
    """Test sending a system message directly"""
    
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
    
    # Get a test user (ethan_d)
    print("👤 Getting user ethan_d...")
    
    # Login as ethan_d to get user ID
    user_login_response = requests.post(f"{base_url}/auth/login", json={
        "email": "ethan_d@example.com",
        "password": "P@$$w0rd!"
    })
    
    if user_login_response.status_code != 200:
        print(f"❌ Failed to login as ethan_d: {user_login_response.status_code}")
        return False
    
    user_info = user_login_response.json()["user"]
    user_token = user_login_response.json()["token"]
    user_headers = {"Authorization": f"Bearer {user_token}"}
    user_id = user_info["id"]
    
    print(f"✅ User ethan_d login successful (ID: {user_id})")
    
    # Check conversations before sending system message
    print("📬 Checking conversations before system message...")
    conversations_before = requests.get(f"{base_url}/messages/conversations", headers=user_headers)
    if conversations_before.status_code == 200:
        conv_count_before = len(conversations_before.json())
        print(f"📋 Conversations before: {conv_count_before}")
    else:
        print(f"❌ Failed to get conversations before: {conversations_before.status_code}")
        conv_count_before = -1
    
    # Now let's try to create a system message by hiding a post
    # But first, let's check if there's a direct endpoint to send system messages
    
    # Get posts by ethan_d
    posts_response = requests.get(f"{base_url}/admin/posts", headers=admin_headers)
    if posts_response.status_code != 200:
        print(f"❌ Failed to get posts: {posts_response.status_code}")
        return False
    
    posts = posts_response.json()
    ethan_posts = [p for p in posts if p['user']['username'] == 'ethan_d' and not p.get('isHidden', False)]
    
    if not ethan_posts:
        print("⚠️ No unhidden posts by ethan_d found")
        return False
    
    test_post = ethan_posts[0]
    print(f"🎯 Using post ID {test_post['id']} for testing")
    
    # Hide the post (this should trigger a system message)
    print("🚫 Hiding post to trigger system message...")
    hide_response = requests.post(
        f"{base_url}/admin/posts/{test_post['id']}/hide",
        headers=admin_headers,
        json={"reason": "Direct system message test"}
    )
    
    if hide_response.status_code != 200:
        print(f"❌ Failed to hide post: {hide_response.status_code}")
        print(f"Response: {hide_response.text}")
        return False
    
    print("✅ Post hidden successfully")
    
    # Wait a moment for the system message to be processed
    import time
    time.sleep(2)
    
    # Check conversations after sending system message
    print("📬 Checking conversations after system message...")
    conversations_after = requests.get(f"{base_url}/messages/conversations", headers=user_headers)
    if conversations_after.status_code == 200:
        conversations = conversations_after.json()
        conv_count_after = len(conversations)
        print(f"📋 Conversations after: {conv_count_after}")
        
        if conv_count_after > conv_count_before:
            print("✅ New conversation detected!")
            for conv in conversations:
                print(f"  Conversation ID: {conv['id']}")
                print(f"  Other participant: {conv['otherParticipant']['username']}")
                print(f"  Unread count: {conv.get('unreadCount', 0)}")
                if conv.get('lastMessage'):
                    print(f"  Last message: {conv['lastMessage']['content'][:100]}...")
            return True
        else:
            print("❌ No new conversations found")
            
            # Let's check if there are any conversations at all
            print("\n🔍 Debugging conversation retrieval...")
            
            # Try to get conversations with different parameters
            conversations_debug = requests.get(f"{base_url}/messages/conversations?page=1&pageSize=50", headers=user_headers)
            if conversations_debug.status_code == 200:
                debug_conversations = conversations_debug.json()
                print(f"📋 Debug conversations (page 1, size 50): {len(debug_conversations)}")
                
                # Print raw response for debugging
                print(f"📋 Raw response: {json.dumps(debug_conversations, indent=2)}")
            else:
                print(f"❌ Failed to get debug conversations: {conversations_debug.status_code}")
            
            return False
    else:
        print(f"❌ Failed to get conversations after: {conversations_after.status_code}")
        print(f"Response: {conversations_after.text}")
        return False

if __name__ == "__main__":
    print("🧪 Testing Direct System Message")
    print("=" * 50)
    
    success = test_direct_system_message()
    
    if success:
        print("\n✅ System message test successful!")
    else:
        print("\n❌ System message test failed")
        print("🔍 Check the API logs and database for issues")
