'use client';

import { useState } from 'react';
import { X, MessageSquare, AlertTriangle } from 'lucide-react';
import { adminApi } from '@/lib/api';
import { AppealType, CreateAppealDto } from '@/types';
import { useMutation } from '@tanstack/react-query';

interface AppealModalProps {
  isOpen: boolean;
  onClose: () => void;
  postId?: number;
  commentId?: number;
  contentType: 'post' | 'comment';
  hiddenReason?: string;
  onSuccess?: () => void;
}

export default function AppealModal({
  isOpen,
  onClose,
  postId,
  commentId,
  contentType,
  hiddenReason,
  onSuccess
}: AppealModalProps) {
  const [reason, setReason] = useState('');
  const [additionalInfo, setAdditionalInfo] = useState('');
  const [isSubmitted, setIsSubmitted] = useState(false);

  const createAppealMutation = useMutation({
    mutationFn: (data: CreateAppealDto) => adminApi.createUserAppeal(data),
    onSuccess: () => {
      setIsSubmitted(true);
      onSuccess?.(); // Call the success callback to refresh the post data
    },
    onError: (error: any) => {
      console.error('Failed to submit appeal:', error);
      alert('Failed to submit appeal. Please try again.');
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!reason.trim()) {
      alert('Please provide a reason for your appeal.');
      return;
    }

    const appealData: CreateAppealDto = {
      type: AppealType.ContentRemoval,
      reason: reason.trim(),
      additionalInfo: additionalInfo.trim() || undefined,
      targetPostId: postId,
      targetCommentId: commentId,
    };

    createAppealMutation.mutate(appealData);
  };

  const handleClose = () => {
    if (!createAppealMutation.isPending) {
      setReason('');
      setAdditionalInfo('');
      setIsSubmitted(false);
      onClose();
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg max-w-md w-full max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200">
          <div className="flex items-center space-x-2">
            <MessageSquare className="w-5 h-5 text-blue-500" />
            <h2 className="text-lg font-semibold text-gray-900">
              Appeal Content Removal
            </h2>
          </div>
          <button
            onClick={handleClose}
            disabled={createAppealMutation.isPending}
            className="text-gray-400 hover:text-gray-600 disabled:opacity-50"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content */}
        <div className="p-4">
          {isSubmitted ? (
            <div className="text-center py-6">
              <div className="w-12 h-12 bg-green-100 rounded-full flex items-center justify-center mx-auto mb-4">
                <MessageSquare className="w-6 h-6 text-green-600" />
              </div>
              <h3 className="text-lg font-medium text-gray-900 mb-2">Appeal Submitted</h3>
              <p className="text-gray-600 mb-4">
                Your appeal has been submitted successfully. Our moderation team will review it and get back to you.
              </p>
              <p className="text-sm text-gray-500 mb-6">
                You can check the status of your appeal in your notifications.
              </p>
              <button
                onClick={handleClose}
                className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors"
              >
                Close
              </button>
            </div>
          ) : (
            <form onSubmit={handleSubmit} className="space-y-4">
              {/* Context */}
              <div className="bg-gray-50 rounded-lg p-3">
                <h4 className="text-sm font-medium text-gray-900 mb-1">
                  Appealing: {contentType} removal
                </h4>
                {hiddenReason && (
                  <p className="text-sm text-gray-600">
                    <span className="font-medium">Original reason:</span> {hiddenReason}
                  </p>
                )}
              </div>

              {/* Reason */}
              <div>
                <label htmlFor="reason" className="block text-sm font-medium text-gray-700 mb-2">
                  Why do you believe this decision was incorrect? *
                </label>
                <textarea
                  id="reason"
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
                  rows={4}
                  maxLength={2000}
                  placeholder="Please explain why you believe your content was incorrectly removed..."
                  required
                  disabled={createAppealMutation.isPending}
                />
                <p className="text-xs text-gray-500 mt-1">
                  {reason.length}/2000 characters
                </p>
              </div>

              {/* Additional Info */}
              <div>
                <label htmlFor="additionalInfo" className="block text-sm font-medium text-gray-700 mb-2">
                  Additional Information (Optional)
                </label>
                <textarea
                  id="additionalInfo"
                  value={additionalInfo}
                  onChange={(e) => setAdditionalInfo(e.target.value)}
                  className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent resize-none"
                  rows={3}
                  maxLength={2000}
                  placeholder="Any additional context or information that might help with your appeal..."
                  disabled={createAppealMutation.isPending}
                />
                <p className="text-xs text-gray-500 mt-1">
                  {additionalInfo.length}/2000 characters
                </p>
              </div>

              {/* Warning */}
              <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-3">
                <div className="flex items-start space-x-2">
                  <AlertTriangle className="w-4 h-4 text-yellow-600 mt-0.5 flex-shrink-0" />
                  <div className="text-sm text-yellow-800">
                    <p className="font-medium mb-1">Please note:</p>
                    <ul className="list-disc list-inside space-y-1 text-xs">
                      <li>Appeals are reviewed by our moderation team</li>
                      <li>Frivolous or repeated appeals may result in restrictions</li>
                      <li>Review typically takes 1-3 business days</li>
                    </ul>
                  </div>
                </div>
              </div>

              {/* Actions */}
              <div className="flex space-x-3 pt-4">
                <button
                  type="button"
                  onClick={handleClose}
                  disabled={createAppealMutation.isPending}
                  className="flex-1 px-4 py-2 border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={createAppealMutation.isPending || !reason.trim()}
                  className="flex-1 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                >
                  {createAppealMutation.isPending ? 'Submitting...' : 'Submit Appeal'}
                </button>
              </div>
            </form>
          )}
        </div>
      </div>
    </div>
  );
}
