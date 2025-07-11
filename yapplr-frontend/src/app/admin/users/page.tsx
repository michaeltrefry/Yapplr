'use client';

import { useEffect, useState } from 'react';
import { useSearchParams } from 'next/navigation';
import { adminApi } from '@/lib/api';
import { AdminUser, UserRole, UserStatus } from '@/types';
import { SuspendUserModal } from '@/components/admin';
import {
  Users,
  Search,
  Filter,
  Ban,
  Clock,
  Shield,
  UserCheck,
  Calendar,
  Mail,
  MoreVertical,
} from 'lucide-react';

export default function UsersPage() {
  const searchParams = useSearchParams();
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<UserStatus | ''>('');
  const [roleFilter, setRoleFilter] = useState<UserRole | ''>('');
  const [page, setPage] = useState(1);
  const [suspendModalOpen, setSuspendModalOpen] = useState(false);
  const [userToSuspend, setUserToSuspend] = useState<AdminUser | null>(null);

  useEffect(() => {
    // Get initial filters from URL params
    if (searchParams) {
      const status = searchParams.get('status');
      const role = searchParams.get('role');

      if (status) setStatusFilter(parseInt(status) as UserStatus);
      if (role) setRoleFilter(parseInt(role) as UserRole);
    }
  }, [searchParams]);

  useEffect(() => {
    fetchUsers();
  }, [statusFilter, roleFilter, page]);

  const fetchUsers = async () => {
    try {
      setLoading(true);
      const usersData = await adminApi.getUsers(
        page,
        25,
        statusFilter || undefined,
        roleFilter || undefined
      );
      setUsers(usersData);
    } catch (error) {
      console.error('Failed to fetch users:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleSuspendUser = (userId: number) => {
    const user = users.find(u => u.id === userId);
    if (user) {
      setUserToSuspend(user);
      setSuspendModalOpen(true);
    }
  };

  const handleSuspendSubmit = async (reason: string, days: number | null) => {
    if (!userToSuspend) return;

    const suspendedUntil = days
      ? new Date(Date.now() + days * 24 * 60 * 60 * 1000).toISOString()
      : undefined;

    try {
      await adminApi.suspendUser(userToSuspend.id, { reason, suspendedUntil });
      fetchUsers();
    } catch (error) {
      console.error('Failed to suspend user:', error);
      throw error; // Let the modal handle the error display
    }
  };

  const handleSuspendModalClose = () => {
    setSuspendModalOpen(false);
    setUserToSuspend(null);
  };

  const handleUnsuspendUser = async (userId: number) => {
    if (!confirm('Are you sure you want to unsuspend this user?')) return;

    try {
      await adminApi.unsuspendUser(userId);
      fetchUsers();
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
      fetchUsers();
    } catch (error) {
      console.error('Failed to ban user:', error);
      alert('Failed to ban user');
    }
  };

  const handleUnbanUser = async (userId: number) => {
    if (!confirm('Are you sure you want to unban this user?')) return;

    try {
      await adminApi.unbanUser(userId);
      fetchUsers();
    } catch (error) {
      console.error('Failed to unban user:', error);
      alert('Failed to unban user');
    }
  };

  const handleChangeRole = async (userId: number, newRole: UserRole) => {
    const reason = prompt('Enter reason for role change:');
    if (!reason) return;

    if (!confirm(`Are you sure you want to change this user's role to ${UserRole[newRole]}?`)) return;

    try {
      await adminApi.changeUserRole(userId, { role: newRole, reason });
      fetchUsers();
    } catch (error) {
      console.error('Failed to change user role:', error);
      alert('Failed to change user role');
    }
  };

  const getStatusBadge = (status: UserStatus) => {
    switch (status) {
      case UserStatus.Active:
        return <span className="bg-green-100 text-green-800 px-2 py-1 rounded-full text-xs">Active</span>;
      case UserStatus.Suspended:
        return <span className="bg-yellow-100 text-yellow-800 px-2 py-1 rounded-full text-xs">Suspended</span>;
      case UserStatus.Banned:
        return <span className="bg-red-100 text-red-800 px-2 py-1 rounded-full text-xs">Banned</span>;
      case UserStatus.ShadowBanned:
        return <span className="bg-purple-100 text-purple-800 px-2 py-1 rounded-full text-xs">Shadow Banned</span>;
      default:
        return null;
    }
  };

  const getRoleBadge = (role: UserRole) => {
    switch (role) {
      case UserRole.Admin:
        return <span className="bg-red-100 text-red-800 px-2 py-1 rounded-full text-xs">Admin</span>;
      case UserRole.Moderator:
        return <span className="bg-blue-100 text-blue-800 px-2 py-1 rounded-full text-xs">Moderator</span>;
      case UserRole.User:
        return <span className="bg-gray-100 text-gray-800 px-2 py-1 rounded-full text-xs">User</span>;
      default:
        return null;
    }
  };

  const filteredUsers = users.filter(user =>
    user.username.toLowerCase().includes(searchTerm.toLowerCase()) ||
    user.email.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">User Management</h1>
        <p className="text-gray-600">Manage user accounts and permissions</p>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg shadow p-6">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Search</label>
            <div className="relative">
              <Search className="h-5 w-5 text-gray-400 absolute left-3 top-3" />
              <input
                type="text"
                placeholder="Search users..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10 w-full border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Status</label>
            <select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value as UserStatus | '')}
              className="w-full border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">All Statuses</option>
              <option value={UserStatus.Active}>Active</option>
              <option value={UserStatus.Suspended}>Suspended</option>
              <option value={UserStatus.Banned}>Banned</option>
              <option value={UserStatus.ShadowBanned}>Shadow Banned</option>
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Role</label>
            <select
              value={roleFilter}
              onChange={(e) => setRoleFilter(e.target.value as UserRole | '')}
              className="w-full border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">All Roles</option>
              <option value={UserRole.User}>User</option>
              <option value={UserRole.Moderator}>Moderator</option>
              <option value={UserRole.Admin}>Admin</option>
            </select>
          </div>
          <div className="flex items-end">
            <button
              onClick={fetchUsers}
              className="w-full bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 transition-colors"
            >
              Apply Filters
            </button>
          </div>
        </div>
      </div>

      {/* Users Table */}
      <div className="bg-white rounded-lg shadow overflow-hidden">
        {loading ? (
          <div className="flex items-center justify-center h-64">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
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
                    Status
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Role
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Stats
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Joined
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {filteredUsers.map((user) => (
                  <tr key={user.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center">
                        <div className="h-10 w-10 bg-gray-300 rounded-full flex items-center justify-center">
                          <Users className="h-6 w-6 text-gray-600" />
                        </div>
                        <div className="ml-4">
                          <div className="text-sm font-medium text-gray-900">@{user.username}</div>
                          <div className="text-sm text-gray-500 flex items-center">
                            <Mail className="h-4 w-4 mr-1" />
                            {user.email}
                            {user.emailVerified && (
                              <UserCheck className="h-4 w-4 ml-1 text-green-500" />
                            )}
                          </div>
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      {getStatusBadge(user.status)}
                      {user.suspendedUntil && (
                        <div className="text-xs text-gray-500 mt-1">
                          Until {new Date(user.suspendedUntil).toLocaleDateString()}
                        </div>
                      )}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      {getRoleBadge(user.role)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      <div>{user.postCount} posts</div>
                      <div>{user.followerCount} followers</div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      <div className="flex items-center">
                        <Calendar className="h-4 w-4 mr-1" />
                        {new Date(user.createdAt).toLocaleDateString()}
                      </div>
                      {user.lastLoginAt && (
                        <div className="text-xs text-gray-400 mt-1">
                          Last login: {new Date(user.lastLoginAt).toLocaleDateString()}
                        </div>
                      )}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                      <div className="flex flex-wrap gap-2">
                        {user.status === UserStatus.Active && (
                          <>
                            <button
                              onClick={() => handleSuspendUser(user.id)}
                              className="inline-flex items-center px-2 py-1 bg-yellow-100 text-yellow-800 rounded-md hover:bg-yellow-200 transition-colors text-xs"
                            >
                              <Clock className="h-3 w-3 mr-1" />
                              Suspend
                            </button>
                            <button
                              onClick={() => handleBanUser(user.id)}
                              className="inline-flex items-center px-2 py-1 bg-red-100 text-red-800 rounded-md hover:bg-red-200 transition-colors text-xs"
                            >
                              <Ban className="h-3 w-3 mr-1" />
                              Ban
                            </button>
                          </>
                        )}
                        {user.status === UserStatus.Suspended && (
                          <button
                            onClick={() => handleUnsuspendUser(user.id)}
                            className="inline-flex items-center px-2 py-1 bg-green-100 text-green-800 rounded-md hover:bg-green-200 transition-colors text-xs"
                          >
                            <UserCheck className="h-3 w-3 mr-1" />
                            Unsuspend
                          </button>
                        )}
                        {(user.status === UserStatus.Banned || user.status === UserStatus.ShadowBanned) && (
                          <button
                            onClick={() => handleUnbanUser(user.id)}
                            className="inline-flex items-center px-2 py-1 bg-green-100 text-green-800 rounded-md hover:bg-green-200 transition-colors text-xs"
                          >
                            <UserCheck className="h-3 w-3 mr-1" />
                            Unban
                          </button>
                        )}
                        {user.role !== UserRole.Admin && (
                          <button
                            onClick={() => handleChangeRole(user.id, UserRole.Moderator)}
                            className="inline-flex items-center px-2 py-1 bg-blue-100 text-blue-800 rounded-md hover:bg-blue-200 transition-colors text-xs"
                          >
                            <Shield className="h-3 w-3 mr-1" />
                            Make Moderator
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {filteredUsers.length === 0 && !loading && (
        <div className="text-center py-12">
          <Users className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <p className="text-gray-500">No users found matching your criteria</p>
        </div>
      )}

      {/* Suspend User Modal */}
      <SuspendUserModal
        isOpen={suspendModalOpen}
        onClose={handleSuspendModalClose}
        onSubmit={handleSuspendSubmit}
        username={userToSuspend?.username || ''}
        isLoading={loading}
      />
    </div>
  );
}
