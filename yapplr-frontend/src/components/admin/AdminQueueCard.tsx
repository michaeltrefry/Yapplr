'use client';

import React, { useState } from 'react';
import { AdminPost, AdminComment, SystemTag } from '@/types';
import { ContentHeader } from './ContentHeader';
import { ContentBody } from './ContentBody';
import { ContentFooter } from './ContentFooter';
import { InlineActionForm } from './InlineActionForm';
import { Brain, Tag } from 'lucide-react';

export interface AdminQueueCardProps {
  content: AdminPost | AdminComment;
  contentType: 'post' | 'comment';
  onHide?: (id: number, reason: string) => Promise<void>;
  onApproveAiSuggestion?: (postId: number, tagId: number) => Promise<void>;
  onRejectAiSuggestion?: (postId: number, tagId: number) => Promise<void>;
  onBulkApproveAiSuggestions?: (postId: number, tagIds: number[]) => Promise<void>;
  onBulkRejectAiSuggestions?: (postId: number, tagIds: number[]) => Promise<void>;
  actionLoading?: boolean;
  showAiSuggestions?: boolean;
  className?: string;
}

export function AdminQueueCard({
  content,
  contentType,
  onHide,
  onApproveAiSuggestion,
  onRejectAiSuggestion,
  onBulkApproveAiSuggestions,
  onBulkRejectAiSuggestions,
  actionLoading = false,
  showAiSuggestions = true,
  className = '',
}: AdminQueueCardProps) {
  const [showActionForm, setShowActionForm] = useState(false);
  const [expandedAiSuggestions, setExpandedAiSuggestions] = useState(false);

  const isPost = contentType === 'post';
  const post = content as AdminPost;

  const handleActionClick = (action: 'hide' | 'unhide' | 'tag') => {
    if (action === 'hide') {
      setShowActionForm(true);
    }
  };

  const handleActionSubmit = async (reason: string) => {
    try {
      if (onHide) {
        await onHide(content.id, reason);
      }
      setShowActionForm(false);
    } catch (error) {
      console.error('Action failed:', error);
    }
  };

  const handleActionCancel = () => {
    setShowActionForm(false);
  };

  const toggleAiSuggestions = () => {
    setExpandedAiSuggestions(!expandedAiSuggestions);
  };

  const handleApproveAiSuggestion = async (tagId: number) => {
    if (onApproveAiSuggestion && isPost) {
      await onApproveAiSuggestion(post.id, tagId);
    }
  };

  const handleRejectAiSuggestion = async (tagId: number) => {
    if (onRejectAiSuggestion && isPost) {
      await onRejectAiSuggestion(post.id, tagId);
    }
  };

  const handleBulkApproveAiSuggestions = async (tagIds: number[]) => {
    if (onBulkApproveAiSuggestions && isPost) {
      await onBulkApproveAiSuggestions(post.id, tagIds);
    }
  };

  const handleBulkRejectAiSuggestions = async (tagIds: number[]) => {
    if (onBulkRejectAiSuggestions && isPost) {
      await onBulkRejectAiSuggestions(post.id, tagIds);
    }
  };

  return (
    <div className={`bg-white border border-gray-200 rounded-lg shadow-sm ${className}`}>
      <div className="p-6">
        <ContentHeader
          content={content}
          contentType={contentType}
          onActionClick={handleActionClick}
          actionLoading={actionLoading}
        />
        
        <ContentBody
          content={content}
          contentType={contentType}
          showPostContext={!isPost}
        />

        {/* AI Suggested Tags for Posts */}
        {isPost && showAiSuggestions && post.aiSuggestedTags && post.aiSuggestedTags.length > 0 && (
          <div className="mt-4 border-t border-gray-200 pt-4">
            <button
              onClick={toggleAiSuggestions}
              className="flex items-center text-sm text-blue-600 hover:text-blue-800 mb-2"
            >
              <Brain className="h-4 w-4 mr-1" />
              AI Suggestions ({post.aiSuggestedTags.filter(tag => !tag.isApproved && !tag.isRejected).length} pending)
              <span className="ml-1">
                {expandedAiSuggestions ? '▼' : '▶'}
              </span>
            </button>
            {expandedAiSuggestions && (
              <div className="space-y-3">
                <div className="flex flex-wrap gap-2">
                  {post.aiSuggestedTags.map((tag) => (
                    <div key={tag.id} className="flex items-center space-x-2">
                      <span
                        className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${
                          tag.isApproved 
                            ? 'bg-green-100 text-green-800' 
                            : tag.isRejected 
                            ? 'bg-red-100 text-red-800'
                            : 'bg-blue-100 text-blue-800'
                        }`}
                      >
                        <Tag className="h-3 w-3 mr-1" />
                        {tag.tagName}
                      </span>
                      {!tag.isApproved && !tag.isRejected && (
                        <div className="flex space-x-1">
                          <button
                            onClick={() => handleApproveAiSuggestion(tag.id)}
                            className="text-green-600 hover:text-green-800 text-xs"
                          >
                            ✓
                          </button>
                          <button
                            onClick={() => handleRejectAiSuggestion(tag.id)}
                            className="text-red-600 hover:text-red-800 text-xs"
                          >
                            ✗
                          </button>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
                
                {post.aiSuggestedTags.some(tag => !tag.isApproved && !tag.isRejected) && (
                  <div className="flex space-x-2">
                    <button
                      onClick={() => handleBulkApproveAiSuggestions(
                        post.aiSuggestedTags
                          .filter(tag => !tag.isApproved && !tag.isRejected)
                          .map(tag => tag.id)
                      )}
                      className="px-3 py-1 bg-green-100 text-green-800 rounded-md hover:bg-green-200 transition-colors text-xs"
                    >
                      Approve All
                    </button>
                    <button
                      onClick={() => handleBulkRejectAiSuggestions(
                        post.aiSuggestedTags
                          .filter(tag => !tag.isApproved && !tag.isRejected)
                          .map(tag => tag.id)
                      )}
                      className="px-3 py-1 bg-red-100 text-red-800 rounded-md hover:bg-red-200 transition-colors text-xs"
                    >
                      Reject All
                    </button>
                  </div>
                )}
              </div>
            )}
          </div>
        )}
        
        <ContentFooter
          content={content}
          contentType={contentType}
        />
        
        {showActionForm && (
          <InlineActionForm
            actionType="hide"
            contentType={contentType}
            onSubmit={handleActionSubmit}
            onCancel={handleActionCancel}
          />
        )}
      </div>
    </div>
  );
}
