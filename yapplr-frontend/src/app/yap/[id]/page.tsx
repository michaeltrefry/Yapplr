'use client';

import { use, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useRouter, useSearchParams } from 'next/navigation';
import Link from 'next/link';
import { postApi } from '@/lib/api';
import { useAuth } from '@/contexts/AuthContext';
import PostCard from '@/components/PostCard';
import Sidebar from '@/components/Sidebar';
import { ArrowLeft } from 'lucide-react';

interface PostPageProps {
  params: Promise<{
    id: string;
  }>;
}

export default function PostPage({ params }: PostPageProps) {
  const { user } = useAuth();
  const { id } = use(params);
  const router = useRouter();
  const searchParams = useSearchParams();
  const scrollToComment = searchParams?.get('scrollToComment');

  const { data: post, isLoading, error } = useQuery({
    queryKey: ['post', parseInt(id)],
    queryFn: () => postApi.getPost(parseInt(id)),
  });

  // Scroll to comment if specified in URL
  useEffect(() => {
    if (post && scrollToComment) {
      const timer = setTimeout(() => {
        const commentElement = document.getElementById(`comment-${scrollToComment}`);
        if (commentElement) {
          commentElement.scrollIntoView({
            behavior: 'smooth',
            block: 'center'
          });
          // Add a highlight effect
          commentElement.classList.add('bg-blue-50', 'border-l-4', 'border-blue-500');
          setTimeout(() => {
            commentElement.classList.remove('bg-blue-50', 'border-l-4', 'border-blue-500');
          }, 3000);
        }
      }, 100); // Small delay to ensure DOM is ready

      return () => clearTimeout(timer);
    }
  }, [post, scrollToComment]);

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50">
        <div className="max-w-6xl mx-auto flex">
          <Sidebar />
          <main className="flex-1 max-w-2xl mx-auto px-4 py-8">
            <div className="bg-white rounded-lg shadow-sm p-6">
              <div className="animate-pulse">
                <div className="flex items-center space-x-3 mb-4">
                  <div className="w-10 h-10 bg-gray-300 rounded-full"></div>
                  <div className="space-y-2">
                    <div className="h-4 bg-gray-300 rounded w-24"></div>
                    <div className="h-3 bg-gray-300 rounded w-16"></div>
                  </div>
                </div>
                <div className="space-y-3">
                  <div className="h-4 bg-gray-300 rounded w-full"></div>
                  <div className="h-4 bg-gray-300 rounded w-3/4"></div>
                </div>
              </div>
            </div>
          </main>
        </div>
      </div>
    );
  }

  if (error || !post) {
    return (
      <div className="min-h-screen bg-gray-50">
        <div className="max-w-6xl mx-auto flex">
          <Sidebar />
          <main className="flex-1 max-w-2xl mx-auto px-4 py-8">
            <div className="bg-white rounded-lg shadow-sm p-6 text-center">
              <h1 className="text-xl font-semibold text-gray-900 mb-2">Yap not found</h1>
              <p className="text-gray-600 mb-4">
                The yap you&apos;re looking for doesn&apos;t exist or has been deleted.
              </p>
              <button
                onClick={() => router.push('/')}
                className="bg-blue-500 text-white px-4 py-2 rounded-lg hover:bg-blue-600 transition-colors"
              >
                Go to Home
              </button>
            </div>
          </main>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-6xl mx-auto flex">
        {user && <Sidebar />}
        <main className={`flex-1 max-w-2xl mx-auto px-4 py-8 ${!user ? 'max-w-4xl' : ''}`}>
          {/* Header */}
          <div className="mb-6 flex items-center justify-between">
            <button
              onClick={() => router.back()}
              className="flex items-center space-x-2 text-gray-600 hover:text-gray-900 transition-colors"
            >
              <ArrowLeft className="w-5 h-5" />
              <span>Back</span>
            </button>

            {!user && (
              <div className="flex items-center space-x-4">
                <Link
                  href={`/login?redirect=${encodeURIComponent(`/yap/${id}`)}`}
                  className="text-blue-600 hover:text-blue-700 font-medium"
                >
                  Sign in
                </Link>
                <Link
                  href={`/register?redirect=${encodeURIComponent(`/yap/${id}`)}`}
                  className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors"
                >
                  Sign up
                </Link>
              </div>
            )}
          </div>

          {/* Post */}
          <PostCard
            post={post}
            showCommentsDefault={true}
            showBorder={true}
          />

          {!user && (
            <div className="mt-6 p-4 bg-blue-50 border border-blue-200 rounded-lg text-center">
              <p className="text-blue-800 mb-3">
                Join Yapplr to like, comment, and share yaps!
              </p>
              <div className="flex items-center justify-center space-x-4">
                <Link
                  href={`/login?redirect=${encodeURIComponent(`/yap/${id}`)}`}
                  className="text-blue-600 hover:text-blue-700 font-medium"
                >
                  Sign in
                </Link>
                <Link
                  href={`/register?redirect=${encodeURIComponent(`/yap/${id}`)}`}
                  className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors"
                >
                  Sign up
                </Link>
              </div>
            </div>
          )}
        </main>
      </div>
    </div>
  );
}
