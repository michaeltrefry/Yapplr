'use client';

import { parseContentParts, type ContentPart } from '@/utils/gifUtils';
import { ContentHighlight } from '@/utils/contentUtils';
import { MentionHighlight } from '@/utils/mentionUtils';

interface ContentWithGifsProps {
  content: string;
  className?: string;
  maxGifWidth?: number;
  highlightType?: 'content' | 'mention';
}

export default function ContentWithGifs({ content, className = '', maxGifWidth = 200, highlightType = 'content' }: ContentWithGifsProps) {
  const parts = parseContentParts(content);

  // Debug logging
  if (content.includes('[GIF:')) {
    console.log('ContentWithGifs - Processing content with GIF:', content);
    console.log('ContentWithGifs - Parsed parts:', parts);
  }

  return (
    <div className={className}>
      {parts.map((part, index) => {
        if (part.type === 'text') {
          return (
            <span key={index}>
              {highlightType === 'mention' ? (
                <MentionHighlight content={part.content} />
              ) : (
                <ContentHighlight content={part.content} />
              )}
            </span>
          );
        } else if (part.type === 'gif' && part.gif) {
          const { gif } = part;
          // Calculate display dimensions while maintaining aspect ratio
          const aspectRatio = gif.height / gif.width;
          const displayWidth = Math.min(gif.width, maxGifWidth);
          const displayHeight = displayWidth * aspectRatio;

          return (
            <div key={index} className="inline-block my-2">
              <div className="relative inline-block">
                <img
                  src={gif.previewUrl}
                  alt={gif.title}
                  className="rounded border border-gray-200 cursor-pointer hover:opacity-90 transition-opacity"
                  style={{
                    width: displayWidth,
                    height: displayHeight,
                    maxWidth: '100%',
                  }}
                  onClick={() => {
                    // Open full-size GIF in new tab
                    window.open(gif.url, '_blank');
                  }}
                  loading="lazy"
                />
                <div className="absolute bottom-1 left-1 bg-black bg-opacity-60 text-white text-xs px-1 py-0.5 rounded">
                  GIF
                </div>
              </div>
            </div>
          );
        }
        return null;
      })}
    </div>
  );
}
