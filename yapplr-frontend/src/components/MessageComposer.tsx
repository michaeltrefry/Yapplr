'use client';

import { useState, useRef } from 'react';
import { useMutation, useQueryClient, useQuery } from '@tanstack/react-query';
import { messageApi, imageApi, videoApi, multipleUploadApi } from '@/lib/api';
import { useNotifications } from '@/contexts/NotificationContext';
import { Send, Image as ImageIcon, X } from 'lucide-react';
import Image from 'next/image';

interface MessageComposerProps {
  conversationId: number;
}

export default function MessageComposer({ conversationId }: MessageComposerProps) {
  const [content, setContent] = useState('');
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const [videoPreview, setVideoPreview] = useState<string | null>(null);
  const [uploadedFileName, setUploadedFileName] = useState<string | null>(null);
  const [uploadedVideoFileName, setUploadedVideoFileName] = useState<string | null>(null);
  const [mediaType, setMediaType] = useState<'image' | 'video' | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const queryClient = useQueryClient();
  const { refreshUnreadCount } = useNotifications();

  // Fetch current upload limits
  const { data: maxVideoSize } = useQuery({
    queryKey: ['maxVideoSize'],
    queryFn: multipleUploadApi.getMaxVideoSize,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  const { data: maxImageSize } = useQuery({
    queryKey: ['maxImageSize'],
    queryFn: multipleUploadApi.getMaxImageSize,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

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

  const sendMessageMutation = useMutation({
    mutationFn: messageApi.sendMessageToConversation,
    onSuccess: () => {
      setContent('');
      setImagePreview(null);
      setVideoPreview(null);
      setUploadedFileName(null);
      setUploadedVideoFileName(null);
      setMediaType(null);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
      if (textareaRef.current) {
        textareaRef.current.style.height = 'auto';
      }
      
      // Invalidate queries to refresh messages and conversations
      queryClient.invalidateQueries({ queryKey: ['messages', conversationId] });
      queryClient.invalidateQueries({ queryKey: ['conversations'] });
      queryClient.invalidateQueries({ queryKey: ['conversation', conversationId] });

      // Refresh unread count for the recipient
      refreshUnreadCount();
    },
  });

  const handleMediaSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      // Determine if this is an image or video
      const isVideo = file.type.startsWith('video/');
      const isImage = file.type.startsWith('image/');

      if (!isImage && !isVideo) {
        alert('Please select a valid image or video file');
        return;
      }

      if (isVideo) {
        // Validate video file type
        const allowedVideoTypes = ['video/mp4', 'video/avi', 'video/x-msvideo', 'video/mov', 'video/quicktime', 'video/wmv', 'video/flv', 'video/webm', 'video/x-matroska'];
        if (!allowedVideoTypes.includes(file.type)) {
          alert('Please select a valid video file (MP4, AVI, MOV, WMV, FLV, WebM, MKV)');
          return;
        }

        // Validate video file size
        const maxVideoSizeBytes = maxVideoSize?.maxVideoSizeBytes || (100 * 1024 * 1024); // Fallback to 100MB
        if (file.size > maxVideoSizeBytes) {
          const maxSizeDisplay = maxVideoSizeBytes >= (1024 * 1024 * 1024)
            ? `${Math.round(maxVideoSizeBytes / (1024 * 1024 * 1024) * 10) / 10}GB`
            : `${Math.round(maxVideoSizeBytes / (1024 * 1024))}MB`;
          alert(`Video file size must be less than ${maxSizeDisplay}`);
          return;
        }

        setMediaType('video');

        // Create video preview
        const reader = new FileReader();
        reader.onload = (e) => {
          setVideoPreview(e.target?.result as string);
        };
        reader.readAsDataURL(file);

        // Clear image if present
        setImagePreview(null);
        setUploadedFileName(null);

        // Upload immediately
        uploadVideoMutation.mutate(file);
      } else {
        // Handle image
        const allowedImageTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp'];
        if (!allowedImageTypes.includes(file.type)) {
          alert('Please select a valid image file (JPG, PNG, GIF, WebP)');
          return;
        }

        // Validate image file size
        const maxImageSizeBytes = maxImageSize?.maxImageSizeBytes || (5 * 1024 * 1024); // Fallback to 5MB
        if (file.size > maxImageSizeBytes) {
          const maxSizeDisplay = `${Math.round(maxImageSizeBytes / (1024 * 1024))}MB`;
          alert(`Image file size must be less than ${maxSizeDisplay}`);
          return;
        }

        setMediaType('image');

        // Create image preview
        const reader = new FileReader();
        reader.onload = (e) => {
          setImagePreview(e.target?.result as string);
        };
        reader.readAsDataURL(file);

        // Clear video if present
        setVideoPreview(null);
        setUploadedVideoFileName(null);

        // Upload immediately
        uploadImageMutation.mutate(file);
      }
    }
  };

  const removeMedia = () => {
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

    // Must have either content or media
    if (!content.trim() && !uploadedFileName && !uploadedVideoFileName) return;

    sendMessageMutation.mutate({
      conversationId,
      content: content.trim() || undefined,
      imageFileName: uploadedFileName || undefined,
      videoFileName: uploadedVideoFileName || undefined,
    });
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit(e);
    }
  };

  const handleTextareaChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setContent(e.target.value);
    
    // Auto-resize textarea
    const textarea = e.target;
    textarea.style.height = 'auto';
    textarea.style.height = Math.min(textarea.scrollHeight, 120) + 'px';
  };

  const isLoading = sendMessageMutation.isPending || uploadImageMutation.isPending || uploadVideoMutation.isPending;
  const canSend = (content.trim() || uploadedFileName) && !isLoading;

  return (
    <form onSubmit={handleSubmit} className="p-4">
      {/* Media Preview */}
      {(imagePreview || videoPreview) && (
        <div className="mb-3 relative inline-block">
          <div className="relative rounded-lg overflow-hidden border border-gray-200">
            {imagePreview && (
              <Image
                src={imagePreview}
                alt="Image preview"
                width={200}
                height={150}
                className="object-cover"
              />
            )}
            {videoPreview && (
              <video
                src={videoPreview}
                width={200}
                height={150}
                controls
                className="object-cover"
              >
                Your browser does not support the video tag.
              </video>
            )}
            <button
              type="button"
              onClick={removeMedia}
              className="absolute top-2 right-2 bg-black bg-opacity-50 text-white rounded-full p-1 hover:bg-opacity-70 transition-opacity"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
          {(uploadImageMutation.isPending || uploadVideoMutation.isPending) && (
            <div className="absolute inset-0 bg-black bg-opacity-50 flex items-center justify-center rounded-lg">
              <div className="text-white text-sm">
                {uploadVideoMutation.isPending ? 'Uploading video...' : 'Uploading image...'}
              </div>
            </div>
          )}
        </div>
      )}

      {/* Input Area */}
      <div className="flex items-end space-x-3">
        {/* Hidden Media Input */}
        <input
          ref={fileInputRef}
          type="file"
          accept="image/jpeg,image/jpg,image/png,image/gif,image/webp,image/heic,video/mp4,video/mov,video/quicktime,video/avi,video/x-msvideo,video/wmv,video/flv,video/webm,video/x-matroska"
          multiple={false}
          onChange={handleMediaSelect}
          className="hidden"
        />

        {/* Media Upload Button */}
        <button
          type="button"
          onClick={() => fileInputRef.current?.click()}
          className="p-2 text-gray-500 hover:text-blue-600 hover:bg-blue-50 rounded-full transition-colors flex-shrink-0 border-none bg-transparent focus:outline-none"
          title="Add photo or video from library"
          disabled={isLoading}
          style={{
            border: 'none',
            background: 'transparent',
            boxShadow: 'none'
          }}
        >
          <ImageIcon className="w-5 h-5" />
        </button>

        {/* Text Input */}
        <div className="flex-1 relative">
          <textarea
            ref={textareaRef}
            value={content}
            onChange={handleTextareaChange}
            onKeyDown={handleKeyDown}
            placeholder="Type a message..."
            className="w-full resize-none border border-gray-300 rounded-2xl px-4 py-3 pr-12 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent min-h-[44px] message-textarea"
            rows={1}
            maxLength={1000}
            disabled={isLoading}
            style={{ lineHeight: '1.5' }}
          />

          {/* Send Button */}
          <button
            type="submit"
            disabled={!canSend}
            className={`absolute right-2 top-1/2 transform -translate-y-1/2 p-2 rounded-full transition-colors ${
              canSend
                ? 'text-blue-600 hover:bg-blue-50'
                : 'text-gray-400 cursor-not-allowed'
            }`}
          >
            <Send className="w-4 h-4" />
          </button>
        </div>
      </div>

      {/* Character Count */}
      {content.length > 800 && (
        <div className="text-right mt-1">
          <span className={`text-xs ${content.length > 950 ? 'text-red-500' : 'text-gray-500'}`}>
            {content.length}/1000
          </span>
        </div>
      )}
    </form>
  );
}
