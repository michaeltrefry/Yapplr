'use client';

import React, { useState } from 'react';
import { UserReport, UserReportStatus } from '@/types';
import {
  Flag,
  User,
  Calendar,
  ExternalLink,
  EyeOff,
  X
} from 'lucide-react';
import { format } from 'date-fns';
import { UserLink } from './UserLink';
import { SystemTagDisplay } from './SystemTagDisplay';
import { InlineActionForm } from './InlineActionForm';

export interface AdminUserReportCardProps {
  report: UserReport;
  onHideContent?: (contentId: number, contentType: 'post' | 'comment', reason: string) => Promise<void>;
  onDismissReport?: (reportId: number, notes: string) => Promise<void>;
  actionLoading?: boolean;
  className?: string;
}

export function AdminUserReportCard({
  report,
  onHideContent,
  onDismissReport,
  actionLoading = false,
  className = '',
}: AdminUserReportCardProps) {
  const [showHideForm, setShowHideForm] = useState(false);
  const [showDismissForm, setShowDismissForm] = useState(false);

  const getStatusBadge = (status: UserReportStatus) => {
    switch (status) {
      case UserReportStatus.Pending:
        return <span className="bg-yellow-100 text-yellow-800 px-2 py-1 rounded-full text-xs font-medium">Pending</span>;
      case UserReportStatus.Reviewed:
        return <span className="bg-blue-100 text-blue-800 px-2 py-1 rounded-full text-xs font-medium">Reviewed</span>;
      case UserReportStatus.Dismissed:
        return <span className="bg-gray-100 text-gray-800 px-2 py-1 rounded-full text-xs font-medium">Dismissed</span>;
      case UserReportStatus.ActionTaken:
        return <span className="bg-green-100 text-green-800 px-2 py-1 rounded-full text-xs font-medium">Action Taken</span>;
      default:
        return null;
    }
  };

  const getContentLink = () => {
    if (report.post) {
      return `/yap/${report.post.id}`;
    } else if (report.comment) {
      return `/yap/${report.comment.postId}#comment-${report.comment.id}`;
    }
    return '#';
  };

  const getContentType = (): 'post' | 'comment' => {
    return report.post ? 'post' : 'comment';
  };

  const getContentId = (): number => {
    return report.post?.id || report.comment?.id || 0;
  };

  const getContentAuthor = () => {
    return report.post?.user.username || report.comment?.user.username || 'Unknown';
  };

  const getContent = () => {
    return report.post?.content || report.comment?.content || 'Content not available';
  };

  const isContentHidden = () => {
    return report.post?.isHidden || report.comment?.isHidden || false;
  };

  const handleHideClick = () => {
    setShowHideForm(true);
  };

  const handleDismissClick = () => {
    setShowDismissForm(true);
  };

  const handleHideSubmit = async (reason: string) => {
    try {
      if (onHideContent) {
        await onHideContent(getContentId(), getContentType(), reason);
      }
      setShowHideForm(false);
    } catch (error) {
      console.error('Failed to hide content:', error);
    }
  };

  const handleDismissSubmit = async (notes: string) => {
    try {
      if (onDismissReport) {
        await onDismissReport(report.id, notes);
      }
      setShowDismissForm(false);
    } catch (error) {
      console.error('Failed to dismiss report:', error);
    }
  };

  const handleCancel = () => {
    setShowHideForm(false);
    setShowDismissForm(false);
  };

  return (
    <div className={`bg-white border border-gray-200 rounded-lg shadow-sm ${className}`}>
      <div className="p-6">
        {/* Header */}
        <div className="flex items-start justify-between mb-4">
          <div className="flex items-start space-x-3">
            <Flag className="h-5 w-5 text-red-500 flex-shrink-0 mt-1" />
            <div>
              <div className="flex items-center space-x-3">
                <User className="h-5 w-5 text-gray-400 flex-shrink-0" />
                <div>
                  <div className="flex items-center space-x-2">
                    <span className="font-medium text-gray-900">Report #{report.id}</span>
                    <span className="text-gray-500">by</span>
                    <UserLink username={report.reportedByUsername} />
                  </div>
                  <div className="flex items-center text-xs text-gray-500 mt-1">
                    <Calendar className="h-3 w-3 mr-1" />
                    {format(new Date(report.createdAt), 'MMM d, yyyy HH:mm')}
                  </div>
                </div>
              </div>
            </div>
          </div>
          
          {/* Right side: Status and Actions */}
          <div className="flex items-center space-x-3">
            {getStatusBadge(report.status)}

            {/* Action Buttons */}
            {report.status === UserReportStatus.Pending && (
              <div className="flex space-x-2">
                {!isContentHidden() && (
                  <button
                    onClick={handleHideClick}
                    disabled={actionLoading}
                    className="flex items-center px-3 py-1 bg-red-100 text-red-800 rounded-md hover:bg-red-200 transition-colors disabled:opacity-50"
                  >
                    <EyeOff className="h-4 w-4 mr-1" />
                    Hide Content
                  </button>
                )}
                <button
                  onClick={handleDismissClick}
                  disabled={actionLoading}
                  className="flex items-center px-3 py-1 bg-gray-100 text-gray-800 rounded-md hover:bg-gray-200 transition-colors disabled:opacity-50"
                >
                  <X className="h-4 w-4 mr-1" />
                  Dismiss
                </button>
              </div>
            )}
          </div>
        </div>

        {/* Reported Content */}
        <div className="space-y-4">
          <div className="bg-gray-50 p-3 rounded-lg">
            <div className="flex items-center justify-between mb-2">
              <span className="text-sm font-medium text-gray-700">
                Reported {report.post ? 'Post' : 'Comment'}:
              </span>
              <div className="flex items-center space-x-2">
                {isContentHidden() && (
                  <span className="bg-red-100 text-red-800 px-2 py-1 rounded-full text-xs font-medium">
                    Hidden
                  </span>
                )}
                <a
                  href={getContentLink()}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-blue-600 hover:text-blue-800 text-sm flex items-center gap-1"
                >
                  <span>View {report.post ? 'Post' : 'Comment'}</span>
                  <ExternalLink className="h-3 w-3" />
                </a>
              </div>
            </div>
            <a
              href={getContentLink()}
              target="_blank"
              rel="noopener noreferrer"
              className="text-gray-900 hover:text-blue-600 block group"
            >
              <div className="flex items-start gap-2">
                <span className="flex-1 text-sm leading-relaxed">
                  {getContent()}
                </span>
                <ExternalLink className="h-4 w-4 text-gray-400 group-hover:text-blue-600 flex-shrink-0 mt-0.5" />
              </div>
            </a>
            <div className="flex items-center space-x-2 text-sm text-gray-500 mt-2">
              <User className="h-4 w-4" />
              <span>by</span>
              <UserLink username={getContentAuthor()} />
            </div>
          </div>
        </div>

        {/* Report Details */}
        <div className="space-y-3">
          <div>
            <span className="text-sm font-medium text-gray-700">Reason:</span>
            <p className="text-gray-900 mt-1">{report.reason}</p>
          </div>

          {report.systemTags.length > 0 && (
            <div>
              <span className="text-sm font-medium text-gray-700 mb-2 block">Selected Categories:</span>
              <SystemTagDisplay tags={report.systemTags} />
            </div>
          )}

          {report.reviewNotes && (
            <div>
              <span className="text-sm font-medium text-gray-700">Review Notes:</span>
              <p className="text-gray-900 mt-1">{report.reviewNotes}</p>
              <p className="text-sm text-gray-500 mt-1">
                Reviewed by @{report.reviewedByUsername} on{' '}
                {report.reviewedAt && new Date(report.reviewedAt).toLocaleDateString()}
              </p>
            </div>
          )}



          {/* Inline Action Forms */}
          {showHideForm && (
            <InlineActionForm
              actionType="hide"
              contentType={report.post ? 'post' : 'comment'}
              onSubmit={(reason) => handleHideSubmit(reason)}
              onCancel={handleCancel}
            />
          )}

          {showDismissForm && (
            <div className="mt-4 p-4 bg-gray-50 rounded-lg border-t border-gray-200">
              <div className="mb-3">
                <h4 className="text-sm font-medium text-gray-900 mb-2">
                  Dismiss Report
                </h4>
                <label htmlFor="notes" className="block text-sm text-gray-700 mb-1">
                  Review notes (optional):
                </label>
                <textarea
                  id="notes"
                  rows={3}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  placeholder="Enter review notes..."
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' && e.ctrlKey) {
                      const target = e.target as HTMLTextAreaElement;
                      handleDismissSubmit(target.value);
                    }
                  }}
                />
              </div>
              <div className="flex space-x-2">
                <button
                  onClick={(e) => {
                    const textarea = e.currentTarget.parentElement?.parentElement?.querySelector('textarea') as HTMLTextAreaElement;
                    handleDismissSubmit(textarea?.value || '');
                  }}
                  className="px-4 py-2 bg-gray-600 text-white rounded-md text-sm font-medium hover:bg-gray-700 transition-colors"
                >
                  Dismiss Report
                </button>
                <button
                  onClick={handleCancel}
                  className="px-4 py-2 bg-gray-200 text-gray-700 rounded-md text-sm font-medium hover:bg-gray-300 transition-colors"
                >
                  Cancel
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
