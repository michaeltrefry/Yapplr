'use client';

import { useEffect, useState } from 'react';
import { adminApi } from '@/lib/api';
import { AdminComment } from '@/types';
import { AdminContentList } from '@/components/admin';
import { Filter } from 'lucide-react';

export default function AdminCommentsPage() {
  const [comments, setComments] = useState<AdminComment[]>([]);
  const [loading, setLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize] = useState(25);
  const [totalPages, setTotalPages] = useState(1);
  const [isHiddenFilter, setIsHiddenFilter] = useState<boolean | undefined>(undefined);
  const [searchTerm, setSearchTerm] = useState('');
  const [actionLoading, setActionLoading] = useState<number | null>(null);

  useEffect(() => {
    fetchComments();
  }, [currentPage, isHiddenFilter]);

  const fetchComments = async () => {
    try {
      setLoading(true);
      const data = await adminApi.getComments(currentPage, pageSize, isHiddenFilter);
      setComments(data);
      // Note: You may need to update the API to return total count for pagination
      setTotalPages(Math.ceil(data.length / pageSize) || 1);
    } catch (error) {
      console.error('Failed to fetch comments:', error);
    } finally {
      setLoading(false);
    }
  };

  const refreshComments = async () => {
    try {
      const data = await adminApi.getComments(currentPage, pageSize, isHiddenFilter);
      setComments(data);
      setTotalPages(Math.ceil(data.length / pageSize) || 1);
    } catch (error) {
      console.error('Failed to refresh comments:', error);
    }
  };

  const handleHideComment = async (commentId: number, reason: string) => {
    try {
      setActionLoading(commentId);
      await adminApi.hideComment(commentId, { reason });
      await refreshComments();
    } catch (error) {
      console.error('Failed to hide comment:', error);
    } finally {
      setActionLoading(null);
    }
  };

  const handleUnhideComment = async (commentId: number) => {
    try {
      setActionLoading(commentId);
      await adminApi.unhideComment(commentId);
      await refreshComments();
    } catch (error) {
      console.error('Failed to unhide comment:', error);
    } finally {
      setActionLoading(null);
    }
  };

  const handleTagComment = async (commentId: number, tagIds: number[]) => {
    try {
      setActionLoading(commentId);
      // Apply each selected tag to the comment
      for (const tagId of tagIds) {
        await adminApi.applySystemTagToComment(commentId, {
          systemTagId: tagId,
          reason: 'Applied via admin interface'
        });
      }
      await refreshComments();
    } catch (error) {
      console.error('Failed to tag comment:', error);
    } finally {
      setActionLoading(null);
    }
  };

  const handleBulkHide = async (commentIds: number[], reason: string) => {
    try {
      // Note: Bulk hide for comments not yet implemented in API
      // For now, hide comments individually
      await Promise.all(commentIds.map(id => adminApi.hideComment(id, { reason })));
      await refreshComments();
    } catch (error) {
      console.error('Failed to bulk hide comments:', error);
    }
  };

  const handleBulkUnhide = async (commentIds: number[]) => {
    try {
      // Note: Bulk unhide for comments not yet implemented in API
      // For now, unhide comments individually
      await Promise.all(commentIds.map(id => adminApi.unhideComment(id)));
      await refreshComments();
    } catch (error) {
      console.error('Failed to bulk unhide comments:', error);
    }
  };

  const filteredComments = comments.filter(comment =>
    searchTerm === '' ||
    comment.content.toLowerCase().includes(searchTerm.toLowerCase()) ||
    comment.user.username.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const handleSearchChange = (term: string) => {
    setSearchTerm(term);
  };

  const handlePageChange = (page: number) => {
    setCurrentPage(page);
  };

  const filters = (
    <select
      value={isHiddenFilter === undefined ? 'all' : isHiddenFilter.toString()}
      onChange={(e) => {
        const value = e.target.value;
        setIsHiddenFilter(value === 'all' ? undefined : value === 'true');
        setCurrentPage(1);
      }}
      className="px-3 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500"
    >
      <option value="all">All Comments</option>
      <option value="false">Visible Comments</option>
      <option value="true">Hidden Comments</option>
    </select>
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Comment Management</h1>
        <p className="text-gray-600">Moderate and manage user comments</p>
      </div>

      <AdminContentList
        items={filteredComments}
        contentType="comment"
        loading={loading}
        searchTerm={searchTerm}
        onSearchChange={handleSearchChange}
        onHide={handleHideComment}
        onUnhide={handleUnhideComment}
        onTag={handleTagComment}
        onBulkHide={handleBulkHide}
        onBulkUnhide={handleBulkUnhide}
        actionLoading={actionLoading}
        showEngagement={false}
        showPostContext={true}
        showBulkActions={true}
        showSearch={true}
        showFilters={true}
        filters={filters}
        pagination={{
          currentPage,
          totalPages,
          onPageChange: handlePageChange,
        }}
      />
    </div>
  );
}
