'use client';

import { useState } from 'react';
import { PostMedia, MediaType, VideoProcessingStatus } from '@/types';
import { Play, ChevronLeft, ChevronRight, X } from 'lucide-react';
import Image from 'next/image';

interface MediaGalleryProps {
  mediaItems: PostMedia[];
  className?: string;
}

interface MediaViewerProps {
  mediaItems: PostMedia[];
  currentIndex: number;
  isOpen: boolean;
  onClose: () => void;
  onNext: () => void;
  onPrevious: () => void;
}

function MediaViewer({ mediaItems, currentIndex, isOpen, onClose, onNext, onPrevious }: MediaViewerProps) {
  if (!isOpen || !mediaItems[currentIndex]) return null;

  const currentMedia = mediaItems[currentIndex];
  const hasMultiple = mediaItems.length > 1;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-90 z-50 flex items-center justify-center">
      {/* Close button */}
      <button
        onClick={onClose}
        className="absolute top-4 right-4 text-white hover:text-gray-300 z-10"
      >
        <X className="w-8 h-8" />
      </button>

      {/* Navigation buttons */}
      {hasMultiple && (
        <>
          <button
            onClick={onPrevious}
            className="absolute left-4 top-1/2 transform -translate-y-1/2 text-white hover:text-gray-300 z-10"
            disabled={currentIndex === 0}
          >
            <ChevronLeft className="w-8 h-8" />
          </button>
          <button
            onClick={onNext}
            className="absolute right-4 top-1/2 transform -translate-y-1/2 text-white hover:text-gray-300 z-10"
            disabled={currentIndex === mediaItems.length - 1}
          >
            <ChevronRight className="w-8 h-8" />
          </button>
        </>
      )}

      {/* Media content */}
      <div className="max-w-full max-h-full p-4">
        {currentMedia.mediaType === MediaType.Image && currentMedia.imageUrl ? (
          <img
            src={currentMedia.imageUrl}
            alt={`Media ${currentIndex + 1}`}
            className="max-w-full max-h-full object-contain"
          />
        ) : currentMedia.mediaType === MediaType.Video && currentMedia.videoUrl ? (
          <video
            src={currentMedia.videoUrl}
            poster={currentMedia.videoThumbnailUrl}
            controls
            className="max-w-full max-h-full"
            autoPlay
          >
            Your browser does not support the video tag.
          </video>
        ) : null}
      </div>

      {/* Counter */}
      {hasMultiple && (
        <div className="absolute bottom-4 left-1/2 transform -translate-x-1/2 text-white text-sm">
          {currentIndex + 1} of {mediaItems.length}
        </div>
      )}
    </div>
  );
}

