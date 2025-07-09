'use client';

import React from 'react';
import { ExternalLink } from 'lucide-react';

export interface UserLinkProps {
  username: string;
  className?: string;
}

export function UserLink({
  username,
  className = '',
}: UserLinkProps) {
  return (
    <a
      href={`/profile/${username}`}
      target="_blank"
      rel="noopener noreferrer"
      className={`font-medium text-blue-600 hover:text-blue-800 inline-flex items-center gap-1 ${className}`}
    >
      @{username}
      <ExternalLink className="h-3 w-3" />
    </a>
  );
}
