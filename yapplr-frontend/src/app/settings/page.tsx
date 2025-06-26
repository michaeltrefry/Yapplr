'use client';

import { useAuth } from '@/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import { useEffect } from 'react';
import Sidebar from '@/components/Sidebar';
import Link from 'next/link';
import { Shield, ArrowRight } from 'lucide-react';

export default function SettingsPage() {
  const { user, isLoading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!isLoading && !user) {
      router.push('/login');
    }
  }, [user, isLoading, router]);

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-lg">Loading...</div>
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
              <h1 className="text-xl font-bold">Settings</h1>
            </div>

            {/* Settings Content */}
            <div className="p-6">
              <div className="space-y-4">
                {/* Blocklist Setting */}
                <Link
                  href="/settings/blocklist"
                  className="flex items-center justify-between p-4 bg-white border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
                >
                  <div className="flex items-center space-x-3">
                    <div className="p-2 bg-red-100 rounded-lg">
                      <Shield className="w-5 h-5 text-red-600" />
                    </div>
                    <div>
                      <h3 className="font-semibold text-gray-900">Blocklist</h3>
                      <p className="text-sm text-gray-600">
                        Manage users you've blocked
                      </p>
                    </div>
                  </div>
                  <ArrowRight className="w-5 h-5 text-gray-400" />
                </Link>

                {/* Placeholder for future settings */}
                <div className="p-4 bg-gray-50 border border-gray-200 rounded-lg">
                  <div className="text-center text-gray-500">
                    <p className="text-sm">More settings coming soon...</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
