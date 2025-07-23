'use client';

import { useEffect, useState } from 'react';
import {
  CreditCard,
  Search,
  RefreshCw,
  CheckCircle,
  XCircle,
  Clock,
  RotateCcw,
  ExternalLink,
  DollarSign,
} from 'lucide-react';

interface Transaction {
  id: number;
  userId: number;
  username: string;
  paymentProvider: string;
  externalTransactionId: string;
  amount: number;
  currency: string;
  status: string;
  type: string;
  description: string;
  processedAt: string;
  failedAt: string;
  failureReason: string;
  createdAt: string;
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

export default function PaymentTransactionsPage() {
  const [transactions, setTransactions] = useState<PagedResult<Transaction>>({
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
  const [statusFilter, setStatusFilter] = useState('');
  const [providerFilter, setProviderFilter] = useState('');
  const [typeFilter, setTypeFilter] = useState('');
  const [currentPage, setCurrentPage] = useState(1);

  useEffect(() => {
    fetchTransactions();
  }, [currentPage, statusFilter, providerFilter, typeFilter]);

  const fetchTransactions = async () => {
    try {
      setLoading(true);
      const params = new URLSearchParams({
        page: currentPage.toString(),
        pageSize: '25',
        ...(statusFilter && { status: statusFilter }),
        ...(providerFilter && { provider: providerFilter }),
      });

      const response = await fetch(`/api/admin/payments/transactions?${params}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
        },
      });

      if (response.ok) {
        const data = await response.json();
        setTransactions(data);
      }
    } catch (error) {
      console.error('Failed to fetch transactions:', error);
    } finally {
      setLoading(false);
    }
  };

  const refundTransaction = async (transactionId: number) => {
    const reason = prompt('Enter refund reason:');
    if (!reason) return;

    try {
      const response = await fetch(`/api/admin/payments/transactions/${transactionId}/refund`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
        },
        body: JSON.stringify({
          reason: reason,
        }),
      });

      if (response.ok) {
        fetchTransactions();
        alert('Refund processed successfully');
      } else {
        alert('Failed to process refund');
      }
    } catch (error) {
      console.error('Failed to refund transaction:', error);
      alert('Failed to process refund');
    }
  };

  const getStatusBadge = (status: string) => {
    const statusConfig = {
      Completed: { color: 'bg-green-100 text-green-800', icon: CheckCircle },
      Failed: { color: 'bg-red-100 text-red-800', icon: XCircle },
      Pending: { color: 'bg-yellow-100 text-yellow-800', icon: Clock },
      Processing: { color: 'bg-blue-100 text-blue-800', icon: Clock },
      Refunded: { color: 'bg-gray-100 text-gray-800', icon: RotateCcw },
    };

    const config = statusConfig[status as keyof typeof statusConfig] || statusConfig.Pending;
    const Icon = config.icon;

    return (
      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${config.color}`}>
        <Icon className="w-3 h-3 mr-1" />
        {status}
      </span>
    );
  };

  const getTypeBadge = (type: string) => {
    const typeConfig = {
      Subscription: { color: 'bg-blue-100 text-blue-800' },
      OneTime: { color: 'bg-purple-100 text-purple-800' },
      Refund: { color: 'bg-gray-100 text-gray-800' },
      Chargeback: { color: 'bg-red-100 text-red-800' },
    };

    const config = typeConfig[type as keyof typeof typeConfig] || typeConfig.OneTime;

    return (
      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${config.color}`}>
        {type}
      </span>
    );
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

  const filteredTransactions = transactions.items.filter(txn =>
    txn.username.toLowerCase().includes(searchTerm.toLowerCase()) ||
    txn.externalTransactionId.toLowerCase().includes(searchTerm.toLowerCase()) ||
    (txn.description && txn.description.toLowerCase().includes(searchTerm.toLowerCase()))
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Transaction History</h1>
          <p className="text-gray-600">View and manage payment transactions</p>
        </div>
        <button
          onClick={fetchTransactions}
          className="flex items-center px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
        >
          <RefreshCw className="w-4 h-4 mr-2" />
          Refresh
        </button>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow p-6">
        <div className="grid grid-cols-1 md:grid-cols-5 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Search</label>
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
              <input
                type="text"
                placeholder="Search transactions..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10 w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
          
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Status</label>
            <select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">All Statuses</option>
              <option value="Completed">Completed</option>
              <option value="Failed">Failed</option>
              <option value="Pending">Pending</option>
              <option value="Processing">Processing</option>
              <option value="Refunded">Refunded</option>
            </select>
          </div>
          
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Provider</label>
            <select
              value={providerFilter}
              onChange={(e) => setProviderFilter(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">All Providers</option>
              <option value="PayPal">PayPal</option>
              <option value="Stripe">Stripe</option>
            </select>
          </div>
          
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Type</label>
            <select
              value={typeFilter}
              onChange={(e) => setTypeFilter(e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">All Types</option>
              <option value="Subscription">Subscription</option>
              <option value="OneTime">One-time</option>
              <option value="Refund">Refund</option>
              <option value="Chargeback">Chargeback</option>
            </select>
          </div>
          
          <div className="flex items-end">
            <button
              onClick={() => {
                setStatusFilter('');
                setProviderFilter('');
                setTypeFilter('');
                setSearchTerm('');
                setCurrentPage(1);
              }}
              className="w-full px-4 py-2 bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 transition-colors"
            >
              Clear Filters
            </button>
          </div>
        </div>
      </div>

      {/* Transactions Table */}
      <div className="bg-white rounded-lg shadow overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="text-lg font-semibold text-gray-900">
            Transactions ({transactions.totalCount})
          </h2>
        </div>
        
        {loading ? (
          <div className="flex items-center justify-center h-64">
            <RefreshCw className="h-8 w-8 animate-spin text-blue-500" />
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Transaction
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    User
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Amount
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Status
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Type
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Provider
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Date
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {filteredTransactions.map((transaction) => (
                  <tr key={transaction.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div>
                        <div className="text-sm font-medium text-gray-900">
                          {transaction.externalTransactionId}
                        </div>
                        {transaction.description && (
                          <div className="text-sm text-gray-500">{transaction.description}</div>
                        )}
                        {transaction.failureReason && (
                          <div className="text-sm text-red-600 mt-1">{transaction.failureReason}</div>
                        )}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {transaction.username}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className={`text-sm font-medium ${
                        transaction.amount < 0 ? 'text-red-600' : 'text-gray-900'
                      }`}>
                        {formatCurrency(transaction.amount, transaction.currency)}
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      {getStatusBadge(transaction.status)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      {getTypeBadge(transaction.type)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {transaction.paymentProvider}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {formatDate(transaction.processedAt || transaction.createdAt)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                      {transaction.status === 'Completed' && transaction.type === 'Subscription' && (
                        <button
                          onClick={() => refundTransaction(transaction.id)}
                          className="text-red-600 hover:text-red-900"
                          title="Refund transaction"
                        >
                          <RotateCcw className="w-4 h-4" />
                        </button>
                      )}
                      <a
                        href={`/admin/users/${transaction.userId}`}
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
        {transactions.totalPages > 1 && (
          <div className="px-6 py-4 border-t border-gray-200 flex items-center justify-between">
            <div className="text-sm text-gray-700">
              Showing {((currentPage - 1) * 25) + 1} to {Math.min(currentPage * 25, transactions.totalCount)} of {transactions.totalCount} results
            </div>
            <div className="flex space-x-2">
              <button
                onClick={() => setCurrentPage(currentPage - 1)}
                disabled={!transactions.hasPreviousPage}
                className="px-3 py-1 border border-gray-300 rounded text-sm disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-50"
              >
                Previous
              </button>
              <span className="px-3 py-1 text-sm text-gray-700">
                Page {currentPage} of {transactions.totalPages}
              </span>
              <button
                onClick={() => setCurrentPage(currentPage + 1)}
                disabled={!transactions.hasNextPage}
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
