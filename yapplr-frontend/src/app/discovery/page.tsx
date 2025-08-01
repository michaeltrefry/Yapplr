'use client';

import { useState } from 'react';
import { TrendingDashboard, TrendingAnalytics, ExplorePage, TopicDiscovery, TopicManagement, PersonalizedTopicFeed, PersonalizationInsights, PersonalizationControls } from '@/components/discovery';
import Sidebar from '@/components/Sidebar';

export default function DiscoveryPage() {
  const [selectedHashtag, setSelectedHashtag] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'trending' | 'explore' | 'topics' | 'personalized'>('trending');

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <div className="max-w-7xl mx-auto flex">
        {/* Sidebar */}
        <div className="w-16 lg:w-64 fixed h-full z-10">
          <Sidebar />
        </div>

        {/* Main Content */}
        <div className="flex-1 ml-16 lg:ml-64">
          <div className="p-6">
            {/* Header */}
            <div className="mb-8">
              <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">
                🚀 Discovery Hub
              </h1>
              <p className="text-gray-600 dark:text-gray-400">
                Explore trending content, discover new topics, and get personalized recommendations
              </p>
            </div>

            {/* Tab Navigation */}
            <div className="mb-8">
              <div className="flex space-x-1 bg-white dark:bg-gray-800 rounded-lg p-1 shadow-sm">
                <button
                  onClick={() => setActiveTab('trending')}
                  className={`px-6 py-3 rounded-md font-medium transition-colors flex items-center space-x-2 ${
                    activeTab === 'trending'
                      ? 'bg-blue-500 text-white shadow-sm'
                      : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
                  }`}
                >
                  <span>🔥</span>
                  <span>Trending</span>
                </button>
                <button
                  onClick={() => setActiveTab('explore')}
                  className={`px-6 py-3 rounded-md font-medium transition-colors flex items-center space-x-2 ${
                    activeTab === 'explore'
                      ? 'bg-blue-500 text-white shadow-sm'
                      : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
                  }`}
                >
                  <span>🌟</span>
                  <span>Explore</span>
                </button>
                <button
                  onClick={() => setActiveTab('topics')}
                  className={`px-6 py-3 rounded-md font-medium transition-colors flex items-center space-x-2 ${
                    activeTab === 'topics'
                      ? 'bg-blue-500 text-white shadow-sm'
                      : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
                  }`}
                >
                  <span>🎯</span>
                  <span>Topics</span>
                </button>
                <button
                  onClick={() => setActiveTab('personalized')}
                  className={`px-6 py-3 rounded-md font-medium transition-colors flex items-center space-x-2 ${
                    activeTab === 'personalized'
                      ? 'bg-blue-500 text-white shadow-sm'
                      : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
                  }`}
                >
                  <span>✨</span>
                  <span>For You</span>
                </button>
              </div>
            </div>

            {/* Content */}
            {activeTab === 'trending' && (
              <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
                <div className="xl:col-span-2">
                  <TrendingDashboard 
                    onHashtagClick={(hashtag) => setSelectedHashtag(hashtag)}
                  />
                </div>
                <div className="xl:col-span-1">
                  {selectedHashtag ? (
                    <TrendingAnalytics 
                      hashtagName={selectedHashtag}
                      onClose={() => setSelectedHashtag(null)}
                    />
                  ) : (
                    <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
                      <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                        📊 Analytics
                      </h3>
                      <div className="text-center py-12">
                        <div className="text-6xl mb-4">📈</div>
                        <p className="text-gray-500 dark:text-gray-400 mb-2">
                          Click on a hashtag to view detailed analytics
                        </p>
                        <p className="text-sm text-gray-400 dark:text-gray-500">
                          See velocity trends, engagement rates, and growth patterns
                        </p>
                      </div>
                    </div>
                  )}
                </div>
              </div>
            )}

            {activeTab === 'explore' && (
              <ExplorePage />
            )}

            {activeTab === 'topics' && (
              <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
                <div className="xl:col-span-2 space-y-6">
                  <TopicDiscovery />
                  <PersonalizedTopicFeed />
                </div>
                <div className="xl:col-span-1">
                  <TopicManagement />
                </div>
              </div>
            )}

            {activeTab === 'personalized' && (
              <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
                <div className="xl:col-span-2">
                  <PersonalizationInsights />
                </div>
                <div className="xl:col-span-1">
                  <PersonalizationControls />
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
