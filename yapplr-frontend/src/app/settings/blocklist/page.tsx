'use client';

import { useAuth } from '@/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import { useEffect } from 'react';
import Sidebar from '@/components/Sidebar';
import BlockedUsersList from '@/components/BlockedUsersList';
import Link from 'next/link';
import { ArrowLeft } from 'lucide-react';

export default function BlocklistPage() {
  const { user, isLoading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!isLoading && !user) {
      router.push('/login');
    }
  }, [user, isLoading, router]);

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="text-lg text-gray-900">Loading...</div>
      </div>
    );
  }

  if (!user) {
    return null;
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
          <div className="max-w-2xl mx-auto lg:border-x border-gray-200 min-h-screen bg-white">
            {/* Header */}
            <div className="sticky top-0 bg-white/80 backdrop-blur-md border-b border-gray-200 p-4">
              <div className="flex items-center space-x-3">
                <Link
                  href="/settings"
                  className="p-2 hover:bg-gray-100 rounded-full transition-colors"
                >
                  <ArrowLeft className="w-5 h-5 text-gray-600 />
                </Link>
                <h1 className="text-xl font-bold text-gray-900">Blocklist</h1>
              </div>
            </div>

            {/* Blocklist Content */}
            <div className="p-6">
              <div className="mb-6">
                <p className="text-gray-600">
                  Users you've blocked won't be able to see your posts or interact with you.
                  You won't see their content either.
                </p>
              </div>

              <BlockedUsersList />
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
