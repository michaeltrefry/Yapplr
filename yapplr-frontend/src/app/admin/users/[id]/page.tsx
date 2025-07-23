'use client';

import { useEffect, useState } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { adminApi } from '@/lib/api';
import { AdminUserDetails, UserRole, UserStatus } from '@/types';
import { SuspendUserModal } from '@/components/admin';
import SubscriptionTierBadge from '@/components/SubscriptionTierBadge';
import {
  ArrowLeft,
  User,
  Mail,
  Calendar,
  Shield,
  TrendingUp,
  Activity,
  AlertTriangle,
  Settings,
  Clock,
  Ban,
  UserCheck,
  Edit,
  Save,
  X,
  CheckCircle,
  XCircle,
  Zap,
  BarChart3,
  History,
  Flag,
} from 'lucide-react';

export default function UserDetailsPage() {
  const params = useParams();
  const router = useRouter();
  const userId = params?.id ? parseInt(params.id as string) : 0;
  
  const [userDetails, setUserDetails] = useState<AdminUserDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showSuspendModal, setShowSuspendModal] = useState(false);
  const [editingRole, setEditingRole] = useState(false);
  const [editingRateLimit, setEditingRateLimit] = useState(false);
  const [newRole, setNewRole] = useState<UserRole>(UserRole.User);
  const [roleChangeReason, setRoleChangeReason] = useState('');
  const [rateLimitSettings, setRateLimitSettings] = useState({
    rateLimitingEnabled: null as boolean | null,
    trustBasedRateLimitingEnabled: null as boolean | null,
    reason: ''
  });

  useEffect(() => {
    if (userId > 0) {
      fetchUserDetails();
    }
  }, [userId]);

  const fetchUserDetails = async () => {
    try {
      setLoading(true);
      const details = await adminApi.getUserDetails(userId);
      setUserDetails(details);
      setNewRole(details.role);
      setRateLimitSettings({
        rateLimitingEnabled: details.rateLimitingEnabled ?? null,
        trustBasedRateLimitingEnabled: details.trustBasedRateLimitingEnabled ?? null,
        reason: ''
      });
    } catch (error) {
      console.error('Failed to fetch user details:', error);
      setError('Failed to load user details');
    } finally {
      setLoading(false);
    }
  };

  const handleSuspendUser = async (userId: number) => {
    setShowSuspendModal(true);
  };

  const handleSuspendSubmit = async (reason: string, days: number | null) => {
    try {
      await adminApi.suspendUser(userId, {
        reason,
        suspendedUntil: days ? new Date(Date.now() + days * 24 * 60 * 60 * 1000).toISOString() : undefined
      });
      setShowSuspendModal(false);
      fetchUserDetails();
    } catch (error) {
      console.error('Failed to suspend user:', error);
      alert('Failed to suspend user');
    }
  };

  const handleUnsuspendUser = async (userId: number) => {
    if (!confirm('Are you sure you want to unsuspend this user?')) return;

    try {
      await adminApi.unsuspendUser(userId);
      fetchUserDetails();
    } catch (error) {
      console.error('Failed to unsuspend user:', error);
      alert('Failed to unsuspend user');
    }
  };

  const handleBanUser = async (userId: number, isShadowBan = false) => {
    const reason = prompt(`Enter reason for ${isShadowBan ? 'shadow ' : ''}ban:`);
    if (!reason) return;

    if (!confirm(`Are you sure you want to ${isShadowBan ? 'shadow ' : ''}ban this user?`)) return;

    try {
      await adminApi.banUser(userId, { reason, isShadowBan });
      fetchUserDetails();
    } catch (error) {
      console.error('Failed to ban user:', error);
      alert('Failed to ban user');
    }
  };

  const handleUnbanUser = async (userId: number) => {
    if (!confirm('Are you sure you want to unban this user?')) return;

    try {
      await adminApi.unbanUser(userId);
      fetchUserDetails();
    } catch (error) {
      console.error('Failed to unban user:', error);
      alert('Failed to unban user');
    }
  };

  const handleRoleChange = async () => {
    if (!roleChangeReason.trim()) {
      alert('Please provide a reason for the role change');
      return;
    }

    try {
      await adminApi.changeUserRole(userId, { role: newRole, reason: roleChangeReason });
      setEditingRole(false);
      setRoleChangeReason('');
      fetchUserDetails();
    } catch (error) {
      console.error('Failed to change user role:', error);
      alert('Failed to change user role');
    }
  };

  const handleRateLimitUpdate = async () => {
    if (!rateLimitSettings.reason.trim()) {
      alert('Please provide a reason for the rate limiting change');
      return;
    }

    try {
      await adminApi.updateUserRateLimitSettings(userId, rateLimitSettings);
      setEditingRateLimit(false);
      setRateLimitSettings(prev => ({ ...prev, reason: '' }));
      fetchUserDetails();
    } catch (error) {
      console.error('Failed to update rate limiting settings:', error);
      alert('Failed to update rate limiting settings');
    }
  };

  const getStatusBadge = (status: UserStatus) => {
    const statusConfig = {
      [UserStatus.Active]: { color: 'bg-green-100 text-green-800', text: 'Active' },
      [UserStatus.Suspended]: { color: 'bg-yellow-100 text-yellow-800', text: 'Suspended' },
      [UserStatus.ShadowBanned]: { color: 'bg-orange-100 text-orange-800', text: 'Shadow Banned' },
      [UserStatus.Banned]: { color: 'bg-red-100 text-red-800', text: 'Banned' },
    };

    const config = statusConfig[status];
    return (
      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${config.color}`}>
        {config.text}
      </span>
    );
  };

  const getRoleBadge = (role: UserRole) => {
    const roleConfig = {
      [UserRole.User]: { color: 'bg-gray-100 text-gray-800', text: 'User' },
      [UserRole.Moderator]: { color: 'bg-blue-100 text-blue-800', text: 'Moderator' },
      [UserRole.Admin]: { color: 'bg-purple-100 text-purple-800', text: 'Admin' },
      [UserRole.System]: { color: 'bg-indigo-100 text-indigo-800', text: 'System' },
    };

    const config = roleConfig[role];
    return (
      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${config.color}`}>
        <Shield className="h-3 w-3 mr-1" />
        {config.text}
      </span>
    );
  };

  const formatTrustScore = (score: number) => {
    return (score * 100).toFixed(1) + '%';
  };

  const getTrustScoreColor = (score: number) => {
    if (score >= 0.8) return 'text-green-600';
    if (score >= 0.6) return 'text-yellow-600';
    if (score >= 0.4) return 'text-orange-600';
    return 'text-red-600';
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error || !userDetails) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <AlertTriangle className="h-16 w-16 text-red-500 mx-auto mb-4" />
          <h2 className="text-xl font-semibold text-gray-900 mb-2">Error Loading User Details</h2>
          <p className="text-gray-600 mb-4">{error || 'User not found'}</p>
          <button
            onClick={() => router.back()}
            className="inline-flex items-center px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
          >
            <ArrowLeft className="h-4 w-4 mr-2" />
            Go Back
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* Header */}
      <div className="mb-8">
        <button
          onClick={() => router.back()}
          className="inline-flex items-center text-sm text-gray-500 hover:text-gray-700 mb-4"
        >
          <ArrowLeft className="h-4 w-4 mr-1" />
          Back to Users
        </button>
        
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-4">
            <div className="h-16 w-16 bg-gray-300 rounded-full flex items-center justify-center">
              <User className="h-8 w-8 text-gray-600" />
            </div>
            <div>
              <h1 className="text-3xl font-bold text-gray-900">@{userDetails.username}</h1>
              <div className="flex items-center space-x-4 mt-2">
                {getStatusBadge(userDetails.status)}
                {getRoleBadge(userDetails.role)}
                {userDetails.subscriptionTier && (
                  <SubscriptionTierBadge tier={userDetails.subscriptionTier} size="md" showName />
                )}
                {userDetails.emailVerified && (
                  <span className="inline-flex items-center text-sm text-green-600">
                    <UserCheck className="h-4 w-4 mr-1" />
                    Verified
                  </span>
                )}
              </div>
            </div>
          </div>
          
          <div className="flex space-x-2">
            {userDetails.status === UserStatus.Active && (
              <>
                <button
                  onClick={() => handleSuspendUser(userDetails.id)}
                  className="inline-flex items-center px-3 py-2 bg-yellow-100 text-yellow-800 rounded-md hover:bg-yellow-200 transition-colors text-sm"
                >
                  <Clock className="h-4 w-4 mr-1" />
                  Suspend
                </button>
                <button
                  onClick={() => handleBanUser(userDetails.id)}
                  className="inline-flex items-center px-3 py-2 bg-red-100 text-red-800 rounded-md hover:bg-red-200 transition-colors text-sm"
                >
                  <Ban className="h-4 w-4 mr-1" />
                  Ban
                </button>
              </>
            )}
            
            {userDetails.status === UserStatus.Suspended && (
              <button
                onClick={() => handleUnsuspendUser(userDetails.id)}
                className="inline-flex items-center px-3 py-2 bg-green-100 text-green-800 rounded-md hover:bg-green-200 transition-colors text-sm"
              >
                <CheckCircle className="h-4 w-4 mr-1" />
                Unsuspend
              </button>
            )}
            
            {(userDetails.status === UserStatus.Banned || userDetails.status === UserStatus.ShadowBanned) && (
              <button
                onClick={() => handleUnbanUser(userDetails.id)}
                className="inline-flex items-center px-3 py-2 bg-green-100 text-green-800 rounded-md hover:bg-green-200 transition-colors text-sm"
              >
                <CheckCircle className="h-4 w-4 mr-1" />
                Unban
              </button>
            )}
          </div>
        </div>
      </div>

      {/* Content Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Left Column - Profile & Admin Info */}
        <div className="lg:col-span-1 space-y-6">
          {/* Profile Information */}
          <div className="bg-white shadow rounded-lg p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
              <User className="h-5 w-5 mr-2" />
              Profile Information
            </h3>
            
            <div className="space-y-4">
              <div>
                <label className="text-sm font-medium text-gray-500">Email</label>
                <div className="flex items-center mt-1">
                  <Mail className="h-4 w-4 text-gray-400 mr-2" />
                  <span className="text-sm text-gray-900">{userDetails.email}</span>
                </div>
              </div>
              
              {userDetails.bio && (
                <div>
                  <label className="text-sm font-medium text-gray-500">Bio</label>
                  <p className="mt-1 text-sm text-gray-900">{userDetails.bio}</p>
                </div>
              )}
              
              {userDetails.tagline && (
                <div>
                  <label className="text-sm font-medium text-gray-500">Tagline</label>
                  <p className="mt-1 text-sm text-gray-900">{userDetails.tagline}</p>
                </div>
              )}
              
              {userDetails.pronouns && (
                <div>
                  <label className="text-sm font-medium text-gray-500">Pronouns</label>
                  <p className="mt-1 text-sm text-gray-900">{userDetails.pronouns}</p>
                </div>
              )}
              
              <div>
                <label className="text-sm font-medium text-gray-500">Member Since</label>
                <div className="flex items-center mt-1">
                  <Calendar className="h-4 w-4 text-gray-400 mr-2" />
                  <span className="text-sm text-gray-900">
                    {new Date(userDetails.createdAt).toLocaleDateString()}
                  </span>
                </div>
              </div>
              
              {userDetails.lastLoginAt && (
                <div>
                  <label className="text-sm font-medium text-gray-500">Last Login</label>
                  <div className="flex items-center mt-1">
                    <Activity className="h-4 w-4 text-gray-400 mr-2" />
                    <span className="text-sm text-gray-900">
                      {new Date(userDetails.lastLoginAt).toLocaleDateString()}
                    </span>
                  </div>
                </div>
              )}
            </div>
          </div>

          {/* Role Management */}
          <div className="bg-white shadow rounded-lg p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
              <Shield className="h-5 w-5 mr-2" />
              Role Management
            </h3>
            
            {!editingRole ? (
              <div className="flex items-center justify-between">
                <div>
                  <label className="text-sm font-medium text-gray-500">Current Role</label>
                  <div className="mt-1">{getRoleBadge(userDetails.role)}</div>
                </div>
                <button
                  onClick={() => setEditingRole(true)}
                  className="inline-flex items-center px-3 py-1 bg-blue-100 text-blue-800 rounded-md hover:bg-blue-200 transition-colors text-sm"
                >
                  <Edit className="h-4 w-4 mr-1" />
                  Change
                </button>
              </div>
            ) : (
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700">New Role</label>
                  <select
                    value={newRole}
                    onChange={(e) => setNewRole(parseInt(e.target.value) as UserRole)}
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
                  >
                    <option value={UserRole.User}>User</option>
                    <option value={UserRole.Moderator}>Moderator</option>
                    <option value={UserRole.Admin}>Admin</option>
                  </select>
                </div>
                
                <div>
                  <label className="block text-sm font-medium text-gray-700">Reason</label>
                  <textarea
                    value={roleChangeReason}
                    onChange={(e) => setRoleChangeReason(e.target.value)}
                    rows={3}
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
                    placeholder="Reason for role change..."
                  />
                </div>
                
                <div className="flex space-x-2">
                  <button
                    onClick={handleRoleChange}
                    className="inline-flex items-center px-3 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors text-sm"
                  >
                    <Save className="h-4 w-4 mr-1" />
                    Save
                  </button>
                  <button
                    onClick={() => {
                      setEditingRole(false);
                      setNewRole(userDetails.role);
                      setRoleChangeReason('');
                    }}
                    className="inline-flex items-center px-3 py-2 bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 transition-colors text-sm"
                  >
                    <X className="h-4 w-4 mr-1" />
                    Cancel
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Right Column - Trust Score & Activity */}
        <div className="lg:col-span-2 space-y-6">
          {/* Trust Score Overview */}
          <div className="bg-white shadow rounded-lg p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
              <TrendingUp className="h-5 w-5 mr-2" />
              Trust Score Analysis
            </h3>
            
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <div className="text-center">
                  <div className={`text-4xl font-bold ${getTrustScoreColor(userDetails.trustScore)}`}>
                    {formatTrustScore(userDetails.trustScore)}
                  </div>
                  <div className="text-sm text-gray-500 mt-1">Current Trust Score</div>
                </div>
                
                {userDetails.trustScoreFactors && (
                  <div className="mt-4 space-y-2">
                    <h4 className="text-sm font-medium text-gray-700">Contributing Factors</h4>
                    {Object.entries(userDetails.trustScoreFactors.factors).map(([key, value]) => (
                      <div key={key} className="flex justify-between text-sm">
                        <span className="text-gray-600 capitalize">{key.replace(/([A-Z])/g, ' $1')}</span>
                        <span className="text-gray-900">
                          {typeof value === 'number' ? (value * 100).toFixed(1) + '%' : String(value)}
                        </span>
                      </div>
                    ))}
                  </div>
                )}
              </div>
              
              <div>
                <h4 className="text-sm font-medium text-gray-700 mb-3">Recent Trust Score History</h4>
                <div className="space-y-2 max-h-48 overflow-y-auto">
                  {userDetails.recentTrustScoreHistory.map((entry) => (
                    <div key={entry.id} className="flex items-center justify-between text-sm p-2 bg-gray-50 rounded">
                      <div>
                        <div className="font-medium">{entry.reason}</div>
                        <div className="text-gray-500 text-xs">
                          {new Date(entry.createdAt).toLocaleDateString()}
                        </div>
                      </div>
                      <div className={`font-medium ${entry.scoreChange >= 0 ? 'text-green-600' : 'text-red-600'}`}>
                        {entry.scoreChange >= 0 ? '+' : ''}{(entry.scoreChange * 100).toFixed(1)}%
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>

          {/* Rate Limiting Settings */}
          <div className="bg-white shadow rounded-lg p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
              <Zap className="h-5 w-5 mr-2" />
              Rate Limiting Settings
            </h3>

            {!editingRateLimit ? (
              <div className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="text-sm font-medium text-gray-500">Rate Limiting</label>
                    <div className="mt-1 flex items-center">
                      {userDetails.rateLimitingEnabled === null ? (
                        <span className="text-sm text-gray-500">System Default</span>
                      ) : userDetails.rateLimitingEnabled ? (
                        <span className="inline-flex items-center text-sm text-green-600">
                          <CheckCircle className="h-4 w-4 mr-1" />
                          Enabled
                        </span>
                      ) : (
                        <span className="inline-flex items-center text-sm text-red-600">
                          <XCircle className="h-4 w-4 mr-1" />
                          Disabled
                        </span>
                      )}
                    </div>
                  </div>

                  <div>
                    <label className="text-sm font-medium text-gray-500">Trust-Based Rate Limiting</label>
                    <div className="mt-1 flex items-center">
                      {userDetails.trustBasedRateLimitingEnabled === null ? (
                        <span className="text-sm text-gray-500">System Default</span>
                      ) : userDetails.trustBasedRateLimitingEnabled ? (
                        <span className="inline-flex items-center text-sm text-green-600">
                          <CheckCircle className="h-4 w-4 mr-1" />
                          Enabled
                        </span>
                      ) : (
                        <span className="inline-flex items-center text-sm text-red-600">
                          <XCircle className="h-4 w-4 mr-1" />
                          Disabled
                        </span>
                      )}
                    </div>
                  </div>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="text-sm font-medium text-gray-500">Currently Rate Limited</label>
                    <div className="mt-1">
                      {userDetails.isCurrentlyRateLimited ? (
                        <span className="inline-flex items-center text-sm text-red-600">
                          <AlertTriangle className="h-4 w-4 mr-1" />
                          Yes
                        </span>
                      ) : (
                        <span className="inline-flex items-center text-sm text-green-600">
                          <CheckCircle className="h-4 w-4 mr-1" />
                          No
                        </span>
                      )}
                    </div>
                  </div>

                  <div>
                    <label className="text-sm font-medium text-gray-500">Recent Violations (24h)</label>
                    <div className="mt-1">
                      <span className={`text-sm font-medium ${userDetails.recentRateLimitViolations > 0 ? 'text-red-600' : 'text-green-600'}`}>
                        {userDetails.recentRateLimitViolations}
                      </span>
                    </div>
                  </div>
                </div>

                <div className="flex justify-end">
                  <button
                    onClick={() => setEditingRateLimit(true)}
                    className="inline-flex items-center px-3 py-2 bg-blue-100 text-blue-800 rounded-md hover:bg-blue-200 transition-colors text-sm"
                  >
                    <Settings className="h-4 w-4 mr-1" />
                    Configure
                  </button>
                </div>
              </div>
            ) : (
              <div className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-gray-700">Rate Limiting</label>
                    <select
                      value={rateLimitSettings.rateLimitingEnabled === null ? 'default' : rateLimitSettings.rateLimitingEnabled.toString()}
                      onChange={(e) => setRateLimitSettings(prev => ({
                        ...prev,
                        rateLimitingEnabled: e.target.value === 'default' ? null : e.target.value === 'true'
                      }))}
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
                    >
                      <option value="default">System Default</option>
                      <option value="true">Enabled</option>
                      <option value="false">Disabled</option>
                    </select>
                  </div>

                  <div>
                    <label className="block text-sm font-medium text-gray-700">Trust-Based Rate Limiting</label>
                    <select
                      value={rateLimitSettings.trustBasedRateLimitingEnabled === null ? 'default' : rateLimitSettings.trustBasedRateLimitingEnabled.toString()}
                      onChange={(e) => setRateLimitSettings(prev => ({
                        ...prev,
                        trustBasedRateLimitingEnabled: e.target.value === 'default' ? null : e.target.value === 'true'
                      }))}
                      className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
                    >
                      <option value="default">System Default</option>
                      <option value="true">Enabled</option>
                      <option value="false">Disabled</option>
                    </select>
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700">Reason</label>
                  <textarea
                    value={rateLimitSettings.reason}
                    onChange={(e) => setRateLimitSettings(prev => ({ ...prev, reason: e.target.value }))}
                    rows={3}
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
                    placeholder="Reason for rate limiting change..."
                  />
                </div>

                <div className="flex space-x-2">
                  <button
                    onClick={handleRateLimitUpdate}
                    className="inline-flex items-center px-3 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors text-sm"
                  >
                    <Save className="h-4 w-4 mr-1" />
                    Save
                  </button>
                  <button
                    onClick={() => {
                      setEditingRateLimit(false);
                      setRateLimitSettings({
                        rateLimitingEnabled: userDetails.rateLimitingEnabled ?? null,
                        trustBasedRateLimitingEnabled: userDetails.trustBasedRateLimitingEnabled ?? null,
                        reason: ''
                      });
                    }}
                    className="inline-flex items-center px-3 py-2 bg-gray-100 text-gray-700 rounded-md hover:bg-gray-200 transition-colors text-sm"
                  >
                    <X className="h-4 w-4 mr-1" />
                    Cancel
                  </button>
                </div>
              </div>
            )}
          </div>

          {/* Activity Statistics */}
          <div className="bg-white shadow rounded-lg p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
              <BarChart3 className="h-5 w-5 mr-2" />
              Activity Statistics
            </h3>

            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              <div className="text-center">
                <div className="text-2xl font-bold text-blue-600">{userDetails.postCount}</div>
                <div className="text-sm text-gray-500">Posts</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-green-600">{userDetails.commentCount}</div>
                <div className="text-sm text-gray-500">Comments</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-purple-600">{userDetails.likeCount}</div>
                <div className="text-sm text-gray-500">Likes Given</div>
              </div>
              <div className="text-center">
                <div className="text-2xl font-bold text-orange-600">{userDetails.followerCount}</div>
                <div className="text-sm text-gray-500">Followers</div>
              </div>
            </div>

            <div className="mt-6 grid grid-cols-2 gap-4">
              <div className="text-center">
                <div className="text-xl font-bold text-red-600">{userDetails.reportCount}</div>
                <div className="text-sm text-gray-500">Reports Made</div>
              </div>
              <div className="text-center">
                <div className="text-xl font-bold text-yellow-600">{userDetails.moderationActionCount}</div>
                <div className="text-sm text-gray-500">Moderation Actions</div>
              </div>
            </div>
          </div>

          {/* Recent Moderation Actions */}
          <div className="bg-white shadow rounded-lg p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
              <History className="h-5 w-5 mr-2" />
              Recent Moderation Actions
            </h3>

            {userDetails.recentModerationActions.length > 0 ? (
              <div className="space-y-3">
                {userDetails.recentModerationActions.map((action) => (
                  <div key={action.id} className="flex items-start justify-between p-3 bg-gray-50 rounded-lg">
                    <div className="flex-1">
                      <div className="flex items-center space-x-2">
                        <Flag className="h-4 w-4 text-gray-400" />
                        <span className="text-sm font-medium text-gray-900">{action.action}</span>
                      </div>
                      <p className="text-sm text-gray-600 mt-1">{action.reason}</p>
                      {action.details && (
                        <p className="text-xs text-gray-500 mt-1">{action.details}</p>
                      )}
                      <div className="flex items-center space-x-4 mt-2 text-xs text-gray-500">
                        <span>By: {action.performedByUsername}</span>
                        <span>{new Date(action.createdAt).toLocaleDateString()}</span>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            ) : (
              <div className="text-center py-8">
                <Flag className="h-12 w-12 text-gray-300 mx-auto mb-4" />
                <p className="text-gray-500">No recent moderation actions</p>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Suspend User Modal */}
      <SuspendUserModal
        isOpen={showSuspendModal}
        onClose={() => setShowSuspendModal(false)}
        onSubmit={handleSuspendSubmit}
        username={userDetails?.username || ''}
      />
    </div>
  );
}
