'use client';

import { TimelineItem } from '@/types';
import PostCard from './PostCard';
import UserAvatar from './UserAvatar';
import { Repeat2 } from 'lucide-react';
import { formatDate } from '@/lib/utils';

interface TimelineItemCardProps {
  item: TimelineItem;
}

export default function TimelineItemCard({ item }: TimelineItemCardProps) {
  if (item.type === 'post') {
    // Regular post
    return <PostCard post={item.post} />;
  }

  // Repost
  return (
    <div className="border-b border-gray-200 bg-white">
      {/* Repost header */}
      <div className="flex items-center px-4 pt-3 pb-2 text-gray-500 text-sm">
        <Repeat2 className="w-4 h-4 mr-2" />
        <UserAvatar user={item.repostedBy!} size="sm" />
        <span className="ml-2">
          <span className="font-semibold text-gray-700">{item.repostedBy!.username}</span> reposted
        </span>
        <span className="ml-2">·</span>
        <span className="ml-2">{formatDate(item.createdAt)}</span>
      </div>
      
      {/* Original post content */}
      <div className="ml-4 border-l-2 border-gray-100 pl-4">
        <PostCard post={item.post} showBorder={false} />
      </div>
    </div>
  );
}
