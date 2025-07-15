'use client';

import React from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { useNotifications } from '@/contexts/NotificationContext';
import { useRouter } from 'next/navigation';
import { useEffect, use } from 'react';
import { useQuery } from '@tanstack/react-query';
import { messageApi } from '@/lib/api';
import Sidebar from '@/components/Sidebar';
import MessagesList from '@/components/MessagesList';
import MessageComposer from '@/components/MessageComposer';
import UserAvatar from '@/components/UserAvatar';
import Link from 'next/link';
import { ArrowLeft } from 'lucide-react';

interface ConversationPageProps {
  params: Promise<{ conversationId: string }>;
}

export default function ConversationPage({ params }: ConversationPageProps) {
  const { conversationId } = use(params);
  const { user, isLoading } = useAuth();
  const { refreshUnreadCount } = useNotifications();
  const router = useRouter();

  useEffect(() => {
    if (!isLoading && !user) {
      router.push('/login');
    }
  }, [user, isLoading, router]);

  const { data: conversation, isLoading: conversationLoading, error } = useQuery({
    queryKey: ['conversation', conversationId],
    queryFn: () => messageApi.getConversation(parseInt(conversationId)),
    enabled: !!user && !!conversationId,
  });

  // Mark conversation as read when user opens it
  useEffect(() => {
    if (conversation && user) {
      messageApi.markConversationAsRead(conversation.id).then(() => {
        // Refresh unread count after marking as read
        refreshUnreadCount();
      });
    }
  }, [conversation, user, refreshUnreadCount]);

  if (isLoading || conversationLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-lg text-gray-900">Loading...</div>
      </div>
    );
  }

  if (!user) {
    return null;
  }

  if (error || !conversation) {
    return (
      <div className="min-h-screen bg-gray-50">
        <div className="max-w-6xl mx-auto flex">
          <div className="w-16 lg:w-64 fixed h-full z-10">
            <Sidebar />
          </div>
          <div className="flex-1 ml-16 lg:ml-64">
            <div className="max-w-2xl mx-auto lg:border-x border-gray-200 min-h-screen bg-white">
              <div className="p-8 text-center">
                <div className="text-red-500">Conversation not found or you don&apos;t have access to it.</div>
                <Link href="/messages" className="text-blue-600 hover:text-blue-800 mt-4 inline-block">
                  ← Back to Messages
                </Link>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  const otherParticipant = conversation.participants.find(p => p.id !== user.id);

  if (!otherParticipant) {
    return (
      <div className="min-h-screen bg-gray-50">
        <div className="max-w-6xl mx-auto flex">
          <div className="w-16 lg:w-64 fixed h-full z-10">
            <Sidebar />
          </div>
          <div className="flex-1 ml-16 lg:ml-64">
            <div className="max-w-2xl mx-auto lg:border-x border-gray-200 min-h-screen bg-white">
              <div className="p-8 text-center">
                <div className="text-red-500">Failed to load conversation.</div>
                <Link href="/messages" className="text-blue-600 hover:text-blue-800 mt-4 inline-block">
                  ← Back to Messages
                </Link>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-6xl mx-auto flex">
        {/* Sidebar */}
        <div className="w-16 lg:w-64 fixed h-full z-10">
          <Sidebar />
        </div>

        {/* Main Content */}
        <div className="flex-1 ml-16 lg:ml-64">
          <div className="max-w-2xl mx-auto lg:border-x border-gray-200 min-h-screen bg-white flex flex-col">
            {/* Header */}
            <div className="sticky top-0 bg-white/80 backdrop-blur-md border-b border-gray-200 p-4 z-20">
              <div className="flex items-center space-x-3">
                <Link
                  href="/messages"
                  className="p-2 hover:bg-gray-100 rounded-full transition-colors"
                >
                  <ArrowLeft className="w-5 h-5 text-gray-600" />
                </Link>
                <UserAvatar user={otherParticipant} size="md" clickable={false} />
                <div>
                  <h1 className="font-semibold text-gray-900">@{otherParticipant.username}</h1>
                  {otherParticipant.bio && (
                    <p className="text-sm text-gray-600 truncate max-w-xs">{otherParticipant.bio}</p>
                  )}
                </div>
              </div>
            </div>

            {/* Messages */}
            <div className="flex-1 overflow-hidden">
              <MessagesList conversationId={parseInt(conversationId)} />
            </div>

            {/* Message Composer */}
            <div className="border-t border-gray-200 bg-white">
              <MessageComposer conversationId={parseInt(conversationId)} />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
