'use client';

import React, { useState } from 'react';
import { AdminPost, AdminComment, SystemTag } from '@/types';
import { ContentHeader } from './ContentHeader';
import { ContentBody } from './ContentBody';
import { ContentFooter } from './ContentFooter';
import { InlineActionForm } from './InlineActionForm';

export interface AdminContentCardProps {
  content: AdminPost | AdminComment;
  contentType: 'post' | 'comment';
  isSelected?: boolean;
  onSelect?: (id: number) => void;
  onHide?: (id: number, reason: string) => Promise<void>;
  onUnhide?: (id: number) => Promise<void>;
  onTag?: (id: number, tagIds: number[]) => Promise<void>;
  actionLoading?: boolean;
  showEngagement?: boolean;
  showPostContext?: boolean;
  className?: string;
}

export function AdminContentCard({
  content,
  contentType,
  isSelected = false,
  onSelect,
  onHide,
  onUnhide,
  onTag,
  actionLoading = false,
  showEngagement = true,
  showPostContext = false,
  className = '',
}: AdminContentCardProps) {
  const [showActionForm, setShowActionForm] = useState(false);
  const [actionType, setActionType] = useState<'hide' | 'tag' | null>(null);

  const handleActionClick = (action: 'hide' | 'unhide' | 'tag') => {
    if (action === 'unhide' && onUnhide) {
      onUnhide(content.id);
      return;
    }
    
    setActionType(action === 'hide' ? 'hide' : 'tag');
    setShowActionForm(true);
  };

  const handleActionSubmit = async (reason: string, tagIds?: number[]) => {
    try {
      if (actionType === 'hide' && onHide) {
        await onHide(content.id, reason);
      } else if (actionType === 'tag' && onTag && tagIds) {
        await onTag(content.id, tagIds);
      }
      setShowActionForm(false);
      setActionType(null);
    } catch (error) {
      console.error('Action failed:', error);
    }
  };

  const handleActionCancel = () => {
    setShowActionForm(false);
    setActionType(null);
  };

  return (
    <div className={`bg-white border border-gray-200 rounded-lg shadow-sm ${content.isHidden ? 'bg-red-50 border-red-200' : ''} ${className}`}>
      <div className="p-6">
        <ContentHeader
          content={content}
          contentType={contentType}
          isSelected={isSelected}
          onSelect={onSelect}
          onActionClick={handleActionClick}
          actionLoading={actionLoading}
        />
        
        <ContentBody
          content={content}
          contentType={contentType}
          showPostContext={showPostContext}
        />
        
        {showEngagement && (
          <ContentFooter
            content={content}
            contentType={contentType}
          />
        )}
        
        {showActionForm && actionType && (
          <InlineActionForm
            actionType={actionType}
            contentType={contentType}
            onSubmit={handleActionSubmit}
            onCancel={handleActionCancel}
          />
        )}
      </div>
    </div>
  );
}
