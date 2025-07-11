'use client';

import { useEffect, useState } from 'react';
import { adminApi } from '@/lib/api';
import { AdminPost } from '@/types';
import { AdminContentList } from '@/components/admin';
import { Filter } from 'lucide-react';

export default function AdminPostsPage() {
  const [posts, setPosts] = useState<AdminPost[]>([]);
  const [loading, setLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize] = useState(25);
  const [totalPages, setTotalPages] = useState(1);
  const [isHiddenFilter, setIsHiddenFilter] = useState<boolean | undefined>(undefined);
  const [searchTerm, setSearchTerm] = useState('');
  const [actionLoading, setActionLoading] = useState<number | null>(null);

  useEffect(() => {
    fetchPosts();
  }, [currentPage, isHiddenFilter]);

  const fetchPosts = async () => {
    try {
      setLoading(true);
      const data = await adminApi.getPosts(currentPage, pageSize, isHiddenFilter);
      setPosts(data);
      // Note: You may need to update the API to return total count for pagination
      setTotalPages(Math.ceil(data.length / pageSize) || 1);
    } catch (error) {
      console.error('Failed to fetch posts:', error);
    } finally {
      setLoading(false);
    }
  };

  const refreshPosts = async () => {
    try {
      const data = await adminApi.getPosts(currentPage, pageSize, isHiddenFilter);
      setPosts(data);
      setTotalPages(Math.ceil(data.length / pageSize) || 1);
    } catch (error) {
      console.error('Failed to refresh posts:', error);
    }
  };

  const handleHidePost = async (postId: number, reason: string) => {
    try {
      setActionLoading(postId);
      await adminApi.hidePost(postId, { reason });
      await refreshPosts();
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
      await refreshPosts();
    } catch (error) {
      console.error('Failed to unhide post:', error);
    } finally {
      setActionLoading(null);
    }
  };

  const handleTagPost = async (postId: number, tagIds: number[]) => {
    try {
      setActionLoading(postId);
      // Apply each selected tag to the post
      for (const tagId of tagIds) {
        await adminApi.applySystemTagToPost(postId, {
          systemTagId: tagId,
          reason: 'Applied via admin interface'
        });
      }
      await refreshPosts();
    } catch (error) {
      console.error('Failed to tag post:', error);
    } finally {
      setActionLoading(null);
    }
  };

  const handleBulkHide = async (postIds: number[], reason: string) => {
    try {
      await adminApi.bulkHidePosts(postIds, reason);
      await refreshPosts();
    } catch (error) {
      console.error('Failed to bulk hide posts:', error);
    }
  };

  const handleBulkUnhide = async (postIds: number[]) => {
    try {
      // Note: Bulk unhide for posts not yet implemented in API
      // For now, unhide posts individually
      await Promise.all(postIds.map(id => adminApi.unhidePost(id)));
      await refreshPosts();
    } catch (error) {
      console.error('Failed to bulk unhide posts:', error);
    }
  };

  const filteredPosts = posts.filter(post =>
    searchTerm === '' ||
    post.content.toLowerCase().includes(searchTerm.toLowerCase()) ||
    post.user.username.toLowerCase().includes(searchTerm.toLowerCase())
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
      <option value="all">All Posts</option>
      <option value="false">Visible Posts</option>
      <option value="true">Hidden Posts</option>
    </select>
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Post Management</h1>
        <p className="text-gray-600">Moderate and manage user posts</p>
      </div>

      <AdminContentList
        items={filteredPosts}
        contentType="post"
        loading={loading}
        searchTerm={searchTerm}
        onSearchChange={handleSearchChange}
        onHide={handleHidePost}
        onUnhide={handleUnhidePost}
        onTag={handleTagPost}
        onBulkHide={handleBulkHide}
        onBulkUnhide={handleBulkUnhide}
        actionLoading={actionLoading}
        showEngagement={true}
        showPostContext={false}
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
