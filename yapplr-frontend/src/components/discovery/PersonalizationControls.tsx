'use client';

import React, { useState, useEffect } from 'react';
import { personalizationApi } from '@/lib/api';
import { UserPersonalizationProfileDto, PersonalizedFeedConfigDto } from '@/types';

interface PersonalizationControlsProps {
  className?: string;
}

export default function PersonalizationControls({ className = '' }: PersonalizationControlsProps) {
  const [profile, setProfile] = useState<UserPersonalizationProfileDto | null>(null);
  const [feedConfig, setFeedConfig] = useState<PersonalizedFeedConfigDto>({
    userId: 0,
    postLimit: 20,
    diversityWeight: 0.3,
    noveltyWeight: 0.2,
    socialWeight: 0.3,
    qualityThreshold: 0.5,
    includeExperimental: false,
    preferredContentTypes: [],
    excludedTopics: [],
    feedType: 'main'
  });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadProfile();
  }, []);

  const loadProfile = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await personalizationApi.getProfile();
      setProfile(data);
      
      // Initialize feed config with profile data
      setFeedConfig(prev => ({
        ...prev,
        userId: data.userId,
        diversityWeight: data.diversityPreference,
        noveltyWeight: data.noveltyPreference,
        socialWeight: data.socialInfluenceFactor,
        qualityThreshold: data.qualityThreshold
      }));
    } catch (err) {
      console.error('Failed to load personalization profile:', err);
      setError('Failed to load profile');
    } finally {
      setLoading(false);
    }
  };

  const updateProfile = async (forceRebuild: boolean = false) => {
    try {
      setSaving(true);
      const updated = await personalizationApi.updateProfile(forceRebuild);
      setProfile(updated);
    } catch (err) {
      console.error('Failed to update profile:', err);
      setError('Failed to update profile');
    } finally {
      setSaving(false);
    }
  };

  const handleConfigChange = (key: keyof PersonalizedFeedConfigDto, value: any) => {
    setFeedConfig(prev => ({ ...prev, [key]: value }));
  };

  const getConfidenceColor = (confidence: number) => {
    if (confidence > 0.8) return 'text-green-500 bg-green-100 dark:bg-green-900/30';
    if (confidence > 0.6) return 'text-blue-500 bg-blue-100 dark:bg-blue-900/30';
    if (confidence > 0.4) return 'text-yellow-500 bg-yellow-100 dark:bg-yellow-900/30';
    return 'text-red-500 bg-red-100 dark:bg-red-900/30';
  };

  const getConfidenceLabel = (confidence: number) => {
    if (confidence > 0.8) return 'Excellent';
    if (confidence > 0.6) return 'Good';
    if (confidence > 0.4) return 'Fair';
    return 'Poor';
  };

  if (loading) {
    return (
      <div className={`personalization-controls ${className}`}>
        <div className="animate-pulse space-y-6">
          <div className="h-8 bg-gray-200 dark:bg-gray-700 rounded"></div>
          <div className="space-y-4">
            {[...Array(5)].map((_, i) => (
              <div key={i} className="h-16 bg-gray-200 dark:bg-gray-700 rounded-lg"></div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  if (error || !profile) {
    return (
      <div className={`personalization-controls ${className}`}>
        <div className="text-center py-12">
          <div className="text-6xl mb-4">😕</div>
          <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
            Something went wrong
          </h3>
          <p className="text-gray-500 dark:text-gray-400 mb-4">
            {error || 'Unable to load personalization settings'}
          </p>
          <button 
            onClick={loadProfile}
            className="px-6 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className={`personalization-controls ${className}`}>
      {/* Header */}
      <div className="mb-6">
        <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">
          ⚙️ Personalization Settings
        </h3>
        <p className="text-gray-600 dark:text-gray-400">
          Control how your feed is personalized and recommendations are generated
        </p>
      </div>

      {/* Profile Status */}
      <div className="mb-8 bg-white dark:bg-gray-800 rounded-lg p-6 border border-gray-200 dark:border-gray-700">
        <div className="flex items-center justify-between mb-4">
          <h4 className="text-lg font-semibold text-gray-900 dark:text-white">
            Profile Status
          </h4>
          <span className={`px-3 py-1 rounded-full text-sm font-medium ${getConfidenceColor(profile.personalizationConfidence)}`}>
            {getConfidenceLabel(profile.personalizationConfidence)} ({(profile.personalizationConfidence * 100).toFixed(0)}%)
          </span>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4">
          <div className="text-center">
            <div className="text-2xl font-bold text-gray-900 dark:text-white">
              {profile.dataPointCount.toLocaleString()}
            </div>
            <div className="text-sm text-gray-500 dark:text-gray-400">
              Data Points
            </div>
          </div>
          <div className="text-center">
            <div className="text-2xl font-bold text-gray-900 dark:text-white">
              {Object.keys(profile.interestScores).length}
            </div>
            <div className="text-sm text-gray-500 dark:text-gray-400">
              Tracked Interests
            </div>
          </div>
          <div className="text-center">
            <div className="text-2xl font-bold text-gray-900 dark:text-white">
              {profile.algorithmVersion}
            </div>
            <div className="text-sm text-gray-500 dark:text-gray-400">
              Algorithm Version
            </div>
          </div>
        </div>

        <div className="flex space-x-3">
          <button
            onClick={() => updateProfile(false)}
            disabled={saving}
            className="px-4 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 disabled:opacity-50 transition-colors"
          >
            {saving ? 'Updating...' : 'Update Profile'}
          </button>
          <button
            onClick={() => updateProfile(true)}
            disabled={saving}
            className="px-4 py-2 bg-orange-500 text-white rounded-lg hover:bg-orange-600 disabled:opacity-50 transition-colors"
          >
            {saving ? 'Rebuilding...' : 'Rebuild Profile'}
          </button>
        </div>
      </div>

      {/* Feed Configuration */}
      <div className="bg-white dark:bg-gray-800 rounded-lg p-6 border border-gray-200 dark:border-gray-700">
        <h4 className="text-lg font-semibold text-gray-900 dark:text-white mb-6">
          Feed Preferences
        </h4>

        <div className="space-y-6">
          {/* Post Limit */}
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Posts per Feed Load: {feedConfig.postLimit}
            </label>
            <input
              type="range"
              min="10"
              max="50"
              step="5"
              value={feedConfig.postLimit}
              onChange={(e) => handleConfigChange('postLimit', parseInt(e.target.value))}
              className="w-full"
            />
            <div className="flex justify-between text-xs text-gray-500 dark:text-gray-400 mt-1">
              <span>10</span>
              <span>50</span>
            </div>
          </div>

          {/* Diversity Weight */}
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Content Diversity: {(feedConfig.diversityWeight * 100).toFixed(0)}%
            </label>
            <input
              type="range"
              min="0"
              max="1"
              step="0.1"
              value={feedConfig.diversityWeight}
              onChange={(e) => handleConfigChange('diversityWeight', parseFloat(e.target.value))}
              className="w-full"
            />
            <div className="text-xs text-gray-500 dark:text-gray-400 mt-1">
              Higher values show more varied content types and topics
            </div>
          </div>

          {/* Novelty Weight */}
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Novelty Preference: {(feedConfig.noveltyWeight * 100).toFixed(0)}%
            </label>
            <input
              type="range"
              min="0"
              max="1"
              step="0.1"
              value={feedConfig.noveltyWeight}
              onChange={(e) => handleConfigChange('noveltyWeight', parseFloat(e.target.value))}
              className="w-full"
            />
            <div className="text-xs text-gray-500 dark:text-gray-400 mt-1">
              Higher values prioritize new and trending content
            </div>
          </div>

          {/* Social Weight */}
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Social Influence: {(feedConfig.socialWeight * 100).toFixed(0)}%
            </label>
            <input
              type="range"
              min="0"
              max="1"
              step="0.1"
              value={feedConfig.socialWeight}
              onChange={(e) => handleConfigChange('socialWeight', parseFloat(e.target.value))}
              className="w-full"
            />
            <div className="text-xs text-gray-500 dark:text-gray-400 mt-1">
              Higher values show more content from people you follow
            </div>
          </div>

          {/* Quality Threshold */}
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Quality Threshold: {(feedConfig.qualityThreshold * 100).toFixed(0)}%
            </label>
            <input
              type="range"
              min="0"
              max="1"
              step="0.1"
              value={feedConfig.qualityThreshold}
              onChange={(e) => handleConfigChange('qualityThreshold', parseFloat(e.target.value))}
              className="w-full"
            />
            <div className="text-xs text-gray-500 dark:text-gray-400 mt-1">
              Higher values filter out lower quality content
            </div>
          </div>

          {/* Content Types */}
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">
              Preferred Content Types
            </label>
            <div className="grid grid-cols-2 md:grid-cols-3 gap-2">
              {['text', 'image', 'video', 'link', 'poll', 'quote'].map((type) => (
                <label key={type} className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    checked={feedConfig.preferredContentTypes.includes(type)}
                    onChange={(e) => {
                      if (e.target.checked) {
                        handleConfigChange('preferredContentTypes', [...feedConfig.preferredContentTypes, type]);
                      } else {
                        handleConfigChange('preferredContentTypes', feedConfig.preferredContentTypes.filter(t => t !== type));
                      }
                    }}
                    className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                  />
                  <span className="text-sm text-gray-700 dark:text-gray-300 capitalize">{type}</span>
                </label>
              ))}
            </div>
          </div>

          {/* Experimental Features */}
          <div className="flex items-center space-x-3">
            <input
              type="checkbox"
              id="includeExperimental"
              checked={feedConfig.includeExperimental}
              onChange={(e) => handleConfigChange('includeExperimental', e.target.checked)}
              className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
            />
            <label htmlFor="includeExperimental" className="text-sm text-gray-700 dark:text-gray-300">
              Include experimental recommendations
            </label>
          </div>
        </div>

        {/* Save Button */}
        <div className="mt-6 pt-6 border-t border-gray-200 dark:border-gray-700">
          <button
            onClick={() => {
              // This would save the feed configuration
              console.log('Saving feed config:', feedConfig);
            }}
            className="px-6 py-2 bg-green-500 text-white rounded-lg hover:bg-green-600 transition-colors"
          >
            Save Preferences
          </button>
        </div>
      </div>

      {/* Profile Insights */}
      <div className="mt-6 bg-gray-50 dark:bg-gray-700 rounded-lg p-4">
        <h5 className="font-medium text-gray-900 dark:text-white mb-3">
          Profile Last Updated
        </h5>
        <div className="text-sm text-gray-600 dark:text-gray-400">
          <div>ML Update: {new Date(profile.lastMLUpdate).toLocaleString()}</div>
          <div>Data Points: {profile.dataPointCount.toLocaleString()}</div>
          <div>Algorithm: v{profile.algorithmVersion}</div>
        </div>
      </div>
    </div>
  );
}
