'use client';

import { useState } from 'react';
import { Post } from '@/types';
import { X, Copy, Check, ExternalLink } from 'lucide-react';
import UserAvatar from './UserAvatar';

interface ShareModalProps {
  isOpen: boolean;
  onClose: () => void;
  post: Post;
}

export default function ShareModal({ isOpen, onClose, post }: ShareModalProps) {
  const [copied, setCopied] = useState(false);

  if (!isOpen) return null;

  // Generate the post URL
  const postUrl = `${window.location.origin}/yap/${post.id}`;

  // Social media sharing URLs
  const shareUrls = {
    twitter: `https://twitter.com/intent/tweet?text=${encodeURIComponent(
      `Check out this yap by @${post.user.username}`
    )}&url=${encodeURIComponent(postUrl)}`,
    facebook: `https://www.facebook.com/sharer/sharer.php?u=${encodeURIComponent(postUrl)}`,
    linkedin: `https://www.linkedin.com/sharing/share-offsite/?url=${encodeURIComponent(postUrl)}`,
    reddit: `https://reddit.com/submit?url=${encodeURIComponent(postUrl)}&title=${encodeURIComponent(
      `Yap by ${post.user.username}`
    )}`,
  };

  const handleCopyLink = async () => {
    try {
      await navigator.clipboard.writeText(postUrl);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (err) {
      console.error('Failed to copy link:', err);
    }
  };

  const handleSocialShare = (platform: keyof typeof shareUrls) => {
    window.open(shareUrls[platform], '_blank', 'width=600,height=400');
  };

  return (
    <div
      className="fixed inset-0 flex items-center justify-center z-50"
      style={{ backgroundColor: 'rgba(107, 114, 128, 0.3)' }}
      onClick={onClose}
    >
      <div
        className="bg-white dark:bg-gray-800 rounded-lg p-6 w-full max-w-md mx-4"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-xl font-semibold text-gray-900 dark:text-white">Share Post</h2>
          <button
            onClick={onClose}
            className="text-gray-400 dark:text-gray-500 hover:text-gray-600 dark:hover:text-gray-300 p-1 rounded-full hover:bg-gray-100 dark:hover:bg-gray-700"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Post Preview */}
        <div className="mb-6 p-4 bg-gray-50 dark:bg-gray-700 rounded-lg">
          <div className="flex items-center space-x-3 mb-2">
            <UserAvatar user={post.user} size="sm" />
            <div>
              <p className="font-semibold text-sm text-gray-900 dark:text-white">@{post.user.username}</p>
            </div>
          </div>
          <p className="text-sm text-gray-700 dark:text-gray-300 overflow-hidden" style={{
            display: '-webkit-box',
            WebkitLineClamp: 3,
            WebkitBoxOrient: 'vertical'
          }}>
            {post.content}
          </p>
        </div>

        {/* Copy Link */}
        <div className="mb-6">
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            Copy link to post
          </label>
          <div className="flex items-center space-x-2">
            <input
              type="text"
              value={postUrl}
              readOnly
              className="flex-1 px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md text-sm bg-gray-50 dark:bg-gray-700 text-gray-900 dark:text-white"
            />
            <button
              onClick={handleCopyLink}
              className="flex items-center space-x-1 px-3 py-2 bg-blue-500 dark:bg-blue-600 text-white rounded-md hover:bg-blue-600 dark:hover:bg-blue-700 transition-colors"
            >
              {copied ? (
                <>
                  <Check className="w-4 h-4" />
                  <span className="text-sm">Copied!</span>
                </>
              ) : (
                <>
                  <Copy className="w-4 h-4" />
                  <span className="text-sm">Copy</span>
                </>
              )}
            </button>
          </div>
        </div>

        {/* Social Media Sharing */}
        <div>
          <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">
            Share to social media
          </label>
          <div className="grid grid-cols-2 gap-3">
            <button
              onClick={() => handleSocialShare('twitter')}
              className="flex items-center justify-center space-x-2 p-3 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-blue-50 dark:hover:bg-blue-900/20 hover:border-blue-300 dark:hover:border-blue-500 transition-colors group"
            >
              <div className="w-5 h-5 bg-blue-400 rounded flex items-center justify-center">
                <span className="text-white text-xs font-bold">𝕏</span>
              </div>
              <span className="text-sm font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">Twitter</span>
              <ExternalLink className="w-4 h-4 text-gray-400 dark:text-gray-500 group-hover:text-blue-500 dark:group-hover:text-blue-400" />
            </button>

            <button
              onClick={() => handleSocialShare('facebook')}
              className="flex items-center justify-center space-x-2 p-3 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-blue-50 dark:hover:bg-blue-900/20 hover:border-blue-300 dark:hover:border-blue-500 transition-colors group"
            >
              <div className="w-5 h-5 bg-blue-600 rounded flex items-center justify-center">
                <span className="text-white text-xs font-bold">f</span>
              </div>
              <span className="text-sm font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">Facebook</span>
              <ExternalLink className="w-4 h-4 text-gray-400 dark:text-gray-500 group-hover:text-blue-500 dark:group-hover:text-blue-400" />
            </button>

            <button
              onClick={() => handleSocialShare('linkedin')}
              className="flex items-center justify-center space-x-2 p-3 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-blue-50 dark:hover:bg-blue-900/20 hover:border-blue-300 dark:hover:border-blue-500 transition-colors group"
            >
              <div className="w-5 h-5 bg-blue-700 rounded flex items-center justify-center">
                <span className="text-white text-xs font-bold">in</span>
              </div>
              <span className="text-sm font-medium text-gray-900 dark:text-white group-hover:text-blue-600 dark:group-hover:text-blue-400">LinkedIn</span>
              <ExternalLink className="w-4 h-4 text-gray-400 dark:text-gray-500 group-hover:text-blue-500 dark:group-hover:text-blue-400" />
            </button>

            <button
              onClick={() => handleSocialShare('reddit')}
              className="flex items-center justify-center space-x-2 p-3 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-orange-50 dark:hover:bg-orange-900/20 hover:border-orange-300 dark:hover:border-orange-500 transition-colors group"
            >
              <div className="w-5 h-5 bg-orange-500 rounded flex items-center justify-center">
                <span className="text-white text-xs font-bold">r</span>
              </div>
              <span className="text-sm font-medium text-gray-900 dark:text-white group-hover:text-orange-600 dark:group-hover:text-orange-400">Reddit</span>
              <ExternalLink className="w-4 h-4 text-gray-400 dark:text-gray-500 group-hover:text-orange-500 dark:group-hover:text-orange-400" />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
