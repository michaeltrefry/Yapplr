'use client';

import { useState } from 'react';
import { Group } from '@/types';
import { useAuth } from '@/contexts/AuthContext';
import GroupList from '@/components/GroupList';
import CreateGroupModal from '@/components/CreateGroupModal';
import Sidebar from '@/components/Sidebar';
import { Search, Plus, Users, TrendingUp } from 'lucide-react';

export default function GroupsPage() {
  const { user } = useAuth();
  const [searchQuery, setSearchQuery] = useState('');
  const [activeTab, setActiveTab] = useState<'all' | 'my-groups' | 'trending'>('all');
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);

  const handleGroupCreated = (group: Group) => {
    setRefreshKey(prev => prev + 1);
    setActiveTab('my-groups'); // Switch to my groups tab to show the new group
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    // The search will be handled by the GroupList component
  };

  const clearSearch = () => {
    setSearchQuery('');
  };

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      <div className="max-w-7xl mx-auto flex">
        {/* Left Sidebar */}
        {user && (
          <div className="w-16 lg:w-64 fixed h-full z-10">
            <Sidebar />
          </div>
        )}

        {/* Main Content */}
        <div className={`flex-1 ${user ? 'ml-16 lg:ml-64' : ''}`}>
          <div className="max-w-4xl mx-auto px-4 py-8">
        {/* Header */}
        <div className="mb-8">
          <div className="flex items-center justify-between mb-6">
            <div>
              <h1 className="text-3xl font-bold text-gray-900 dark:text-white">
                Groups
              </h1>
              <p className="text-gray-600 dark:text-gray-300 mt-2">
                Discover and join communities that share your interests
              </p>
            </div>
            
            {user && (
              <button
                onClick={() => setShowCreateModal(true)}
                className="flex items-center space-x-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
              >
                <Plus size={20} />
                <span>Create Group</span>
              </button>
            )}
          </div>

          {/* Search Bar */}
          <form onSubmit={handleSearch} className="mb-6">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" size={20} />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search groups..."
                className="w-full pl-10 pr-4 py-3 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-800 dark:text-white"
              />
              {searchQuery && (
                <button
                  type="button"
                  onClick={clearSearch}
                  className="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
                >
                  ×
                </button>
              )}
            </div>
          </form>

          {/* Tabs */}
          <div className="flex space-x-1 bg-gray-100 dark:bg-gray-800 rounded-lg p-1">
            <button
              onClick={() => setActiveTab('all')}
              className={`flex items-center space-x-2 px-4 py-2 rounded-md transition-colors ${
                activeTab === 'all'
                  ? 'bg-white dark:bg-gray-700 text-blue-600 dark:text-blue-400 shadow-sm'
                  : 'text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white'
              }`}
            >
              <Users size={16} />
              <span>All Groups</span>
            </button>
            
            {user && (
              <button
                onClick={() => setActiveTab('my-groups')}
                className={`flex items-center space-x-2 px-4 py-2 rounded-md transition-colors ${
                  activeTab === 'my-groups'
                    ? 'bg-white dark:bg-gray-700 text-blue-600 dark:text-blue-400 shadow-sm'
                    : 'text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white'
                }`}
              >
                <Users size={16} />
                <span>My Groups</span>
              </button>
            )}
            
            <button
              onClick={() => setActiveTab('trending')}
              className={`flex items-center space-x-2 px-4 py-2 rounded-md transition-colors ${
                activeTab === 'trending'
                  ? 'bg-white dark:bg-gray-700 text-blue-600 dark:text-blue-400 shadow-sm'
                  : 'text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white'
              }`}
            >
              <TrendingUp size={16} />
              <span>Trending</span>
            </button>
          </div>
        </div>

        {/* Content */}
        <div className="space-y-6">
          {searchQuery ? (
            <div>
              <h2 className="text-lg font-semibold text-gray-900 dark:text-white mb-4">
                Search Results for "{searchQuery}"
              </h2>
              <GroupList 
                key={`search-${searchQuery}-${refreshKey}`}
                searchQuery={searchQuery}
              />
            </div>
          ) : (
            <div>
              {activeTab === 'all' && (
                <GroupList 
                  key={`all-${refreshKey}`}
                />
              )}
              
              {activeTab === 'my-groups' && user && (
                <GroupList 
                  key={`my-groups-${refreshKey}`}
                  showMyGroups={true}
                />
              )}
              
              {activeTab === 'trending' && (
                <div className="text-center py-12">
                  <TrendingUp size={48} className="mx-auto text-gray-400 mb-4" />
                  <h3 className="text-lg font-medium text-gray-900 dark:text-white mb-2">
                    Trending Groups
                  </h3>
                  <p className="text-gray-600 dark:text-gray-300">
                    Coming soon! We're working on trending group algorithms.
                  </p>
                </div>
              )}
            </div>
          )}
        </div>

        {/* Create Group Modal */}
        <CreateGroupModal
          isOpen={showCreateModal}
          onClose={() => setShowCreateModal(false)}
          onGroupCreated={handleGroupCreated}
        />
          </div>
        </div>
      </div>
    </div>
  );
}
