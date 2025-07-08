'use client';

import { useState, useEffect } from 'react';
import { X, Flag, AlertTriangle } from 'lucide-react';
import { userReportApi, adminApi } from '@/lib/api';
import { SystemTag, CreateUserReportDto } from '@/types';
import { useMutation, useQuery } from '@tanstack/react-query';

interface ReportModalProps {
  isOpen: boolean;
  onClose: () => void;
  postId?: number;
  commentId?: number;
  contentType: 'post' | 'comment';
  contentPreview: string;
}

export default function ReportModal({
  isOpen,
  onClose,
  postId,
  commentId,
  contentType,
  contentPreview
}: ReportModalProps) {
  const [reason, setReason] = useState('');
  const [selectedTagIds, setSelectedTagIds] = useState<number[]>([]);
  const [isSubmitted, setIsSubmitted] = useState(false);

  // Get system tags for reporting
  const { data: systemTags = [] } = useQuery({
    queryKey: ['systemTags'],
    queryFn: () => adminApi.getSystemTags(),
    enabled: isOpen,
  });

  // Filter tags that are appropriate for user reporting
  const reportingTags = systemTags.filter(tag =>
    tag.isActive && (
      tag.category === 1 || // Violation
      tag.category === 5 || // Safety
      tag.category === 0 || // ContentWarning
      tag.category === 3    // Quality (includes Spam)
    )
  );

  const createReportMutation = useMutation({
    mutationFn: (data: CreateUserReportDto) => userReportApi.createReport(data),
    onSuccess: () => {
      setIsSubmitted(true);
    },
    onError: (error: any) => {
      console.error('Failed to submit report:', error);
      // Could add error state here if needed
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!reason.trim()) {
      alert('Please provide a reason for reporting this content.');
      return;
    }

    const reportData: CreateUserReportDto = {
      postId,
      commentId,
      reason: reason.trim(),
      systemTagIds: selectedTagIds,
    };

    createReportMutation.mutate(reportData);
  };

  const handleTagToggle = (tagId: number) => {
    setSelectedTagIds(prev => 
      prev.includes(tagId) 
        ? prev.filter(id => id !== tagId)
        : [...prev, tagId]
    );
  };

  const handleClose = () => {
    if (!createReportMutation.isPending) {
      onClose();
      setReason('');
      setSelectedTagIds([]);
      setIsSubmitted(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg max-w-md w-full max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200">
          <div className="flex items-center space-x-2">
            <Flag className="w-5 h-5 text-red-500" />
            <h2 className="text-lg font-semibold text-gray-900">
              Report {contentType}
            </h2>
          </div>
          <button
            onClick={handleClose}
            disabled={createReportMutation.isPending}
            className="text-gray-400 hover:text-gray-600 disabled:opacity-50"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {isSubmitted ? (
          /* Success Message */
          <div className="p-6 text-center">
            <div className="w-16 h-16 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg className="w-8 h-8 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <h3 className="text-lg font-semibold text-gray-900 mb-2">Report Submitted Successfully</h3>
            <p className="text-gray-600 mb-6">
              Thank you for helping keep our community safe. Our moderation team will review this report.
            </p>
            <button
              onClick={handleClose}
              className="px-6 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              Close
            </button>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="p-4 space-y-4">
            {/* Content Preview */}
            <div className="bg-gray-50 p-3 rounded-lg">
              <p className="text-sm text-gray-600 mb-1">Reporting this {contentType}:</p>
              <p className="text-sm text-gray-900 line-clamp-3">
                {contentPreview}
              </p>
            </div>

          {/* System Tags Selection */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              What type of issue are you reporting? (Select all that apply)
            </label>
            <div className="space-y-2 max-h-32 overflow-y-auto">
              {reportingTags.map((tag) => (
                <label
                  key={tag.id}
                  className="flex items-center space-x-2 cursor-pointer hover:bg-gray-50 p-2 rounded"
                >
                  <input
                    type="checkbox"
                    checked={selectedTagIds.includes(tag.id)}
                    onChange={() => handleTagToggle(tag.id)}
                    className="rounded border-gray-300 text-red-600 focus:ring-red-500"
                  />
                  <span className="text-sm text-gray-700">{tag.name}</span>
                  {tag.description && (
                    <span className="text-xs text-gray-500">
                      - {tag.description}
                    </span>
                  )}
                </label>
              ))}
            </div>
          </div>

          {/* Reason Text Area */}
          <div>
            <label htmlFor="reason" className="block text-sm font-medium text-gray-700 mb-2">
              Please explain why you're reporting this content *
            </label>
            <textarea
              id="reason"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Describe the issue with this content..."
              rows={4}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-red-500 focus:border-transparent resize-none"
              required
            />
          </div>

          {/* Warning Message */}
          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-3">
            <div className="flex items-start space-x-2">
              <AlertTriangle className="w-4 h-4 text-yellow-600 mt-0.5 flex-shrink-0" />
              <div className="text-sm text-yellow-800">
                <p className="font-medium">Please note:</p>
                <ul className="mt-1 list-disc list-inside space-y-1">
                  <li>False reports may result in action against your account</li>
                  <li>Reports are reviewed by our moderation team</li>
                  <li>You'll be notified of the outcome when available</li>
                </ul>
              </div>
            </div>
          </div>

          {/* Action Buttons */}
          <div className="flex space-x-3 pt-2">
            <button
              type="button"
              onClick={handleClose}
              disabled={createReportMutation.isPending}
              className="flex-1 px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 border border-gray-300 rounded-md hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-500 disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={createReportMutation.isPending || !reason.trim()}
              className="flex-1 px-4 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {createReportMutation.isPending ? 'Submitting...' : 'Submit Report'}
            </button>
          </div>
        </form>
        )}
      </div>
    </div>
  );
}
