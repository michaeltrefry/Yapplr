'use client';

import { useState, useRef, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { postApi, imageApi, videoApi, tagApi, multipleUploadApi } from '@/lib/api';
import { useMutation, useQueryClient, useQuery } from '@tanstack/react-query';
import { Image as ImageIcon, Video, X, Hash, Globe, Users, Lock, ChevronDown, AlertCircle } from 'lucide-react';
import { PostPrivacy, UserStatus, MediaType, MediaFile, UploadedFile } from '@/types';

export default function CreatePost() {
  const [content, setContent] = useState('');
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [selectedFileUrls, setSelectedFileUrls] = useState<string[]>([]);
  const [uploadedFiles, setUploadedFiles] = useState<UploadedFile[]>([]);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadProgress, setUploadProgress] = useState<{ [key: string]: number }>({});
  const [privacy, setPrivacy] = useState<PostPrivacy>(PostPrivacy.Public);
  const [showPrivacyDropdown, setShowPrivacyDropdown] = useState(false);
  const [showHashtagSuggestions, setShowHashtagSuggestions] = useState(false);

  // Legacy state for backward compatibility
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const [uploadedFileName, setUploadedFileName] = useState<string | null>(null);
  const [videoPreview, setVideoPreview] = useState<string | null>(null);
  const [uploadedVideoFileName, setUploadedVideoFileName] = useState<string | null>(null);
  const [mediaType, setMediaType] = useState<'image' | 'video' | null>(null);
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

  // Create and cleanup object URLs when selectedFiles change to prevent memory leaks
  useEffect(() => {
    // Cleanup previous URLs
    selectedFileUrls.forEach(url => URL.revokeObjectURL(url));

    // Create new URLs for image files
    const newUrls = selectedFiles.map(file =>
      file.type.startsWith('image/') ? URL.createObjectURL(file) : ''
    );
    setSelectedFileUrls(newUrls);

    // Cleanup on unmount
    return () => {
      newUrls.forEach(url => {
        if (url) URL.revokeObjectURL(url);
      });
    };
  }, [selectedFiles]);

  // Get trending hashtags for suggestions
  const { data: trendingTags } = useQuery({
    queryKey: ['trending-tags-suggestions'],
    queryFn: () => tagApi.getTrendingTags(5),
    enabled: showHashtagSuggestions,
  });

  // Legacy upload mutations for backward compatibility
  const uploadImageMutation = useMutation({
    mutationFn: imageApi.uploadImage,
    onSuccess: (data) => {
      setUploadedFileName(data.fileName);
    },
  });

  const uploadVideoMutation = useMutation({
    mutationFn: videoApi.uploadVideo,
    onSuccess: (data) => {
      setUploadedVideoFileName(data.fileName);
    },
  });

  // New multiple file upload mutation
  const multipleUploadMutation = useMutation({
    mutationFn: multipleUploadApi.uploadMultipleFiles,
    onMutate: () => {
      setIsUploading(true);
    },
    onSuccess: (data) => {
      console.log('Upload success response:', data);
      console.log('Uploaded files:', data.uploadedFiles);
      data.uploadedFiles.forEach((file, index) => {
        console.log(`File ${index}:`, file);
        console.log(`  fileName: ${file.fileName}`);
        console.log(`  fileUrl: ${file.fileUrl}`);
        console.log(`  mediaType: ${file.mediaType}`);
      });

      setUploadedFiles(prev => [...prev, ...data.uploadedFiles]);
      setIsUploading(false);

      // Clear selected files and their URLs since they're now uploaded
      selectedFileUrls.forEach(url => {
        if (url) URL.revokeObjectURL(url);
      });
      setSelectedFiles([]);
      setSelectedFileUrls([]);

      if (data.errors.length > 0) {
        // Show errors to user
        const errorMessages = data.errors.map(e => `${e.originalFileName}: ${e.errorMessage}`).join('\n');
        alert(`Some files failed to upload:\n${errorMessages}`);
      }
    },
    onError: () => {
      setIsUploading(false);
      alert('Failed to upload files. Please try again.');
    },
  });

  const createPostMutation = useMutation({
    mutationFn: postApi.createPost,
    onSuccess: () => {
      resetForm();
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
    },
  });

  const createPostWithMediaMutation = useMutation({
    mutationFn: postApi.createPostWithMedia,
    onSuccess: () => {
      resetForm();
      queryClient.invalidateQueries({ queryKey: ['timeline'] });
    },
  });

  const resetForm = () => {
    // Clean up all object URLs
    selectedFileUrls.forEach(url => {
      if (url) URL.revokeObjectURL(url);
    });
    setContent('');
    setSelectedFiles([]);
    setSelectedFileUrls([]);
    setUploadedFiles([]);
    setImagePreview(null);
    setUploadedFileName(null);
    setVideoPreview(null);
    setUploadedVideoFileName(null);
    setMediaType(null);
    setPrivacy(PostPrivacy.Public);
    setShowHashtagSuggestions(false);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleMediaSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files || []);
    if (files.length === 0) return;

    // Check if adding these files would exceed the limit
    const totalFiles = selectedFiles.length + uploadedFiles.length + files.length;
    if (totalFiles > 10) {
      alert(`Maximum 10 files allowed. You currently have ${selectedFiles.length + uploadedFiles.length} files selected.`);
      return;
    }

    // Validate each file
    const validFiles: File[] = [];
    const errors: string[] = [];

    for (const file of files) {
      const isVideo = file.type.startsWith('video/');
      const isImage = file.type.startsWith('image/');

      if (!isImage && !isVideo) {
        errors.push(`${file.name}: Unsupported file type. Please select images or videos only.`);
        continue;
      }

      if (isVideo) {
        const allowedVideoTypes = ['video/mp4', 'video/avi', 'video/x-msvideo', 'video/mov', 'video/quicktime', 'video/wmv', 'video/flv', 'video/webm', 'video/x-matroska'];
        if (!allowedVideoTypes.includes(file.type)) {
          errors.push(`${file.name}: Unsupported video format. Supported: MP4, AVI, MOV, WMV, FLV, WebM, MKV`);
          continue;
        }

        if (file.size > 100 * 1024 * 1024) {
          errors.push(`${file.name}: Video file too large. Maximum size is 100MB.`);
          continue;
        }
      } else if (isImage) {
        const allowedImageTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp'];
        if (!allowedImageTypes.includes(file.type)) {
          errors.push(`${file.name}: Unsupported image format. Supported: JPG, PNG, GIF, WebP`);
          continue;
        }

        if (file.size > 5 * 1024 * 1024) {
          errors.push(`${file.name}: Image file too large. Maximum size is 5MB.`);
          continue;
        }
      }

      validFiles.push(file);
    }

    if (errors.length > 0) {
      alert(`Some files were rejected:\n${errors.join('\n')}`);
    }

    if (validFiles.length > 0) {
      setSelectedFiles(prev => [...prev, ...validFiles]);

      // Upload files immediately
      multipleUploadMutation.mutate(validFiles);
    }

    // Clear the input
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const removeFile = (index: number, isUploaded: boolean = false) => {
    if (isUploaded) {
      setUploadedFiles(prev => prev.filter((_, i) => i !== index));
    } else {
      // Clean up object URL before removing
      if (selectedFileUrls[index]) {
        URL.revokeObjectURL(selectedFileUrls[index]);
      }
      setSelectedFiles(prev => prev.filter((_, i) => i !== index));
      setSelectedFileUrls(prev => prev.filter((_, i) => i !== index));
    }
  };

  const removeAllMedia = () => {
    // Clean up all object URLs
    selectedFileUrls.forEach(url => {
      if (url) URL.revokeObjectURL(url);
    });
    setSelectedFiles([]);
    setSelectedFileUrls([]);
    setUploadedFiles([]);
    // Legacy cleanup
    setImagePreview(null);
    setVideoPreview(null);
    setUploadedFileName(null);
    setUploadedVideoFileName(null);
    setMediaType(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!content.trim()) return;

    // Use new multiple media API if we have uploaded files
    if (uploadedFiles.length > 0) {
      const mediaFiles: MediaFile[] = uploadedFiles.map(file => ({
        fileName: file.fileName,
        mediaType: file.mediaType,
        width: file.width,
        height: file.height,
        fileSizeBytes: file.fileSizeBytes,
        duration: file.duration,
      }));

      createPostWithMediaMutation.mutate({
        content: content.trim(),
        privacy: privacy,
        mediaFiles: mediaFiles,
      });
    } else {
      // Use legacy API for backward compatibility
      createPostMutation.mutate({
        content: content.trim(),
        imageFileName: uploadedFileName || undefined,
        videoFileName: uploadedVideoFileName || undefined,
        privacy: privacy,
      });
    }
  };

  const remainingChars = 256 - content.length;

  // Check if user is suspended
  const isSuspended = user?.status === UserStatus.Suspended;
  const suspensionEndDate = user?.suspendedUntil ? new Date(user.suspendedUntil) : null;
  const suspensionReason = user?.suspensionReason;

  // If user is suspended, show suspension message instead of create post form
  if (isSuspended) {
    return (
      <div className="border-b border-gray-200 p-4 bg-white">
        <div className="flex space-x-3">
          {/* Avatar */}
          <div className="w-12 h-12 bg-blue-600 rounded-full flex items-center justify-center flex-shrink-0">
            <span className="text-white font-semibold text-lg">
              {user?.username.charAt(0).toUpperCase()}
            </span>
          </div>

          {/* Suspension Message */}
          <div className="flex-1">
            <div className="bg-red-50 border border-red-200 rounded-lg p-4">
              <div className="flex items-start space-x-3">
                <AlertCircle className="w-5 h-5 text-red-500 mt-0.5 flex-shrink-0" />
                <div className="flex-1">
                  <h3 className="text-sm font-medium text-red-800 mb-1">
                    Account Suspended
                  </h3>
                  <p className="text-sm text-red-700 mb-2">
                    Your account has been suspended and you cannot create posts or interact with content.
                  </p>
                  {suspensionEndDate && (
                    <p className="text-sm text-red-700 mb-2">
                      <strong>Suspension ends:</strong> {suspensionEndDate.toLocaleDateString()} at {suspensionEndDate.toLocaleTimeString()}
                    </p>
                  )}
                  {!suspensionEndDate && (
                    <p className="text-sm text-red-700 mb-2">
                      <strong>Duration:</strong> Indefinite
                    </p>
                  )}
                  {suspensionReason && (
                    <p className="text-sm text-red-700 mb-2">
                      <strong>Reason:</strong> {suspensionReason}
                    </p>
                  )}
                  <p className="text-sm text-red-600">
                    You can still browse and view content, but posting and interactions are disabled.
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

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
              className="w-full text-xl placeholder-gray-500 border border-gray-200 rounded-lg resize-none focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-opacity-50 focus:border-blue-500 bg-transparent text-gray-900 p-3 transition-all duration-200"
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

            {/* Hidden Unified Media Input */}
            <input
              ref={fileInputRef}
              type="file"
              accept="image/jpeg,image/jpg,image/png,image/gif,image/webp,image/heic,video/mp4,video/mov,video/quicktime,video/avi,video/x-msvideo,video/wmv,video/flv,video/webm,video/x-matroska"
              multiple={true}
              onChange={handleMediaSelect}
              className="hidden"
            />

            {/* Multiple Media Preview */}
            {(uploadedFiles.length > 0 || selectedFiles.length > 0) && (
              <div className="mt-3">
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm text-gray-600">
                    {uploadedFiles.length + selectedFiles.length} file(s) selected (max 10)
                  </span>
                  <button
                    type="button"
                    onClick={removeAllMedia}
                    className="text-red-500 hover:text-red-700 text-sm"
                  >
                    Remove all
                  </button>
                </div>

                <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
                  {/* Uploaded Files */}
                  {uploadedFiles.map((file, index) => {
                    console.log(`Rendering uploaded file ${index}:`, file);
                    return (
                      <div key={`uploaded-${index}`} className="relative">
                        {file.mediaType === MediaType.Image ? (
                          <img
                            src={file.fileUrl}
                            alt={`Uploaded ${index + 1}`}
                            className="w-full h-24 object-cover rounded-lg border border-gray-200"
                            onError={(e) => {
                              console.error(`Image failed to load for file ${index}:`, file.fileUrl);
                              console.error('Error event:', e);
                            }}
                            onLoad={() => {
                              console.log(`Image loaded successfully for file ${index}:`, file.fileUrl);
                            }}
                          />
                        ) : (
                          <div className="w-full h-24 bg-gray-100 rounded-lg border border-gray-200 flex items-center justify-center">
                            <Video className="w-8 h-8 text-gray-400" />
                          </div>
                        )}
                        <button
                          type="button"
                          onClick={() => removeFile(index, true)}
                          className="absolute -top-1 -right-1 bg-red-500 text-white rounded-full p-1 hover:bg-red-600 transition-colors"
                        >
                          <X className="w-3 h-3" />
                        </button>
                        <div className="absolute bottom-1 left-1 bg-green-500 text-white text-xs px-1 rounded">
                          ✓
                        </div>
                      </div>
                    );
                  })}

                  {/* Files being uploaded */}
                  {selectedFiles.map((file, index) => (
                    <div key={`selected-${index}`} className="relative">
                      {file.type.startsWith('image/') && selectedFileUrls[index] ? (
                        <img
                          src={selectedFileUrls[index]}
                          alt={`Selected ${index + 1}`}
                          className="w-full h-24 object-cover rounded-lg border border-gray-200"
                        />
                      ) : (
                        <div className="w-full h-24 bg-gray-100 rounded-lg border border-gray-200 flex items-center justify-center">
                          <Video className="w-8 h-8 text-gray-400" />
                        </div>
                      )}
                      <button
                        type="button"
                        onClick={() => removeFile(index, false)}
                        className="absolute -top-1 -right-1 bg-red-500 text-white rounded-full p-1 hover:bg-red-600 transition-colors"
                      >
                        <X className="w-3 h-3" />
                      </button>
                      {isUploading && (
                        <div className="absolute inset-0 bg-black bg-opacity-50 flex items-center justify-center rounded-lg">
                          <div className="text-white text-xs">Uploading...</div>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Legacy Image Preview (for backward compatibility) */}
            {imagePreview && !uploadedFiles.length && !selectedFiles.length && (
              <div className="mt-3 relative">
                <img
                  src={imagePreview}
                  alt="Preview"
                  className="max-w-full h-auto rounded-lg border border-gray-200"
                />
                <button
                  type="button"
                  onClick={removeAllMedia}
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

            {/* Legacy Video Preview (for backward compatibility) */}
            {videoPreview && !uploadedFiles.length && !selectedFiles.length && (
              <div className="mt-3 relative">
                <video
                  src={videoPreview}
                  controls
                  className="max-w-full h-auto rounded-lg border border-gray-200"
                  style={{ maxHeight: '400px' }}
                >
                  Your browser does not support the video tag.
                </video>
                <button
                  type="button"
                  onClick={removeAllMedia}
                  className="absolute top-2 right-2 bg-gray-800 bg-opacity-75 text-white rounded-full p-1 hover:bg-opacity-100 transition-opacity"
                >
                  <X className="w-4 h-4" />
                </button>
                {uploadVideoMutation.isPending && (
                  <div className="absolute inset-0 bg-black bg-opacity-50 flex items-center justify-center rounded-lg">
                    <div className="text-white text-sm">Uploading video...</div>
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
                  className="text-gray-500 hover:bg-gray-100 p-2 rounded-full transition-colors flex items-center gap-1 sm:gap-2"
                  title={`Add photos/videos (${uploadedFiles.length + selectedFiles.length}/10)`}
                  disabled={isUploading || uploadImageMutation.isPending || uploadVideoMutation.isPending || (uploadedFiles.length + selectedFiles.length) >= 10}
                >
                  <ImageIcon className="w-5 h-5" />
                  <span className="hidden sm:inline text-sm">
                    {uploadedFiles.length + selectedFiles.length > 0
                      ? `Add More (${uploadedFiles.length + selectedFiles.length}/10)`
                      : 'Photo/Video'}
                  </span>
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
                  disabled={!content.trim() || remainingChars < 0 || createPostMutation.isPending || createPostWithMediaMutation.isPending || isUploading || uploadImageMutation.isPending || uploadVideoMutation.isPending}
                  className="bg-blue-500 text-white px-6 py-2 rounded-full font-semibold hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  {createPostMutation.isPending || createPostWithMediaMutation.isPending ? 'Yapping...' :
                   isUploading ? 'Uploading files...' :
                   uploadImageMutation.isPending ? 'Uploading image...' :
                   uploadVideoMutation.isPending ? 'Uploading video...' : 'Yap'}
                </button>
              </div>
            </div>
          </div>
        </div>
      </form>
    </div>
  );
}
