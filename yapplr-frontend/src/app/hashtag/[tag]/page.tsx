'use client';

import { useParams } from 'next/navigation';
import { useInfiniteQuery, useQuery } from '@tanstack/react-query';
import { tagApi } from '@/lib/api';
import { Hash, ArrowLeft } from 'lucide-react';
import Link from 'next/link';
import Sidebar from '@/components/Sidebar';
import PostCard from '@/components/PostCard';
import { useInView } from 'react-intersection-observer';
import { useEffect } from 'react';
import { formatNumber } from '@/lib/utils';

export default function HashtagPage() {
  const params = useParams();
  const tag = params?.tag as string;
  const decodedTag = tag ? decodeURIComponent(tag) : '';

  const { ref, inView } = useInView();

  // Get tag information
  const { data: tagInfo, isLoading: tagLoading } = useQuery({
    queryKey: ['tag', decodedTag],
    queryFn: () => tagApi.getTag(decodedTag),
    retry: false,
    enabled: !!decodedTag,
  });

  // Get posts for this tag with infinite scroll
  const {
    data,
    fetchNextPage,
    hasNextPage,
    isFetchingNextPage,
    isLoading: postsLoading,
    error
  } = useInfiniteQuery({
    queryKey: ['tag-posts', decodedTag],
    queryFn: ({ pageParam = 1 }) => tagApi.getPostsByTag(decodedTag, pageParam, 25),
    getNextPageParam: (lastPage, pages) => {
      return lastPage && Array.isArray(lastPage) && lastPage.length === 25 ? pages.length + 1 : undefined;
    },
    initialPageParam: 1,
    enabled: !!decodedTag,
  });

  // Fetch next page when scrolling to bottom
  useEffect(() => {
    if (inView && hasNextPage && !isFetchingNextPage) {
      fetchNextPage();
    }
  }, [inView, hasNextPage, isFetchingNextPage, fetchNextPage]);

  if (!decodedTag) {
    return <div>Invalid hashtag</div>;
  }

  const posts = data?.pages.flat() || [];

  if (error) {
    return (
      <div className="min-h-screen bg-gray-50">
        <div className="max-w-6xl mx-auto flex">
          {/* Sidebar */}
          <div className="w-16 lg:w-64 fixed h-full z-10">
            <Sidebar />
          </div>

          {/* Main Content */}
          <div className="flex-1 ml-16 lg:ml-64">
            <div className="max-w-2xl mx-auto">
              {/* Header */}
              <div className="bg-white border-b border-gray-200 px-4 py-3 sticky top-0 z-20">
                <div className="flex items-center space-x-3">
                  <Link href="/search" className="p-2 hover:bg-gray-100 rounded-full">
                    <ArrowLeft className="w-5 h-5" />
                  </Link>
                  <div>
                    <h1 className="text-xl font-bold text-gray-900">#{decodedTag}</h1>
                    <p className="text-sm text-gray-500">Hashtag not found</p>
                  </div>
                </div>
              </div>

              {/* Error Message */}
              <div className="p-8 text-center">
                <Hash className="w-16 h-16 text-gray-300 mx-auto mb-4" />
                <h2 className="text-xl font-semibold text-gray-900 mb-2">Hashtag not found</h2>
                <p className="text-gray-600">
                  The hashtag #{decodedTag} doesn&apos;t exist or has no posts yet.
                </p>
                <Link
                  href="/search"
                  className="inline-block mt-4 bg-blue-500 text-white px-6 py-2 rounded-full hover:bg-blue-600 transition-colors"
                >
                  Search for other hashtags
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
          <div className="max-w-2xl mx-auto">
            {/* Header */}
            <div className="bg-white border-b border-gray-200 px-4 py-3 sticky top-0 z-20">
              <div className="flex items-center space-x-3">
                <Link href="/search" className="p-2 hover:bg-gray-100 rounded-full">
                  <ArrowLeft className="w-5 h-5" />
                </Link>
                <div className="flex-1">
                  <h1 className="text-xl font-bold text-gray-900">#{decodedTag}</h1>
                  {tagLoading ? (
                    <div className="h-4 bg-gray-200 rounded w-20 animate-pulse"></div>
                  ) : tagInfo ? (
                    <p className="text-sm text-gray-500">
                      {formatNumber(tagInfo.postCount || 0)} {(tagInfo.postCount || 0) === 1 ? 'post' : 'posts'}
                    </p>
                  ) : (
                    <p className="text-sm text-gray-500">No posts yet</p>
                  )}
                </div>
              </div>
            </div>

            {/* Posts */}
            <div className="divide-y divide-gray-200">
              {postsLoading ? (
                // Loading skeleton
                Array.from({ length: 5 }).map((_, i) => (
                  <div key={i} className="p-4 bg-white">
                    <div className="flex space-x-3">
                      <div className="w-12 h-12 bg-gray-200 rounded-full animate-pulse"></div>
                      <div className="flex-1 space-y-2">
                        <div className="h-4 bg-gray-200 rounded w-1/4 animate-pulse"></div>
                        <div className="h-4 bg-gray-200 rounded w-3/4 animate-pulse"></div>
                        <div className="h-4 bg-gray-200 rounded w-1/2 animate-pulse"></div>
                      </div>
                    </div>
                  </div>
                ))
              ) : posts.length > 0 ? (
                <>
                  {posts.map((post) => (
                    <PostCard key={post.id} post={post} />
                  ))}
                  
                  {/* Infinite scroll trigger */}
                  <div ref={ref} className="h-10 flex items-center justify-center">
                    {isFetchingNextPage && (
                      <div className="text-gray-500">Loading more posts...</div>
                    )}
                  </div>
                </>
              ) : (
                // Empty state
                <div className="p-8 text-center bg-white">
                  <Hash className="w-16 h-16 text-gray-300 mx-auto mb-4" />
                  <h2 className="text-xl font-semibold text-gray-900 mb-2">No posts yet</h2>
                  <p className="text-gray-600">
                    Be the first to post with #{decodedTag}!
                  </p>
                  <Link
                    href="/"
                    className="inline-block mt-4 bg-blue-500 text-white px-6 py-2 rounded-full hover:bg-blue-600 transition-colors"
                  >
                    Create a post
                  </Link>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
