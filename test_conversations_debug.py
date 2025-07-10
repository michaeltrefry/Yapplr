#!/usr/bin/env python3

import requests
import json

def test_conversations_debug():
    """Debug conversations endpoint to see what's happening"""
    
    base_url = "http://localhost:5161/api"
    
    # Login as ethan_d (the user who should have received the system message)
    print("🔐 Logging in as ethan_d...")
    login_response = requests.post(f"{base_url}/auth/login", json={
        "email": "ethan_d@example.com",
        "password": "P@$$w0rd!"
    })
    
    if login_response.status_code != 200:
        print(f"❌ Failed to login: {login_response.status_code}")
        print(f"Response: {login_response.text}")
        return False
    
    token = login_response.json()["token"]
    headers = {"Authorization": f"Bearer {token}"}
    user_info = login_response.json()["user"]
    
    print(f"✅ Login successful for user ID {user_info['id']}")
    
    # Get conversations
    print("📬 Getting conversations...")
    conversations_response = requests.get(f"{base_url}/messages/conversations", headers=headers)
    
    if conversations_response.status_code != 200:
        print(f"❌ Failed to get conversations: {conversations_response.status_code}")
        print(f"Response: {conversations_response.text}")
        return False
    
    conversations = conversations_response.json()
    print(f"📋 Found {len(conversations)} conversations")
    
    if conversations:
        for i, conv in enumerate(conversations):
            print(f"  Conversation {i+1}:")
            print(f"    ID: {conv['id']}")
            print(f"    Other participant: {conv['otherParticipant']['username']}")
            print(f"    Unread count: {conv.get('unreadCount', 0)}")
            if conv.get('lastMessage'):
                print(f"    Last message: {conv['lastMessage']['content'][:50]}...")
    else:
        print("  No conversations found")
    
    # Let's also check if there are any system conversations in the database
    # by looking at the raw database or checking with admin privileges
    print("\n🔍 Checking with admin privileges...")
    
    # Login as admin
    admin_login_response = requests.post(f"{base_url}/auth/login", json={
        "email": "admin@yapplr.com",
        "password": "P@$$w0rd!"
    })
    
    if admin_login_response.status_code == 200:
        admin_token = admin_login_response.json()["token"]
        admin_headers = {"Authorization": f"Bearer {admin_token}"}
        
        # Check if there are any conversations for this user from admin perspective
        # (This would require an admin endpoint, which might not exist)
        print("✅ Admin login successful")
        
        # Let's try to send a test system message to see if it works
        print("📨 Attempting to send a test system message...")
        
        # We need to find a way to trigger a system message
        # Let's try hiding another post by the same user
        posts_response = requests.get(f"{base_url}/admin/posts", headers=admin_headers)
        if posts_response.status_code == 200:
            posts = posts_response.json()
            ethan_posts = [p for p in posts if p['user']['username'] == 'ethan_d' and not p.get('isHidden', False)]
            
            if ethan_posts:
                test_post = ethan_posts[0]
                print(f"🎯 Hiding another post by ethan_d (ID: {test_post['id']})")
                
                hide_response = requests.post(
                    f"{base_url}/admin/posts/{test_post['id']}/hide",
                    headers=admin_headers,
                    json={"reason": "Second test for system message debugging"}
                )
                
                if hide_response.status_code == 200:
                    print("✅ Second post hidden successfully")
                    
                    # Now check conversations again
                    print("📬 Checking conversations again...")
                    conversations_response2 = requests.get(f"{base_url}/messages/conversations", headers=headers)
                    
                    if conversations_response2.status_code == 200:
                        conversations2 = conversations_response2.json()
                        print(f"📋 Now found {len(conversations2)} conversations")
                        
                        for i, conv in enumerate(conversations2):
                            print(f"  Conversation {i+1}:")
                            print(f"    ID: {conv['id']}")
                            print(f"    Other participant: {conv['otherParticipant']['username']}")
                            print(f"    Unread count: {conv.get('unreadCount', 0)}")
                            if conv.get('lastMessage'):
                                print(f"    Last message: {conv['lastMessage']['content'][:50]}...")
                        
                        return len(conversations2) > 0
                    else:
                        print(f"❌ Failed to get conversations after second test: {conversations_response2.status_code}")
                else:
                    print(f"❌ Failed to hide second post: {hide_response.status_code}")
            else:
                print("⚠️ No more unhidden posts by ethan_d found")
        else:
            print(f"❌ Failed to get posts for admin: {posts_response.status_code}")
    else:
        print(f"❌ Failed to login as admin: {admin_login_response.status_code}")
    
    return False

if __name__ == "__main__":
    print("🔍 Debugging Conversations Endpoint")
    print("=" * 50)
    
    success = test_conversations_debug()
    
    if success:
        print("\n✅ System conversations are working!")
    else:
        print("\n❌ System conversations are not working properly")
        print("🔍 Check the API implementation and database")
