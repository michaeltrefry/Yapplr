'use client';

import { useState, useRef, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { postApi, multipleUploadApi } from '@/lib/api';
import { useMutation, useQueryClient, useQuery } from '@tanstack/react-query';
import { X, Image as ImageIcon, Video, Globe, Users, Lock, ChevronDown, AlertCircle, Smile } from 'lucide-react';
import { PostPrivacy, Post, MediaFile, UploadedFile, CreateRepostData, CreateRepostWithMediaData } from '@/types';
import UserAvatar from './UserAvatar';
import { formatDate } from '@/lib/utils';
import MediaGallery from './MediaGallery';
import LinkPreviewList from './LinkPreviewList';
import GifPicker from './GifPicker';
import type { SelectedGif } from '@/lib/tenor';

interface RepostModalProps {
  isOpen: boolean;
  onClose: () => void;
  repostedPost: Post;
  onRepostCreated?: () => void;
}

export default function RepostModal({ isOpen, onClose, repostedPost, onRepostCreated }: RepostModalProps) {
  const [content, setContent] = useState('');
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [selectedFileUrls, setSelectedFileUrls] = useState<string[]>([]);
  const [uploadedFiles, setUploadedFiles] = useState<UploadedFile[]>([]);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState<{ [key: string]: number }>({});
  const [privacy, setPrivacy] = useState<PostPrivacy>(PostPrivacy.Public);
  const [showPrivacyDropdown, setShowPrivacyDropdown] = useState(false);
  const [showGifPicker, setShowGifPicker] = useState(false);
  const [selectedGif, setSelectedGif] = useState<SelectedGif | null>(null);

  const fileInputRef = useRef<HTMLInputElement>(null);
  const privacyDropdownRef = useRef<HTMLDivElement>(null);
  const { user } = useAuth();
  const queryClient = useQueryClient();

  // Fetch current upload limits
  const { data: maxVideoSizeData } = useQuery({
    queryKey: ['maxVideoSize'],
    queryFn: multipleUploadApi.getMaxVideoSize,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  const createRepostMutation = useMutation({
    mutationFn: async () => {
      const mediaFiles: MediaFile[] = uploadedFiles.map(file => ({
        fileName: file.fileName,
        mediaType: file.mediaType,
        width: file.width,
        height: file.height,
        fileSizeBytes: file.fileSizeBytes,
        duration: file.duration
      }));

      // Add GIF if selected
      if (selectedGif) {
        mediaFiles.push({
          fileName: `gif_${Date.now()}.gif`,
          mediaType: 'Gif' as any,
          width: selectedGif.width,
          height: selectedGif.height,
          gifUrl: selectedGif.url,
          gifPreviewUrl: selectedGif.previewUrl
        });
      }

      if (mediaFiles.length > 0) {
        const data: CreateRepostWithMediaData = {
          content: content.trim() || undefined, // Allow empty content for simple reposts
          repostedPostId: repostedPost.id,
          privacy,
          mediaFiles
        };
        return await postApi.createRepostWithMedia(data);
      } else {
        const data: CreateRepostData = {
          content: content.trim() || undefined, // Allow empty content for simple reposts
          repostedPostId: repostedPost.id,
          privacy
        };
        return await postApi.createRepost(data);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
      queryClient.invalidateQueries({ queryKey: ['publicTimeline'] });
      queryClient.invalidateQueries({ queryKey: ['userPosts'] });
      queryClient.invalidateQueries({ queryKey: ['userTimeline'] });
      queryClient.invalidateQueries({ queryKey: ['reposts', repostedPost.id] });
      queryClient.invalidateQueries({ queryKey: ['post', repostedPost.id] });

      // Reset form
      setContent('');
      setSelectedFiles([]);
      setSelectedFileUrls([]);
      setUploadedFiles([]);
      setSelectedGif(null);
      setPrivacy(PostPrivacy.Public);

      onRepostCreated?.();
      onClose();
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    // Allow submission even with empty content for simple reposts
    createRepostMutation.mutate();
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files || []);
    if (files.length === 0) return;

    // Check if adding these files would exceed the limit
    if (selectedFiles.length + files.length > 10) {
      alert('You can only upload up to 10 files per repost');
      return;
    }

    const newFiles = files.slice(0, 10 - selectedFiles.length);
    setSelectedFiles(prev => [...prev, ...newFiles]);

    // Create preview URLs
    const newUrls = newFiles.map(file => URL.createObjectURL(file));
    setSelectedFileUrls(prev => [...prev, ...newUrls]);

    // Upload files
    uploadFiles(newFiles);
  };

  const uploadFiles = async (files: File[]) => {
    setIsUploading(true);
    
    try {
      const response = await multipleUploadApi.uploadMultipleFiles(files);

      if (response.uploadedFiles && response.uploadedFiles.length > 0) {
        setUploadedFiles(prev => [...prev, ...response.uploadedFiles]);
      }

      if (response.errors && response.errors.length > 0) {
        console.error('Upload errors:', response.errors);
        alert(`Some files failed to upload: ${response.errors.map(e => e.errorMessage).join(', ')}`);
      }
    } catch (error) {
      console.error('Upload failed:', error);
      alert('Failed to upload files. Please try again.');
    } finally {
      setIsUploading(false);
      setUploadProgress({});
    }
  };

  const removeFile = (index: number) => {
    setSelectedFiles(prev => prev.filter((_, i) => i !== index));
    setSelectedFileUrls(prev => {
      const newUrls = prev.filter((_, i) => i !== index);
      // Revoke the URL to free memory
      URL.revokeObjectURL(prev[index]);
      return newUrls;
    });
    setUploadedFiles(prev => prev.filter((_, i) => i !== index));
  };

  const removeGif = () => {
    setSelectedGif(null);
  };

  const getPrivacyIcon = (privacy: PostPrivacy) => {
    switch (privacy) {
      case PostPrivacy.Public:
        return <Globe className="w-4 h-4" />;
      case PostPrivacy.Followers:
        return <Users className="w-4 h-4" />;
      case PostPrivacy.Private:
        return <Lock className="w-4 h-4" />;
      default:
        return <Globe className="w-4 h-4" />;
    }
  };

  const getPrivacyLabel = (privacy: PostPrivacy) => {
    switch (privacy) {
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

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (privacyDropdownRef.current && !privacyDropdownRef.current.contains(event.target as Node)) {
        setShowPrivacyDropdown(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Clean up URLs when component unmounts
  useEffect(() => {
    return () => {
      selectedFileUrls.forEach(url => URL.revokeObjectURL(url));
    };
  }, []);

  if (!isOpen) return null;

  const maxVideoSize = maxVideoSizeData?.maxVideoSizeBytes || 100 * 1024 * 1024; // Default 100MB
  const maxVideoSizeMB = Math.round(maxVideoSize / (1024 * 1024));

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg max-w-2xl w-full max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between p-4 border-b">
          <h2 className="text-xl font-semibold">Repost</h2>
          <button
            onClick={onClose}
            className="p-2 hover:bg-gray-100 rounded-full transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-4">
          <div className="flex space-x-3 mb-4">
            <UserAvatar user={user!} size="sm" />
            <div className="flex-1">
              <textarea
                value={content}
                onChange={(e) => setContent(e.target.value)}
                placeholder="Add a comment (optional)..."
                className="w-full p-3 border border-gray-300 rounded-lg resize-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                rows={3}
                maxLength={1024}
              />
              <div className="text-sm text-gray-500 mt-1">
                {content.length}/1024 characters
              </div>
            </div>
          </div>

          {/* Media preview */}
          {(selectedFiles.length > 0 || selectedGif) && (
            <div className="mb-4">
              <div className="flex items-center justify-between mb-2">
                <span className="text-sm text-gray-600">
                  {selectedFiles.length} file(s) selected
                </span>
              </div>
              <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
                {selectedFiles.map((file, index) => (
                  <div key={index} className="relative">
                    {file.type.startsWith('image/') && selectedFileUrls[index] ? (
                      <img
                        src={selectedFileUrls[index]}
                        alt={`Selected ${index + 1}`}
                        className="w-full h-24 object-cover rounded-lg border border-gray-200"
                      />
                    ) : (
                      <div className="w-full h-24 bg-gray-100 rounded-lg border border-gray-200 flex items-center justify-center">
                        <span className="text-xs text-gray-500 text-center px-2">
                          {file.name}
                        </span>
                      </div>
                    )}
                    <button
                      type="button"
                      onClick={() => removeFile(index)}
                      className="absolute -top-2 -right-2 bg-red-500 text-white rounded-full w-6 h-6 flex items-center justify-center text-xs hover:bg-red-600"
                    >
                      ×
                    </button>
                  </div>
                ))}
              </div>

              {/* GIF Preview */}
              {selectedGif && (
                <div className="mt-3 relative">
                  <img
                    src={selectedGif.previewUrl}
                    alt={selectedGif.title}
                    className="max-w-full h-auto rounded-lg border border-gray-200"
                    style={{ maxHeight: '200px' }}
                  />
                  <button
                    type="button"
                    onClick={removeGif}
                    className="absolute -top-2 -right-2 bg-red-500 text-white rounded-full w-6 h-6 flex items-center justify-center text-xs hover:bg-red-600"
                  >
                    ×
                  </button>
                </div>
              )}
            </div>
          )}

          {/* Upload progress */}
          {isUploading && (
            <div className="mb-4">
              <div className="flex items-center space-x-2">
                <div className="flex-1 bg-gray-200 rounded-full h-2">
                  <div 
                    className="bg-blue-600 h-2 rounded-full transition-all duration-300"
                    style={{ width: `${uploadProgress['batch'] || 0}%` }}
                  />
                </div>
                <span className="text-sm text-gray-600">{uploadProgress['batch'] || 0}%</span>
              </div>
            </div>
          )}

          {/* Reposted post preview */}
          <div className="border border-gray-200 rounded-lg p-4 mb-4 bg-gray-50">
            <div className="flex items-start space-x-3">
              <UserAvatar user={repostedPost.user} size="sm" />
              <div className="flex-1 min-w-0">
                <div className="flex items-center space-x-2">
                  <span className="font-medium text-gray-900">{repostedPost.user.username}</span>
                  <span className="text-gray-500">@{repostedPost.user.username}</span>
                  <span className="text-gray-500">·</span>
                  <span className="text-gray-500">{formatDate(repostedPost.createdAt)}</span>
                </div>
                <div className="mt-1 text-gray-900 whitespace-pre-wrap break-words">
                  {repostedPost.content}
                </div>
                {repostedPost.mediaItems && repostedPost.mediaItems.length > 0 && (
                  <div className="mt-3">
                    <MediaGallery
                      mediaItems={repostedPost.mediaItems}
                      post={repostedPost}
                    />
                  </div>
                )}
                {repostedPost.linkPreviews && repostedPost.linkPreviews.length > 0 && (
                  <div className="mt-3">
                    <LinkPreviewList linkPreviews={repostedPost.linkPreviews} />
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Action buttons */}
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-4">
              <input
                ref={fileInputRef}
                type="file"
                multiple
                accept="image/*,video/*"
                onChange={handleFileSelect}
                className="hidden"
              />
              
              <button
                type="button"
                onClick={() => fileInputRef.current?.click()}
                disabled={selectedFiles.length >= 10 || isUploading}
                className="flex items-center space-x-2 px-3 py-2 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <ImageIcon className="w-5 h-5" />
                <span>Photo/Video</span>
              </button>

              <button
                type="button"
                onClick={() => setShowGifPicker(!showGifPicker)}
                disabled={selectedGif !== null}
                className="flex items-center space-x-2 px-3 py-2 text-blue-600 hover:bg-blue-50 rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                <Smile className="w-5 h-5" />
                <span>GIF</span>
              </button>

              {/* Privacy selector */}
              <div className="relative" ref={privacyDropdownRef}>
                <button
                  type="button"
                  onClick={() => setShowPrivacyDropdown(!showPrivacyDropdown)}
                  className="flex items-center space-x-2 px-3 py-2 text-gray-600 hover:bg-gray-50 rounded-lg transition-colors"
                >
                  {getPrivacyIcon(privacy)}
                  <span>{getPrivacyLabel(privacy)}</span>
                  <ChevronDown className="w-4 h-4" />
                </button>

                {showPrivacyDropdown && (
                  <div className="absolute bottom-full mb-2 left-0 bg-white border border-gray-200 rounded-lg shadow-lg z-10 min-w-[120px]">
                    {Object.values(PostPrivacy).filter(p => typeof p === 'number').map((privacyOption) => (
                      <button
                        key={privacyOption}
                        type="button"
                        onClick={() => {
                          setPrivacy(privacyOption as PostPrivacy);
                          setShowPrivacyDropdown(false);
                        }}
                        className="w-full flex items-center space-x-2 px-3 py-2 text-left hover:bg-gray-50 first:rounded-t-lg last:rounded-b-lg"
                      >
                        {getPrivacyIcon(privacyOption as PostPrivacy)}
                        <span>{getPrivacyLabel(privacyOption as PostPrivacy)}</span>
                      </button>
                    ))}
                  </div>
                )}
              </div>
            </div>

            <button
              type="submit"
              disabled={createRepostMutation.isPending || isUploading}
              className="px-6 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {createRepostMutation.isPending ? 'Reposting...' : 'Repost'}
            </button>
          </div>

          {/* File size warning */}
          <div className="mt-2 text-sm text-gray-500">
            <AlertCircle className="w-4 h-4 inline mr-1" />
            Max video size: {maxVideoSizeMB}MB. Up to 10 files per repost.
          </div>
        </form>

        {/* GIF Picker */}
        {showGifPicker && (
          <div className="border-t p-4">
            <GifPicker
              isOpen={showGifPicker}
              onSelectGif={(gif) => {
                setSelectedGif(gif);
                setShowGifPicker(false);
              }}
              onClose={() => setShowGifPicker(false)}
            />
          </div>
        )}
      </div>
    </div>
  );
}
