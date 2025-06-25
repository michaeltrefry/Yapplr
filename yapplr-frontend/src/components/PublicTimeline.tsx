'use client';

import { useQuery } from '@tanstack/react-query';
import { postApi } from '@/lib/api';
import TimelineItemCard from './TimelineItemCard';

export default function PublicTimeline() {
  const { data: timelineItems, isLoading, error } = useQuery({
    queryKey: ['publicTimeline'],
    queryFn: () => postApi.getPublicTimeline(),
  });

  if (isLoading) {
    return (
      <div className="p-8 text-center">
        <div className="text-gray-500">Loading yaps...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-8 text-center">
        <div className="text-red-500">Failed to load yaps</div>
      </div>
    );
  }

  if (!timelineItems || timelineItems.length === 0) {
    return (
      <div className="p-8 text-center">
        <div className="text-gray-500">
          <h3 className="text-lg font-semibold mb-2">No yaps yet</h3>
          <p>Be the first to share something!</p>
        </div>
      </div>
    );
  }

  return (
    <div>
      {timelineItems.map((item) => (
        <TimelineItemCard key={`${item.type}-${item.post.id}-${item.createdAt}`} item={item} />
      ))}
    </div>
  );
}
