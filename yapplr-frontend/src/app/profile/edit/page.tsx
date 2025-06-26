'use client';

import { useState, useRef } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useRouter } from 'next/navigation';
import { userApi } from '@/lib/api';
import { useAuth } from '@/contexts/AuthContext';
import { ArrowLeft, Camera } from 'lucide-react';
import Image from 'next/image';
import Sidebar from '@/components/Sidebar';
import UserAvatar from '@/components/UserAvatar';
import type { UpdateUserData } from '@/types';

export default function EditProfilePage() {
  const { user, updateUser } = useAuth();
  const router = useRouter();
  const queryClient = useQueryClient();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [formData, setFormData] = useState<UpdateUserData>({
    bio: user?.bio || '',
    pronouns: user?.pronouns || '',
    tagline: user?.tagline || '',
    birthday: user?.birthday || '',
  });

  const [profileImagePreview, setProfileImagePreview] = useState<string | null>(null);

  const updateMutation = useMutation({
    mutationFn: (data: UpdateUserData) => userApi.updateCurrentUser(data),
    onSuccess: (updatedUser) => {
      // Update the auth context
      updateUser(updatedUser);

      // Invalidate and refetch profile queries
      queryClient.invalidateQueries({ queryKey: ['profile'] });
      queryClient.invalidateQueries({ queryKey: ['currentUser'] });

      // Navigate back to profile
      router.push(`/profile/${user?.username}`);
    },
    onError: (error) => {
      console.error('Failed to update profile:', error);
    },
  });

  const uploadImageMutation = useMutation({
    mutationFn: (file: File) => userApi.uploadProfileImage(file),
    onSuccess: (updatedUser) => {
      updateUser(updatedUser);
      queryClient.invalidateQueries({ queryKey: ['profile'] });
      queryClient.invalidateQueries({ queryKey: ['currentUser'] });
      setProfileImagePreview(null);
    },
    onError: (error) => {
      console.error('Failed to upload profile image:', error);
    },
  });

  const removeImageMutation = useMutation({
    mutationFn: () => userApi.removeProfileImage(),
    onSuccess: (updatedUser) => {
      updateUser(updatedUser);
      queryClient.invalidateQueries({ queryKey: ['profile'] });
      queryClient.invalidateQueries({ queryKey: ['currentUser'] });
      setProfileImagePreview(null);
    },
    onError: (error) => {
      console.error('Failed to remove profile image:', error);
    },
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    // Prepare the data for submission, only including non-empty values
    const submitData: UpdateUserData = {};
    
    if (formData.bio && formData.bio.trim()) {
      submitData.bio = formData.bio.trim();
    }
    
    if (formData.pronouns && formData.pronouns.trim()) {
      submitData.pronouns = formData.pronouns.trim();
    }
    
    if (formData.tagline && formData.tagline.trim()) {
      submitData.tagline = formData.tagline.trim();
    }
    
    if (formData.birthday && formData.birthday.trim()) {
      // Convert to ISO format with UTC timezone
      const date = new Date(formData.birthday + 'T00:00:00.000Z');
      submitData.birthday = date.toISOString();
    }
    
    updateMutation.mutate(submitData);
  };

  const handleCancel = () => {
    router.push(`/profile/${user?.username}`);
  };

  const handleImageSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      // Validate file type
      if (!file.type.startsWith('image/')) {
        alert('Please select an image file');
        return;
      }

      // Validate file size (5MB limit)
      if (file.size > 5 * 1024 * 1024) {
        alert('Image must be less than 5MB');
        return;
      }

      // Create preview
      const reader = new FileReader();
      reader.onload = (e) => {
        setProfileImagePreview(e.target?.result as string);
      };
      reader.readAsDataURL(file);

      // Upload the image
      uploadImageMutation.mutate(file);
    }
  };

  const handleRemoveImage = () => {
    removeImageMutation.mutate();
  };

  const triggerImageSelect = () => {
    fileInputRef.current?.click();
  };

  if (!user) {
    return (
      <div className="min-h-screen bg-gray-50">
        <div className="max-w-6xl mx-auto flex">
          <div className="w-16 lg:w-64 fixed h-full z-10">
            <Sidebar />
          </div>
          <div className="flex-1 ml-16 lg:ml-64">
            <div className="max-w-2xl mx-auto lg:border-x border-gray-200 min-h-screen bg-white">
              <div className="p-8 text-center">
                <div className="text-red-500">Please log in to edit your profile</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-6xl mx-auto flex">
        {/* Sidebar */}
        <div className="w-16 lg:w-64 fixed h-full z-10">
          <Sidebar />
        </div>

        {/* Main Content */}
        <div className="flex-1 ml-16 lg:ml-64">
          <div className="max-w-2xl mx-auto lg:border-x border-gray-200 min-h-screen bg-white">
            {/* Header */}
            <div className="sticky top-0 bg-white/80 backdrop-blur-md border-b border-gray-200 p-4">
              <div className="flex items-center space-x-4">
                <button
                  onClick={handleCancel}
                  className="p-2 hover:bg-gray-100 rounded-full transition-colors"
                  disabled={updateMutation.isPending}
                >
                  <ArrowLeft className="w-5 h-5" />
                </button>
                <div>
                  <h1 className="text-xl font-bold">Edit profile</h1>
                  <p className="text-sm text-gray-500">@{user.username}</p>
                </div>
              </div>
            </div>

            {/* Form */}
            <form onSubmit={handleSubmit} className="p-6 space-y-6">
              {/* Profile Picture Section */}
              <div className="flex items-center space-x-4 pb-6 border-b border-gray-200">
                <div className="relative">
                  {profileImagePreview ? (
                    <Image
                      src={profileImagePreview}
                      alt="Profile preview"
                      width={80}
                      height={80}
                      className="w-20 h-20 rounded-full object-cover"
                    />
                  ) : (
                    <UserAvatar user={user} size="xl" clickable={false} />
                  )}

                  {/* Camera overlay */}
                  <button
                    type="button"
                    onClick={triggerImageSelect}
                    disabled={uploadImageMutation.isPending}
                    className="absolute inset-0 bg-black bg-opacity-50 rounded-full flex items-center justify-center opacity-0 hover:opacity-100 transition-opacity disabled:opacity-50"
                  >
                    <Camera className="w-6 h-6 text-white" />
                  </button>
                </div>

                <div className="flex-1">
                  <h3 className="font-semibold text-gray-900">{user.username}</h3>
                  <p className="text-sm text-gray-500">@{user.username}</p>

                  <div className="flex space-x-2 mt-2">
                    <button
                      type="button"
                      onClick={triggerImageSelect}
                      disabled={uploadImageMutation.isPending}
                      className="text-sm text-blue-600 hover:text-blue-700 disabled:opacity-50"
                    >
                      {uploadImageMutation.isPending ? 'Uploading...' : 'Change photo'}
                    </button>

                    {(user.profileImageFileName || profileImagePreview) && (
                      <button
                        type="button"
                        onClick={handleRemoveImage}
                        disabled={removeImageMutation.isPending}
                        className="text-sm text-red-600 hover:text-red-700 disabled:opacity-50"
                      >
                        {removeImageMutation.isPending ? 'Removing...' : 'Remove'}
                      </button>
                    )}
                  </div>
                </div>

                {/* Hidden file input */}
                <input
                  ref={fileInputRef}
                  type="file"
                  accept="image/*"
                  onChange={handleImageSelect}
                  className="hidden"
                />
              </div>

              {/* Bio */}
              <div>
                <label htmlFor="bio" className="block text-sm font-medium text-gray-700 mb-2">
                  Bio
                </label>
                <textarea
                  id="bio"
                  name="bio"
                  value={formData.bio}
                  onChange={handleChange}
                  placeholder="Tell us about yourself..."
                  maxLength={500}
                  rows={4}
                  className="w-full px-3 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
                />
                <div className="text-xs text-gray-500 mt-1">
                  {formData.bio?.length || 0}/500 characters
                </div>
              </div>

              {/* Pronouns */}
              <div>
                <label htmlFor="pronouns" className="block text-sm font-medium text-gray-700 mb-2">
                  Pronouns
                </label>
                <input
                  type="text"
                  id="pronouns"
                  name="pronouns"
                  value={formData.pronouns}
                  onChange={handleChange}
                  placeholder="e.g., they/them, she/her, he/him"
                  maxLength={100}
                  className="w-full px-3 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                />
              </div>

              {/* Tagline */}
              <div>
                <label htmlFor="tagline" className="block text-sm font-medium text-gray-700 mb-2">
                  Tagline
                </label>
                <input
                  type="text"
                  id="tagline"
                  name="tagline"
                  value={formData.tagline}
                  onChange={handleChange}
                  placeholder="Your personal motto or tagline"
                  maxLength={200}
                  className="w-full px-3 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                />
              </div>

              {/* Birthday */}
              <div>
                <label htmlFor="birthday" className="block text-sm font-medium text-gray-700 mb-2">
                  Birthday
                </label>
                <input
                  type="date"
                  id="birthday"
                  name="birthday"
                  value={formData.birthday ? formData.birthday.split('T')[0] : ''}
                  onChange={handleChange}
                  className="w-full px-3 py-3 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                />
              </div>

              {/* Action Buttons */}
              <div className="flex justify-end space-x-3 pt-6 border-t border-gray-200">
                <button
                  type="button"
                  onClick={handleCancel}
                  disabled={updateMutation.isPending}
                  className="px-6 py-2 text-gray-700 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors disabled:opacity-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={updateMutation.isPending}
                  className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {updateMutation.isPending ? 'Saving...' : 'Save changes'}
                </button>
              </div>
            </form>
          </div>
        </div>
      </div>
    </div>
  );
}
