'use client';

import React, { useState } from 'react';
import { AdminPost, AdminComment } from '@/types';
import { AdminContentCard } from './AdminContentCard';
import { Search, Filter, ChevronLeft, ChevronRight } from 'lucide-react';

export interface AdminContentListProps {
  items: (AdminPost | AdminComment)[];
  contentType: 'post' | 'comment';
  loading?: boolean;
  searchTerm?: string;
  onSearchChange?: (term: string) => void;
  onHide?: (id: number, reason: string) => Promise<void>;
  onUnhide?: (id: number) => Promise<void>;
  onTag?: (id: number, tagIds: number[]) => Promise<void>;
  onBulkHide?: (ids: number[], reason: string) => Promise<void>;
  onBulkUnhide?: (ids: number[]) => Promise<void>;
  actionLoading?: number | null;
  showEngagement?: boolean;
  showPostContext?: boolean;
  showBulkActions?: boolean;
  showSearch?: boolean;
  showFilters?: boolean;
  filters?: React.ReactNode;
  pagination?: {
    currentPage: number;
    totalPages: number;
    onPageChange: (page: number) => void;
  };
  className?: string;
}

export function AdminContentList({
  items,
  contentType,
  loading = false,
  searchTerm = '',
  onSearchChange,
  onHide,
  onUnhide,
  onTag,
  onBulkHide,
  onBulkUnhide,
  actionLoading = null,
  showEngagement = true,
  showPostContext = false,
  showBulkActions = true,
  showSearch = true,
  showFilters = false,
  filters,
  pagination,
  className = '',
}: AdminContentListProps) {
  const [selectedItems, setSelectedItems] = useState<number[]>([]);
  const [showBulkActionForm, setShowBulkActionForm] = useState(false);
  const [bulkActionType, setBulkActionType] = useState<'hide' | 'unhide' | null>(null);
  const [bulkReason, setBulkReason] = useState('');

  const handleSelectItem = (id: number) => {
    setSelectedItems(prev => 
      prev.includes(id) 
        ? prev.filter(itemId => itemId !== id)
        : [...prev, id]
    );
  };

  const handleSelectAll = () => {
    if (selectedItems.length === items.length) {
      setSelectedItems([]);
    } else {
      setSelectedItems(items.map(item => item.id));
    }
  };

  const handleBulkAction = (action: 'hide' | 'unhide') => {
    if (selectedItems.length === 0) return;
    
    if (action === 'unhide' && onBulkUnhide) {
      onBulkUnhide(selectedItems);
      setSelectedItems([]);
      return;
    }
    
    setBulkActionType(action);
    setShowBulkActionForm(true);
  };

  const handleBulkSubmit = async () => {
    if (bulkActionType === 'hide' && onBulkHide && bulkReason.trim()) {
      await onBulkHide(selectedItems, bulkReason);
      setSelectedItems([]);
      setShowBulkActionForm(false);
      setBulkActionType(null);
      setBulkReason('');
    }
  };

  const handleBulkCancel = () => {
    setShowBulkActionForm(false);
    setBulkActionType(null);
    setBulkReason('');
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  return (
    <div className={`space-y-6 ${className}`}>
      {/* Search and Filters */}
      {(showSearch || showFilters) && (
        <div className="flex flex-col sm:flex-row gap-4">
          {showSearch && onSearchChange && (
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
              <input
                type="text"
                placeholder={`Search ${contentType}s...`}
                value={searchTerm}
                onChange={(e) => onSearchChange(e.target.value)}
                className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          )}
          
          {showFilters && filters && (
            <div className="flex items-center space-x-3">
              <Filter className="h-5 w-5 text-gray-400" />
              {filters}
            </div>
          )}
        </div>
      )}

      {/* Bulk Actions */}
      {showBulkActions && selectedItems.length > 0 && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <div className="flex items-center justify-between">
            <span className="text-sm font-medium text-blue-900">
              {selectedItems.length} {contentType}{selectedItems.length !== 1 ? 's' : ''} selected
            </span>
            <div className="flex space-x-2">
              <button
                onClick={() => handleBulkAction('hide')}
                className="px-3 py-1 bg-yellow-600 text-white rounded-md text-sm hover:bg-yellow-700 transition-colors"
              >
                Hide Selected
              </button>
              <button
                onClick={() => handleBulkAction('unhide')}
                className="px-3 py-1 bg-green-600 text-white rounded-md text-sm hover:bg-green-700 transition-colors"
              >
                Unhide Selected
              </button>
            </div>
          </div>
          
          {showBulkActionForm && bulkActionType === 'hide' && (
            <div className="mt-4 pt-4 border-t border-blue-200">
              <label className="block text-sm font-medium text-blue-900 mb-2">
                Reason for hiding selected {contentType}s:
              </label>
              <textarea
                value={bulkReason}
                onChange={(e) => setBulkReason(e.target.value)}
                rows={3}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Enter your reason..."
              />
              <div className="flex space-x-2 mt-3">
                <button
                  onClick={handleBulkSubmit}
                  disabled={!bulkReason.trim()}
                  className={`px-4 py-2 rounded-md text-sm font-medium ${
                    bulkReason.trim()
                      ? 'bg-yellow-600 text-white hover:bg-yellow-700'
                      : 'bg-gray-300 text-gray-500 cursor-not-allowed'
                  } transition-colors`}
                >
                  Hide Selected
                </button>
                <button
                  onClick={handleBulkCancel}
                  className="px-4 py-2 bg-gray-200 text-gray-700 rounded-md text-sm font-medium hover:bg-gray-300 transition-colors"
                >
                  Cancel
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Select All */}
      {showBulkActions && items.length > 0 && (
        <div className="flex items-center">
          <input
            type="checkbox"
            checked={selectedItems.length === items.length}
            onChange={handleSelectAll}
            className="rounded border-gray-300 text-blue-600 focus:ring-blue-500 mr-2"
          />
          <span className="text-sm text-gray-600">
            Select all {items.length} {contentType}{items.length !== 1 ? 's' : ''}
          </span>
        </div>
      )}

      {/* Content List */}
      <div className="space-y-4">
        {items.length === 0 ? (
          <div className="text-center py-12">
            <p className="text-gray-500">No {contentType}s found</p>
          </div>
        ) : (
          items.map((item) => (
            <AdminContentCard
              key={item.id}
              content={item}
              contentType={contentType}
              isSelected={selectedItems.includes(item.id)}
              onSelect={showBulkActions ? handleSelectItem : undefined}
              onHide={onHide}
              onUnhide={onUnhide}
              onTag={onTag}
              actionLoading={actionLoading === item.id}
              showEngagement={showEngagement}
              showPostContext={showPostContext}
            />
          ))
        )}
      </div>

      {/* Pagination */}
      {pagination && pagination.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <div className="text-sm text-gray-700">
            Page {pagination.currentPage} of {pagination.totalPages}
          </div>
          <div className="flex space-x-2">
            <button
              onClick={() => pagination.onPageChange(pagination.currentPage - 1)}
              disabled={pagination.currentPage === 1}
              className="flex items-center px-3 py-2 border border-gray-300 rounded-md text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <ChevronLeft className="h-4 w-4 mr-1" />
              Previous
            </button>
            <button
              onClick={() => pagination.onPageChange(pagination.currentPage + 1)}
              disabled={pagination.currentPage === pagination.totalPages}
              className="flex items-center px-3 py-2 border border-gray-300 rounded-md text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Next
              <ChevronRight className="h-4 w-4 ml-1" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
