'use client';

import { useEffect, useState } from 'react';
import {
  AlertTriangle,
  Search,
  RefreshCw,
  RotateCcw,
  Clock,
  XCircle,
  ExternalLink,
} from 'lucide-react';

interface FailedPayment {
  id: number;
  userId: number;
  username: string;
  paymentProvider: string;
  externalSubscriptionId: string;
  amount: number;
  currency: string;
  failureReason: string;
  retryCount: number;
  lastRetryDate: string;
  nextRetryDate: string;
  failedAt: string;
  canRetry: boolean;
}

interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export default function FailedPaymentsPage() {
  const [failedPayments, setFailedPayments] = useState<PagedResult<FailedPayment>>({
    items: [],
    totalCount: 0,
    page: 1,
    pageSize: 25,
    totalPages: 0,
    hasNextPage: false,
    hasPreviousPage: false,
  });
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [retryingPayments, setRetryingPayments] = useState<Set<number>>(new Set());

  useEffect(() => {
    fetchFailedPayments();
  }, [currentPage]);

  const fetchFailedPayments = async () => {
    try {
      setLoading(true);
      const params = new URLSearchParams({
        page: currentPage.toString(),
        pageSize: '25',
      });

      const response = await fetch(`/api/admin/payments/failed-payments?${params}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
        },
      });

      if (response.ok) {
        const data = await response.json();
        setFailedPayments(data);
      }
    } catch (error) {
      console.error('Failed to fetch failed payments:', error);
    } finally {
      setLoading(false);
    }
  };

  const retryPayment = async (paymentId: number) => {
    if (!confirm('Are you sure you want to retry this payment?')) return;

    try {
      setRetryingPayments(prev => new Set(prev).add(paymentId));
      
      const response = await fetch(`/api/admin/payments/failed-payments/${paymentId}/retry`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
        },
      });

      if (response.ok) {
        fetchFailedPayments();
        alert('Payment retry initiated successfully');
      } else {
        alert('Failed to retry payment');
      }
    } catch (error) {
      console.error('Failed to retry payment:', error);
      alert('Failed to retry payment');
    } finally {
      setRetryingPayments(prev => {
        const newSet = new Set(prev);
        newSet.delete(paymentId);
        return newSet;
      });
    }
  };

  const formatCurrency = (amount: number, currency: string) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency || 'USD',
    }).format(amount);
  };

  const formatDate = (dateString: string) => {
    if (!dateString) return '-';
    return new Date(dateString).toLocaleString();
  };

  const getRetryStatusColor = (retryCount: number, canRetry: boolean) => {
    if (!canRetry) return 'text-red-600';
    if (retryCount === 0) return 'text-yellow-600';
    if (retryCount < 3) return 'text-orange-600';
    return 'text-red-600';
  };

  const filteredPayments = failedPayments.items.filter(payment =>
    payment.username.toLowerCase().includes(searchTerm.toLowerCase()) ||
    payment.failureReason.toLowerCase().includes(searchTerm.toLowerCase()) ||
    payment.externalSubscriptionId.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Failed Payments</h1>
          <p className="text-gray-600">Monitor and retry failed payment attempts</p>
        </div>
        <button
          onClick={fetchFailedPayments}
          className="flex items-center px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
        >
          <RefreshCw className="w-4 h-4 mr-2" />
          Refresh
        </button>
      </div>

      {/* Search */}
      <div className="bg-white rounded-lg shadow p-6">
        <div className="max-w-md">
          <label className="block text-sm font-medium text-gray-700 mb-2">Search</label>
          <div className="relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
            <input
              type="text"
              placeholder="Search by user, reason, or subscription ID..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-10 w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </div>
      </div>

      {/* Failed Payments Table */}
      <div className="bg-white rounded-lg shadow overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="text-lg font-semibold text-gray-900 flex items-center">
            <AlertTriangle className="h-5 w-5 text-red-500 mr-2" />
            Failed Payments ({failedPayments.totalCount})
          </h2>
        </div>
        
        {loading ? (
          <div className="flex items-center justify-center h-64">
            <RefreshCw className="h-8 w-8 animate-spin text-blue-500" />
          </div>
        ) : filteredPayments.length === 0 ? (
          <div className="text-center py-12">
            <AlertTriangle className="w-12 h-12 text-gray-400 mx-auto mb-4" />
            <h3 className="text-lg font-medium text-gray-900 mb-2">No failed payments</h3>
            <p className="text-gray-600">All payments are processing successfully.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    User
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Amount
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Provider
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Failure Reason
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Retry Status
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Failed At
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Next Retry
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {filteredPayments.map((payment) => (
                  <tr key={payment.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div>
                        <div className="text-sm font-medium text-gray-900">
                          {payment.username}
                        </div>
                        <div className="text-sm text-gray-500">
                          {payment.externalSubscriptionId}
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                      {formatCurrency(payment.amount, payment.currency)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {payment.paymentProvider}
                    </td>
                    <td className="px-6 py-4">
                      <div className="text-sm text-red-600 max-w-xs">
                        {payment.failureReason}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className={`text-sm font-medium ${getRetryStatusColor(payment.retryCount, payment.canRetry)}`}>
                        {payment.retryCount} / 3 retries
                      </div>
                      {payment.lastRetryDate && (
                        <div className="text-xs text-gray-500">
                          Last: {formatDate(payment.lastRetryDate)}
                        </div>
                      )}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {formatDate(payment.failedAt)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {payment.canRetry ? formatDate(payment.nextRetryDate) : 'Max retries reached'}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                      {payment.canRetry && (
                        <button
                          onClick={() => retryPayment(payment.id)}
                          disabled={retryingPayments.has(payment.id)}
                          className="text-blue-600 hover:text-blue-900 disabled:opacity-50"
                          title="Retry payment"
                        >
                          {retryingPayments.has(payment.id) ? (
                            <RefreshCw className="w-4 h-4 animate-spin" />
                          ) : (
                            <RotateCcw className="w-4 h-4" />
                          )}
                        </button>
                      )}
                      <a
                        href={`/admin/users/${payment.userId}`}
                        className="text-gray-600 hover:text-gray-900"
                        title="View user"
                      >
                        <ExternalLink className="w-4 h-4" />
                      </a>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination */}
        {failedPayments.totalPages > 1 && (
          <div className="px-6 py-4 border-t border-gray-200 flex items-center justify-between">
            <div className="text-sm text-gray-700">
              Showing {((currentPage - 1) * 25) + 1} to {Math.min(currentPage * 25, failedPayments.totalCount)} of {failedPayments.totalCount} results
            </div>
            <div className="flex space-x-2">
              <button
                onClick={() => setCurrentPage(currentPage - 1)}
                disabled={!failedPayments.hasPreviousPage}
                className="px-3 py-1 border border-gray-300 rounded text-sm disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
              >
                Previous
              </button>
              <span className="px-3 py-1 text-sm text-gray-700">
                Page {currentPage} of {failedPayments.totalPages}
              </span>
              <button
                onClick={() => setCurrentPage(currentPage + 1)}
                disabled={!failedPayments.hasNextPage}
                className="px-3 py-1 border border-gray-300 rounded text-sm disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
              >
                Next
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
