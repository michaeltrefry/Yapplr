'use client';

import { useQuery } from '@tanstack/react-query';
import { userApi } from '@/lib/api';
import Link from 'next/link';
import UserAvatar from './UserAvatar';
import { useState, useEffect, useRef } from 'react';

export default function FollowingList() {
  const [visibleCount, setVisibleCount] = useState(10);
  const containerRef = useRef<HTMLDivElement>(null);

  const { data: following, isLoading } = useQuery({
    queryKey: ['topFollowingWithOnlineStatus'],
    queryFn: () => userApi.getTopFollowingWithOnlineStatus(10),
    refetchInterval: 30000, // Refresh every 30 seconds to update online status
    refetchIntervalInBackground: false,
  });

  // Calculate how many items can fit based on available space
  useEffect(() => {
    const calculateVisibleItems = () => {
      if (!containerRef.current || !following?.length) return;

      const container = containerRef.current;
      const sidebar = container.closest('.h-full');
      if (!sidebar) return;

      // Get the sidebar height and other elements' heights
      const sidebarHeight = sidebar.clientHeight;
      const containerTop = container.getBoundingClientRect().top;
      const sidebarTop = sidebar.getBoundingClientRect().top;

      // Reserve space for user info section (approximately 120px)
      const reservedSpace = 120;
      const availableHeight = sidebarHeight - (containerTop - sidebarTop) - reservedSpace;

      // Each item is approximately 48px (py-2 + avatar + padding)
      const itemHeight = 48;
      const maxItems = Math.floor(availableHeight / itemHeight);

      // Ensure we show at least 3 items but no more than 10
      const newVisibleCount = Math.max(3, Math.min(maxItems, 10));
      setVisibleCount(newVisibleCount);
    };

    calculateVisibleItems();

    // Recalculate on window resize
    const handleResize = () => {
      setTimeout(calculateVisibleItems, 100); // Debounce
    };

    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, [following]);

  if (isLoading) {
    return (
      <div className="px-1 lg:px-3 py-2">
        <div className="text-sm text-gray-500 hidden lg:block">Loading...</div>
        <div className="text-xs text-gray-500 lg:hidden text-center">...</div>
      </div>
    );
  }

  if (!following || following.length === 0) {
    return (
      <div className="px-1 lg:px-3 py-2">
        <div className="text-sm text-gray-500 hidden lg:block">No following yet</div>
        <div className="text-xs text-gray-500 lg:hidden text-center">-</div>
      </div>
    );
  }

  const visibleFollowing = following.slice(0, visibleCount);

  return (
    <div ref={containerRef} className="space-y-1 overflow-hidden">
      {visibleFollowing.map((user) => (
        <Link
          key={user.id}
          href={`/profile/${user.username}`}
          className="flex items-center justify-center lg:justify-start lg:space-x-3 px-1 lg:px-3 py-2 rounded-lg hover:bg-gray-100 transition-colors"
        >
          <div className="relative flex-shrink-0">
            <UserAvatar user={user} size="sm" clickable={false} />
            {user.isOnline && (
              <div className="absolute -bottom-0.5 -right-0.5 w-3 h-3 bg-green-500 border-2 border-white rounded-full"></div>
            )}
          </div>
          <div className="flex-1 min-w-0 hidden lg:block">
            <p className="text-sm font-medium text-gray-900 truncate">
              {user.username}
            </p>
          </div>
        </Link>
      ))}
      {following.length > visibleCount && (
        <div className="px-1 lg:px-3 py-1">
          <div className="text-xs text-gray-400 hidden lg:block">
            +{following.length - visibleCount} more
          </div>
        </div>
      )}
    </div>
  );
}
