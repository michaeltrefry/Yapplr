'use client';

import { useEffect, useState } from 'react';
import { adminApi } from '@/lib/api';
import { UserAppeal, AppealStatus, AppealType, ReviewAppealDto } from '@/types';
import {
  Scale,
  User,
  Calendar,
  AlertTriangle,
  FileText,
  Tag,
  HelpCircle,
  Check,
  X,
  Clock,
  MessageSquare,
  Filter,
} from 'lucide-react';

export default function AdminAppealsPage() {
  const [appeals, setAppeals] = useState<UserAppeal[]>([]);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState<AppealStatus | ''>('');
  const [typeFilter, setTypeFilter] = useState<AppealType | ''>('');
  const [selectedAppeal, setSelectedAppeal] = useState<UserAppeal | null>(null);
  const [reviewNotes, setReviewNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    fetchAppeals();
  }, [statusFilter, typeFilter]);

  const fetchAppeals = async () => {
    try {
      setLoading(true);
      const appealsData = await adminApi.getAppeals(
        1,
        50,
        statusFilter || undefined,
        typeFilter || undefined
      );
      setAppeals(appealsData);
    } catch (error) {
      console.error('Failed to fetch appeals:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleReviewAppeal = async (appealId: number, status: AppealStatus) => {
    if (!reviewNotes.trim()) {
      alert('Please provide review notes');
      return;
    }

    try {
      setSubmitting(true);
      const reviewData: ReviewAppealDto = {
        status,
        reviewNotes: reviewNotes.trim(),
      };
      
      await adminApi.reviewAppeal(appealId, reviewData);
      setSelectedAppeal(null);
      setReviewNotes('');
      fetchAppeals();
    } catch (error) {
      console.error('Failed to review appeal:', error);
      alert('Failed to review appeal');
    } finally {
      setSubmitting(false);
    }
  };

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
        return <span className="bg-yellow-100 text-yellow-800 px-2 py-1 rounded-full text-xs">Pending</span>;
      case AppealStatus.Approved:
        return <span className="bg-green-100 text-green-800 px-2 py-1 rounded-full text-xs">Approved</span>;
      case AppealStatus.Denied:
        return <span className="bg-red-100 text-red-800 px-2 py-1 rounded-full text-xs">Denied</span>;
      default:
        return null;
    }
  };

  const pendingAppeals = appeals.filter(a => a.status === AppealStatus.Pending);
  const reviewedAppeals = appeals.filter(a => a.status !== AppealStatus.Pending);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Appeals Management</h1>
          <p className="text-gray-600">Review and respond to user appeals</p>
        </div>
        <div className="flex items-center space-x-3">
          <Filter className="h-5 w-5 text-gray-400" />
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as AppealStatus | '')}
            className="border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="">All Statuses</option>
            <option value={AppealStatus.Pending}>Pending</option>
            <option value={AppealStatus.Approved}>Approved</option>
            <option value={AppealStatus.Denied}>Denied</option>
          </select>
          <select
            value={typeFilter}
            onChange={(e) => setTypeFilter(e.target.value as AppealType | '')}
            className="border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="">All Types</option>
            <option value={AppealType.Suspension}>Suspension</option>
            <option value={AppealType.Ban}>Ban</option>
            <option value={AppealType.ContentRemoval}>Content Removal</option>
            <option value={AppealType.SystemTag}>System Tag</option>
            <option value={AppealType.Other}>Other</option>
          </select>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center">
            <Clock className="h-8 w-8 text-yellow-500 mr-3" />
            <div>
              <p className="text-2xl font-bold text-gray-900">{pendingAppeals.length}</p>
              <p className="text-sm text-gray-600">Pending Appeals</p>
            </div>
          </div>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center">
            <Check className="h-8 w-8 text-green-500 mr-3" />
            <div>
              <p className="text-2xl font-bold text-gray-900">
                {appeals.filter(a => a.status === AppealStatus.Approved).length}
              </p>
              <p className="text-sm text-gray-600">Approved</p>
            </div>
          </div>
        </div>
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center">
            <X className="h-8 w-8 text-red-500 mr-3" />
            <div>
              <p className="text-2xl font-bold text-gray-900">
                {appeals.filter(a => a.status === AppealStatus.Denied).length}
              </p>
              <p className="text-sm text-gray-600">Denied</p>
            </div>
          </div>
        </div>
      </div>

      {loading ? (
        <div className="flex items-center justify-center h-64">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
        </div>
      ) : (
        <div className="space-y-6">
          {/* Pending Appeals */}
          {pendingAppeals.length > 0 && (
            <div className="bg-white rounded-lg shadow">
              <div className="px-6 py-4 border-b border-gray-200">
                <h2 className="text-lg font-semibold text-gray-900 flex items-center">
                  <Clock className="h-5 w-5 text-yellow-500 mr-2" />
                  Pending Appeals ({pendingAppeals.length})
                </h2>
              </div>
              <div className="divide-y divide-gray-200">
                {pendingAppeals.map((appeal) => (
                  <div key={appeal.id} className="p-6">
                    <div className="flex justify-between items-start mb-4">
                      <div className="flex items-center space-x-3">
                        {getAppealTypeIcon(appeal.type)}
                        <div>
                          <p className="font-medium text-gray-900">
                            {getAppealTypeName(appeal.type)} Appeal
                          </p>
                          <p className="text-sm text-gray-500 flex items-center">
                            <User className="h-4 w-4 mr-1" />
                            @{appeal.username}
                            <Calendar className="h-4 w-4 ml-3 mr-1" />
                            {new Date(appeal.createdAt).toLocaleDateString()}
                          </p>
                        </div>
                      </div>
                      <div className="flex space-x-2">
                        <button
                          onClick={() => setSelectedAppeal(appeal)}
                          className="flex items-center px-3 py-1 bg-blue-100 text-blue-800 rounded-md hover:bg-blue-200 transition-colors"
                        >
                          <MessageSquare className="h-4 w-4 mr-1" />
                          Review
                        </button>
                      </div>
                    </div>

                    <div className="mb-4">
                      <p className="text-gray-900 mb-2"><strong>Reason:</strong></p>
                      <p className="text-gray-700 bg-gray-50 p-3 rounded">{appeal.reason}</p>
                    </div>

                    {appeal.additionalInfo && (
                      <div className="mb-4">
                        <p className="text-gray-900 mb-2"><strong>Additional Information:</strong></p>
                        <p className="text-gray-700 bg-gray-50 p-3 rounded">{appeal.additionalInfo}</p>
                      </div>
                    )}

                    {(appeal.targetPostId || appeal.targetCommentId) && (
                      <div className="text-sm text-gray-500">
                        <strong>Related Content:</strong>
                        {appeal.targetPostId && <span> Post #{appeal.targetPostId}</span>}
                        {appeal.targetCommentId && <span> Comment #{appeal.targetCommentId}</span>}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}

          {/* Reviewed Appeals */}
          {reviewedAppeals.length > 0 && (
            <div className="bg-white rounded-lg shadow">
              <div className="px-6 py-4 border-b border-gray-200">
                <h2 className="text-lg font-semibold text-gray-900">
                  Reviewed Appeals ({reviewedAppeals.length})
                </h2>
              </div>
              <div className="divide-y divide-gray-200">
                {reviewedAppeals.map((appeal) => (
                  <div key={appeal.id} className="p-6">
                    <div className="flex justify-between items-start mb-4">
                      <div className="flex items-center space-x-3">
                        {getAppealTypeIcon(appeal.type)}
                        <div>
                          <p className="font-medium text-gray-900">
                            {getAppealTypeName(appeal.type)} Appeal
                          </p>
                          <p className="text-sm text-gray-500 flex items-center">
                            <User className="h-4 w-4 mr-1" />
                            @{appeal.username}
                            <Calendar className="h-4 w-4 ml-3 mr-1" />
                            {new Date(appeal.createdAt).toLocaleDateString()}
                          </p>
                        </div>
                      </div>
                      {getStatusBadge(appeal.status)}
                    </div>

                    <div className="mb-4">
                      <p className="text-gray-900 mb-2"><strong>Reason:</strong></p>
                      <p className="text-gray-700">{appeal.reason}</p>
                    </div>

                    {appeal.reviewNotes && (
                      <div className="mb-4">
                        <p className="text-gray-900 mb-2"><strong>Review Notes:</strong></p>
                        <p className="text-gray-700 bg-gray-50 p-3 rounded">{appeal.reviewNotes}</p>
                      </div>
                    )}

                    {appeal.reviewedAt && (
                      <div className="text-sm text-gray-500">
                        <strong>Reviewed:</strong> {new Date(appeal.reviewedAt).toLocaleDateString()}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}

          {appeals.length === 0 && (
            <div className="text-center py-12">
              <Scale className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <p className="text-gray-500">No appeals found</p>
            </div>
          )}
        </div>
      )}

      {/* Review Modal */}
      {selectedAppeal && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-2xl w-full mx-4 max-h-[90vh] overflow-y-auto">
            <div className="flex justify-between items-center mb-4">
              <h3 className="text-lg font-semibold text-gray-900">Review Appeal</h3>
              <button
                onClick={() => setSelectedAppeal(null)}
                className="inline-flex items-center px-2 py-1 bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 transition-colors text-xs"
              >
                <X className="h-3 w-3 mr-1" />
                Close
              </button>
            </div>

            <div className="space-y-4 mb-6">
              <div>
                <p className="font-medium text-gray-900">Appeal Type:</p>
                <p className="text-gray-700">{getAppealTypeName(selectedAppeal.type)}</p>
              </div>
              <div>
                <p className="font-medium text-gray-900">User:</p>
                <p className="text-gray-700">@{selectedAppeal.username}</p>
              </div>
              <div>
                <p className="font-medium text-gray-900">Reason:</p>
                <p className="text-gray-700 bg-gray-50 p-3 rounded">{selectedAppeal.reason}</p>
              </div>
              {selectedAppeal.additionalInfo && (
                <div>
                  <p className="font-medium text-gray-900">Additional Information:</p>
                  <p className="text-gray-700 bg-gray-50 p-3 rounded">{selectedAppeal.additionalInfo}</p>
                </div>
              )}
            </div>

            <div className="mb-6">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Review Notes *
              </label>
              <textarea
                value={reviewNotes}
                onChange={(e) => setReviewNotes(e.target.value)}
                rows={4}
                placeholder="Provide detailed notes about your decision..."
                className="w-full border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>

            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setSelectedAppeal(null)}
                className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50 transition-colors"
              >
                Cancel
              </button>
              <button
                onClick={() => handleReviewAppeal(selectedAppeal.id, AppealStatus.Denied)}
                disabled={submitting || !reviewNotes.trim()}
                className="flex items-center px-4 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 transition-colors disabled:opacity-50"
              >
                <X className="h-4 w-4 mr-1" />
                Deny Appeal
              </button>
              <button
                onClick={() => handleReviewAppeal(selectedAppeal.id, AppealStatus.Approved)}
                disabled={submitting || !reviewNotes.trim()}
                className="flex items-center px-4 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 transition-colors disabled:opacity-50"
              >
                <Check className="h-4 w-4 mr-1" />
                Approve Appeal
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
