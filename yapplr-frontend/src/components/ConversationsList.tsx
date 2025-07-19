'use client';

import { useInfiniteQuery } from '@tanstack/react-query';
import { messageApi } from '@/lib/api';
import { useEffect, useRef } from 'react';
import Link from 'next/link';
import UserAvatar from './UserAvatar';
import { formatDistanceToNow } from 'date-fns';
import { MessageCircle, Image as ImageIcon } from 'lucide-react';

export default function ConversationsList() {
  const loadMoreRef = useRef<HTMLDivElement>(null);

  const {
    data,
    isLoading,
    error,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
  } = useInfiniteQuery({
    queryKey: ['conversations'],
    queryFn: ({ pageParam = 1 }) => messageApi.getConversations(pageParam, 25),
    getNextPageParam: (lastPage, allPages) => {
      if (!lastPage || !Array.isArray(lastPage) || lastPage.length < 25) {
        return undefined;
      }
      return allPages.length + 1;
    },
    initialPageParam: 1,
    refetchInterval: 30000, // Refresh every 30 seconds
    refetchIntervalInBackground: false,
  });

  // Intersection Observer for infinite scroll
  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        const target = entries[0];
        if (target.isIntersecting && hasNextPage && !isFetchingNextPage) {
          fetchNextPage();
        }
      },
      {
        threshold: 0.1,
        rootMargin: '100px',
      }
    );

    const currentRef = loadMoreRef.current;
    if (currentRef) {
      observer.observe(currentRef);
    }

    return () => {
      if (currentRef) {
        observer.unobserve(currentRef);
      }
    };
  }, [fetchNextPage, hasNextPage, isFetchingNextPage]);

  if (isLoading) {
    return (
      <div className="p-8 text-center">
        <div className="text-gray-500">Loading conversations...</div>
      </div>
    );
  }

  if (error) {
    console.error('Conversations error:', error);
    return (
      <div className="p-8 text-center">
        <div className="text-red-500">Failed to load conversations</div>
        <div className="text-sm text-gray-500 mt-2">
          {error instanceof Error ? error.message : 'Unknown error'}
        </div>
      </div>
    );
  }

  // Flatten pages and deduplicate conversations by ID
  const allConversations = data?.pages.flat() || [];
  const conversations = allConversations.reduce((acc, conversation) => {
    if (!acc.find(c => c.id === conversation.id)) {
      acc.push(conversation);
    }
    return acc;
  }, [] as typeof allConversations);

  if (conversations.length === 0) {
    return (
      <div className="p-8 text-center">
        <div className="text-gray-500">
          <MessageCircle className="w-16 h-16 mx-auto mb-4 text-gray-300" />
          <h3 className="text-lg font-semibold mb-2 text-gray-900">No conversations yet</h3>
          <p className="text-sm">Start a conversation by visiting someone&apos;s profile and clicking &quot;Message&quot;</p>
        </div>
      </div>
    );
  }

  const formatLastMessageTime = (dateString: string) => {
    try {
      return formatDistanceToNow(new Date(dateString), { addSuffix: true });
    } catch {
      return '';
    }
  };

  const getLastMessagePreview = (conversation: { lastMessage?: { content?: string; imageUrl?: string } }) => {
    if (!conversation.lastMessage) {
      return 'No messages yet';
    }

    const message = conversation.lastMessage;
    if (message.imageUrl && !message.content) {
      return (
        <span className="flex items-center text-gray-500">
          <ImageIcon className="w-4 h-4 mr-1" />
          Photo
        </span>
      );
    }

    if (message.imageUrl && message.content) {
      return (
        <span className="flex items-center">
          <ImageIcon className="w-4 h-4 mr-1 text-gray-500" />
          {message.content}
        </span>
      );
    }

    return message.content;
  };

  return (
    <div>
      {conversations.map((conversation) => (
        <Link
          key={conversation.id}
          href={`/messages/${conversation.id}`}
          className="block border-b border-gray-200 hover:bg-gray-50 transition-colors"
        >
          <div className="p-4 flex items-center space-x-3">
            <div className="relative">
              <UserAvatar user={conversation.otherParticipant} size="lg" clickable={false} />
              {conversation.unreadCount > 0 && (
                <div className="absolute -top-1 -right-1 bg-blue-600 text-white text-xs rounded-full w-5 h-5 flex items-center justify-center">
                  {conversation.unreadCount > 9 ? '9+' : conversation.unreadCount}
                </div>
              )}
            </div>

            <div className="flex-1 min-w-0">
              <div className="flex items-center justify-between">
                <h3 className={`font-semibold truncate ${conversation.unreadCount > 0 ? 'text-gray-900' : 'text-gray-700'}`}>
                  @{conversation.otherParticipant.username}
                </h3>
                {conversation.lastMessage && (
                  <span className="text-xs text-gray-500 ml-2 flex-shrink-0">
                    {formatLastMessageTime(conversation.lastMessage.createdAt)}
                  </span>
                )}
              </div>

              <div className={`text-sm truncate mt-1 ${conversation.unreadCount > 0 ? 'text-gray-900 font-medium' : 'text-gray-500'}`}>
                {getLastMessagePreview(conversation)}
              </div>
            </div>
          </div>
        </Link>
      ))}

      {/* Load more trigger */}
      <div ref={loadMoreRef} className="h-20 flex items-center justify-center">
        {isFetchingNextPage ? (
          <div className="text-gray-500">Loading more conversations...</div>
        ) : hasNextPage ? (
          <div className="text-gray-400 text-sm">Scroll for more</div>
        ) : conversations.length > 0 ? (
          <div className="text-gray-400 text-sm">You&apos;ve reached the end!</div>
        ) : null}
      </div>
    </div>
  );
}
