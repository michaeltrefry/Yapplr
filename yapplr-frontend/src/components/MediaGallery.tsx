'use client';

import { useState } from 'react';
import { PostMedia, MediaType, VideoProcessingStatus, Post } from '@/types';
import { Play, ChevronLeft, ChevronRight, X } from 'lucide-react';
import Image from 'next/image';
import FullScreenPhotoViewer from './FullScreenPhotoViewer';
import { PhotoItem } from './PhotoGrid';

interface MediaGalleryProps {
  mediaItems: PostMedia[];
  post: Post;
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
    <div className="fixed inset-0 bg-background/95 backdrop-blur-sm z-50 flex items-center justify-center">
      {/* Close button */}
      <button
        onClick={onClose}
        className="absolute top-4 right-4 text-foreground hover:bg-surface/50 rounded-full p-2 transition-colors z-10"
      >
        <X className="w-8 h-8" />
      </button>

      {/* Navigation buttons */}
      {hasMultiple && (
        <>
          <button
            onClick={onPrevious}
            className="absolute left-4 top-1/2 transform -translate-y-1/2 text-foreground hover:bg-surface/50 rounded-full p-2 transition-colors z-10 disabled:opacity-50"
            disabled={currentIndex === 0}
          >
            <ChevronLeft className="w-8 h-8" />
          </button>
          <button
            onClick={onNext}
            className="absolute right-4 top-1/2 transform -translate-y-1/2 text-foreground hover:bg-surface/50 rounded-full p-2 transition-colors z-10 disabled:opacity-50"
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
        ) : currentMedia.mediaType === MediaType.Gif && currentMedia.gifUrl ? (
          <div className="relative">
            <img
              src={currentMedia.gifUrl}
              alt={`GIF ${currentIndex + 1}`}
              className="max-w-full max-h-full object-contain"
            />
            <div className="absolute bottom-2 left-2 bg-black bg-opacity-60 text-white text-xs px-2 py-1 rounded">
              GIF
            </div>
          </div>
        ) : null}
      </div>

      {/* Counter */}
      {hasMultiple && (
        <div className="absolute bottom-4 left-1/2 transform -translate-x-1/2 bg-black/50 text-white px-3 py-1 rounded-full text-sm backdrop-blur-sm">
          {currentIndex + 1} of {mediaItems.length}
        </div>
      )}
    </div>
  );
}

export default function MediaGallery({ mediaItems, post, className = '' }: MediaGalleryProps) {
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

  // Convert media items to PhotoItem format for FullScreenPhotoViewer
  const allPhotos: PhotoItem[] = mediaItems
    .filter(item =>
      (item.mediaType === MediaType.Image && item.imageUrl) ||
      (item.mediaType === MediaType.Gif && item.gifUrl)
    )
    .map(item => ({
      post,
      mediaItem: item,
      imageUrl: item.mediaType === MediaType.Gif ? item.gifUrl! : item.imageUrl!
    }));

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
        ) : media.mediaType === MediaType.Gif && media.gifUrl ? (
          <div className="cursor-pointer relative" onClick={() => openViewer(0)}>
            <img
              src={media.gifUrl}
              alt="GIF"
              className="max-w-full h-auto rounded-lg border border-gray-200 hover:opacity-95 transition-opacity"
            />
            <div className="absolute bottom-2 left-2 bg-black bg-opacity-60 text-white text-xs px-2 py-1 rounded">
              GIF
            </div>
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

        {/* Use FullScreenPhotoViewer for images and GIFs with post actions */}
        {viewerIndex !== null && (mediaItems[viewerIndex]?.mediaType === MediaType.Image || mediaItems[viewerIndex]?.mediaType === MediaType.Gif) && (
          <FullScreenPhotoViewer
            post={post}
            mediaItem={mediaItems[viewerIndex]}
            allPhotos={allPhotos}
            currentIndex={allPhotos.findIndex(photo => photo.mediaItem.id === mediaItems[viewerIndex].id)}
            isOpen={true}
            onClose={closeViewer}
            onNavigate={(newIndex) => {
              // Find the media item index from the photo index
              const photoItem = allPhotos[newIndex];
              const mediaIndex = mediaItems.findIndex(item => item.id === photoItem.mediaItem.id);
              if (mediaIndex !== -1) {
                setViewerIndex(mediaIndex);
              }
            }}
          />
        )}

        {/* Use MediaViewer for videos */}
        {viewerIndex !== null && mediaItems[viewerIndex]?.mediaType === MediaType.Video && (
          <MediaViewer
            mediaItems={mediaItems}
            currentIndex={viewerIndex}
            isOpen={true}
            onClose={closeViewer}
            onNext={nextMedia}
            onPrevious={previousMedia}
          />
        )}
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
            ) : media.mediaType === MediaType.Gif && media.gifPreviewUrl ? (
              <div className="w-full h-full min-h-[120px] relative">
                <img
                  src={media.gifPreviewUrl}
                  alt={`GIF ${index + 1}`}
                  className="w-full h-full object-cover group-hover:opacity-95 transition-opacity"
                />
                <div className="absolute bottom-1 left-1 bg-black bg-opacity-60 text-white text-xs px-1 py-0.5 rounded">
                  GIF
                </div>
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

      {/* Use FullScreenPhotoViewer for images and GIFs with post actions */}
      {viewerIndex !== null && (mediaItems[viewerIndex]?.mediaType === MediaType.Image || mediaItems[viewerIndex]?.mediaType === MediaType.Gif) && (
        <FullScreenPhotoViewer
          post={post}
          mediaItem={mediaItems[viewerIndex]}
          allPhotos={allPhotos}
          currentIndex={allPhotos.findIndex(photo => photo.mediaItem.id === mediaItems[viewerIndex].id)}
          isOpen={true}
          onClose={closeViewer}
          onNavigate={(newIndex) => {
            // Find the media item index from the photo index
            const photoItem = allPhotos[newIndex];
            const mediaIndex = mediaItems.findIndex(item => item.id === photoItem.mediaItem.id);
            if (mediaIndex !== -1) {
              setViewerIndex(mediaIndex);
            }
          }}
        />
      )}

      {/* Use MediaViewer for videos */}
      {viewerIndex !== null && mediaItems[viewerIndex]?.mediaType === MediaType.Video && (
        <MediaViewer
          mediaItems={mediaItems}
          currentIndex={viewerIndex}
          isOpen={true}
          onClose={closeViewer}
          onNext={nextMedia}
          onPrevious={previousMedia}
        />
      )}
    </div>
  );
}
