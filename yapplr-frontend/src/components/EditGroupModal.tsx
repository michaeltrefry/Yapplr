'use client';

import { useState, useRef, useEffect } from 'react';
import { Group, UpdateGroup } from '@/types';
import { groupApi } from '@/lib/api';
import { X, Upload, Image as ImageIcon, Trash2 } from 'lucide-react';

interface EditGroupModalProps {
  isOpen: boolean;
  onClose: () => void;
  group: Group;
  onGroupUpdated?: (group: Group) => void;
}

export default function EditGroupModal({ isOpen, onClose, group, onGroupUpdated }: EditGroupModalProps) {
  const [formData, setFormData] = useState<UpdateGroup>({
    name: group.name,
    description: group.description,
    imageFileName: group.imageFileName,
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const [isUploadingImage, setIsUploadingImage] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Reset form when group changes
  useEffect(() => {
    setFormData({
      name: group.name,
      description: group.description,
      imageFileName: group.imageFileName,
    });
    setImageFile(null);
    setImagePreview(null);
    setError(null);
    setShowDeleteConfirm(false);
  }, [group]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleImageSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setImageFile(file);
      const reader = new FileReader();
      reader.onload = (e) => {
        setImagePreview(e.target?.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  const uploadImage = async (): Promise<string | undefined> => {
    if (!imageFile) return undefined;

    setIsUploadingImage(true);
    try {
      const result = await groupApi.uploadGroupImage(imageFile);
      return result.fileName;
    } catch (error) {
      console.error('Failed to upload image:', error);
      throw new Error('Failed to upload image');
    } finally {
      setIsUploadingImage(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);

    try {
      let imageFileName = formData.imageFileName;
      
      if (imageFile) {
        imageFileName = await uploadImage();
      }

      const updateData: UpdateGroup = {
        ...formData,
        imageFileName,
      };

      const updatedGroup = await groupApi.updateGroup(group.id, updateData);
      onGroupUpdated?.(updatedGroup);
      handleClose();
    } catch (err: any) {
      console.error('Failed to update group:', err);
      setError(err.response?.data?.error || 'Failed to update group. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async () => {
    setIsDeleting(true);
    setError(null);

    try {
      await groupApi.deleteGroup(group.id);
      handleClose();
      // Navigate away or refresh the page since group is deleted
      window.location.href = '/groups';
    } catch (err: any) {
      console.error('Failed to delete group:', err);
      setError(err.response?.data?.error || 'Failed to delete group. Please try again.');
    } finally {
      setIsDeleting(false);
    }
  };

  const handleClose = () => {
    setFormData({
      name: group.name,
      description: group.description,
      imageFileName: group.imageFileName,
    });
    setImageFile(null);
    setImagePreview(null);
    setError(null);
    setShowDeleteConfirm(false);
    onClose();
  };

  const removeImage = () => {
    setImageFile(null);
    setImagePreview(null);
    setFormData(prev => ({ ...prev, imageFileName: undefined }));
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const getImageUrl = (imageFileName?: string) => {
    if (!imageFileName) return '';
    const baseUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5161';
    return `${baseUrl}/api/images/${imageFileName}`;
  };

  const currentImageUrl = imagePreview || getImageUrl(formData.imageFileName);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl w-full max-w-md max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b border-gray-200 dark:border-gray-700">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
            Edit Group
          </h2>
          <button
            onClick={handleClose}
            className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors"
          >
            <X size={24} />
          </button>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          {error && (
            <div className="p-3 bg-red-100 border border-red-400 text-red-700 rounded-md text-sm">
              {error}
            </div>
          )}

          {/* Group Image */}
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Group Image
            </label>
            <div className="flex items-center space-x-4">
              {currentImageUrl ? (
                <div className="relative">
                  <img
                    src={currentImageUrl}
                    alt="Group preview"
                    className="w-16 h-16 rounded-lg object-cover"
                  />
                  <button
                    type="button"
                    onClick={removeImage}
                    className="absolute -top-2 -right-2 bg-red-500 text-white rounded-full w-6 h-6 flex items-center justify-center text-xs hover:bg-red-600 transition-colors"
                  >
                    <X size={12} />
                  </button>
                </div>
              ) : (
                <div className="w-16 h-16 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg flex items-center justify-center">
                  <ImageIcon size={24} className="text-gray-400" />
                </div>
              )}
              
              <button
                type="button"
                onClick={() => fileInputRef.current?.click()}
                disabled={isUploadingImage}
                className="px-4 py-2 bg-gray-100 hover:bg-gray-200 dark:bg-gray-700 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-300 rounded-md transition-colors flex items-center space-x-2 disabled:opacity-50"
              >
                <Upload size={16} />
                <span>{isUploadingImage ? 'Uploading...' : 'Change Image'}</span>
              </button>
              
              <input
                ref={fileInputRef}
                type="file"
                accept="image/*"
                onChange={handleImageSelect}
                className="hidden"
              />
            </div>
          </div>

          {/* Group Name */}
          <div>
            <label htmlFor="name" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Group Name *
            </label>
            <input
              type="text"
              id="name"
              name="name"
              value={formData.name}
              onChange={handleInputChange}
              required
              minLength={3}
              maxLength={100}
              className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:text-white"
            />
          </div>

          {/* Group Description */}
          <div>
            <label htmlFor="description" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Description
            </label>
            <textarea
              id="description"
              name="description"
              value={formData.description}
              onChange={handleInputChange}
              maxLength={500}
              rows={3}
              className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-700 dark:text-white resize-none"
            />
            <div className="text-xs text-gray-500 dark:text-gray-400 mt-1">
              {(formData.description || '').length}/500 characters
            </div>
          </div>

          {/* Action Buttons */}
          <div className="flex justify-between pt-4">
            {/* Delete Button */}
            <div>
              {!showDeleteConfirm ? (
                <button
                  type="button"
                  onClick={() => setShowDeleteConfirm(true)}
                  className="px-4 py-2 text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-md transition-colors flex items-center space-x-2"
                >
                  <Trash2 size={16} />
                  <span>Delete Group</span>
                </button>
              ) : (
                <div className="flex items-center space-x-2">
                  <button
                    type="button"
                    onClick={handleDelete}
                    disabled={isDeleting}
                    className="px-3 py-1 bg-red-600 text-white hover:bg-red-700 rounded text-sm disabled:opacity-50"
                  >
                    {isDeleting ? 'Deleting...' : 'Confirm Delete'}
                  </button>
                  <button
                    type="button"
                    onClick={() => setShowDeleteConfirm(false)}
                    className="px-3 py-1 text-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-sm"
                  >
                    Cancel
                  </button>
                </div>
              )}
            </div>

            {/* Save/Cancel Buttons */}
            <div className="flex space-x-3">
              <button
                type="button"
                onClick={handleClose}
                className="px-4 py-2 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-md transition-colors"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={isSubmitting || isUploadingImage || !formData.name.trim()}
                className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {isSubmitting ? 'Saving...' : 'Save Changes'}
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}
