'use client';

import React from 'react';
import { SystemTag } from '@/types';
import { Tag } from 'lucide-react';

export interface SystemTagDisplayProps {
  tags: SystemTag[];
  className?: string;
}

export function SystemTagDisplay({
  tags,
  className = '',
}: SystemTagDisplayProps) {
  if (!tags || tags.length === 0) {
    return null;
  }

  return (
    <div className={`flex flex-wrap gap-2 ${className}`}>
      {tags.map((tag) => (
        <span
          key={tag.id}
          className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium"
          style={{ 
            backgroundColor: `${tag.color}20`, 
            color: tag.color 
          }}
        >
          <Tag className="h-3 w-3 mr-1" />
          {tag.name}
        </span>
      ))}
    </div>
  );
}
