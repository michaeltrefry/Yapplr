'use client';

import { useInfiniteQuery } from '@tanstack/react-query';
import { postApi } from '@/lib/api';
import { useEffect, useRef } from 'react';
import { ArrowUp } from 'lucide-react';
import TimelineItemCard from './TimelineItemCard';

interface UserTimelineProps {
  userId: number;
  isOwnProfile: boolean;
}

export default function UserTimeline({ userId, isOwnProfile }: UserTimelineProps) {
  const loadMoreRef = useRef<HTMLDivElement>(null);

  const {
    data,
    isLoading,
    error,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
  } = useInfiniteQuery({
    queryKey: ['userTimeline', userId],
    queryFn: ({ pageParam = 1 }) => postApi.getUserTimeline(userId, pageParam, 25),
    getNextPageParam: (lastPage, allPages) => {
      // If the last page has fewer than 25 items, we've reached the end
      if (lastPage.length < 25) {
        return undefined;
      }
      return allPages.length + 1;
    },
    initialPageParam: 1,
    enabled: !!userId,
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

  const scrollToTop = () => {
    window.scrollTo({
      top: 0,
      behavior: 'smooth'
    });
  };

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

  const timelineItems = data?.pages.flat() || [];

  if (timelineItems.length === 0) {
    return (
      <div className="p-8 text-center">
        <div className="text-gray-500">
          {isOwnProfile ? "You haven't yapped anything yet" : "No yaps yet"}
        </div>
      </div>
    );
  }

  return (
    <div>
      {timelineItems.map((item) => (
        <TimelineItemCard key={`${item.type}-${item.post.id}-${item.createdAt}`} item={item} />
      ))}
      
      {/* Load more trigger */}
      <div ref={loadMoreRef} className="h-20 flex flex-col items-center justify-center space-y-3">
        {isFetchingNextPage ? (
          <div className="text-gray-500">Loading more yaps...</div>
        ) : hasNextPage ? (
          <div className="text-gray-400 text-sm">Scroll for more</div>
        ) : (
          <div className="flex flex-col items-center space-y-3">
            <div className="text-gray-400 text-sm">You&apos;ve reached the end!</div>
            <button
              onClick={scrollToTop}
              className="flex items-center space-x-2 px-4 py-2 bg-blue-100 text-blue-700 hover:bg-blue-200 rounded-full transition-colors text-sm font-medium"
            >
              <ArrowUp className="w-4 h-4" />
              <span>Return to top</span>
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
