#!/usr/bin/env python3

import requests
import json

def test_database_check():
    """Check if system conversations exist in the database"""
    
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
    
    # Get user ethan_d
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
    
    # Try to send a direct message to the user to create a regular conversation first
    print("📨 Creating a regular conversation for comparison...")
    
    # Login as another user to send a message to ethan_d
    alice_login_response = requests.post(f"{base_url}/auth/login", json={
        "email": "alice_j@example.com",
        "password": "P@$$w0rd!"
    })
    
    if alice_login_response.status_code == 200:
        alice_token = alice_login_response.json()["token"]
        alice_headers = {"Authorization": f"Bearer {alice_token}"}
        alice_id = alice_login_response.json()["user"]["id"]
        
        print(f"✅ Alice login successful (ID: {alice_id})")
        
        # Send a message from alice to ethan_d
        message_response = requests.post(f"{base_url}/messages", 
            headers=alice_headers,
            json={
                "recipientId": user_id,
                "content": "Test message to create a regular conversation"
            }
        )
        
        if message_response.status_code == 201:
            print("✅ Regular message sent successfully")
        else:
            print(f"⚠️ Failed to send regular message: {message_response.status_code}")
    else:
        print(f"⚠️ Failed to login as alice: {alice_login_response.status_code}")
    
    # Now check conversations for ethan_d
    print("📬 Checking conversations for ethan_d...")
    conversations_response = requests.get(f"{base_url}/messages/conversations", headers=user_headers)
    
    if conversations_response.status_code == 200:
        conversations = conversations_response.json()
        print(f"📋 Found {len(conversations)} conversations")
        
        for i, conv in enumerate(conversations):
            print(f"  Conversation {i+1}:")
            print(f"    ID: {conv['id']}")
            print(f"    Participants count: {len(conv.get('participants', []))}")
            print(f"    Other participant: {conv['otherParticipant']['username']}")
            print(f"    Unread count: {conv.get('unreadCount', 0)}")
            if conv.get('lastMessage'):
                print(f"    Last message: {conv['lastMessage']['content'][:50]}...")
    else:
        print(f"❌ Failed to get conversations: {conversations_response.status_code}")
    
    # Now hide a post to trigger system message
    print("\n🚫 Hiding a post to trigger system message...")
    
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
    print(f"🎯 Hiding post ID {test_post['id']}")
    
    hide_response = requests.post(
        f"{base_url}/admin/posts/{test_post['id']}/hide",
        headers=admin_headers,
        json={"reason": "Database check test"}
    )
    
    if hide_response.status_code != 200:
        print(f"❌ Failed to hide post: {hide_response.status_code}")
        print(f"Response: {hide_response.text}")
        return False
    
    print("✅ Post hidden successfully")
    
    # Wait a moment
    import time
    time.sleep(2)
    
    # Check conversations again
    print("📬 Checking conversations again after hiding post...")
    conversations_response2 = requests.get(f"{base_url}/messages/conversations", headers=user_headers)
    
    if conversations_response2.status_code == 200:
        conversations2 = conversations_response2.json()
        print(f"📋 Found {len(conversations2)} conversations")
        
        for i, conv in enumerate(conversations2):
            print(f"  Conversation {i+1}:")
            print(f"    ID: {conv['id']}")
            print(f"    Other participant: {conv['otherParticipant']['username']}")
            print(f"    Unread count: {conv.get('unreadCount', 0)}")
            if conv.get('lastMessage'):
                print(f"    Last message: {conv['lastMessage']['content'][:50]}...")
        
        # Look for system conversation
        system_conv = None
        for conv in conversations2:
            if conv['otherParticipant']['username'] == 'yapplr_system':
                system_conv = conv
                break
        
        if system_conv:
            print("✅ Found system conversation!")
            return True
        else:
            print("❌ No system conversation found")
            
            # Let's try to get messages for any conversation to see if there are system messages
            if conversations2:
                for conv in conversations2:
                    print(f"\n🔍 Checking messages in conversation {conv['id']}...")
                    messages_response = requests.get(
                        f"{base_url}/messages/conversations/{conv['id']}/messages",
                        headers=user_headers
                    )
                    
                    if messages_response.status_code == 200:
                        messages = messages_response.json()
                        print(f"  Found {len(messages)} messages")
                        for msg in messages:
                            sender_info = msg.get('sender', {})
                            print(f"    Message from {sender_info.get('username', 'unknown')}: {msg['content'][:50]}...")
                    else:
                        print(f"  Failed to get messages: {messages_response.status_code}")
            
            return False
    else:
        print(f"❌ Failed to get conversations after hiding: {conversations_response2.status_code}")
        return False

if __name__ == "__main__":
    print("🔍 Database Check for System Conversations")
    print("=" * 50)
    
    success = test_database_check()
    
    if success:
        print("\n✅ System conversations are working!")
    else:
        print("\n❌ System conversations are not working")
        print("🔍 Check the API logs and implementation")
