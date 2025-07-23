'use client';

import { useEffect, useState } from 'react';
import {
  BarChart3,
  TrendingUp,
  TrendingDown,
  DollarSign,
  Users,
  CreditCard,
  Calendar,
  RefreshCw,
} from 'lucide-react';

interface PaymentAnalytics {
  totalRevenue: number;
  monthlyRecurringRevenue: number;
  averageRevenuePerUser: number;
  totalSubscriptions: number;
  activeSubscriptions: number;
  cancelledSubscriptions: number;
  totalTransactions: number;
  successfulTransactions: number;
  failedTransactions: number;
  successRate: number;
  churnRate: number;
  growthRate: number;
  refundRate: number;
  averageTransactionValue: number;
}

interface RevenueAnalytics {
  totalRevenue: number;
  revenueByProvider: Array<{
    provider: string;
    revenue: number;
    percentage: number;
  }>;
  revenueByPeriod: Array<{
    period: string;
    revenue: number;
    transactions: number;
  }>;
  topSubscriptionTiers: Array<{
    tierName: string;
    revenue: number;
    subscriptions: number;
  }>;
}

export default function PaymentAnalyticsPage() {
  const [analytics, setAnalytics] = useState<PaymentAnalytics | null>(null);
  const [revenueAnalytics, setRevenueAnalytics] = useState<RevenueAnalytics | null>(null);
  const [loading, setLoading] = useState(true);
  const [selectedPeriod, setSelectedPeriod] = useState(30);

  useEffect(() => {
    fetchAnalytics();
  }, [selectedPeriod]);

  const fetchAnalytics = async () => {
    try {
      setLoading(true);
      
      // Fetch payment analytics
      const analyticsResponse = await fetch(`/api/admin/payments/analytics/overview?days=${selectedPeriod}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
        },
      });
      
      if (analyticsResponse.ok) {
        const analyticsData = await analyticsResponse.json();
        setAnalytics(analyticsData);
      }

      // Fetch revenue analytics
      const revenueResponse = await fetch(`/api/admin/payments/analytics/revenue?days=${selectedPeriod}`, {
        headers: {
          'Authorization': `Bearer ${localStorage.getItem('token')}`,
        },
      });
      
      if (revenueResponse.ok) {
        const revenueData = await revenueResponse.json();
        setRevenueAnalytics(revenueData);
      }
    } catch (error) {
      console.error('Failed to fetch analytics:', error);
    } finally {
      setLoading(false);
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

  const formatNumber = (value: number) => {
    return new Intl.NumberFormat('en-US').format(value);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <RefreshCw className="h-8 w-8 animate-spin text-blue-500" />
      </div>
    );
  }

  const metricCards = [
    {
      title: 'Total Revenue',
      value: formatCurrency(analytics?.totalRevenue || 0),
      icon: DollarSign,
      color: 'bg-green-500',
      trend: analytics?.growthRate || 0,
    },
    {
      title: 'Monthly Recurring Revenue',
      value: formatCurrency(analytics?.monthlyRecurringRevenue || 0),
      icon: TrendingUp,
      color: 'bg-blue-500',
      trend: analytics?.growthRate || 0,
    },
    {
      title: 'Active Subscriptions',
      value: formatNumber(analytics?.activeSubscriptions || 0),
      icon: Users,
      color: 'bg-purple-500',
      trend: -(analytics?.churnRate || 0),
    },
    {
      title: 'Success Rate',
      value: formatPercentage(analytics?.successRate || 0),
      icon: CreditCard,
      color: 'bg-emerald-500',
      trend: 0,
    },
    {
      title: 'Average Revenue Per User',
      value: formatCurrency(analytics?.averageRevenuePerUser || 0),
      icon: DollarSign,
      color: 'bg-indigo-500',
      trend: 0,
    },
    {
      title: 'Churn Rate',
      value: formatPercentage(analytics?.churnRate || 0),
      icon: TrendingDown,
      color: 'bg-red-500',
      trend: -(analytics?.churnRate || 0),
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Payment Analytics</h1>
          <p className="text-gray-600">Revenue insights and payment performance metrics</p>
        </div>
        <div className="flex items-center space-x-4">
          <select
            value={selectedPeriod}
            onChange={(e) => setSelectedPeriod(Number(e.target.value))}
            className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value={7}>Last 7 days</option>
            <option value={30}>Last 30 days</option>
            <option value={90}>Last 90 days</option>
            <option value={365}>Last year</option>
          </select>
          <button
            onClick={fetchAnalytics}
            className="flex items-center px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
          >
            <RefreshCw className="w-4 h-4 mr-2" />
            Refresh
          </button>
        </div>
      </div>

      {/* Metrics Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {metricCards.map((card) => {
          const Icon = card.icon;
          const isPositiveTrend = card.trend > 0;
          const isNegativeTrend = card.trend < 0;
          
          return (
            <div key={card.title} className="bg-white rounded-lg shadow p-6">
              <div className="flex items-center justify-between">
                <div className={`${card.color} rounded-lg p-3`}>
                  <Icon className="h-6 w-6 text-white" />
                </div>
                {card.trend !== 0 && (
                  <div className={`flex items-center text-sm ${
                    isPositiveTrend ? 'text-green-600' : isNegativeTrend ? 'text-red-600' : 'text-gray-600'
                  }`}>
                    {isPositiveTrend ? (
                      <TrendingUp className="w-4 h-4 mr-1" />
                    ) : isNegativeTrend ? (
                      <TrendingDown className="w-4 h-4 mr-1" />
                    ) : null}
                    {formatPercentage(Math.abs(card.trend))}
                  </div>
                )}
              </div>
              <div className="mt-4">
                <p className="text-sm font-medium text-gray-600">{card.title}</p>
                <p className="text-2xl font-bold text-gray-900">{card.value}</p>
              </div>
            </div>
          );
        })}
      </div>

      {/* Revenue by Provider */}
      {revenueAnalytics?.revenueByProvider && (
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Revenue by Provider</h2>
          <div className="space-y-4">
            {revenueAnalytics.revenueByProvider.map((provider) => (
              <div key={provider.provider} className="flex items-center justify-between">
                <div className="flex items-center">
                  <div className="w-4 h-4 bg-blue-500 rounded mr-3"></div>
                  <span className="text-sm font-medium text-gray-900">{provider.provider}</span>
                </div>
                <div className="flex items-center space-x-4">
                  <span className="text-sm text-gray-600">{formatPercentage(provider.percentage)}</span>
                  <span className="text-sm font-medium text-gray-900">{formatCurrency(provider.revenue)}</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Top Subscription Tiers */}
      {revenueAnalytics?.topSubscriptionTiers && (
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Top Subscription Tiers</h2>
          <div className="overflow-x-auto">
            <table className="min-w-full">
              <thead>
                <tr className="border-b border-gray-200">
                  <th className="text-left py-2 text-sm font-medium text-gray-600">Tier</th>
                  <th className="text-right py-2 text-sm font-medium text-gray-600">Subscriptions</th>
                  <th className="text-right py-2 text-sm font-medium text-gray-600">Revenue</th>
                </tr>
              </thead>
              <tbody>
                {revenueAnalytics.topSubscriptionTiers.map((tier, index) => (
                  <tr key={tier.tierName} className="border-b border-gray-100">
                    <td className="py-3 text-sm text-gray-900">{tier.tierName}</td>
                    <td className="py-3 text-sm text-gray-900 text-right">{formatNumber(tier.subscriptions)}</td>
                    <td className="py-3 text-sm font-medium text-gray-900 text-right">{formatCurrency(tier.revenue)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Transaction Summary */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Transaction Summary</h2>
          <div className="space-y-4">
            <div className="flex justify-between">
              <span className="text-sm text-gray-600">Total Transactions</span>
              <span className="text-sm font-medium text-gray-900">{formatNumber(analytics?.totalTransactions || 0)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-sm text-gray-600">Successful</span>
              <span className="text-sm font-medium text-green-600">{formatNumber(analytics?.successfulTransactions || 0)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-sm text-gray-600">Failed</span>
              <span className="text-sm font-medium text-red-600">{formatNumber(analytics?.failedTransactions || 0)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-sm text-gray-600">Average Transaction Value</span>
              <span className="text-sm font-medium text-gray-900">{formatCurrency(analytics?.averageTransactionValue || 0)}</span>
            </div>
          </div>
        </div>

        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Subscription Summary</h2>
          <div className="space-y-4">
            <div className="flex justify-between">
              <span className="text-sm text-gray-600">Total Subscriptions</span>
              <span className="text-sm font-medium text-gray-900">{formatNumber(analytics?.totalSubscriptions || 0)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-sm text-gray-600">Active</span>
              <span className="text-sm font-medium text-green-600">{formatNumber(analytics?.activeSubscriptions || 0)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-sm text-gray-600">Cancelled</span>
              <span className="text-sm font-medium text-red-600">{formatNumber(analytics?.cancelledSubscriptions || 0)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-sm text-gray-600">Refund Rate</span>
              <span className="text-sm font-medium text-gray-900">{formatPercentage(analytics?.refundRate || 0)}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
