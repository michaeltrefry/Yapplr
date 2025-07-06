'use client';

import { LinkPreview as LinkPreviewType } from '@/types';
import LinkPreview from './LinkPreview';

interface LinkPreviewListProps {
  linkPreviews: LinkPreviewType[];
  className?: string;
}

export default function LinkPreviewList({ linkPreviews, className = '' }: LinkPreviewListProps) {
  if (!linkPreviews || linkPreviews.length === 0) {
    return null;
  }

  return (
    <div className={`space-y-3 ${className}`}>
      {linkPreviews.map((linkPreview) => (
        <LinkPreview
          key={linkPreview.id}
          linkPreview={linkPreview}
        />
      ))}
    </div>
  );
}
