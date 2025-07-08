'use client';

import { useState, useRef, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { postApi, imageApi, tagApi } from '@/lib/api';
import { useMutation, useQueryClient, useQuery } from '@tanstack/react-query';
import { Image as ImageIcon, X, Hash, Globe, Users, Lock, ChevronDown } from 'lucide-react';
import Image from 'next/image';
import { PostPrivacy } from '@/types';

export default function CreatePost() {
  const [content, setContent] = useState('');
  const [, setSelectedFile] = useState<File | null>(null);
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const [uploadedFileName, setUploadedFileName] = useState<string | null>(null);
  const [privacy, setPrivacy] = useState<PostPrivacy>(PostPrivacy.Public);
  const [showPrivacyDropdown, setShowPrivacyDropdown] = useState(false);
  const [showHashtagSuggestions, setShowHashtagSuggestions] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const privacyDropdownRef = useRef<HTMLDivElement>(null);
  const { user } = useAuth();
  const queryClient = useQueryClient();

  // Privacy helper functions
  const getPrivacyIcon = (privacyLevel: PostPrivacy) => {
    switch (privacyLevel) {
      case PostPrivacy.Public:
        return Globe;
      case PostPrivacy.Followers:
        return Users;
      case PostPrivacy.Private:
        return Lock;
      default:
        return Globe;
    }
  };

  const getPrivacyLabel = (privacyLevel: PostPrivacy) => {
    switch (privacyLevel) {
      case PostPrivacy.Public:
        return 'Public';
      case PostPrivacy.Followers:
        return 'Followers';
      case PostPrivacy.Private:
        return 'Private';
      default:
        return 'Public';
    }
  };

  const getPrivacyDescription = (privacyLevel: PostPrivacy) => {
    switch (privacyLevel) {
      case PostPrivacy.Public:
        return 'Anyone can see this post';
      case PostPrivacy.Followers:
        return 'Only your followers can see this post';
      case PostPrivacy.Private:
        return 'Only you can see this post';
      default:
        return 'Anyone can see this post';
    }
  };

  const privacyOptions = [
    { value: PostPrivacy.Public, label: 'Public', description: 'Anyone can see this post' },
    { value: PostPrivacy.Followers, label: 'Followers', description: 'Only your followers can see this post' },
    { value: PostPrivacy.Private, label: 'Private', description: 'Only you can see this post' },
  ];

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (privacyDropdownRef.current && !privacyDropdownRef.current.contains(event.target as Node)) {
        setShowPrivacyDropdown(false);
      }
    };

    if (showPrivacyDropdown) {
      document.addEventListener('mousedown', handleClickOutside);
      return () => document.removeEventListener('mousedown', handleClickOutside);
    }
  }, [showPrivacyDropdown]);

  // Get trending hashtags for suggestions
  const { data: trendingTags } = useQuery({
    queryKey: ['trending-tags-suggestions'],
    queryFn: () => tagApi.getTrendingTags(5),
    enabled: showHashtagSuggestions,
  });

  const uploadImageMutation = useMutation({
    mutationFn: imageApi.uploadImage,
    onSuccess: (data) => {
      setUploadedFileName(data.fileName);
    },
  });

  const createPostMutation = useMutation({
    mutationFn: postApi.createPost,
    onSuccess: () => {
      setContent('');
      setSelectedFile(null);
      setImagePreview(null);
      setUploadedFileName(null);
      setPrivacy(PostPrivacy.Public);
      setShowHashtagSuggestions(false); // Hide hashtag suggestions after posting
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
    },
  });

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      // Validate file type
      const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp'];
      if (!allowedTypes.includes(file.type)) {
        alert('Please select a valid image file (JPG, PNG, GIF, WebP)');
        return;
      }

      // Validate file size (5MB)
      if (file.size > 5 * 1024 * 1024) {
        alert('File size must be less than 5MB');
        return;
      }

      setSelectedFile(file);

      // Create preview
      const reader = new FileReader();
      reader.onload = (e) => {
        setImagePreview(e.target?.result as string);
      };
      reader.readAsDataURL(file);

      // Upload immediately
      uploadImageMutation.mutate(file);
    }
  };

  const removeImage = () => {
    setSelectedFile(null);
    setImagePreview(null);
    setUploadedFileName(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!content.trim()) return;

    createPostMutation.mutate({
      content: content.trim(),
      imageFileName: uploadedFileName || undefined,
      privacy: privacy,
    });
  };

  const remainingChars = 256 - content.length;

  return (
    <div className="border-b border-gray-200 p-4 bg-white">
      <form onSubmit={handleSubmit}>
        <div className="flex space-x-3">
          {/* Avatar */}
          <div className="w-12 h-12 bg-blue-600 rounded-full flex items-center justify-center flex-shrink-0">
            <span className="text-white font-semibold text-lg">
              {user?.username.charAt(0).toUpperCase()}
            </span>
          </div>

          {/* Content */}
          <div className="flex-1">
            <textarea
              value={content}
              onChange={(e) => {
                setContent(e.target.value);
                // Show hashtag suggestions only when user types #
                setShowHashtagSuggestions(e.target.value.includes('#'));
              }}
              placeholder="What's happening?"
              className="w-full text-xl placeholder-gray-500 border-none resize-none focus:outline-none bg-transparent text-gray-900"
              rows={3}
              maxLength={256}
            />

            {/* Hashtag Suggestions */}
            {showHashtagSuggestions && trendingTags && trendingTags.length > 0 && (
              <div className="mt-2 p-3 bg-gray-50 rounded-lg border border-gray-200">
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center space-x-2">
                    <Hash className="w-4 h-4 text-gray-500" />
                    <span className="text-sm font-medium text-gray-700">Trending hashtags</span>
                  </div>
                  <button
                    type="button"
                    onClick={() => setShowHashtagSuggestions(false)}
                    className="text-gray-400 hover:text-gray-600 p-1"
                    title="Hide suggestions"
                  >
                    <X className="w-3 h-3" />
                  </button>
                </div>
                <div className="flex flex-wrap gap-2">
                  {trendingTags.map((tag) => (
                    <button
                      key={tag.id}
                      type="button"
                      onClick={() => {
                        const newContent = content + (content.endsWith(' ') || content === '' ? '' : ' ') + `#${tag.name} `;
                        setContent(newContent);
                        // Keep suggestions visible so users can add multiple hashtags
                      }}
                      className="inline-flex items-center px-2 py-1 bg-blue-100 text-blue-800 text-sm rounded-full hover:bg-blue-200 transition-colors"
                    >
                      #{tag.name}
                    </button>
                  ))}
                </div>
              </div>
            )}

            {/* Hidden File Input */}
            <input
              ref={fileInputRef}
              type="file"
              accept="image/*"
              onChange={handleFileSelect}
              className="hidden"
            />

            {/* Image Preview */}
            {imagePreview && (
              <div className="mt-3 relative">
                <Image
                  src={imagePreview}
                  alt="Preview"
                  width={500}
                  height={300}
                  className="max-w-full h-auto rounded-lg border border-gray-200"
                />
                <button
                  type="button"
                  onClick={removeImage}
                  className="absolute top-2 right-2 bg-gray-800 bg-opacity-75 text-white rounded-full p-1 hover:bg-opacity-100 transition-opacity"
                >
                  <X className="w-4 h-4" />
                </button>
                {uploadImageMutation.isPending && (
                  <div className="absolute inset-0 bg-black bg-opacity-50 flex items-center justify-center rounded-lg">
                    <div className="text-white text-sm">Uploading...</div>
                  </div>
                )}
              </div>
            )}

            {/* Actions */}
            <div className="flex items-center justify-between mt-4">
              <div className="flex items-center space-x-4">
                <button
                  type="button"
                  onClick={() => fileInputRef.current?.click()}
                  className="text-blue-500 hover:bg-blue-50 p-2 rounded-full transition-colors"
                  title="Add image"
                  disabled={uploadImageMutation.isPending}
                >
                  <ImageIcon className="w-5 h-5" />
                </button>

                {/* Privacy Selector */}
                <div className="relative" ref={privacyDropdownRef}>
                  <button
                    type="button"
                    onClick={() => setShowPrivacyDropdown(!showPrivacyDropdown)}
                    className="flex items-center space-x-2 bg-transparent border border-gray-300 rounded-full px-3 py-1 text-sm text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-colors"
                  >
                    {(() => {
                      const IconComponent = getPrivacyIcon(privacy);
                      return <IconComponent className="w-4 h-4" />;
                    })()}
                    <span>{getPrivacyLabel(privacy)}</span>
                    <ChevronDown className="w-3 h-3" />
                  </button>

                  {showPrivacyDropdown && (
                    <div className="absolute top-full left-0 mt-1 w-64 bg-white border border-gray-200 rounded-lg shadow-lg z-10">
                      {privacyOptions.map((option) => {
                        const IconComponent = getPrivacyIcon(option.value);
                        return (
                          <button
                            key={option.value}
                            type="button"
                            onClick={() => {
                              setPrivacy(option.value);
                              setShowPrivacyDropdown(false);
                            }}
                            className={`w-full flex items-start space-x-3 px-4 py-3 text-left hover:bg-gray-50 transition-colors ${
                              privacy === option.value ? 'bg-blue-50 border-l-2 border-blue-500' : ''
                            }`}
                          >
                            <IconComponent className="w-4 h-4 mt-0.5 text-gray-600" />
                            <div className="flex-1">
                              <div className="font-medium text-gray-900">{option.label}</div>
                              <div className="text-sm text-gray-500">{option.description}</div>
                            </div>
                          </button>
                        );
                      })}
                    </div>
                  )}
                </div>
              </div>

              <div className="flex items-center space-x-3">
                <span
                  className={`text-sm ${
                    remainingChars < 20
                      ? remainingChars < 0
                        ? 'text-red-500'
                        : 'text-orange-500'
                      : 'text-gray-500'
                  }`}
                >
                  {remainingChars}
                </span>
                <button
                  type="submit"
                  disabled={!content.trim() || remainingChars < 0 || createPostMutation.isPending || uploadImageMutation.isPending}
                  className="bg-blue-500 text-white px-6 py-2 rounded-full font-semibold hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  {createPostMutation.isPending ? 'Yapping...' : uploadImageMutation.isPending ? 'Uploading...' : 'Yap'}
                </button>
              </div>
            </div>
          </div>
        </div>
      </form>
    </div>
  );
}
