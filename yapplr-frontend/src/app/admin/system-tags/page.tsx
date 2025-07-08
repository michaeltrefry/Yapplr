'use client';

import { useEffect, useState } from 'react';
import { adminApi } from '@/lib/api';
import { SystemTag, SystemTagCategory, CreateSystemTagDto, UpdateSystemTagDto } from '@/types';
import {
  Tag,
  Plus,
  Edit,
  Trash2,
  Eye,
  EyeOff,
  Save,
  X,
} from 'lucide-react';

export default function SystemTagsPage() {
  const [tags, setTags] = useState<SystemTag[]>([]);
  const [loading, setLoading] = useState(true);
  const [editingTag, setEditingTag] = useState<SystemTag | null>(null);
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [createForm, setCreateForm] = useState<CreateSystemTagDto>({
    name: '',
    description: '',
    category: SystemTagCategory.ContentWarning,
    isVisibleToUsers: false,
    color: '#6B7280',
    sortOrder: 0,
  });

  useEffect(() => {
    fetchTags();
  }, []);

  const fetchTags = async () => {
    try {
      setLoading(true);
      const tagsData = await adminApi.getSystemTags();
      setTags(tagsData);
    } catch (error) {
      console.error('Failed to fetch system tags:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleCreateTag = async () => {
    try {
      await adminApi.createSystemTag(createForm);
      setShowCreateForm(false);
      setCreateForm({
        name: '',
        description: '',
        category: SystemTagCategory.ContentWarning,
        isVisibleToUsers: false,
        color: '#6B7280',
        sortOrder: 0,
      });
      fetchTags();
    } catch (error) {
      console.error('Failed to create system tag:', error);
      alert('Failed to create system tag');
    }
  };

  const handleUpdateTag = async (tag: SystemTag) => {
    try {
      const updateData: UpdateSystemTagDto = {
        name: tag.name,
        description: tag.description,
        category: tag.category,
        isVisibleToUsers: tag.isVisibleToUsers,
        isActive: tag.isActive,
        color: tag.color,
        icon: tag.icon,
        sortOrder: tag.sortOrder,
      };
      await adminApi.updateSystemTag(tag.id, updateData);
      setEditingTag(null);
      fetchTags();
    } catch (error) {
      console.error('Failed to update system tag:', error);
      alert('Failed to update system tag');
    }
  };

  const handleDeleteTag = async (tagId: number) => {
    if (!confirm('Are you sure you want to delete this system tag?')) return;

    try {
      await adminApi.deleteSystemTag(tagId);
      fetchTags();
    } catch (error) {
      console.error('Failed to delete system tag:', error);
      alert('Failed to delete system tag');
    }
  };

  const getCategoryName = (category: SystemTagCategory) => {
    switch (category) {
      case SystemTagCategory.ContentWarning:
        return 'Content Warning';
      case SystemTagCategory.Violation:
        return 'Violation';
      case SystemTagCategory.ModerationStatus:
        return 'Moderation Status';
      case SystemTagCategory.Quality:
        return 'Quality';
      case SystemTagCategory.Legal:
        return 'Legal';
      case SystemTagCategory.Safety:
        return 'Safety';
      default:
        return 'Unknown';
    }
  };

  const groupedTags = tags.reduce((acc, tag) => {
    const category = getCategoryName(tag.category);
    if (!acc[category]) {
      acc[category] = [];
    }
    acc[category].push(tag);
    return acc;
  }, {} as Record<string, SystemTag[]>);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">System Tags</h1>
          <p className="text-gray-600">Manage system tags for content moderation</p>
        </div>
        <button
          onClick={() => setShowCreateForm(true)}
          className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 transition-colors flex items-center"
        >
          <Plus className="h-4 w-4 mr-2" />
          Create Tag
        </button>
      </div>

      {/* Create Form */}
      {showCreateForm && (
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Create System Tag</h2>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Name</label>
              <input
                type="text"
                value={createForm.name}
                onChange={(e) => setCreateForm({ ...createForm, name: e.target.value })}
                className="w-full border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Category</label>
              <select
                value={createForm.category}
                onChange={(e) => setCreateForm({ ...createForm, category: parseInt(e.target.value) as SystemTagCategory })}
                className="w-full border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value={SystemTagCategory.ContentWarning}>Content Warning</option>
                <option value={SystemTagCategory.Violation}>Violation</option>
                <option value={SystemTagCategory.ModerationStatus}>Moderation Status</option>
                <option value={SystemTagCategory.Quality}>Quality</option>
                <option value={SystemTagCategory.Legal}>Legal</option>
                <option value={SystemTagCategory.Safety}>Safety</option>
              </select>
            </div>
            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-gray-700 mb-2">Description</label>
              <textarea
                value={createForm.description}
                onChange={(e) => setCreateForm({ ...createForm, description: e.target.value })}
                rows={3}
                className="w-full border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Color</label>
              <input
                type="color"
                value={createForm.color}
                onChange={(e) => setCreateForm({ ...createForm, color: e.target.value })}
                className="w-full h-10 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">Sort Order</label>
              <input
                type="number"
                value={createForm.sortOrder}
                onChange={(e) => setCreateForm({ ...createForm, sortOrder: parseInt(e.target.value) })}
                className="w-full border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div className="md:col-span-2">
              <label className="flex items-center">
                <input
                  type="checkbox"
                  checked={createForm.isVisibleToUsers}
                  onChange={(e) => setCreateForm({ ...createForm, isVisibleToUsers: e.target.checked })}
                  className="mr-2"
                />
                <span className="text-sm text-gray-700">Visible to users</span>
              </label>
            </div>
          </div>
          <div className="flex justify-end space-x-3 mt-6">
            <button
              onClick={() => setShowCreateForm(false)}
              className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
            <button
              onClick={handleCreateTag}
              className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
            >
              Create Tag
            </button>
          </div>
        </div>
      )}

      {/* Tags by Category */}
      {loading ? (
        <div className="flex items-center justify-center h-64">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
        </div>
      ) : (
        <div className="space-y-6">
          {Object.entries(groupedTags).map(([category, categoryTags]) => (
            <div key={category} className="bg-white rounded-lg shadow">
              <div className="px-6 py-4 border-b border-gray-200">
                <h2 className="text-lg font-semibold text-gray-900">{category}</h2>
              </div>
              <div className="p-6">
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                  {categoryTags
                    .sort((a, b) => a.sortOrder - b.sortOrder)
                    .map((tag) => (
                      <div
                        key={tag.id}
                        className="border border-gray-200 rounded-lg p-4 hover:shadow-md transition-shadow"
                      >
                        {editingTag?.id === tag.id ? (
                          <div className="space-y-3">
                            <input
                              type="text"
                              value={editingTag.name}
                              onChange={(e) => setEditingTag({ ...editingTag, name: e.target.value })}
                              className="w-full border border-gray-300 rounded-md px-2 py-1 text-sm"
                            />
                            <textarea
                              value={editingTag.description}
                              onChange={(e) => setEditingTag({ ...editingTag, description: e.target.value })}
                              rows={2}
                              className="w-full border border-gray-300 rounded-md px-2 py-1 text-sm"
                            />
                            <div className="flex items-center space-x-2">
                              <input
                                type="color"
                                value={editingTag.color}
                                onChange={(e) => setEditingTag({ ...editingTag, color: e.target.value })}
                                className="w-8 h-8 border border-gray-300 rounded"
                              />
                              <label className="flex items-center text-sm">
                                <input
                                  type="checkbox"
                                  checked={editingTag.isVisibleToUsers}
                                  onChange={(e) => setEditingTag({ ...editingTag, isVisibleToUsers: e.target.checked })}
                                  className="mr-1"
                                />
                                Visible
                              </label>
                              <label className="flex items-center text-sm">
                                <input
                                  type="checkbox"
                                  checked={editingTag.isActive}
                                  onChange={(e) => setEditingTag({ ...editingTag, isActive: e.target.checked })}
                                  className="mr-1"
                                />
                                Active
                              </label>
                            </div>
                            <div className="flex justify-end space-x-2">
                              <button
                                onClick={() => setEditingTag(null)}
                                className="inline-flex items-center px-2 py-1 bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 transition-colors text-xs"
                              >
                                <X className="h-3 w-3 mr-1" />
                                Cancel
                              </button>
                              <button
                                onClick={() => handleUpdateTag(editingTag)}
                                className="inline-flex items-center px-2 py-1 bg-green-100 text-green-800 rounded-md hover:bg-green-200 transition-colors text-xs"
                              >
                                <Save className="h-3 w-3 mr-1" />
                                Save
                              </button>
                            </div>
                          </div>
                        ) : (
                          <>
                            <div className="flex items-center justify-between mb-2">
                              <div className="flex items-center">
                                <div
                                  className="w-4 h-4 rounded-full mr-2"
                                  style={{ backgroundColor: tag.color }}
                                />
                                <span className="font-medium text-gray-900">{tag.name}</span>
                              </div>
                              <div className="flex items-center space-x-1">
                                {tag.isVisibleToUsers ? (
                                  <Eye className="h-4 w-4 text-green-500" />
                                ) : (
                                  <EyeOff className="h-4 w-4 text-gray-400" />
                                )}
                                {!tag.isActive && (
                                  <span className="text-xs bg-red-100 text-red-800 px-1 rounded">Inactive</span>
                                )}
                              </div>
                            </div>
                            <p className="text-sm text-gray-600 mb-3">{tag.description}</p>
                            <div className="flex justify-end space-x-2">
                              <button
                                onClick={() => setEditingTag(tag)}
                                className="inline-flex items-center px-2 py-1 bg-blue-100 text-blue-800 rounded-md hover:bg-blue-200 transition-colors text-xs"
                              >
                                <Edit className="h-3 w-3 mr-1" />
                                Edit
                              </button>
                              <button
                                onClick={() => handleDeleteTag(tag.id)}
                                className="inline-flex items-center px-2 py-1 bg-red-100 text-red-800 rounded-md hover:bg-red-200 transition-colors text-xs"
                              >
                                <Trash2 className="h-3 w-3 mr-1" />
                                Delete
                              </button>
                            </div>
                          </>
                        )}
                      </div>
                    ))}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {tags.length === 0 && !loading && (
        <div className="text-center py-12">
          <Tag className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <p className="text-gray-500">No system tags found</p>
        </div>
      )}
    </div>
  );
}
