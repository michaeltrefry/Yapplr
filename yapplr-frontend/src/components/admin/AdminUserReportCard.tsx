'use client';

import React, { useState } from 'react';
import { UserReport, UserReportStatus } from '@/types';
import { 
  Flag, 
  User, 
  Calendar, 
  ExternalLink, 
  EyeOff, 
  X, 
  MessageSquare 
} from 'lucide-react';
import { UserLink } from './UserLink';
import { SystemTagDisplay } from './SystemTagDisplay';
import { InlineActionForm } from './InlineActionForm';

export interface AdminUserReportCardProps {
  report: UserReport;
  onHideContent?: (reportId: number, reason: string) => Promise<void>;
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
        await onHideContent(report.id, reason);
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
          <div className="flex items-center space-x-3">
            <Flag className="h-5 w-5 text-red-500 flex-shrink-0" />
            <div>
              <h3 className="font-medium text-gray-900">
                Report #{report.id}
              </h3>
              <div className="flex items-center text-sm text-gray-500 mt-1">
                <User className="h-4 w-4 mr-1" />
                <span>Reported by </span>
                <UserLink username={report.reportedByUsername} className="ml-1" />
                <Calendar className="h-4 w-4 ml-3 mr-1" />
                <span>{new Date(report.createdAt).toLocaleDateString()}</span>
              </div>
            </div>
          </div>
          
          <div className="flex items-center space-x-3">
            {getStatusBadge(report.status)}
          </div>
        </div>

        {/* Reported Content */}
        <div className="bg-gray-50 p-4 rounded-lg mb-4">
          <div className="flex items-center justify-between mb-2">
            <span className="text-sm font-medium text-gray-700">
              Reported {report.post ? 'Post' : 'Comment'}:
            </span>
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
          <p className="text-gray-900 mb-2">{getContent()}</p>
          <div className="flex items-center space-x-2 text-sm text-gray-500">
            <User className="h-4 w-4" />
            <UserLink username={getContentAuthor()} />
            {isContentHidden() && (
              <span className="bg-red-100 text-red-800 px-2 py-1 rounded-full text-xs font-medium ml-2">
                Hidden
              </span>
            )}
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

          {/* Action Buttons - only show for pending reports */}
          {report.status === UserReportStatus.Pending && (
            <div className="pt-4 border-t border-gray-200">
              <div className="flex space-x-3">
                {!isContentHidden() && (
                  <button
                    onClick={handleHideClick}
                    disabled={actionLoading}
                    className="flex items-center px-3 py-2 bg-red-100 text-red-800 rounded-md hover:bg-red-200 transition-colors disabled:opacity-50"
                  >
                    <EyeOff className="h-4 w-4 mr-1" />
                    Hide {report.post ? 'Post' : 'Comment'}
                  </button>
                )}
                <button
                  onClick={handleDismissClick}
                  disabled={actionLoading}
                  className="flex items-center px-3 py-2 bg-gray-100 text-gray-800 rounded-md hover:bg-gray-200 transition-colors disabled:opacity-50"
                >
                  <X className="h-4 w-4 mr-1" />
                  Dismiss Report
                </button>
              </div>
            </div>
          )}

          {/* Inline Action Forms */}
          {showHideForm && (
            <div className="mt-4 p-4 bg-gray-50 rounded-lg border-t border-gray-200">
              <div className="mb-3">
                <h4 className="text-sm font-medium text-gray-900 mb-2">
                  Hide {report.post ? 'Post' : 'Comment'}
                </h4>
                <label htmlFor="reason" className="block text-sm text-gray-700 mb-1">
                  Reason for hiding this {report.post ? 'post' : 'comment'}:
                </label>
                <textarea
                  id="reason"
                  rows={3}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  placeholder="Enter your reason..."
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' && e.ctrlKey) {
                      const target = e.target as HTMLTextAreaElement;
                      if (target.value.trim()) {
                        handleHideSubmit(target.value);
                      }
                    }
                  }}
                />
              </div>
              <div className="flex space-x-2">
                <button
                  onClick={(e) => {
                    const textarea = e.currentTarget.parentElement?.parentElement?.querySelector('textarea') as HTMLTextAreaElement;
                    if (textarea?.value.trim()) {
                      handleHideSubmit(textarea.value);
                    }
                  }}
                  className="px-4 py-2 bg-red-600 text-white rounded-md text-sm font-medium hover:bg-red-700 transition-colors"
                >
                  Hide {report.post ? 'Post' : 'Comment'}
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
                      handleDismissSubmit(target.value || 'No action needed');
                    }
                  }}
                />
              </div>
              <div className="flex space-x-2">
                <button
                  onClick={(e) => {
                    const textarea = e.currentTarget.parentElement?.parentElement?.querySelector('textarea') as HTMLTextAreaElement;
                    handleDismissSubmit(textarea?.value || 'No action needed');
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
