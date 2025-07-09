'use client';

import React from 'react';

export interface StatusBadgeProps {
  isHidden: boolean;
  hiddenReason?: string;
  className?: string;
}

export function StatusBadge({
  isHidden,
  hiddenReason,
  className = '',
}: StatusBadgeProps) {
  if (!isHidden) {
    return (
      <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-800 ${className}`}>
        Visible
      </span>
    );
  }

  return (
    <div className="text-right">
      <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium bg-red-100 text-red-800 ${className}`}>
        Hidden
      </span>
      {hiddenReason && (
        <div className="text-xs text-gray-500 mt-1 max-w-32 truncate" title={hiddenReason}>
          {hiddenReason}
        </div>
      )}
    </div>
  );
}
