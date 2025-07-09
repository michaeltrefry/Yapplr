'use client';

import React from 'react';
import { UserReport } from '@/types';
import { AdminUserReportCard } from './AdminUserReportCard';
import { Flag } from 'lucide-react';

export interface AdminUserReportListProps {
  reports: UserReport[];
  loading?: boolean;
  onHideContent?: (reportId: number, reason: string) => Promise<void>;
  onDismissReport?: (reportId: number, notes: string) => Promise<void>;
  actionLoading?: number | null;
  emptyMessage?: string;
  className?: string;
}

export function AdminUserReportList({
  reports,
  loading = false,
  onHideContent,
  onDismissReport,
  actionLoading = null,
  emptyMessage = 'No user reports to review',
  className = '',
}: AdminUserReportListProps) {
  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  if (reports.length === 0) {
    return (
      <div className="text-center py-12">
        <Flag className="h-12 w-12 text-gray-400 mx-auto mb-4" />
        <p className="text-gray-500">{emptyMessage}</p>
      </div>
    );
  }

  return (
    <div className={`space-y-4 ${className}`}>
      {reports.map((report) => (
        <AdminUserReportCard
          key={report.id}
          report={report}
          onHideContent={onHideContent}
          onDismissReport={onDismissReport}
          actionLoading={actionLoading === report.id}
        />
      ))}
    </div>
  );
}
