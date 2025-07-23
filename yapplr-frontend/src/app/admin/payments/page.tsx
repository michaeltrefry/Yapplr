'use client';

import { useEffect, useState } from 'react';
import { adminApi } from '@/lib/api';
import {
  DollarSign,
  CreditCard,
  TrendingUp,
  Users,
  AlertTriangle,
  CheckCircle,
  XCircle,
  Clock,
  RefreshCw,
  Eye,
  Settings,
  BarChart3,
} from 'lucide-react';
import Link from 'next/link';

interface PaymentStats {
  totalRevenue: number;
  monthlyRecurringRevenue: number;
  activeSubscriptions: number;
  totalTransactions: number;
  successfulTransactions: number;
  failedTransactions: number;
  successRate: number;
  churnRate: number;
}

interface ProviderStatus {
  name: string;
  isEnabled: boolean;
  isAvailable: boolean;
  environment: string;
  activeSubscriptions: number;
  totalRevenue: number;
  healthStatus: string;
}

export default function PaymentAdminPage() {
  const [stats, setStats] = useState<PaymentStats | null>(null);
  const [providers, setProviders] = useState<ProviderStatus[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchPaymentData();
  }, []);

  const fetchPaymentData = async () => {
    try {
      setLoading(true);
      
      // Fetch payment analytics
      const analyticsResponse = await fetch('/api/admin/payments/analytics/overview', {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
        },
      });
      
      if (analyticsResponse.ok) {
        const analyticsData = await analyticsResponse.json();
        setStats(analyticsData);
      }

      // Fetch provider status
      const providersResponse = await fetch('/api/admin/payments/providers', {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
        },
      });
      
      if (providersResponse.ok) {
        const providersData = await providersResponse.json();
        setProviders(providersData);
      }
    } catch (error) {
      console.error('Failed to fetch payment data:', error);
    } finally {
      setLoading(false);
    }
  };

  const testProvider = async (providerName: string) => {
    try {
      const response = await fetch(`/api/admin/payments/providers/${providerName}/test`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
        },
      });
      
      if (response.ok) {
        const result = await response.json();
        alert(result.success ? `${providerName} is working correctly` : `${providerName} test failed`);
      }
    } catch (error) {
      console.error(`Failed to test ${providerName}:`, error);
      alert(`Failed to test ${providerName}`);
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
    }).format(amount);
  };

  const formatPercentage = (value: number) => {
    return `${value.toFixed(1)}%`;
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <RefreshCw className="h-8 w-8 animate-spin text-blue-500" />
      </div>
    );
  }

  const statCards = [
    {
      title: 'Total Revenue',
      value: formatCurrency(stats?.totalRevenue || 0),
      icon: DollarSign,
      color: 'bg-green-500',
      href: '/admin/payments/analytics',
    },
    {
      title: 'Monthly Recurring Revenue',
      value: formatCurrency(stats?.monthlyRecurringRevenue || 0),
      icon: TrendingUp,
      color: 'bg-blue-500',
      href: '/admin/payments/analytics',
    },
    {
      title: 'Active Subscriptions',
      value: stats?.activeSubscriptions || 0,
      icon: Users,
      color: 'bg-purple-500',
      href: '/admin/payments/subscriptions',
    },
    {
      title: 'Success Rate',
      value: formatPercentage(stats?.successRate || 0),
      icon: CheckCircle,
      color: 'bg-emerald-500',
      href: '/admin/payments/transactions',
    },
    {
      title: 'Total Transactions',
      value: stats?.totalTransactions || 0,
      icon: CreditCard,
      color: 'bg-indigo-500',
      href: '/admin/payments/transactions',
    },
    {
      title: 'Failed Payments',
      value: stats?.failedTransactions || 0,
      icon: XCircle,
      color: 'bg-red-500',
      href: '/admin/payments/failed',
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Payment Administration</h1>
        <p className="text-gray-600">Monitor and manage payment processing and subscriptions</p>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {statCards.map((card) => {
          const Icon = card.icon;
          return (
            <Link
              key={card.title}
              href={card.href}
              className="bg-white rounded-lg shadow p-6 hover:shadow-md transition-shadow"
            >
              <div className="flex items-center">
                <div className={`${card.color} rounded-lg p-3`}>
                  <Icon className="h-6 w-6 text-white" />
                </div>
                <div className="ml-4">
                  <p className="text-sm font-medium text-gray-600">{card.title}</p>
                  <p className="text-2xl font-bold text-gray-900">{card.value}</p>
                </div>
              </div>
            </Link>
          );
        })}
      </div>

      {/* Payment Providers */}
      <div className="bg-white rounded-lg shadow">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="text-lg font-semibold text-gray-900">Payment Providers</h2>
        </div>
        <div className="p-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {providers.map((provider) => (
              <div key={provider.name} className="border border-gray-200 rounded-lg p-4">
                <div className="flex items-center justify-between mb-4">
                  <div className="flex items-center">
                    <div className={`w-3 h-3 rounded-full mr-3 ${
                      provider.isAvailable ? 'bg-green-500' : 'bg-red-500'
                    }`} />
                    <h3 className="text-lg font-medium text-gray-900">{provider.name}</h3>
                  </div>
                  <div className="flex items-center space-x-2">
                    <span className={`px-2 py-1 text-xs font-medium rounded-full ${
                      provider.isEnabled 
                        ? 'bg-green-100 text-green-800' 
                        : 'bg-gray-100 text-gray-800'
                    }`}>
                      {provider.isEnabled ? 'Enabled' : 'Disabled'}
                    </span>
                    <span className="px-2 py-1 text-xs font-medium rounded-full bg-blue-100 text-blue-800">
                      {provider.environment}
                    </span>
                  </div>
                </div>
                
                <div className="grid grid-cols-2 gap-4 mb-4">
                  <div>
                    <p className="text-sm text-gray-600">Active Subscriptions</p>
                    <p className="text-lg font-semibold text-gray-900">{provider.activeSubscriptions}</p>
                  </div>
                  <div>
                    <p className="text-sm text-gray-600">Total Revenue</p>
                    <p className="text-lg font-semibold text-gray-900">{formatCurrency(provider.totalRevenue)}</p>
                  </div>
                </div>
                
                <div className="flex items-center justify-between">
                  <span className={`text-sm font-medium ${
                    provider.healthStatus === 'Healthy' ? 'text-green-600' : 'text-red-600'
                  }`}>
                    {provider.healthStatus}
                  </span>
                  <button
                    onClick={() => testProvider(provider.name)}
                    className="px-3 py-1 text-sm bg-blue-100 text-blue-700 rounded hover:bg-blue-200 transition-colors"
                  >
                    Test Connection
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Quick Actions */}
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Quick Actions</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <Link
            href="/admin/payments/subscriptions"
            className="flex items-center p-4 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
          >
            <Users className="h-8 w-8 text-blue-500 mr-3" />
            <div>
              <p className="font-medium text-gray-900">Manage Subscriptions</p>
              <p className="text-sm text-gray-600">View and manage user subscriptions</p>
            </div>
          </Link>
          
          <Link
            href="/admin/payments/transactions"
            className="flex items-center p-4 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
          >
            <CreditCard className="h-8 w-8 text-green-500 mr-3" />
            <div>
              <p className="font-medium text-gray-900">Transaction History</p>
              <p className="text-sm text-gray-600">View payment transactions</p>
            </div>
          </Link>
          
          <Link
            href="/admin/payments/failed"
            className="flex items-center p-4 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
          >
            <AlertTriangle className="h-8 w-8 text-red-500 mr-3" />
            <div>
              <p className="font-medium text-gray-900">Failed Payments</p>
              <p className="text-sm text-gray-600">Review and retry failed payments</p>
            </div>
          </Link>
          
          <Link
            href="/admin/payments/analytics"
            className="flex items-center p-4 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
          >
            <BarChart3 className="h-8 w-8 text-purple-500 mr-3" />
            <div>
              <p className="font-medium text-gray-900">Analytics</p>
              <p className="text-sm text-gray-600">Revenue and payment insights</p>
            </div>
          </Link>
        </div>
      </div>
    </div>
  );
}
