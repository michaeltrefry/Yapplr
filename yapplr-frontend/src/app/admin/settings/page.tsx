'use client';

import { useState, useEffect } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { useRouter } from 'next/navigation';
import { UserRole } from '@/types';
import {
  Settings,
  Shield,
  Zap,
  Users,
  Clock,
  AlertTriangle,
  Save,
  RotateCcw,
  Bell,
  Bug,
  ArrowRight
} from 'lucide-react';
import Link from 'next/link';
import { NotificationStatus } from '@/components/NotificationStatus';
import api from '@/lib/api';

interface RateLimitConfig {
  enabled: boolean;
  trustBasedEnabled: boolean;
  burstProtectionEnabled: boolean;
  autoBlockingEnabled: boolean;
  autoBlockViolationThreshold: number;
  autoBlockDurationHours: number;
  applyToAdmins: boolean;
  applyToModerators: boolean;
  fallbackMultiplier: number;
}

export default function AdminSettingsPage() {
  const { user, isLoading } = useAuth();
  const router = useRouter();
  const [config, setConfig] = useState<RateLimitConfig>({
    enabled: true,
    trustBasedEnabled: true,
    burstProtectionEnabled: true,
    autoBlockingEnabled: true,
    autoBlockViolationThreshold: 15,
    autoBlockDurationHours: 2,
    applyToAdmins: false,
    applyToModerators: false,
    fallbackMultiplier: 1.0,
  });
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  useEffect(() => {
    if (!isLoading) {
      if (!user) {
        router.push('/login');
        return;
      }

      if (user.role !== UserRole.Admin) {
        router.push('/admin');
        return;
      }

      fetchConfig();
    }
  }, [user, isLoading, router]);

  const fetchConfig = async () => {
    try {
      setLoading(true);
      const response = await api.get('/security/admin/rate-limits/config');
      setConfig(response.data);
    } catch (error) {
      console.error('Failed to fetch rate limiting configuration');
    } finally {
      setLoading(false);
    }
  };

  const saveConfig = async () => {
    try {
      setSaving(true);
      await api.put('/security/admin/rate-limits/config', {
        ...config,
        reason: 'Updated via admin settings interface',
      });
      setMessage({ type: 'success', text: 'Rate limiting configuration updated successfully!' });
      setTimeout(() => setMessage(null), 5000);
    } catch (error) {
      console.error('Error saving configuration:', error);
      setMessage({ type: 'error', text: 'Failed to update configuration. Please try again.' });
      setTimeout(() => setMessage(null), 5000);
    } finally {
      setSaving(false);
    }
  };

  const resetToDefaults = () => {
    setConfig({
      enabled: true,
      trustBasedEnabled: true,
      burstProtectionEnabled: true,
      autoBlockingEnabled: true,
      autoBlockViolationThreshold: 15,
      autoBlockDurationHours: 2,
      applyToAdmins: false,
      applyToModerators: false,
      fallbackMultiplier: 1.0,
    });
  };

  if (isLoading || loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900 flex items-center">
          <Settings className="w-8 h-8 mr-3" />
          System Settings
        </h1>
        <p className="text-gray-600">Configure system-wide settings and policies</p>
      </div>

      {/* Message */}
      {message && (
        <div className={`p-4 rounded-lg ${
          message.type === 'success' 
            ? 'bg-green-50 text-green-800 border border-green-200' 
            : 'bg-red-50 text-red-800 border border-red-200'
        }`}>
          {message.text}
        </div>
      )}

      {/* Rate Limiting Configuration */}
      <div className="bg-white rounded-lg shadow p-6">
        <div className="flex items-center mb-6">
          <Shield className="w-6 h-6 text-blue-600 mr-3" />
          <h2 className="text-xl font-semibold text-gray-900">Rate Limiting Configuration</h2>
        </div>

        <div className="space-y-6">
          {/* Global Settings */}
          <div>
            <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
              <Zap className="w-5 h-5 mr-2" />
              Global Settings
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                <div>
                  <div className="font-medium text-gray-900">Enable Rate Limiting</div>
                  <div className="text-sm text-gray-600">Master switch for all rate limiting</div>
                </div>
                <label className="relative inline-flex items-center cursor-pointer">
                  <input
                    type="checkbox"
                    checked={config.enabled}
                    onChange={(e) => setConfig({ ...config, enabled: e.target.checked })}
                    className="sr-only peer"
                  />
                  <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                </label>
              </div>

              <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                <div>
                  <div className="font-medium text-gray-900">Trust-Based Rate Limiting</div>
                  <div className="text-sm text-gray-600">Use trust scores to adjust limits</div>
                </div>
                <label className="relative inline-flex items-center cursor-pointer">
                  <input
                    type="checkbox"
                    checked={config.trustBasedEnabled}
                    onChange={(e) => setConfig({ ...config, trustBasedEnabled: e.target.checked })}
                    className="sr-only peer"
                  />
                  <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                </label>
              </div>

              <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                <div>
                  <div className="font-medium text-gray-900">Burst Protection</div>
                  <div className="text-sm text-gray-600">Prevent rapid-fire requests</div>
                </div>
                <label className="relative inline-flex items-center cursor-pointer">
                  <input
                    type="checkbox"
                    checked={config.burstProtectionEnabled}
                    onChange={(e) => setConfig({ ...config, burstProtectionEnabled: e.target.checked })}
                    className="sr-only peer"
                  />
                  <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                </label>
              </div>

              <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                <div>
                  <div className="font-medium text-gray-900">Auto-Blocking</div>
                  <div className="text-sm text-gray-600">Automatically block repeat violators</div>
                </div>
                <label className="relative inline-flex items-center cursor-pointer">
                  <input
                    type="checkbox"
                    checked={config.autoBlockingEnabled}
                    onChange={(e) => setConfig({ ...config, autoBlockingEnabled: e.target.checked })}
                    className="sr-only peer"
                  />
                  <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                </label>
              </div>
            </div>
          </div>

          {/* Auto-Blocking Settings */}
          {config.autoBlockingEnabled && (
            <div>
              <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
                <AlertTriangle className="w-5 h-5 mr-2" />
                Auto-Blocking Settings
              </h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Violation Threshold
                  </label>
                  <input
                    type="number"
                    min="1"
                    max="100"
                    value={config.autoBlockViolationThreshold}
                    onChange={(e) => setConfig({ ...config, autoBlockViolationThreshold: parseInt(e.target.value) })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                  <p className="text-xs text-gray-500 mt-1">Number of violations before auto-blocking</p>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Block Duration (Hours)
                  </label>
                  <input
                    type="number"
                    min="1"
                    max="168"
                    value={config.autoBlockDurationHours}
                    onChange={(e) => setConfig({ ...config, autoBlockDurationHours: parseInt(e.target.value) })}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                  <p className="text-xs text-gray-500 mt-1">Duration of automatic blocks</p>
                </div>
              </div>
            </div>
          )}

          {/* Role Exemptions */}
          <div>
            <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
              <Users className="w-5 h-5 mr-2" />
              Role Exemptions
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                <div>
                  <div className="font-medium text-gray-900">Apply to Administrators</div>
                  <div className="text-sm text-gray-600">Rate limit admin users</div>
                </div>
                <label className="relative inline-flex items-center cursor-pointer">
                  <input
                    type="checkbox"
                    checked={config.applyToAdmins}
                    onChange={(e) => setConfig({ ...config, applyToAdmins: e.target.checked })}
                    className="sr-only peer"
                  />
                  <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                </label>
              </div>

              <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
                <div>
                  <div className="font-medium text-gray-900">Apply to Moderators</div>
                  <div className="text-sm text-gray-600">Rate limit moderator users</div>
                </div>
                <label className="relative inline-flex items-center cursor-pointer">
                  <input
                    type="checkbox"
                    checked={config.applyToModerators}
                    onChange={(e) => setConfig({ ...config, applyToModerators: e.target.checked })}
                    className="sr-only peer"
                  />
                  <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                </label>
              </div>
            </div>
          </div>

          {/* Fallback Settings */}
          {!config.trustBasedEnabled && (
            <div>
              <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
                <Clock className="w-5 h-5 mr-2" />
                Fallback Settings
              </h3>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Fallback Multiplier
                </label>
                <input
                  type="number"
                  min="0.1"
                  max="10"
                  step="0.1"
                  value={config.fallbackMultiplier}
                  onChange={(e) => setConfig({ ...config, fallbackMultiplier: parseFloat(e.target.value) })}
                  className="w-full max-w-xs px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <p className="text-xs text-gray-500 mt-1">Rate limit multiplier when trust-based is disabled</p>
              </div>
            </div>
          )}

          {/* Development & Testing Tools */}
          <div>
            <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
              <Bug className="w-5 h-5 mr-2" />
              Development & Testing Tools
            </h3>

            {/* Notification Status */}
            <div className="p-4 bg-white border border-gray-200 rounded-lg mb-4">
              <div className="flex items-center space-x-3 mb-3">
                <div className="p-2 bg-blue-100 rounded-lg">
                  <Bell className="w-5 h-5 text-blue-600" />
                </div>
                <div>
                  <h4 className="font-semibold text-gray-900">Notification Status</h4>
                  <p className="text-sm text-gray-600">
                    Current notification delivery method and system status
                  </p>
                </div>
              </div>
              <NotificationStatus showDetails={true} />
            </div>

            {/* Debug Center Link */}
            <Link
              href="/debug/notifications"
              className="flex items-center justify-between p-4 bg-white border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
            >
              <div className="flex items-center space-x-3">
                <div className="p-2 bg-orange-100 rounded-lg">
                  <Bug className="w-5 h-5 text-orange-600" />
                </div>
                <div>
                  <h4 className="font-semibold text-gray-900">Debug Center</h4>
                  <p className="text-sm text-gray-600">
                    Test notification fallback scenarios and system diagnostics
                  </p>
                </div>
              </div>
              <ArrowRight className="w-5 h-5 text-gray-400" />
            </Link>
          </div>

          {/* Action Buttons */}
          <div className="flex justify-between pt-6 border-t border-gray-200">
            <button
              onClick={resetToDefaults}
              className="inline-flex items-center px-4 py-2 border border-gray-300 rounded-md text-gray-700 bg-white hover:bg-gray-50 transition-colors"
            >
              <RotateCcw className="w-4 h-4 mr-2" />
              Reset to Defaults
            </button>
            <button
              onClick={saveConfig}
              disabled={saving}
              className="inline-flex items-center px-6 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {saving ? (
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
              ) : (
                <Save className="w-4 h-4 mr-2" />
              )}
              {saving ? 'Saving...' : 'Save Configuration'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
