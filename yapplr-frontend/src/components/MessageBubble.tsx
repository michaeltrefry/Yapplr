'use client';

import { Message } from '@/types';
import UserAvatar from './UserAvatar';
import { formatDistanceToNow } from 'date-fns';
import { getApiBaseUrl } from '@/lib/config';

interface MessageBubbleProps {
  message: Message;
  isCurrentUser: boolean;
  showAvatar: boolean;
}

export default function MessageBubble({ message, isCurrentUser, showAvatar }: MessageBubbleProps) {
  const formatMessageTime = (dateString: string) => {
    try {
      return formatDistanceToNow(new Date(dateString), { addSuffix: true });
    } catch {
      return '';
    }
  };

  return (
    <div className={`flex ${isCurrentUser ? 'justify-end' : 'justify-start'} ${showAvatar ? 'mt-4' : 'mt-1'}`}>
      {/* Avatar for other users */}
      {!isCurrentUser && (
        <div className="mr-3 flex-shrink-0">
          {showAvatar ? (
            <UserAvatar user={message.sender} size="sm" />
          ) : (
            <div className="w-8 h-8" /> // Spacer to align messages
          )}
        </div>
      )}

      {/* Message content */}
      <div className={`max-w-xs lg:max-w-md ${isCurrentUser ? 'order-1' : 'order-2'}`}>
        {/* Sender name and time (only for other users and when showing avatar) */}
        {!isCurrentUser && showAvatar && (
          <div className="flex items-center space-x-2 mb-1">
            <span className="text-sm font-medium text-gray-900">@{message.sender.username}</span>
            <span className="text-xs text-gray-500">{formatMessageTime(message.createdAt)}</span>
          </div>
        )}

        {/* Message bubble */}
        <div
          className={`rounded-2xl px-4 py-2 ${
            isCurrentUser
              ? 'bg-blue-600 text-white rounded-br-md'
              : 'bg-gray-100 text-gray-900 rounded-bl-md'
          }`}
        >
          {/* Image attachment */}
          {message.imageUrl && (
            <div className="mb-2">
              <div className="relative rounded-lg overflow-hidden">
                <img
                  src={`${getApiBaseUrl()}${message.imageUrl}`}
                  alt="Message attachment"
                  className="object-cover max-w-full h-auto w-[300px] h-[200px]"
                />
              </div>
            </div>
          )}

          {/* Text content */}
          {message.content && (
            <div className="whitespace-pre-wrap break-words">
              {message.content}
            </div>
          )}

          {/* Time for current user messages */}
          {isCurrentUser && (
            <div className="text-xs text-blue-100 mt-1 text-right">
              {formatMessageTime(message.createdAt)}
            </div>
          )}
        </div>

        {/* Edited indicator */}
        {message.isEdited && (
          <div className={`text-xs text-gray-500 mt-1 ${isCurrentUser ? 'text-right' : 'text-left'}`}>
            edited
          </div>
        )}
      </div>

      {/* Spacer for current user to push message to right */}
      {isCurrentUser && <div className="ml-3 flex-shrink-0 w-8" />}
    </div>
  );
}
