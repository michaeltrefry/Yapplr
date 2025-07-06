'use client';

import { useEffect, useState } from 'react';
import { adminApi } from '@/lib/api';
import { AuditLog, AuditAction } from '@/types';
import {
  FileSearch,
  Filter,
  Calendar,
  User,
  Shield,
  AlertTriangle,
  Eye,
  Search,
} from 'lucide-react';

export default function AuditLogsPage() {
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [actionFilter, setActionFilter] = useState<AuditAction | ''>('');
  const [searchTerm, setSearchTerm] = useState('');
  const [expandedLog, setExpandedLog] = useState<number | null>(null);

  useEffect(() => {
    fetchLogs();
  }, [page, actionFilter]);

  const fetchLogs = async () => {
    try {
      setLoading(true);
      const logsData = await adminApi.getAuditLogs(
        page,
        25,
        actionFilter || undefined
      );
      setLogs(logsData);
    } catch (error) {
      console.error('Failed to fetch audit logs:', error);
    } finally {
      setLoading(false);
    }
  };

  const getActionIcon = (action: AuditAction) => {
    if (action >= 100 && action < 200) {
      return <User className="h-4 w-4 text-blue-500" />;
    } else if (action >= 200 && action < 300) {
      return <FileSearch className="h-4 w-4 text-orange-500" />;
    } else if (action >= 300 && action < 400) {
      return <Shield className="h-4 w-4 text-purple-500" />;
    } else if (action >= 400 && action < 500) {
      return <AlertTriangle className="h-4 w-4 text-red-500" />;
    } else {
      return <Eye className="h-4 w-4 text-gray-500" />;
    }
  };

  const getActionCategory = (action: AuditAction) => {
    if (action >= 100 && action < 200) return 'User Action';
    if (action >= 200 && action < 300) return 'Content Action';
    if (action >= 300 && action < 400) return 'System Action';
    if (action >= 400 && action < 500) return 'Security Action';
    if (action >= 500) return 'Bulk Action';
    return 'Unknown';
  };

  const getActionName = (action: AuditAction) => {
    switch (action) {
      case AuditAction.UserSuspended: return 'User Suspended';
      case AuditAction.UserBanned: return 'User Banned';
      case AuditAction.UserShadowBanned: return 'User Shadow Banned';
      case AuditAction.UserUnsuspended: return 'User Unsuspended';
      case AuditAction.UserUnbanned: return 'User Unbanned';
      case AuditAction.UserRoleChanged: return 'User Role Changed';
      case AuditAction.UserForcePasswordReset: return 'Force Password Reset';
      case AuditAction.UserEmailVerificationToggled: return 'Email Verification Toggled';
      case AuditAction.PostHidden: return 'Post Hidden';
      case AuditAction.PostDeleted: return 'Post Deleted';
      case AuditAction.PostRestored: return 'Post Restored';
      case AuditAction.PostSystemTagAdded: return 'Post System Tag Added';
      case AuditAction.PostSystemTagRemoved: return 'Post System Tag Removed';
      case AuditAction.CommentHidden: return 'Comment Hidden';
      case AuditAction.CommentDeleted: return 'Comment Deleted';
      case AuditAction.CommentRestored: return 'Comment Restored';
      case AuditAction.CommentSystemTagAdded: return 'Comment System Tag Added';
      case AuditAction.CommentSystemTagRemoved: return 'Comment System Tag Removed';
      case AuditAction.SystemTagCreated: return 'System Tag Created';
      case AuditAction.SystemTagUpdated: return 'System Tag Updated';
      case AuditAction.SystemTagDeleted: return 'System Tag Deleted';
      case AuditAction.IpBlocked: return 'IP Blocked';
      case AuditAction.IpUnblocked: return 'IP Unblocked';
      case AuditAction.SecurityIncidentReported: return 'Security Incident Reported';
      case AuditAction.BulkContentDeleted: return 'Bulk Content Deleted';
      case AuditAction.BulkContentHidden: return 'Bulk Content Hidden';
      case AuditAction.BulkUsersActioned: return 'Bulk Users Actioned';
      default: return 'Unknown Action';
    }
  };

  const getCategoryColor = (action: AuditAction) => {
    if (action >= 100 && action < 200) return 'bg-blue-100 text-blue-800';
    if (action >= 200 && action < 300) return 'bg-orange-100 text-orange-800';
    if (action >= 300 && action < 400) return 'bg-purple-100 text-purple-800';
    if (action >= 400 && action < 500) return 'bg-red-100 text-red-800';
    if (action >= 500) return 'bg-gray-100 text-gray-800';
    return 'bg-gray-100 text-gray-800';
  };

  const filteredLogs = logs.filter(log =>
    log.performedByUsername.toLowerCase().includes(searchTerm.toLowerCase()) ||
    (log.targetUsername && log.targetUsername.toLowerCase().includes(searchTerm.toLowerCase())) ||
    (log.reason && log.reason.toLowerCase().includes(searchTerm.toLowerCase()))
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Audit Logs</h1>
        <p className="text-gray-600">Track all administrative actions and changes</p>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow p-6">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Search</label>
            <div className="relative">
              <Search className="h-5 w-5 text-gray-400 absolute left-3 top-3" />
              <input
                type="text"
                placeholder="Search logs..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10 w-full border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Action Type</label>
            <select
              value={actionFilter}
              onChange={(e) => setActionFilter(e.target.value as AuditAction | '')}
              className="w-full border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">All Actions</option>
              <optgroup label="User Actions">
                <option value={AuditAction.UserSuspended}>User Suspended</option>
                <option value={AuditAction.UserBanned}>User Banned</option>
                <option value={AuditAction.UserUnsuspended}>User Unsuspended</option>
                <option value={AuditAction.UserRoleChanged}>Role Changed</option>
              </optgroup>
              <optgroup label="Content Actions">
                <option value={AuditAction.PostHidden}>Post Hidden</option>
                <option value={AuditAction.PostDeleted}>Post Deleted</option>
                <option value={AuditAction.CommentHidden}>Comment Hidden</option>
                <option value={AuditAction.CommentDeleted}>Comment Deleted</option>
              </optgroup>
              <optgroup label="System Actions">
                <option value={AuditAction.SystemTagCreated}>System Tag Created</option>
                <option value={AuditAction.SystemTagUpdated}>System Tag Updated</option>
                <option value={AuditAction.SystemTagDeleted}>System Tag Deleted</option>
              </optgroup>
            </select>
          </div>
          <div className="flex items-end">
            <button
              onClick={fetchLogs}
              className="w-full bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 transition-colors"
            >
              Apply Filters
            </button>
          </div>
        </div>
      </div>

      {/* Audit Logs */}
      <div className="bg-white rounded-lg shadow overflow-hidden">
        {loading ? (
          <div className="flex items-center justify-center h-64">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
          </div>
        ) : (
          <div className="divide-y divide-gray-200">
            {filteredLogs.map((log) => (
              <div key={log.id} className="p-6 hover:bg-gray-50">
                <div className="flex items-start justify-between">
                  <div className="flex items-start space-x-3">
                    <div className="flex-shrink-0">
                      {getActionIcon(log.action)}
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center space-x-2 mb-1">
                        <span className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getCategoryColor(log.action)}`}>
                          {getActionCategory(log.action)}
                        </span>
                        <span className="text-sm font-medium text-gray-900">
                          {getActionName(log.action)}
                        </span>
                      </div>
                      <div className="text-sm text-gray-600">
                        <span className="font-medium">@{log.performedByUsername}</span>
                        {log.targetUsername && (
                          <>
                            <span> performed action on </span>
                            <span className="font-medium">@{log.targetUsername}</span>
                          </>
                        )}
                        {log.targetPostId && (
                          <>
                            <span> on post </span>
                            <span className="font-medium">#{log.targetPostId}</span>
                          </>
                        )}
                        {log.targetCommentId && (
                          <>
                            <span> on comment </span>
                            <span className="font-medium">#{log.targetCommentId}</span>
                          </>
                        )}
                      </div>
                      {log.reason && (
                        <div className="mt-2 text-sm text-gray-700">
                          <span className="font-medium">Reason:</span> {log.reason}
                        </div>
                      )}
                      {expandedLog === log.id && log.details && (
                        <div className="mt-2 text-sm text-gray-600">
                          <span className="font-medium">Details:</span> {log.details}
                        </div>
                      )}
                      {expandedLog === log.id && log.ipAddress && (
                        <div className="mt-1 text-sm text-gray-500">
                          <span className="font-medium">IP Address:</span> {log.ipAddress}
                        </div>
                      )}
                    </div>
                  </div>
                  <div className="flex items-center space-x-2">
                    <div className="text-right">
                      <div className="text-sm text-gray-500 flex items-center">
                        <Calendar className="h-4 w-4 mr-1" />
                        {new Date(log.createdAt).toLocaleDateString()}
                      </div>
                      <div className="text-xs text-gray-400">
                        {new Date(log.createdAt).toLocaleTimeString()}
                      </div>
                    </div>
                    {(log.details || log.ipAddress) && (
                      <button
                        onClick={() => setExpandedLog(expandedLog === log.id ? null : log.id)}
                        className="text-blue-600 hover:text-blue-800 text-sm"
                      >
                        {expandedLog === log.id ? 'Less' : 'More'}
                      </button>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {filteredLogs.length === 0 && !loading && (
        <div className="text-center py-12">
          <FileSearch className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <p className="text-gray-500">No audit logs found matching your criteria</p>
        </div>
      )}

      {/* Pagination */}
      {filteredLogs.length > 0 && (
        <div className="flex justify-center space-x-2">
          <button
            onClick={() => setPage(Math.max(1, page - 1))}
            disabled={page === 1}
            className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            Previous
          </button>
          <span className="px-4 py-2 text-gray-700">
            Page {page}
          </span>
          <button
            onClick={() => setPage(page + 1)}
            disabled={filteredLogs.length < 25}
            className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}
