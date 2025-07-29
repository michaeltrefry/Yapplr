'use client';

import { useEffect, useState } from 'react';
import { signalRMessagingService } from '@/lib/signalRMessaging';

interface TypingIndicatorProps {
  conversationId: number;
}

interface TypingUser {
  userId: number;
  username: string;
  timestamp: Date;
}

export default function TypingIndicator({ conversationId }: TypingIndicatorProps) {
  const [typingUsers, setTypingUsers] = useState<TypingUser[]>([]);

  useEffect(() => {

    const handleTypingEvent = (action: 'started' | 'stopped', data: any) => {
      if (data.conversationId !== conversationId) {
        return;
      }

      setTypingUsers(prev => {
        if (action === 'started') {
          // Add user to typing list if not already there
          const existingUser = prev.find(u => u.userId === data.userId);
          if (!existingUser) {
            const newUsers = [...prev, {
              userId: data.userId,
              username: data.username,
              timestamp: new Date(data.timestamp)
            }];
            return newUsers;
          }
          return prev;
        } else {
          // Remove user from typing list
          const newUsers = prev.filter(u => u.userId !== data.userId);
          return newUsers;
        }
      });
    };

    // Add typing listener
    signalRMessagingService.addTypingListener(handleTypingEvent);

    // Cleanup function
    return () => {
      signalRMessagingService.removeTypingListener(handleTypingEvent);
    };
  }, [conversationId]);

  // Auto-cleanup typing indicators after 5 seconds of inactivity
  useEffect(() => {
    const interval = setInterval(() => {
      const now = new Date();
      setTypingUsers(prev => 
        prev.filter(user => {
          const timeDiff = now.getTime() - user.timestamp.getTime();
          return timeDiff < 5000; // Remove if older than 5 seconds
        })
      );
    }, 1000);

    return () => clearInterval(interval);
  }, []);

  if (typingUsers.length === 0) {
    return null;
  }

  const getTypingText = () => {
    if (typingUsers.length === 1) {
      return `@${typingUsers[0].username} is typing`;
    } else if (typingUsers.length === 2) {
      return `@${typingUsers[0].username} and @${typingUsers[1].username} are typing`;
    } else {
      return `@${typingUsers[0].username} and ${typingUsers.length - 1} others are typing`;
    }
  };

  return (
    <div className="px-4 py-2 text-sm text-gray-500 italic">
      <div className="flex items-center space-x-2">
        <span>{getTypingText()}</span>
        <div className="flex space-x-1">
          <div className="w-1 h-1 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '0ms' }}></div>
          <div className="w-1 h-1 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '150ms' }}></div>
          <div className="w-1 h-1 bg-gray-400 rounded-full animate-bounce" style={{ animationDelay: '300ms' }}></div>
        </div>
      </div>
    </div>
  );
}
