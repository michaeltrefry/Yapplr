'use client';

import { useEffect, useState } from 'react';
import { adminApi } from '@/lib/api';
import { AdminPost, SystemTag, HideContentDto } from '@/types';
import {
  Eye,
  EyeOff,
  Trash2,
  Tag,
  Calendar,
  User,
  MessageSquare,
  Heart,
  Repeat,
  Search,
  Filter,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';
import { format } from 'date-fns';

export default function AdminPostsPage() {
  const [posts, setPosts] = useState<AdminPost[]>([]);
  const [loading, setLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize] = useState(25);
  const [isHiddenFilter, setIsHiddenFilter] = useState<boolean | undefined>(undefined);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedPosts, setSelectedPosts] = useState<number[]>([]);
  const [showBulkActions, setShowBulkActions] = useState(false);
  const [actionLoading, setActionLoading] = useState<number | null>(null);

  useEffect(() => {
    fetchPosts();
  }, [currentPage, isHiddenFilter]);

  const fetchPosts = async () => {
    try {
      setLoading(true);
      const data = await adminApi.getPosts(currentPage, pageSize, isHiddenFilter);
      setPosts(data);
    } catch (error) {
      console.error('Failed to fetch posts:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleHidePost = async (postId: number, reason: string) => {
    try {
      setActionLoading(postId);
      await adminApi.hidePost(postId, { reason });
      await fetchPosts();
    } catch (error) {
      console.error('Failed to hide post:', error);
    } finally {
      setActionLoading(null);
    }
  };

  const handleUnhidePost = async (postId: number) => {
    try {
      setActionLoading(postId);
      await adminApi.unhidePost(postId);
      await fetchPosts();
    } catch (error) {
      console.error('Failed to unhide post:', error);
    } finally {
      setActionLoading(null);
    }
  };

  const handleDeletePost = async (postId: number, reason: string) => {
    try {
      setActionLoading(postId);
      await adminApi.deletePost(postId, { reason });
      await fetchPosts();
    } catch (error) {
      console.error('Failed to delete post:', error);
    } finally {
      setActionLoading(null);
    }
  };

  const handleBulkHide = async () => {
    const reason = prompt('Enter reason for hiding posts:');
    if (!reason) return;

    try {
      await adminApi.bulkHidePosts(selectedPosts, reason);
      setSelectedPosts([]);
      setShowBulkActions(false);
      await fetchPosts();
    } catch (error) {
      console.error('Failed to bulk hide posts:', error);
    }
  };

  const handleBulkDelete = async () => {
    const reason = prompt('Enter reason for deleting posts:');
    if (!reason) return;

    if (!confirm(`Are you sure you want to delete ${selectedPosts.length} posts? This action cannot be undone.`)) {
      return;
    }

    try {
      await adminApi.bulkDeletePosts(selectedPosts, reason);
      setSelectedPosts([]);
      setShowBulkActions(false);
      await fetchPosts();
    } catch (error) {
      console.error('Failed to bulk delete posts:', error);
    }
  };

  const togglePostSelection = (postId: number) => {
    setSelectedPosts(prev => 
      prev.includes(postId) 
        ? prev.filter(id => id !== postId)
        : [...prev, postId]
    );
  };

  const filteredPosts = posts.filter(post =>
    searchTerm === '' ||
    post.content.toLowerCase().includes(searchTerm.toLowerCase()) ||
    post.user.username.toLowerCase().includes(searchTerm.toLowerCase())
  );

  useEffect(() => {
    setShowBulkActions(selectedPosts.length > 0);
  }, [selectedPosts]);

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
        <h1 className="text-2xl font-bold text-gray-900">Post Management</h1>
        <p className="text-gray-600">Moderate and manage user posts</p>
      </div>

      {/* Filters and Search */}
      <div className="bg-white rounded-lg shadow p-6">
        <div className="flex flex-col sm:flex-row gap-4">
          <div className="flex-1">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
              <input
                type="text"
                placeholder="Search posts or users..."
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
              <option value="all">All Posts</option>
              <option value="false">Visible Posts</option>
              <option value="true">Hidden Posts</option>
            </select>
          </div>
        </div>
      </div>

      {/* Bulk Actions */}
      {showBulkActions && (
        <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
          <div className="flex items-center justify-between">
            <span className="text-blue-800 font-medium">
              {selectedPosts.length} post{selectedPosts.length !== 1 ? 's' : ''} selected
            </span>
            <div className="flex gap-2">
              <button
                onClick={handleBulkHide}
                className="px-3 py-1 bg-yellow-600 text-white rounded hover:bg-yellow-700 text-sm"
              >
                Bulk Hide
              </button>
              <button
                onClick={handleBulkDelete}
                className="px-3 py-1 bg-red-600 text-white rounded hover:bg-red-700 text-sm"
              >
                Bulk Delete
              </button>
              <button
                onClick={() => {
                  setSelectedPosts([]);
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

      {/* Posts List */}
      <div className="bg-white rounded-lg shadow overflow-hidden">
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  <input
                    type="checkbox"
                    checked={selectedPosts.length === filteredPosts.length && filteredPosts.length > 0}
                    onChange={(e) => {
                      if (e.target.checked) {
                        setSelectedPosts(filteredPosts.map(p => p.id));
                      } else {
                        setSelectedPosts([]);
                      }
                    }}
                    className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                  />
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Post
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Author
                </th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Stats
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
              {filteredPosts.map((post) => (
                <tr key={post.id} className={post.isHidden ? 'bg-red-50' : ''}>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <input
                      type="checkbox"
                      checked={selectedPosts.includes(post.id)}
                      onChange={() => togglePostSelection(post.id)}
                      className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                    />
                  </td>
                  <td className="px-6 py-4">
                    <div className="max-w-xs">
                      <p className="text-sm text-gray-900 truncate">{post.content}</p>
                      {post.imageFileName && (
                        <span className="text-xs text-gray-500">📷 Has image</span>
                      )}
                      <div className="flex items-center gap-2 mt-1">
                        {post.systemTags.map((tag) => (
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
                      <span className="text-sm text-gray-900">@{post.user.username}</span>
                    </div>
                    <div className="text-xs text-gray-500">
                      {format(new Date(post.createdAt), 'MMM d, yyyy HH:mm')}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    <div className="flex flex-col gap-1">
                      <div className="flex items-center">
                        <Heart className="h-4 w-4 mr-1" />
                        {post.likeCount}
                      </div>
                      <div className="flex items-center">
                        <MessageSquare className="h-4 w-4 mr-1" />
                        {post.commentCount}
                      </div>
                      <div className="flex items-center">
                        <Repeat className="h-4 w-4 mr-1" />
                        {post.repostCount}
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    {post.isHidden ? (
                      <div>
                        <span className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-red-100 text-red-800">
                          Hidden
                        </span>
                        {post.hiddenReason && (
                          <p className="text-xs text-gray-500 mt-1">{post.hiddenReason}</p>
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
                      {post.isHidden ? (
                        <button
                          onClick={() => handleUnhidePost(post.id)}
                          disabled={actionLoading === post.id}
                          className="text-green-600 hover:text-green-900 disabled:opacity-50"
                        >
                          <Eye className="h-4 w-4" />
                        </button>
                      ) : (
                        <button
                          onClick={() => {
                            const reason = prompt('Enter reason for hiding this post:');
                            if (reason) handleHidePost(post.id, reason);
                          }}
                          disabled={actionLoading === post.id}
                          className="text-yellow-600 hover:text-yellow-900 disabled:opacity-50"
                        >
                          <EyeOff className="h-4 w-4" />
                        </button>
                      )}
                      <button
                        onClick={() => {
                          const reason = prompt('Enter reason for deleting this post:');
                          if (reason && confirm('Are you sure? This action cannot be undone.')) {
                            handleDeletePost(post.id, reason);
                          }
                        }}
                        disabled={actionLoading === post.id}
                        className="text-red-600 hover:text-red-900 disabled:opacity-50"
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {filteredPosts.length === 0 && (
          <div className="text-center py-12">
            <p className="text-gray-500">No posts found</p>
          </div>
        )}
      </div>

      {/* Pagination */}
      <div className="flex items-center justify-between">
        <div className="text-sm text-gray-700">
          Showing page {currentPage} of posts
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
            disabled={filteredPosts.length < pageSize}
            className="px-3 py-2 border border-gray-300 rounded-md disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
          >
            <ChevronRight className="h-4 w-4" />
          </button>
        </div>
      </div>
    </div>
  );
}
