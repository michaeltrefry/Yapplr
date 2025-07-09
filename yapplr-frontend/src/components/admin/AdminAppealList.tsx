'use client';

import React from 'react';
import { UserAppeal, AppealStatus } from '@/types';
import { AdminAppealCard } from './AdminAppealCard';
import { Scale, Clock, Check, X } from 'lucide-react';

export interface AdminAppealListProps {
  appeals: UserAppeal[];
  loading?: boolean;
  onReview?: (appeal: UserAppeal) => void;
  showSections?: boolean;
  emptyMessage?: string;
  className?: string;
}

export function AdminAppealList({
  appeals,
  loading = false,
  onReview,
  showSections = true,
  emptyMessage = 'No appeals found',
  className = '',
}: AdminAppealListProps) {
  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  if (appeals.length === 0) {
    return (
      <div className="text-center py-12">
        <Scale className="h-12 w-12 text-gray-400 mx-auto mb-4" />
        <p className="text-gray-500">{emptyMessage}</p>
      </div>
    );
  }

  if (!showSections) {
    return (
      <div className={`space-y-4 ${className}`}>
        {appeals.map((appeal) => (
          <AdminAppealCard
            key={appeal.id}
            appeal={appeal}
            onReview={onReview}
            showReviewButton={appeal.status === AppealStatus.Pending}
          />
        ))}
      </div>
    );
  }

  const pendingAppeals = appeals.filter(a => a.status === AppealStatus.Pending);
  const reviewedAppeals = appeals.filter(a => a.status !== AppealStatus.Pending);

  return (
    <div className={`space-y-6 ${className}`}>
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
                <AdminAppealCard
                  appeal={appeal}
                  onReview={onReview}
                  showReviewButton={true}
                  className="border-0 shadow-none p-0"
                />
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
                <AdminAppealCard
                  appeal={appeal}
                  onReview={onReview}
                  showReviewButton={false}
                  className="border-0 shadow-none p-0"
                />
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
