'use client';

import React from 'react';
import { AdminPost, AdminComment } from '@/types';
import { AdminQueueCard } from './AdminQueueCard';
import { FileText, MessageSquare } from 'lucide-react';

export interface AdminQueueListProps {
  items: (AdminPost | AdminComment)[];
  contentType: 'post' | 'comment';
  loading?: boolean;
  onHide?: (id: number, reason: string) => Promise<void>;
  onApproveAiSuggestion?: (postId: number, tagId: number) => Promise<void>;
  onRejectAiSuggestion?: (postId: number, tagId: number) => Promise<void>;
  onBulkApproveAiSuggestions?: (postId: number, tagIds: number[]) => Promise<void>;
  onBulkRejectAiSuggestions?: (postId: number, tagIds: number[]) => Promise<void>;
  actionLoading?: number | null;
  showAiSuggestions?: boolean;
  emptyMessage?: string;
  className?: string;
}

export function AdminQueueList({
  items,
  contentType,
  loading = false,
  onHide,
  onApproveAiSuggestion,
  onRejectAiSuggestion,
  onBulkApproveAiSuggestions,
  onBulkRejectAiSuggestions,
  actionLoading = null,
  showAiSuggestions = true,
  emptyMessage,
  className = '',
}: AdminQueueListProps) {
  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  if (items.length === 0) {
    const Icon = contentType === 'post' ? FileText : MessageSquare;
    const defaultMessage = `No flagged ${contentType}s to review`;
    
    return (
      <div className="text-center py-12">
        <Icon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
        <p className="text-gray-500">{emptyMessage || defaultMessage}</p>
      </div>
    );
  }

  return (
    <div className={`space-y-4 ${className}`}>
      {items.map((item) => (
        <AdminQueueCard
          key={item.id}
          content={item}
          contentType={contentType}
          onHide={onHide}
          onApproveAiSuggestion={onApproveAiSuggestion}
          onRejectAiSuggestion={onRejectAiSuggestion}
          onBulkApproveAiSuggestions={onBulkApproveAiSuggestions}
          onBulkRejectAiSuggestions={onBulkRejectAiSuggestions}
          actionLoading={actionLoading === item.id}
          showAiSuggestions={showAiSuggestions}
        />
      ))}
    </div>
  );
}
