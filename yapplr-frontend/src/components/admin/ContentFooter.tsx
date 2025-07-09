'use client';

import React from 'react';
import { AdminPost, AdminComment } from '@/types';
import { Heart, MessageSquare, Repeat } from 'lucide-react';

export interface ContentFooterProps {
  content: AdminPost | AdminComment;
  contentType: 'post' | 'comment';
}

export function ContentFooter({
  content,
  contentType,
}: ContentFooterProps) {
  const isPost = contentType === 'post';
  const post = content as AdminPost;

  if (!isPost) {
    // Comments don't have engagement stats
    return null;
  }

  return (
    <div className="flex items-center justify-between pt-4 border-t border-gray-200 text-sm text-gray-500">
      <div className="flex space-x-6">
        <div className="flex items-center">
          <Heart className="h-4 w-4 mr-1" />
          <span>{post.likeCount || 0} likes</span>
        </div>
        <div className="flex items-center">
          <MessageSquare className="h-4 w-4 mr-1" />
          <span>{post.commentCount || 0} comments</span>
        </div>
        <div className="flex items-center">
          <Repeat className="h-4 w-4 mr-1" />
          <span>{post.repostCount || 0} reposts</span>
        </div>
      </div>
    </div>
  );
}
