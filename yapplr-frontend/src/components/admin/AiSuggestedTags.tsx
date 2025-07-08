'use client';

import { useState } from 'react';
import { AiSuggestedTag } from '@/types';
import { Check, X, AlertTriangle, Brain, Clock, User } from 'lucide-react';
import { format } from 'date-fns';

interface AiSuggestedTagsProps {
  tags: AiSuggestedTag[];
  onApprove?: (tagId: number, reason?: string) => Promise<void>;
  onReject?: (tagId: number, reason?: string) => Promise<void>;
  onBulkApprove?: (tagIds: number[], reason?: string) => Promise<void>;
  onBulkReject?: (tagIds: number[], reason?: string) => Promise<void>;
  showActions?: boolean;
}

export default function AiSuggestedTags({
  tags,
  onApprove,
  onReject,
  onBulkApprove,
  onBulkReject,
  showActions = true
}: AiSuggestedTagsProps) {
  const [selectedTags, setSelectedTags] = useState<Set<number>>(new Set());
  const [showReasonModal, setShowReasonModal] = useState<{
    type: 'approve' | 'reject' | 'bulk-approve' | 'bulk-reject';
    tagId?: number;
  } | null>(null);
  const [reason, setReason] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const pendingTags = tags.filter(tag => !tag.isApproved && !tag.isRejected);
  const approvedTags = tags.filter(tag => tag.isApproved);
  const rejectedTags = tags.filter(tag => tag.isRejected);

  const handleSelectTag = (tagId: number) => {
    const newSelected = new Set(selectedTags);
    if (newSelected.has(tagId)) {
      newSelected.delete(tagId);
    } else {
      newSelected.add(tagId);
    }
    setSelectedTags(newSelected);
  };

  const handleSelectAll = () => {
    if (selectedTags.size === pendingTags.length) {
      setSelectedTags(new Set());
    } else {
      setSelectedTags(new Set(pendingTags.map(tag => tag.id)));
    }
  };

  const handleAction = async () => {
    if (!showReasonModal) return;

    setIsLoading(true);
    try {
      switch (showReasonModal.type) {
        case 'approve':
          if (showReasonModal.tagId && onApprove) {
            await onApprove(showReasonModal.tagId, reason || undefined);
          }
          break;
        case 'reject':
          if (showReasonModal.tagId && onReject) {
            await onReject(showReasonModal.tagId, reason || undefined);
          }
          break;
        case 'bulk-approve':
          if (onBulkApprove) {
            await onBulkApprove(Array.from(selectedTags), reason || undefined);
            setSelectedTags(new Set());
          }
          break;
        case 'bulk-reject':
          if (onBulkReject) {
            await onBulkReject(Array.from(selectedTags), reason || undefined);
            setSelectedTags(new Set());
          }
          break;
      }
    } finally {
      setIsLoading(false);
      setShowReasonModal(null);
      setReason('');
    }
  };

  const getRiskLevelColor = (riskLevel: string) => {
    switch (riskLevel.toUpperCase()) {
      case 'HIGH':
        return 'bg-red-100 text-red-800 border-red-200';
      case 'MEDIUM':
        return 'bg-yellow-100 text-yellow-800 border-yellow-200';
      case 'LOW':
        return 'bg-green-100 text-green-800 border-green-200';
      case 'MINIMAL':
        return 'bg-gray-100 text-gray-800 border-gray-200';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-200';
    }
  };

  const getCategoryColor = (category: string) => {
    switch (category) {
      case 'Violation':
        return 'bg-red-50 text-red-700 border-red-200';
      case 'ContentWarning':
        return 'bg-orange-50 text-orange-700 border-orange-200';
      case 'Safety':
        return 'bg-yellow-50 text-yellow-700 border-yellow-200';
      default:
        return 'bg-blue-50 text-blue-700 border-blue-200';
    }
  };

  if (tags.length === 0) {
    return (
      <div className="text-center py-8">
        <Brain className="h-12 w-12 text-gray-400 mx-auto mb-4" />
        <p className="text-gray-500">No AI suggestions available</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Pending Tags */}
      {pendingTags.length > 0 && (
        <div>
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-medium text-gray-900 flex items-center">
              <Clock className="h-5 w-5 mr-2 text-yellow-500" />
              Pending AI Suggestions ({pendingTags.length})
            </h3>
            {showActions && pendingTags.length > 1 && (
              <div className="flex items-center space-x-2">
                <button
                  onClick={handleSelectAll}
                  className="text-sm text-blue-600 hover:text-blue-800"
                >
                  {selectedTags.size === pendingTags.length ? 'Deselect All' : 'Select All'}
                </button>
                {selectedTags.size > 0 && (
                  <>
                    <button
                      onClick={() => setShowReasonModal({ type: 'bulk-approve' })}
                      className="px-3 py-1 bg-green-600 text-white text-sm rounded hover:bg-green-700"
                    >
                      Approve Selected ({selectedTags.size})
                    </button>
                    <button
                      onClick={() => setShowReasonModal({ type: 'bulk-reject' })}
                      className="px-3 py-1 bg-red-600 text-white text-sm rounded hover:bg-red-700"
                    >
                      Reject Selected ({selectedTags.size})
                    </button>
                  </>
                )}
              </div>
            )}
          </div>

          <div className="space-y-3">
            {pendingTags.map((tag) => (
              <div key={tag.id} className="bg-white border border-gray-200 rounded-lg p-4">
                <div className="flex items-start justify-between">
                  <div className="flex items-start space-x-3">
                    {showActions && (
                      <input
                        type="checkbox"
                        checked={selectedTags.has(tag.id)}
                        onChange={() => handleSelectTag(tag.id)}
                        className="mt-1 h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
                      />
                    )}
                    <div className="flex-1">
                      <div className="flex items-center space-x-2 mb-2">
                        <span className={`px-2 py-1 text-xs font-medium rounded-full border ${getCategoryColor(tag.category)}`}>
                          {tag.category}
                        </span>
                        <span className="font-medium text-gray-900">{tag.tagName}</span>
                        <span className={`px-2 py-1 text-xs font-medium rounded border ${getRiskLevelColor(tag.riskLevel)}`}>
                          {tag.riskLevel} Risk
                        </span>
                      </div>
                      <div className="flex items-center space-x-4 text-sm text-gray-500">
                        <span>Confidence: {(tag.confidence * 100).toFixed(1)}%</span>
                        <span>Suggested: {format(new Date(tag.suggestedAt), 'MMM d, yyyy HH:mm')}</span>
                        {tag.requiresReview && (
                          <span className="flex items-center text-orange-600">
                            <AlertTriangle className="h-4 w-4 mr-1" />
                            Requires Review
                          </span>
                        )}
                      </div>
                    </div>
                  </div>
                  {showActions && (
                    <div className="flex items-center space-x-2">
                      <button
                        onClick={() => setShowReasonModal({ type: 'approve', tagId: tag.id })}
                        className="p-2 text-green-600 hover:bg-green-50 rounded"
                        title="Approve"
                      >
                        <Check className="h-4 w-4" />
                      </button>
                      <button
                        onClick={() => setShowReasonModal({ type: 'reject', tagId: tag.id })}
                        className="p-2 text-red-600 hover:bg-red-50 rounded"
                        title="Reject"
                      >
                        <X className="h-4 w-4" />
                      </button>
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Approved Tags */}
      {approvedTags.length > 0 && (
        <div>
          <h3 className="text-lg font-medium text-gray-900 flex items-center mb-4">
            <Check className="h-5 w-5 mr-2 text-green-500" />
            Approved Suggestions ({approvedTags.length})
          </h3>
          <div className="space-y-2">
            {approvedTags.map((tag) => (
              <div key={tag.id} className="bg-green-50 border border-green-200 rounded-lg p-3">
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <span className={`px-2 py-1 text-xs font-medium rounded-full border ${getCategoryColor(tag.category)}`}>
                      {tag.category}
                    </span>
                    <span className="font-medium text-gray-900">{tag.tagName}</span>
                  </div>
                  <div className="flex items-center space-x-2 text-sm text-gray-500">
                    {tag.approvedByUsername && (
                      <span className="flex items-center">
                        <User className="h-4 w-4 mr-1" />
                        {tag.approvedByUsername}
                      </span>
                    )}
                    {tag.approvedAt && (
                      <span>{format(new Date(tag.approvedAt), 'MMM d, yyyy')}</span>
                    )}
                  </div>
                </div>
                {tag.approvalReason && (
                  <p className="mt-2 text-sm text-gray-600">{tag.approvalReason}</p>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Rejected Tags */}
      {rejectedTags.length > 0 && (
        <div>
          <h3 className="text-lg font-medium text-gray-900 flex items-center mb-4">
            <X className="h-5 w-5 mr-2 text-red-500" />
            Rejected Suggestions ({rejectedTags.length})
          </h3>
          <div className="space-y-2">
            {rejectedTags.map((tag) => (
              <div key={tag.id} className="bg-red-50 border border-red-200 rounded-lg p-3">
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    <span className={`px-2 py-1 text-xs font-medium rounded-full border ${getCategoryColor(tag.category)}`}>
                      {tag.category}
                    </span>
                    <span className="font-medium text-gray-900">{tag.tagName}</span>
                  </div>
                  <div className="flex items-center space-x-2 text-sm text-gray-500">
                    {tag.approvedByUsername && (
                      <span className="flex items-center">
                        <User className="h-4 w-4 mr-1" />
                        {tag.approvedByUsername}
                      </span>
                    )}
                    {tag.approvedAt && (
                      <span>{format(new Date(tag.approvedAt), 'MMM d, yyyy')}</span>
                    )}
                  </div>
                </div>
                {tag.approvalReason && (
                  <p className="mt-2 text-sm text-gray-600">{tag.approvalReason}</p>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Reason Modal */}
      {showReasonModal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 w-full max-w-md">
            <h3 className="text-lg font-medium text-gray-900 mb-4">
              {showReasonModal.type.includes('approve') ? 'Approve' : 'Reject'} AI Suggestion
              {showReasonModal.type.includes('bulk') && `s (${selectedTags.size})`}
            </h3>
            <div className="mb-4">
              <label htmlFor="reason" className="block text-sm font-medium text-gray-700 mb-2">
                Reason (optional)
              </label>
              <textarea
                id="reason"
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                rows={3}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Enter reason for this decision..."
              />
            </div>
            <div className="flex justify-end space-x-3">
              <button
                onClick={() => {
                  setShowReasonModal(null);
                  setReason('');
                }}
                className="px-4 py-2 text-gray-700 bg-gray-100 rounded hover:bg-gray-200"
                disabled={isLoading}
              >
                Cancel
              </button>
              <button
                onClick={handleAction}
                disabled={isLoading}
                className={`px-4 py-2 text-white rounded ${
                  showReasonModal.type.includes('approve')
                    ? 'bg-green-600 hover:bg-green-700'
                    : 'bg-red-600 hover:bg-red-700'
                } disabled:opacity-50`}
              >
                {isLoading ? 'Processing...' : (showReasonModal.type.includes('approve') ? 'Approve' : 'Reject')}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
