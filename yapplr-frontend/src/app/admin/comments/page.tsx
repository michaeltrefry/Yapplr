'use client';

import { useEffect, useState } from 'react';
import { adminApi } from '@/lib/api';
import { AdminComment } from '@/types';
import {
  Eye,
  EyeOff,
  Tag,
  User,
  Search,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';
import { format } from 'date-fns';

export default function AdminCommentsPage() {
  const [comments, setComments] = useState<AdminComment[]>([]);
  const [loading, setLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize] = useState(25);
  const [isHiddenFilter, setIsHiddenFilter] = useState<boolean | undefined>(undefined);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedComments, setSelectedComments] = useState<number[]>([]);
  const [showBulkActions, setShowBulkActions] = useState(false);
  const [actionLoading, setActionLoading] = useState<number | null>(null);

  useEffect(() => {
    fetchComments();
  }, [currentPage, isHiddenFilter]);

  const fetchComments = async () => {
    try {
      setLoading(true);
      const data = await adminApi.getComments(currentPage, pageSize, isHiddenFilter);
      setComments(data);
    } catch (error) {
      console.error('Failed to fetch comments:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleHideComment = async (commentId: number, reason: string) => {
    try {
      setActionLoading(commentId);
      await adminApi.hideComment(commentId, { reason });
      await fetchComments();
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
      await fetchComments();
    } catch (error) {
      console.error('Failed to unhide comment:', error);
    } finally {
      setActionLoading(null);
    }
  };



  const toggleCommentSelection = (commentId: number) => {
    setSelectedComments(prev => 
      prev.includes(commentId) 
        ? prev.filter(id => id !== commentId)
        : [...prev, commentId]
    );
  };

  const filteredComments = comments.filter(comment =>
    searchTerm === '' ||
    comment.content.toLowerCase().includes(searchTerm.toLowerCase()) ||
    comment.user.username.toLowerCase().includes(searchTerm.toLowerCase())
  );

  useEffect(() => {
    setShowBulkActions(selectedComments.length > 0);
  }, [selectedComments]);

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Comment Management</h1>
        <p className="text-gray-600">Moderate and manage user comments</p>
      </div>

      {/* Filters and Search */}
      <div className="bg-white rounded-lg shadow p-6">
        <div className="flex flex-col sm:flex-row gap-4">
          <div className="flex-1">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
              <input
                type="text"
                placeholder="Search comments or users..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10 pr-4 py-2 border border-gray-300 rounded-md focus:ring-blue-500 focus:border-blue-500 w-full"
              />
            </div>
          </div>
          <div className="flex gap-2">
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
          </div>
        </div>
      </div>

      {/* Bulk Actions */}
      {showBulkActions && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <div className="flex items-center justify-between">
            <span className="text-blue-800 font-medium">
              {selectedComments.length} comment{selectedComments.length !== 1 ? 's' : ''} selected
            </span>
            <div className="flex gap-2">
              <button
                onClick={() => {
                  setSelectedComments([]);
                  setShowBulkActions(false);
                }}
                className="px-3 py-1 bg-gray-600 text-white rounded hover:bg-gray-700 text-sm"
              >
                Clear
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Comments List */}
      <div className="bg-white rounded-lg shadow overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  <input
                    type="checkbox"
                    checked={selectedComments.length === filteredComments.length && filteredComments.length > 0}
                    onChange={(e) => {
                      if (e.target.checked) {
                        setSelectedComments(filteredComments.map(c => c.id));
                      } else {
                        setSelectedComments([]);
                      }
                    }}
                    className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                  />
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Comment
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Author
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Post
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Status
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {filteredComments.map((comment) => (
                <tr key={comment.id} className={comment.isHidden ? 'bg-red-50' : ''}>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <input
                      type="checkbox"
                      checked={selectedComments.includes(comment.id)}
                      onChange={() => toggleCommentSelection(comment.id)}
                      className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                    />
                  </td>
                  <td className="px-6 py-4">
                    <div className="max-w-xs">
                      <p className="text-sm text-gray-900">{comment.content}</p>
                      <div className="flex items-center gap-2 mt-1">
                        {comment.systemTags?.map((tag) => (
                          <span
                            key={tag.id}
                            className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-purple-100 text-purple-800"
                          >
                            <Tag className="h-3 w-3 mr-1" />
                            {tag.name}
                          </span>
                        ))}
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center">
                      <User className="h-4 w-4 text-gray-400 mr-2" />
                      <span className="text-sm text-gray-900">@{comment.user.username}</span>
                    </div>
                    <div className="text-xs text-gray-500">
                      {format(new Date(comment.createdAt), 'MMM d, yyyy HH:mm')}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-900">
                      Post #{comment.postId}
                    </div>
                    <div className="text-xs text-gray-500">
                      Post details
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    {comment.isHidden ? (
                      <div>
                        <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-red-100 text-red-800">
                          Hidden
                        </span>
                        {comment.hiddenReason && (
                          <p className="text-xs text-gray-500 mt-1">{comment.hiddenReason}</p>
                        )}
                      </div>
                    ) : (
                      <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800">
                        Visible
                      </span>
                    )}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                    <div className="flex gap-2">
                      {comment.isHidden ? (
                        <button
                          onClick={() => handleUnhideComment(comment.id)}
                          disabled={actionLoading === comment.id}
                          className="text-green-600 hover:text-green-900 disabled:opacity-50"
                        >
                          <Eye className="h-4 w-4" />
                        </button>
                      ) : (
                        <button
                          onClick={() => {
                            const reason = prompt('Enter reason for hiding this comment:');
                            if (reason) handleHideComment(comment.id, reason);
                          }}
                          disabled={actionLoading === comment.id}
                          className="text-yellow-600 hover:text-yellow-900 disabled:opacity-50"
                        >
                          <EyeOff className="h-4 w-4" />
                        </button>
                      )}

                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {filteredComments.length === 0 && (
          <div className="text-center py-12">
            <p className="text-gray-500">No comments found</p>
          </div>
        )}
      </div>

      {/* Pagination */}
      <div className="flex items-center justify-between">
        <div className="text-sm text-gray-700">
          Showing page {currentPage} of comments
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
            disabled={currentPage === 1}
            className="px-3 py-2 border border-gray-300 rounded-md disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
          >
            <ChevronLeft className="h-4 w-4" />
          </button>
          <button
            onClick={() => setCurrentPage(prev => prev + 1)}
            disabled={filteredComments.length < pageSize}
            className="px-3 py-2 border border-gray-300 rounded-md disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
          >
            <ChevronRight className="h-4 w-4" />
          </button>
        </div>
      </div>
    </div>
  );
}