export default function MediaGallery({ mediaItems, className = '' }: MediaGalleryProps) {
  const [viewerIndex, setViewerIndex] = useState<number | null>(null);

  if (!mediaItems || mediaItems.length === 0) return null;

  const openViewer = (index: number) => {
    setViewerIndex(index);
  };

  const closeViewer = () => {
    setViewerIndex(null);
  };

  const nextMedia = () => {
    if (viewerIndex !== null && viewerIndex < mediaItems.length - 1) {
      setViewerIndex(viewerIndex + 1);
    }
  };

  const previousMedia = () => {
    if (viewerIndex !== null && viewerIndex > 0) {
      setViewerIndex(viewerIndex - 1);
    }
  };

  // Single media item
  if (mediaItems.length === 1) {
    const media = mediaItems[0];
    return (
      <div className={`mt-3 ${className}`}>
        {media.mediaType === MediaType.Image && media.imageUrl ? (
          <div className="cursor-pointer" onClick={() => openViewer(0)}>
            <img
              src={media.imageUrl}
              alt="Post media"
              className="max-w-full h-auto rounded-lg border border-gray-200 hover:opacity-95 transition-opacity"
            />
          </div>
        ) : media.mediaType === MediaType.Video ? (
          <div>
            {media.videoProcessingStatus === VideoProcessingStatus.Completed && media.videoUrl ? (
              <video
                src={media.videoUrl}
                poster={media.videoThumbnailUrl}
                controls
                className="max-w-full h-auto rounded-lg border border-gray-200"
                style={{ maxHeight: '400px' }}
              >
                Your browser does not support the video tag.
              </video>
            ) : (
              <div className="p-4 bg-gray-50 rounded-lg border border-gray-200">
                <div className="flex items-center space-x-2">
                  <Play className="w-5 h-5 text-gray-400" />
                  <div>
                    {media.videoProcessingStatus === VideoProcessingStatus.Processing && (
                      <p className="text-sm text-gray-600">Video is processing...</p>
                    )}
                    {media.videoProcessingStatus === VideoProcessingStatus.Failed && (
                      <p className="text-sm text-red-600">Video processing failed</p>
                    )}
                    {media.videoProcessingStatus === VideoProcessingStatus.Pending && (
                      <p className="text-sm text-gray-600">Video is queued for processing...</p>
                    )}
                  </div>
                </div>
              </div>
            )}
          </div>
        ) : null}

        <MediaViewer
          mediaItems={mediaItems}
          currentIndex={viewerIndex || 0}
          isOpen={viewerIndex !== null}
          onClose={closeViewer}
          onNext={nextMedia}
          onPrevious={previousMedia}
        />
      </div>
    );
  }

  // Multiple media items - gallery layout
  const getGridClass = () => {
    if (mediaItems.length === 2) return 'grid-cols-2';
    if (mediaItems.length === 3) return 'grid-cols-3';
    if (mediaItems.length === 4) return 'grid-cols-2';
    return 'grid-cols-3'; // 5+ items
  };

  const getItemClass = (index: number) => {
    if (mediaItems.length === 4 && index >= 2) return 'col-span-1';
    if (mediaItems.length >= 5 && index === 0) return 'col-span-2 row-span-2';
    return 'col-span-1';
  };

  return (
    <div className={`mt-3 ${className}`}>
      <div className={`grid ${getGridClass()} gap-1 max-h-96 overflow-hidden rounded-lg`}>
        {mediaItems.slice(0, 6).map((media, index) => (
          <div
            key={media.id}
            className={`relative cursor-pointer group ${getItemClass(index)} ${
              index >= 5 ? 'hidden' : ''
            }`}
            onClick={() => openViewer(index)}
          >
            {media.mediaType === MediaType.Image && media.imageUrl ? (
              <div className="w-full h-full min-h-[120px]">
                <img
                  src={media.imageUrl}
                  alt={`Media ${index + 1}`}
                  className="w-full h-full object-cover group-hover:opacity-95 transition-opacity"
                />
              </div>
            ) : media.mediaType === MediaType.Video && media.videoThumbnailUrl ? (
              <div className="w-full h-full min-h-[120px] relative">
                <img
                  src={media.videoThumbnailUrl}
                  alt={`Video ${index + 1}`}
                  className="w-full h-full object-cover group-hover:opacity-95 transition-opacity"
                />
                <div className="absolute inset-0 flex items-center justify-center">
                  <Play className="w-8 h-8 text-white drop-shadow-lg" />
                </div>
              </div>
            ) : (
              <div className="w-full h-full min-h-[120px] bg-gray-100 flex items-center justify-center">
                <Play className="w-8 h-8 text-gray-400" />
              </div>
            )}

            {/* Show count overlay for last visible item if there are more */}
            {index === 5 && mediaItems.length > 6 && (
              <div className="absolute inset-0 bg-black bg-opacity-50 flex items-center justify-center">
                <span className="text-white text-lg font-semibold">
                  +{mediaItems.length - 6}
                </span>
              </div>
            )}
          </div>
        ))}
      </div>

      <MediaViewer
        mediaItems={mediaItems}
        currentIndex={viewerIndex || 0}
        isOpen={viewerIndex !== null}
        onClose={closeViewer}
        onNext={nextMedia}
        onPrevious={previousMedia}
      />
    </div>
  );
}
