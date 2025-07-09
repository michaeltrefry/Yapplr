'use client';

import React, { useState, useEffect } from 'react';
import { SystemTag } from '@/types';
import { adminApi } from '@/lib/api';

export interface InlineActionFormProps {
  actionType: 'hide' | 'tag';
  contentType: 'post' | 'comment';
  onSubmit: (reason: string, tagIds?: number[]) => Promise<void>;
  onCancel: () => void;
}

export function InlineActionForm({
  actionType,
  contentType,
  onSubmit,
  onCancel,
}: InlineActionFormProps) {
  const [reason, setReason] = useState('');
  const [selectedTagIds, setSelectedTagIds] = useState<number[]>([]);
  const [availableTags, setAvailableTags] = useState<SystemTag[]>([]);
  const [loading, setLoading] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (actionType === 'tag') {
      fetchAvailableTags();
    }
  }, [actionType]);

  const fetchAvailableTags = async () => {
    try {
      setLoading(true);
      const tags = await adminApi.getSystemTags();
      setAvailableTags(tags);
    } catch (error) {
      console.error('Failed to fetch system tags:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async () => {
    if (actionType === 'hide' && !reason.trim()) {
      return;
    }
    if (actionType === 'tag' && selectedTagIds.length === 0) {
      return;
    }

    try {
      setSubmitting(true);
      await onSubmit(reason, selectedTagIds);
    } catch (error) {
      console.error('Action failed:', error);
    } finally {
      setSubmitting(false);
    }
  };

  const toggleTag = (tagId: number) => {
    setSelectedTagIds(prev => 
      prev.includes(tagId) 
        ? prev.filter(id => id !== tagId)
        : [...prev, tagId]
    );
  };

  const isValid = actionType === 'hide' ? reason.trim() : selectedTagIds.length > 0;

  return (
    <div className="mt-4 p-4 bg-gray-50 rounded-lg border-t border-gray-200">
      <div className="mb-3">
        <h4 className="text-sm font-medium text-gray-900 mb-2">
          {actionType === 'hide' ? `Hide ${contentType}` : `Tag ${contentType}`}
        </h4>
        
        {actionType === 'hide' ? (
          <div>
            <label htmlFor="reason" className="block text-sm text-gray-700 mb-1">
              Reason for hiding this {contentType}:
            </label>
            <textarea
              id="reason"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              rows={3}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              placeholder="Enter your reason..."
            />
          </div>
        ) : (
          <div>
            <label className="block text-sm text-gray-700 mb-2">
              Select tags to apply:
            </label>
            {loading ? (
              <div className="text-sm text-gray-500">Loading tags...</div>
            ) : (
              <div className="space-y-2 max-h-40 overflow-y-auto">
                {availableTags.map((tag) => (
                  <label key={tag.id} className="flex items-center">
                    <input
                      type="checkbox"
                      checked={selectedTagIds.includes(tag.id)}
                      onChange={() => toggleTag(tag.id)}
                      className="rounded border-gray-300 text-blue-600 focus:ring-blue-500 mr-2"
                    />
                    <span
                      className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium"
                      style={{ 
                        backgroundColor: `${tag.color}20`, 
                        color: tag.color 
                      }}
                    >
                      {tag.name}
                    </span>
                  </label>
                ))}
              </div>
            )}
          </div>
        )}
      </div>
      
      <div className="flex space-x-2">
        <button
          onClick={handleSubmit}
          disabled={!isValid || submitting}
          className={`px-4 py-2 rounded-md text-sm font-medium transition-colors ${
            isValid && !submitting
              ? actionType === 'hide'
                ? 'bg-yellow-600 text-white hover:bg-yellow-700'
                : 'bg-purple-600 text-white hover:bg-purple-700'
              : 'bg-gray-300 text-gray-500 cursor-not-allowed'
          }`}
        >
          {submitting ? 'Processing...' : actionType === 'hide' ? `Hide ${contentType}` : `Apply Tags`}
        </button>
        <button
          onClick={onCancel}
          disabled={submitting}
          className="px-4 py-2 bg-gray-200 text-gray-700 rounded-md text-sm font-medium hover:bg-gray-300 transition-colors disabled:opacity-50"
        >
          Cancel
        </button>
      </div>
    </div>
  );
}
