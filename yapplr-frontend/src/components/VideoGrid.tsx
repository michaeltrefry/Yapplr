'use client';

import { useInfiniteQuery } from '@tanstack/react-query';
import { postApi } from '@/lib/api';
import { useEffect, useRef, useState } from 'react';
import { Post, PostMedia, MediaType } from '@/types';
import { Play } from 'lucide-react';

interface VideoGridProps {
  userId: number;
  onVideoClick: (post: Post, mediaItem?: PostMedia, allVideos?: VideoItem[], currentIndex?: number) => void;
}

export interface VideoItem {
  post: Post;
  mediaItem: PostMedia;
  videoUrl: string;
  thumbnailUrl?: string;
}

export default function VideoGrid({ userId, onVideoClick }: VideoGridProps) {
  const loadMoreRef = useRef<HTMLDivElement>(null);

  const {
    data,
    isLoading,
    error,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
  } = useInfiniteQuery({
    queryKey: ['userVideos', userId],
    queryFn: ({ pageParam = 1 }) => postApi.getUserVideos(userId, pageParam, 25),
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

  // Extract all videos from posts (including multiple videos per post)
  const extractVideosFromPosts = (posts: Post[]): VideoItem[] => {
    const videos: VideoItem[] = [];
    
    posts.forEach(post => {
      if (post.mediaItems && post.mediaItems.length > 0) {
        // Handle posts with multiple media items
        post.mediaItems.forEach(mediaItem => {
          if (mediaItem.mediaType === MediaType.Video && mediaItem.videoUrl) {
            videos.push({
              post,
              mediaItem,
              videoUrl: mediaItem.videoUrl,
              thumbnailUrl: mediaItem.videoThumbnailUrl
            });
          }
        });
      } else if (post.videoUrl) {
        // Handle legacy single video posts
        videos.push({
          post,
          mediaItem: {
            id: 0, // Legacy posts don't have separate media items
            mediaType: MediaType.Video,
            videoUrl: post.videoUrl,
            videoThumbnailUrl: post.videoThumbnailUrl,
            createdAt: post.createdAt
          },
          videoUrl: post.videoUrl,
          thumbnailUrl: post.videoThumbnailUrl
        });
      }
    });
    
    return videos;
  };

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
        <div className="text-gray-500">Loading videos...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-8 text-center">
        <div className="text-red-500">Failed to load videos</div>
      </div>
    );
  }

  const posts = data?.pages.flat() || [];
  const videos = extractVideosFromPosts(posts);

  if (videos.length === 0) {
    return (
      <div className="p-8 text-center">
        <div className="text-gray-500">No videos yet</div>
      </div>
    );
  }

  return (
    <div className="p-4">
      {/* Video Grid - 3 columns */}
      <div className="grid grid-cols-3 gap-1">
        {videos.map((videoItem, index) => (
          <div
            key={`${videoItem.post.id}-${videoItem.mediaItem.id}-${index}`}
            className="aspect-square cursor-pointer group border border-gray-300 bg-black relative"
            onClick={() => onVideoClick(videoItem.post, videoItem.mediaItem, videos, index)}
          >
            {/* Video thumbnail */}
            {videoItem.thumbnailUrl ? (
              <img
                src={videoItem.thumbnailUrl}
                alt="Video thumbnail"
                className="w-full h-full object-cover transition-transform duration-200 group-hover:scale-105"
                onLoad={() => console.log('Video thumbnail loaded:', videoItem.thumbnailUrl)}
                onError={() => console.error('Video thumbnail failed to load:', videoItem.thumbnailUrl)}
              />
            ) : (
              <div className="w-full h-full bg-gray-800 flex items-center justify-center">
                <div className="text-gray-400 text-sm">No thumbnail</div>
              </div>
            )}
            
            {/* Play button overlay */}
            <div className="absolute inset-0 flex items-center justify-center bg-black/20 group-hover:bg-black/40 transition-colors">
              <div className="bg-white/90 rounded-full p-3 group-hover:bg-white transition-colors">
                <Play className="w-6 h-6 text-black fill-current" />
              </div>
            </div>
          </div>
        ))}
      </div>

      {/* Loading indicator for infinite scroll */}
      {isFetchingNextPage && (
        <div className="p-4 text-center">
          <div className="text-gray-500">Loading more videos...</div>
        </div>
      )}

      {/* Invisible element to trigger infinite scroll */}
      <div ref={loadMoreRef} className="h-1" />
    </div>
  );
}
