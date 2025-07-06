'use client';

import { useState, useRef } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { messageApi, imageApi } from '@/lib/api';
import { useNotifications } from '@/contexts/NotificationContext';
import { Send, Image as ImageIcon, X } from 'lucide-react';
import Image from 'next/image';

interface MessageComposerProps {
  conversationId: number;
}

export default function MessageComposer({ conversationId }: MessageComposerProps) {
  const [content, setContent] = useState('');
  const [imagePreview, setImagePreview] = useState<string | null>(null);
  const [uploadedFileName, setUploadedFileName] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const queryClient = useQueryClient();
  const { refreshUnreadCount } = useNotifications();

  const uploadImageMutation = useMutation({
    mutationFn: imageApi.uploadImage,
    onSuccess: (data) => {
      setUploadedFileName(data.fileName);
    },
  });

  const sendMessageMutation = useMutation({
    mutationFn: messageApi.sendMessageToConversation,
    onSuccess: () => {
      setContent('');
      setImagePreview(null);
      setUploadedFileName(null);
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
    setImagePreview(null);
    setUploadedFileName(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    // Must have either content or image
    if (!content.trim() && !uploadedFileName) return;

    sendMessageMutation.mutate({
      conversationId,
      content: content.trim() || undefined,
      imageFileName: uploadedFileName || undefined,
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

  const isLoading = sendMessageMutation.isPending || uploadImageMutation.isPending;
  const canSend = (content.trim() || uploadedFileName) && !isLoading;

  return (
    <form onSubmit={handleSubmit} className="p-4">
      {/* Image Preview */}
      {imagePreview && (
        <div className="mb-3 relative inline-block">
          <div className="relative rounded-lg overflow-hidden border border-gray-200">
            <Image
              src={imagePreview}
              alt="Image preview"
              width={200}
              height={150}
              className="object-cover"
            />
            <button
              type="button"
              onClick={removeImage}
              className="absolute top-2 right-2 bg-black bg-opacity-50 text-white rounded-full p-1 hover:bg-opacity-70 transition-opacity"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
          {uploadImageMutation.isPending && (
            <div className="absolute inset-0 bg-black bg-opacity-50 flex items-center justify-center rounded-lg">
              <div className="text-white text-sm">Uploading...</div>
            </div>
          )}
        </div>
      )}

      {/* Input Area */}
      <div className="flex items-end space-x-3">
        {/* Hidden File Input */}
        <input
          ref={fileInputRef}
          type="file"
          accept="image/*"
          onChange={handleFileSelect}
          className="hidden"
        />

        {/* Image Upload Button */}
        <button
          type="button"
          onClick={() => fileInputRef.current?.click()}
          className="p-2 text-gray-500 hover:text-blue-600 hover:bg-blue-50 rounded-full transition-colors flex-shrink-0 border-none bg-transparent focus:outline-none"
          title="Add image"
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
