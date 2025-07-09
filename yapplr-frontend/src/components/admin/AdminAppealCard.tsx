'use client';

import React from 'react';
import { UserAppeal, AppealStatus, AppealType } from '@/types';
import { 
  Scale, 
  User, 
  Calendar, 
  AlertTriangle, 
  FileText, 
  Tag, 
  HelpCircle, 
  MessageSquare,
  ExternalLink 
} from 'lucide-react';
import { UserLink } from './UserLink';
import { StatusBadge } from './StatusBadge';

export interface AdminAppealCardProps {
  appeal: UserAppeal;
  onReview?: (appeal: UserAppeal) => void;
  showReviewButton?: boolean;
  className?: string;
}

export function AdminAppealCard({
  appeal,
  onReview,
  showReviewButton = true,
  className = '',
}: AdminAppealCardProps) {
  const getAppealTypeIcon = (type: AppealType) => {
    switch (type) {
      case AppealType.Suspension:
        return <AlertTriangle className="h-5 w-5 text-yellow-500" />;
      case AppealType.Ban:
        return <AlertTriangle className="h-5 w-5 text-red-500" />;
      case AppealType.ContentRemoval:
        return <FileText className="h-5 w-5 text-orange-500" />;
      case AppealType.SystemTag:
        return <Tag className="h-5 w-5 text-purple-500" />;
      case AppealType.Other:
        return <HelpCircle className="h-5 w-5 text-blue-500" />;
      default:
        return <Scale className="h-5 w-5 text-gray-500" />;
    }
  };

  const getAppealTypeName = (type: AppealType) => {
    switch (type) {
      case AppealType.Suspension:
        return 'Account Suspension';
      case AppealType.Ban:
        return 'Account Ban';
      case AppealType.ContentRemoval:
        return 'Content Removal';
      case AppealType.SystemTag:
        return 'Content Tag';
      case AppealType.Other:
        return 'Other';
      default:
        return 'Unknown';
    }
  };

  const getStatusBadge = (status: AppealStatus) => {
    switch (status) {
      case AppealStatus.Pending:
        return <span className="bg-yellow-100 text-yellow-800 px-2 py-1 rounded-full text-xs font-medium">Pending</span>;
      case AppealStatus.Approved:
        return <span className="bg-green-100 text-green-800 px-2 py-1 rounded-full text-xs font-medium">Approved</span>;
      case AppealStatus.Denied:
        return <span className="bg-red-100 text-red-800 px-2 py-1 rounded-full text-xs font-medium">Denied</span>;
      default:
        return null;
    }
  };

  return (
    <div className={`bg-white border border-gray-200 rounded-lg shadow-sm ${className}`}>
      <div className="p-6">
        {/* Header */}
        <div className="flex items-start justify-between mb-4">
          <div className="flex items-center space-x-3">
            {getAppealTypeIcon(appeal.type)}
            <div>
              <p className="font-medium text-gray-900">
                {getAppealTypeName(appeal.type)} Appeal
              </p>
              <div className="flex items-center text-sm text-gray-500 mt-1">
                <User className="h-4 w-4 mr-1" />
                <UserLink username={appeal.username} />
                <Calendar className="h-4 w-4 ml-3 mr-1" />
                <span>{new Date(appeal.createdAt).toLocaleDateString()}</span>
              </div>
            </div>
          </div>
          
          <div className="flex items-center space-x-3">
            {getStatusBadge(appeal.status)}
            {showReviewButton && appeal.status === AppealStatus.Pending && onReview && (
              <button
                onClick={() => onReview(appeal)}
                className="flex items-center px-3 py-1 bg-blue-100 text-blue-800 rounded-md hover:bg-blue-200 transition-colors"
              >
                <MessageSquare className="h-4 w-4 mr-1" />
                Review
              </button>
            )}
          </div>
        </div>

        {/* Content */}
        <div className="space-y-4">
          <div>
            <p className="text-sm font-medium text-gray-900 mb-2">Reason:</p>
            <div className="bg-gray-50 p-3 rounded-lg">
              <p className="text-sm text-gray-700">{appeal.reason}</p>
            </div>
          </div>

          {appeal.additionalInfo && (
            <div>
              <p className="text-sm font-medium text-gray-900 mb-2">Additional Information:</p>
              <div className="bg-gray-50 p-3 rounded-lg">
                <p className="text-sm text-gray-700">{appeal.additionalInfo}</p>
              </div>
            </div>
          )}

          {appeal.reviewNotes && (
            <div>
              <p className="text-sm font-medium text-gray-900 mb-2">Review Notes:</p>
              <div className="bg-gray-50 p-3 rounded-lg">
                <p className="text-sm text-gray-700">{appeal.reviewNotes}</p>
              </div>
            </div>
          )}

          {/* Related Content */}
          {(appeal.targetPostId || appeal.targetCommentId) && (
            <div className="text-sm text-gray-600 bg-gray-50 p-3 rounded-lg">
              <span className="font-medium">Related Content:</span>
              {appeal.targetPostId && (
                <a
                  href={`/yap/${appeal.targetPostId}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="ml-2 text-blue-600 hover:text-blue-800 inline-flex items-center gap-1"
                >
                  Post #{appeal.targetPostId}
                  <ExternalLink className="h-3 w-3" />
                </a>
              )}
              {appeal.targetCommentId && (
                <span className="ml-2 text-blue-600">
                  Comment #{appeal.targetCommentId}
                </span>
              )}
            </div>
          )}

          {/* Review Information */}
          {appeal.reviewedAt && (
            <div className="text-sm text-gray-500 pt-3 border-t border-gray-200">
              <strong>Reviewed:</strong> {new Date(appeal.reviewedAt).toLocaleDateString()}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
