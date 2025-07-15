'use client';

import { useInfiniteQuery } from '@tanstack/react-query';
import { postApi } from '@/lib/api';
import { useEffect, useRef, useState } from 'react';
import { Post } from '@/types';

interface PhotoGridProps {
  userId: number;
  onPhotoClick: (post: Post) => void;
}

export default function PhotoGrid({ userId, onPhotoClick }: PhotoGridProps) {
  const loadMoreRef = useRef<HTMLDivElement>(null);

  const {
    data,
    isLoading,
    error,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
  } = useInfiniteQuery({
    queryKey: ['userPhotos', userId],
    queryFn: ({ pageParam = 1 }) => postApi.getUserPhotos(userId, pageParam, 25),
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

  if (isLoading) {
    return (
      <div className="p-8 text-center">
        <div className="text-gray-500">Loading photos...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-8 text-center">
        <div className="text-red-500">Failed to load photos</div>
      </div>
    );
  }

  const photos = data?.pages.flat() || [];

  if (photos.length === 0) {
    return (
      <div className="p-8 text-center">
        <div className="text-gray-500">No photos yet</div>
      </div>
    );
  }

  return (
    <div className="p-4">
      {/* Photo Grid - 3 columns */}
      <div className="grid grid-cols-3 gap-1">
        {photos.map((post) => (
          <div
            key={post.id}
            className="aspect-square cursor-pointer group border border-gray-300 bg-white"
            onClick={() => onPhotoClick(post)}
          >
            {post.imageUrl ? (
              <img
                src={post.imageUrl}
                alt="Photo"
                className="w-full h-full object-cover transition-transform duration-200 group-hover:scale-105"
                onLoad={() => console.log('Image loaded:', post.imageUrl)}
                onError={() => console.error('Image failed to load:', post.imageUrl)}
              />
            ) : (
              <div className="w-full h-full flex items-center justify-center text-gray-500 text-xs bg-gray-100">
                No Image
              </div>
            )}
          </div>
        ))}
      </div>

      {/* Loading indicator for infinite scroll */}
      {hasNextPage && (
        <div ref={loadMoreRef} className="p-4 text-center">
          {isFetchingNextPage ? (
            <div className="text-gray-500">Loading more photos...</div>
          ) : (
            <div className="text-gray-400">Scroll for more</div>
          )}
        </div>
      )}
    </div>
  );
}
