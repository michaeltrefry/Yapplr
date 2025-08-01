'use client';

import React, { useState, useEffect } from 'react';
import { topicApi } from '@/lib/api';
import { TopicFollowDto, UpdateTopicFollowDto } from '@/types';

interface TopicManagementProps {
  className?: string;
}

export default function TopicManagement({ className = '' }: TopicManagementProps) {
  const [followedTopics, setFollowedTopics] = useState<TopicFollowDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [editingTopic, setEditingTopic] = useState<string | null>(null);
  const [filterMainFeed, setFilterMainFeed] = useState<boolean | null>(null);

  useEffect(() => {
    loadFollowedTopics();
  }, [filterMainFeed]);

  const loadFollowedTopics = async () => {
    try {
      setLoading(true);
      setError(null);
      const topics = await topicApi.getUserTopics(filterMainFeed || undefined);
      setFollowedTopics(topics);
    } catch (err) {
      console.error('Failed to load followed topics:', err);
      setError('Failed to load followed topics');
    } finally {
      setLoading(false);
    }
  };

  const handleUnfollow = async (topicName: string) => {
    try {
      await topicApi.unfollowTopic(topicName);
      setFollowedTopics(prev => prev.filter(t => t.topicName !== topicName));
    } catch (err) {
      console.error('Failed to unfollow topic:', err);
    }
  };

  const handleUpdateTopic = async (topicName: string, updates: UpdateTopicFollowDto) => {
    try {
      const updated = await topicApi.updateTopicFollow(topicName, updates);
      setFollowedTopics(prev => 
        prev.map(t => t.topicName === topicName ? updated : t)
      );
      setEditingTopic(null);
    } catch (err) {
      console.error('Failed to update topic:', err);
    }
  };

  const getTopicIcon = (category: string) => {
    const icons: Record<string, string> = {
      'Technology': '💻', 'Sports': '⚽', 'Entertainment': '🎬', 'Gaming': '🎮',
      'News': '📰', 'Music': '🎵', 'Art': '🎨', 'Food': '🍕', 'Travel': '✈️', 'Fashion': '👗'
    };
    return icons[category] || '🏷️';
  };

  const getInterestLevelColor = (level: number) => {
    if (level >= 0.8) return 'text-green-500 bg-green-100 dark:bg-green-900/30';
    if (level >= 0.6) return 'text-blue-500 bg-blue-100 dark:bg-blue-900/30';
    if (level >= 0.4) return 'text-yellow-500 bg-yellow-100 dark:bg-yellow-900/30';
    return 'text-gray-500 bg-gray-100 dark:bg-gray-900/30';
  };

  const getInterestLevelLabel = (level: number) => {
    if (level >= 0.8) return 'High';
    if (level >= 0.6) return 'Medium';
    if (level >= 0.4) return 'Low';
    return 'Minimal';
  };

  if (loading) {
    return (
      <div className={`topic-management ${className}`}>
        <div className="animate-pulse space-y-4">
          <div className="h-8 bg-gray-200 dark:bg-gray-700 rounded"></div>
          {[...Array(5)].map((_, i) => (
            <div key={i} className="h-24 bg-gray-200 dark:bg-gray-700 rounded-lg"></div>
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={`topic-management ${className}`}>
        <div className="text-center py-12">
          <div className="text-6xl mb-4">😕</div>
          <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
            Something went wrong
          </h3>
          <p className="text-gray-500 dark:text-gray-400 mb-4">{error}</p>
          <button 
            onClick={loadFollowedTopics}
            className="px-6 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className={`topic-management ${className}`}>
      {/* Header */}
      <div className="mb-6">
        <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
          🎯 Your Topics
        </h3>
        <p className="text-gray-600 dark:text-gray-400">
          Manage your followed topics and preferences
        </p>
      </div>

      {/* Filter */}
      <div className="mb-6">
        <div className="flex space-x-2">
          <button
            onClick={() => setFilterMainFeed(null)}
            className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
              filterMainFeed === null
                ? 'bg-blue-500 text-white'
                : 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-600'
            }`}
          >
            All Topics ({followedTopics.length})
          </button>
          <button
            onClick={() => setFilterMainFeed(true)}
            className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
              filterMainFeed === true
                ? 'bg-blue-500 text-white'
                : 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-600'
            }`}
          >
            In Main Feed
          </button>
          <button
            onClick={() => setFilterMainFeed(false)}
            className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
              filterMainFeed === false
                ? 'bg-blue-500 text-white'
                : 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-300 dark:hover:bg-gray-600'
            }`}
          >
            Excluded from Feed
          </button>
        </div>
      </div>

      {/* Topics List */}
      {followedTopics.length === 0 ? (
        <div className="text-center py-12">
          <div className="text-6xl mb-4">🎯</div>
          <h4 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
            No topics followed yet
          </h4>
          <p className="text-gray-500 dark:text-gray-400 mb-4">
            Start following topics to see personalized content
          </p>
          <button className="px-6 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors">
            Discover Topics
          </button>
        </div>
      ) : (
        <div className="space-y-4">
          {followedTopics.map((topic) => (
            <div 
              key={topic.id}
              className="bg-white dark:bg-gray-800 rounded-lg p-6 border border-gray-200 dark:border-gray-700"
            >
              <div className="flex items-start justify-between">
                <div className="flex items-start space-x-4 flex-1">
                  <span className="text-2xl">{getTopicIcon(topic.category)}</span>
                  
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center space-x-3 mb-2">
                      <h4 className="text-lg font-semibold text-gray-900 dark:text-white">
                        {topic.topicName}
                      </h4>
                      <span className={`px-2 py-1 rounded-full text-xs font-medium ${getInterestLevelColor(topic.interestLevel)}`}>
                        {getInterestLevelLabel(topic.interestLevel)}
                      </span>
                    </div>
                    
                    <p className="text-sm text-gray-500 dark:text-gray-400 mb-2">
                      {topic.category}
                    </p>
                    
                    {topic.topicDescription && (
                      <p className="text-sm text-gray-600 dark:text-gray-400 mb-3">
                        {topic.topicDescription}
                      </p>
                    )}

                    {/* Related Hashtags */}
                    {topic.relatedHashtags.length > 0 && (
                      <div className="flex flex-wrap gap-1 mb-3">
                        {topic.relatedHashtags.slice(0, 5).map((hashtag) => (
                          <span 
                            key={hashtag}
                            className="px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 rounded-full text-xs"
                          >
                            #{hashtag}
                          </span>
                        ))}
                        {topic.relatedHashtags.length > 5 && (
                          <span className="text-xs text-gray-500 dark:text-gray-400">
                            +{topic.relatedHashtags.length - 5} more
                          </span>
                        )}
                      </div>
                    )}

                    {/* Settings */}
                    {editingTopic === topic.topicName ? (
                      <TopicEditForm 
                        topic={topic}
                        onSave={(updates) => handleUpdateTopic(topic.topicName, updates)}
                        onCancel={() => setEditingTopic(null)}
                      />
                    ) : (
                      <div className="flex items-center space-x-4 text-sm">
                        <div className="flex items-center space-x-2">
                          <span className={`w-2 h-2 rounded-full ${topic.includeInMainFeed ? 'bg-green-500' : 'bg-gray-400'}`}></span>
                          <span className="text-gray-600 dark:text-gray-400">
                            {topic.includeInMainFeed ? 'In main feed' : 'Excluded from feed'}
                          </span>
                        </div>
                        
                        <div className="flex items-center space-x-2">
                          <span className={`w-2 h-2 rounded-full ${topic.enableNotifications ? 'bg-blue-500' : 'bg-gray-400'}`}></span>
                          <span className="text-gray-600 dark:text-gray-400">
                            {topic.enableNotifications ? 'Notifications on' : 'Notifications off'}
                          </span>
                        </div>
                      </div>
                    )}
                  </div>
                </div>

                {/* Actions */}
                <div className="flex items-center space-x-2 ml-4">
                  <button
                    onClick={() => setEditingTopic(editingTopic === topic.topicName ? null : topic.topicName)}
                    className="p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors"
                    title="Edit settings"
                  >
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                    </svg>
                  </button>
                  
                  <button
                    onClick={() => handleUnfollow(topic.topicName)}
                    className="p-2 text-red-400 hover:text-red-600 transition-colors"
                    title="Unfollow topic"
                  >
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

// Topic Edit Form Component
interface TopicEditFormProps {
  topic: TopicFollowDto;
  onSave: (updates: UpdateTopicFollowDto) => void;
  onCancel: () => void;
}

function TopicEditForm({ topic, onSave, onCancel }: TopicEditFormProps) {
  const [interestLevel, setInterestLevel] = useState(topic.interestLevel);
  const [includeInMainFeed, setIncludeInMainFeed] = useState(topic.includeInMainFeed);
  const [enableNotifications, setEnableNotifications] = useState(topic.enableNotifications);
  const [notificationThreshold, setNotificationThreshold] = useState(topic.notificationThreshold);

  const handleSave = () => {
    onSave({
      interestLevel,
      includeInMainFeed,
      enableNotifications,
      notificationThreshold
    });
  };

  return (
    <div className="bg-gray-50 dark:bg-gray-700 rounded-lg p-4 mt-3">
      <h5 className="font-medium text-gray-900 dark:text-white mb-4">Topic Settings</h5>
      
      <div className="space-y-4">
        {/* Interest Level */}
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            Interest Level: {(interestLevel * 100).toFixed(0)}%
          </label>
          <input
            type="range"
            min="0"
            max="1"
            step="0.1"
            value={interestLevel}
            onChange={(e) => setInterestLevel(parseFloat(e.target.value))}
            className="w-full"
          />
        </div>

        {/* Include in Main Feed */}
        <div className="flex items-center">
          <input
            type="checkbox"
            id="includeInMainFeed"
            checked={includeInMainFeed}
            onChange={(e) => setIncludeInMainFeed(e.target.checked)}
            className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
          />
          <label htmlFor="includeInMainFeed" className="ml-2 text-sm text-gray-700 dark:text-gray-300">
            Include in main feed
          </label>
        </div>

        {/* Enable Notifications */}
        <div className="flex items-center">
          <input
            type="checkbox"
            id="enableNotifications"
            checked={enableNotifications}
            onChange={(e) => setEnableNotifications(e.target.checked)}
            className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
          />
          <label htmlFor="enableNotifications" className="ml-2 text-sm text-gray-700 dark:text-gray-300">
            Enable notifications
          </label>
        </div>

        {/* Notification Threshold */}
        {enableNotifications && (
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Notification Threshold: {notificationThreshold}
            </label>
            <input
              type="range"
              min="1"
              max="10"
              step="1"
              value={notificationThreshold}
              onChange={(e) => setNotificationThreshold(parseInt(e.target.value))}
              className="w-full"
            />
            <div className="text-xs text-gray-500 dark:text-gray-400 mt-1">
              Notify when {notificationThreshold} or more posts are trending
            </div>
          </div>
        )}
      </div>

      {/* Actions */}
      <div className="flex justify-end space-x-3 mt-4">
        <button
          onClick={onCancel}
          className="px-4 py-2 text-gray-600 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200"
        >
          Cancel
        </button>
        <button
          onClick={handleSave}
          className="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
        >
          Save Changes
        </button>
      </div>
    </div>
  );
}
