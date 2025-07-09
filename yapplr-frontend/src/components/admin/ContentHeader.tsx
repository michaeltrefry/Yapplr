'use client';

import React from 'react';
import { AdminPost, AdminComment } from '@/types';
import { User, Calendar, Eye, EyeOff, Tag, ExternalLink } from 'lucide-react';
import { format } from 'date-fns';
import { StatusBadge } from './StatusBadge';
import { UserLink } from './UserLink';

export interface ContentHeaderProps {
  content: AdminPost | AdminComment;
  contentType: 'post' | 'comment';
  isSelected?: boolean;
  onSelect?: (id: number) => void;
  onActionClick: (action: 'hide' | 'unhide' | 'tag') => void;
  actionLoading?: boolean;
}

export function ContentHeader({
  content,
  contentType,
  isSelected = false,
  onSelect,
  onActionClick,
  actionLoading = false,
}: ContentHeaderProps) {
  return (
    <div className="flex items-start justify-between mb-4">
      {/* Left side: Checkbox, User Info, Timestamp */}
      <div className="flex items-start space-x-3">
        {onSelect && (
          <input
            type="checkbox"
            checked={isSelected}
            onChange={() => onSelect(content.id)}
            className="mt-1 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
          />
        )}
        
        <div className="flex items-center space-x-3">
          <User className="h-5 w-5 text-gray-400 flex-shrink-0" />
          <div>
            <UserLink username={content.user.username} />
            <div className="flex items-center text-xs text-gray-500 mt-1">
              <Calendar className="h-3 w-3 mr-1" />
              {format(new Date(content.createdAt), 'MMM d, yyyy HH:mm')}
            </div>
          </div>
        </div>
      </div>

      {/* Right side: Status and Actions */}
      <div className="flex items-center space-x-3">
        <StatusBadge 
          isHidden={content.isHidden}
          hiddenReason={content.hiddenReason}
        />
        
        <div className="flex space-x-2">
          {content.isHidden ? (
            <button
              onClick={() => onActionClick('unhide')}
              disabled={actionLoading}
              className="flex items-center px-3 py-1 bg-green-100 text-green-800 rounded-md hover:bg-green-200 transition-colors disabled:opacity-50"
            >
              <Eye className="h-4 w-4 mr-1" />
              Unhide
            </button>
          ) : (
            <button
              onClick={() => onActionClick('hide')}
              disabled={actionLoading}
              className="flex items-center px-3 py-1 bg-yellow-100 text-yellow-800 rounded-md hover:bg-yellow-200 transition-colors disabled:opacity-50"
            >
              <EyeOff className="h-4 w-4 mr-1" />
              Hide
            </button>
          )}
          
          <button
            onClick={() => onActionClick('tag')}
            disabled={actionLoading}
            className="flex items-center px-3 py-1 bg-purple-100 text-purple-800 rounded-md hover:bg-purple-200 transition-colors disabled:opacity-50"
          >
            <Tag className="h-4 w-4 mr-1" />
            Tag
          </button>
        </div>
      </div>
    </div>
  );
}
