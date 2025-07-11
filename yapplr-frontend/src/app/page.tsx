'use client';

import { useAuth } from '@/contexts/AuthContext';
import { useEffect, useState } from 'react';
import Timeline from '@/components/Timeline';
import PublicTimeline from '@/components/PublicTimeline';
import Sidebar from '@/components/Sidebar';
import CreatePost from '@/components/CreatePost';
import TrendingWidget from '@/components/TrendingWidget';
import Link from 'next/link';

export default function Home() {
  const { user, isLoading } = useAuth();
  const [showPublicFeed, setShowPublicFeed] = useState(false);

  useEffect(() => {
    if (!isLoading && !user) {
      setShowPublicFeed(true);
    }
  }, [user, isLoading]);

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-lg text-gray-900">Loading...</div>
      </div>
    );
  }

  // Show public feed for unauthenticated users
  if (!user && showPublicFeed) {
    return (
      <div className="min-h-screen bg-gray-50">
        <div className="max-w-6xl mx-auto flex">
          {/* Main Content */}
          <div className="flex-1">
            <div className="max-w-2xl mx-auto lg:border-x border-gray-200 min-h-screen bg-white">
              {/* Header */}
              <div className="sticky top-0 bg-white/80 backdrop-blur-md border-b border-gray-200 p-4">
                <div className="flex items-center justify-between">
                  <h1 className="text-xl font-bold text-blue-600">Yapplr</h1>
                  <div className="flex items-center space-x-4">
                    <Link
                      href="/login"
                      className="text-blue-600 hover:text-blue-700 font-medium"
                    >
                      Sign in
                    </Link>
                    <Link
                      href="/register"
                      className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors"
                    >
                      Sign up
                    </Link>
                  </div>
                </div>
              </div>

              {/* Welcome Section */}
              <div className="p-6 border-b border-gray-200 bg-gradient-to-r from-blue-50 to-purple-50">
                <h2 className="text-2xl font-bold text-gray-900 mb-2">Welcome to Yapplr</h2>
                <p className="text-gray-600 mb-4">
                  Join the conversation! Share your thoughts, connect with others, and discover what&apos;s happening.
                </p>
                <Link
                  href="/register"
                  className="inline-block bg-blue-600 text-white px-6 py-3 rounded-lg hover:bg-blue-700 transition-colors font-semibold"
                >
                  Get Started
                </Link>
              </div>

              {/* Public Timeline */}
              <PublicTimeline />
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (!user) {
    return null;
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-7xl mx-auto flex">
        {/* Left Sidebar */}
        <div className="w-16 lg:w-64 fixed h-full z-10">
          <Sidebar />
        </div>

        {/* Main Content */}
        <div className="flex-1 ml-16 lg:ml-64 lg:mr-80">
          <div className="max-w-2xl mx-auto lg:border-x border-gray-200 min-h-screen bg-white">
            {/* Header */}
            <div className="sticky top-0 bg-white/80 backdrop-blur-md border-b border-gray-200 p-4">
              <h1 className="text-xl font-bold text-gray-900">Home</h1>
            </div>

            {/* Create Post */}
            <CreatePost />

            {/* Timeline */}
            <Timeline />
          </div>
        </div>

        {/* Right Sidebar - Hidden on mobile, visible on large screens */}
        <div className="hidden lg:block w-80 fixed right-0 h-full overflow-y-auto">
          <div className="p-4 space-y-4">
            {/* Trending Hashtags Widget */}
            <TrendingWidget limit={8} />

            {/* Additional widgets could go here */}
            <div className="bg-white rounded-lg border border-gray-200 p-4">
              <h3 className="font-bold text-gray-900 mb-3">Quick Actions</h3>
              <div className="space-y-2">
                <Link
                  href="/search"
                  className="block text-blue-600 hover:text-blue-800 text-sm"
                >
                  Search users & hashtags
                </Link>
                <Link
                  href="/trending"
                  className="block text-blue-600 hover:text-blue-800 text-sm"
                >
                  View all trending topics
                </Link>
              </div>
            </div>

            {/* Legal Links Widget */}
            <div className="bg-white rounded-lg border border-gray-200 p-4">
              <h3 className="font-bold text-gray-900 mb-3">Legal</h3>
              <div className="space-y-2">
                <Link
                  href="/privacy"
                  target="_blank"
                  className="block text-blue-600 hover:text-blue-800 text-sm"
                >
                  Privacy Policy
                </Link>
                <Link
                  href="/terms"
                  target="_blank"
                  className="block text-blue-600 hover:text-blue-800 text-sm"
                >
                  Terms of Service
                </Link>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
