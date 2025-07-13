'use client';

import { LinkPreview as LinkPreviewType, LinkPreviewStatus } from '@/types';
import { ExternalLink, AlertTriangle, Clock, Globe } from 'lucide-react';
import Image from 'next/image';

interface LinkPreviewProps {
  linkPreview: LinkPreviewType;
  className?: string;
}

export default function LinkPreview({ linkPreview, className = '' }: LinkPreviewProps) {
  const { url, title, description, imageUrl, siteName, youTubeVideoId, status, errorMessage } = linkPreview;

  // Don't render anything for pending status
  if (status === LinkPreviewStatus.Pending) {
    return (
      <div className={`border border-gray-200 rounded-lg p-3 bg-gray-50 ${className}`}>
        <div className="flex items-center space-x-2 text-gray-500">
          <Clock className="w-4 h-4 animate-spin" />
          <span className="text-sm">Loading preview...</span>
        </div>
      </div>
    );
  }

  // Handle error states
  if (status !== LinkPreviewStatus.Success) {
    return (
      <div className={`border border-red-200 rounded-lg p-3 bg-red-50 ${className}`}>
        <div className="flex items-start space-x-2">
          <AlertTriangle className="w-4 h-4 text-red-500 mt-0.5 flex-shrink-0" />
          <div className="flex-1 min-w-0">
            <div className="flex items-center space-x-2">
              <ExternalLink className="w-3 h-3 text-red-500" />
              <a
                href={url}
                target="_blank"
                rel="noopener noreferrer"
                className="text-sm text-red-700 hover:text-red-900 hover:underline font-medium truncate"
              >
                {url}
              </a>
            </div>
            <p className="text-xs text-red-600 mt-1">
              {getErrorMessage(status, errorMessage)}
            </p>
          </div>
        </div>
      </div>
    );
  }

  // Success state - render full preview
  // If this is a YouTube video, render the embedded player instead of a regular preview
  if (youTubeVideoId) {
    return (
      <div className={`border border-gray-200 rounded-lg overflow-hidden ${className}`}>
        <div className="relative w-full" style={{ paddingBottom: '56.25%' /* 16:9 aspect ratio */ }}>
          <iframe
            src={`https://www.youtube.com/embed/${youTubeVideoId}`}
            title={title || 'YouTube video'}
            className="absolute top-0 left-0 w-full h-full"
            frameBorder="0"
            allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
            allowFullScreen
          />
        </div>

        <div className="p-3">
          <div className="flex items-center space-x-2 text-gray-500 text-xs mb-2">
            <Globe className="w-3 h-3" />
            <span className="truncate">YouTube</span>
            <a
              href={url}
              target="_blank"
              rel="noopener noreferrer"
              className="text-gray-500 hover:text-gray-700"
            >
              <ExternalLink className="w-3 h-3" />
            </a>
          </div>

          {title && (
            <h3 className="font-semibold text-gray-900 text-sm mb-1 overflow-hidden" style={{
              display: '-webkit-box',
              WebkitLineClamp: 2,
              WebkitBoxOrient: 'vertical'
            }}>
              {title}
            </h3>
          )}

          {description && (
            <p className="text-gray-600 text-xs overflow-hidden" style={{
              display: '-webkit-box',
              WebkitLineClamp: 2,
              WebkitBoxOrient: 'vertical'
            }}>
              {description}
            </p>
          )}
        </div>
      </div>
    );
  }

  // Regular link preview for non-YouTube links
  return (
    <a
      href={url}
      target="_blank"
      rel="noopener noreferrer"
      className={`block border border-gray-200 rounded-lg overflow-hidden hover:border-gray-300 transition-colors ${className}`}
    >
      {imageUrl && (
        <div className="relative w-full h-48 bg-gray-100">
          <Image
            src={imageUrl}
            alt={title || 'Link preview'}
            fill
            className="object-cover"
            onError={(e) => {
              // Hide image if it fails to load
              e.currentTarget.style.display = 'none';
            }}
          />
        </div>
      )}

      <div className="p-3">
        <div className="flex items-center space-x-2 text-gray-500 text-xs mb-2">
          <Globe className="w-3 h-3" />
          <span className="truncate">
            {siteName || new URL(url).hostname}
          </span>
          <ExternalLink className="w-3 h-3" />
        </div>

        {title && (
          <h3 className="font-semibold text-gray-900 text-sm mb-1 overflow-hidden" style={{
            display: '-webkit-box',
            WebkitLineClamp: 2,
            WebkitBoxOrient: 'vertical'
          }}>
            {title}
          </h3>
        )}

        {description && (
          <p className="text-gray-600 text-xs overflow-hidden" style={{
            display: '-webkit-box',
            WebkitLineClamp: 2,
            WebkitBoxOrient: 'vertical'
          }}>
            {description}
          </p>
        )}

        {!title && !description && (
          <p className="text-gray-600 text-xs truncate">
            {url}
          </p>
        )}
      </div>
    </a>
  );
}

function getErrorMessage(status: LinkPreviewStatus, errorMessage?: string): string {
  if (errorMessage) {
    return errorMessage;
  }

  switch (status) {
    case LinkPreviewStatus.NotFound:
      return 'Page not found (404)';
    case LinkPreviewStatus.Unauthorized:
      return 'Authentication required (401)';
    case LinkPreviewStatus.Forbidden:
      return 'Access forbidden (403)';
    case LinkPreviewStatus.Timeout:
      return 'Request timed out';
    case LinkPreviewStatus.NetworkError:
      return 'Network error occurred';
    case LinkPreviewStatus.InvalidUrl:
      return 'Invalid URL format';
    case LinkPreviewStatus.TooLarge:
      return 'Content too large';
    case LinkPreviewStatus.UnsupportedContent:
      return 'Unsupported content type';
    default:
      return 'Unable to load preview';
  }
}
