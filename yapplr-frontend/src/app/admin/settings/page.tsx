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
  ArrowRight,
  Upload,
  Image,
  Video,
  FileText
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

interface UploadSettings {
  maxImageSizeBytes: number;
  maxVideoSizeBytes: number;
  maxVideoDurationSeconds: number;
  maxMediaFilesPerPost: number;
  allowedImageExtensions: string;
  allowedVideoExtensions: string;
  deleteOriginalAfterProcessing: boolean;
  videoTargetBitrate: number;
  videoMaxWidth: number;
  videoMaxHeight: number;
  updatedAt: string;
  updatedByUsername?: string;
  updateReason?: string;
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

  // Upload settings state
  const [uploadSettings, setUploadSettings] = useState<UploadSettings>({
    maxImageSizeBytes: 5 * 1024 * 1024, // 5MB
    maxVideoSizeBytes: 1024 * 1024 * 1024, // 1GB
    maxVideoDurationSeconds: 300, // 5 minutes
    maxMediaFilesPerPost: 10,
    allowedImageExtensions: '.jpg,.jpeg,.png,.gif,.webp',
    allowedVideoExtensions: '.mp4,.avi,.mov,.wmv,.flv,.webm,.mkv,.3gp',
    deleteOriginalAfterProcessing: true,
    videoTargetBitrate: 2000,
    videoMaxWidth: 1920,
    videoMaxHeight: 1080,
    updatedAt: new Date().toISOString(),
  });
  const [uploadLoading, setUploadLoading] = useState(true);
  const [uploadSaving, setUploadSaving] = useState(false);

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
      fetchUploadSettings();
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

  // Upload settings functions
  const fetchUploadSettings = async () => {
    try {
      setUploadLoading(true);
      const response = await api.get('/admin/upload-settings');
      setUploadSettings(response.data);
    } catch (error) {
      console.error('Failed to fetch upload settings');
    } finally {
      setUploadLoading(false);
    }
  };

  const saveUploadSettings = async () => {
    try {
      setUploadSaving(true);
      await api.put('/admin/upload-settings', {
        ...uploadSettings,
        updateReason: 'Updated via admin settings interface',
      });
      setMessage({ type: 'success', text: 'Upload settings updated successfully!' });
      setTimeout(() => setMessage(null), 5000);
    } catch (error) {
      console.error('Error saving upload settings:', error);
      setMessage({ type: 'error', text: 'Failed to update upload settings. Please try again.' });
      setTimeout(() => setMessage(null), 5000);
    } finally {
      setUploadSaving(false);
    }
  };

  const resetUploadToDefaults = async () => {
    try {
      setUploadSaving(true);
      const response = await api.post('/admin/upload-settings/reset', {
        reason: 'Reset to defaults via admin interface'
      });
      setUploadSettings(response.data);
      setMessage({ type: 'success', text: 'Upload settings reset to defaults!' });
      setTimeout(() => setMessage(null), 5000);
    } catch (error) {
      console.error('Error resetting upload settings:', error);
      setMessage({ type: 'error', text: 'Failed to reset upload settings. Please try again.' });
      setTimeout(() => setMessage(null), 5000);
    } finally {
      setUploadSaving(false);
    }
  };

  // Helper functions for unit conversion
  const bytesToMB = (bytes: number) => Math.round(bytes / (1024 * 1024));
  const bytesToGB = (bytes: number) => Math.round(bytes / (1024 * 1024 * 1024) * 10) / 10;
  const mbToBytes = (mb: number) => mb * 1024 * 1024;
  const gbToBytes = (gb: number) => gb * 1024 * 1024 * 1024;
  const secondsToMinutes = (seconds: number) => Math.round(seconds / 60);
  const minutesToSeconds = (minutes: number) => minutes * 60;

  if (isLoading || loading || uploadLoading) {
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

      {/* Upload Settings Configuration */}
      <div className="bg-white rounded-lg shadow p-6">
        <div className="flex items-center mb-6">
          <Upload className="w-6 h-6 text-green-600 mr-3" />
          <h2 className="text-xl font-semibold text-gray-900">Upload Settings Configuration</h2>
        </div>

        <div className="space-y-6">
          {/* File Size Limits */}
          <div>
            <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
              <FileText className="w-5 h-5 mr-2" />
              File Size Limits
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Maximum Image Size (MB)
                </label>
                <input
                  type="number"
                  min="1"
                  max="100"
                  value={bytesToMB(uploadSettings.maxImageSizeBytes)}
                  onChange={(e) => setUploadSettings({
                    ...uploadSettings,
                    maxImageSizeBytes: mbToBytes(parseInt(e.target.value) || 5)
                  })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                />
                <p className="text-xs text-gray-500 mt-1">Current: {bytesToMB(uploadSettings.maxImageSizeBytes)}MB</p>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Maximum Video Size (GB)
                </label>
                <input
                  type="number"
                  min="0.1"
                  max="10"
                  step="0.1"
                  value={bytesToGB(uploadSettings.maxVideoSizeBytes)}
                  onChange={(e) => setUploadSettings({
                    ...uploadSettings,
                    maxVideoSizeBytes: gbToBytes(parseFloat(e.target.value) || 1)
                  })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                />
                <p className="text-xs text-gray-500 mt-1">Current: {bytesToGB(uploadSettings.maxVideoSizeBytes)}GB</p>
              </div>
            </div>
          </div>

          {/* Media Limits */}
          <div>
            <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
              <Video className="w-5 h-5 mr-2" />
              Media Limits
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Maximum Video Duration (minutes)
                </label>
                <input
                  type="number"
                  min="1"
                  max="60"
                  value={secondsToMinutes(uploadSettings.maxVideoDurationSeconds)}
                  onChange={(e) => setUploadSettings({
                    ...uploadSettings,
                    maxVideoDurationSeconds: minutesToSeconds(parseInt(e.target.value) || 5)
                  })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                />
                <p className="text-xs text-gray-500 mt-1">Current: {secondsToMinutes(uploadSettings.maxVideoDurationSeconds)} minutes</p>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Maximum Files Per Post
                </label>
                <input
                  type="number"
                  min="1"
                  max="20"
                  value={uploadSettings.maxMediaFilesPerPost}
                  onChange={(e) => setUploadSettings({
                    ...uploadSettings,
                    maxMediaFilesPerPost: parseInt(e.target.value) || 10
                  })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                />
                <p className="text-xs text-gray-500 mt-1">Current: {uploadSettings.maxMediaFilesPerPost} files</p>
              </div>
            </div>
          </div>

          {/* File Extensions */}
          <div>
            <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
              <Image className="w-5 h-5 mr-2" />
              Allowed File Extensions
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Image Extensions
                </label>
                <input
                  type="text"
                  value={uploadSettings.allowedImageExtensions}
                  onChange={(e) => setUploadSettings({
                    ...uploadSettings,
                    allowedImageExtensions: e.target.value
                  })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                  placeholder=".jpg,.jpeg,.png,.gif,.webp"
                />
                <p className="text-xs text-gray-500 mt-1">Comma-separated list of extensions</p>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Video Extensions
                </label>
                <input
                  type="text"
                  value={uploadSettings.allowedVideoExtensions}
                  onChange={(e) => setUploadSettings({
                    ...uploadSettings,
                    allowedVideoExtensions: e.target.value
                  })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                  placeholder=".mp4,.avi,.mov,.wmv,.flv,.webm,.mkv"
                />
                <p className="text-xs text-gray-500 mt-1">Comma-separated list of extensions</p>
              </div>
            </div>
          </div>

          {/* Video Processing Settings */}
          <div>
            <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
              <Settings className="w-5 h-5 mr-2" />
              Video Processing Settings
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Target Bitrate (kbps)
                </label>
                <input
                  type="number"
                  min="500"
                  max="10000"
                  value={uploadSettings.videoTargetBitrate}
                  onChange={(e) => setUploadSettings({
                    ...uploadSettings,
                    videoTargetBitrate: parseInt(e.target.value) || 2000
                  })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Max Width (pixels)
                </label>
                <input
                  type="number"
                  min="480"
                  max="4096"
                  value={uploadSettings.videoMaxWidth}
                  onChange={(e) => setUploadSettings({
                    ...uploadSettings,
                    videoMaxWidth: parseInt(e.target.value) || 1920
                  })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  Max Height (pixels)
                </label>
                <input
                  type="number"
                  min="360"
                  max="2160"
                  value={uploadSettings.videoMaxHeight}
                  onChange={(e) => setUploadSettings({
                    ...uploadSettings,
                    videoMaxHeight: parseInt(e.target.value) || 1080
                  })}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-green-500"
                />
              </div>
            </div>
          </div>

          {/* Processing Options */}
          <div>
            <h3 className="text-lg font-medium text-gray-900 mb-4">Processing Options</h3>
            <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
              <div>
                <div className="font-medium text-gray-900">Delete Original After Processing</div>
                <div className="text-sm text-gray-600">Remove original video files after successful processing</div>
              </div>
              <label className="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  checked={uploadSettings.deleteOriginalAfterProcessing}
                  onChange={(e) => setUploadSettings({
                    ...uploadSettings,
                    deleteOriginalAfterProcessing: e.target.checked
                  })}
                  className="sr-only peer"
                />
                <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-green-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-green-600"></div>
              </label>
            </div>
          </div>

          {/* Last Updated Info */}
          {uploadSettings.updatedByUsername && (
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <div className="text-sm text-blue-800">
                <strong>Last updated:</strong> {new Date(uploadSettings.updatedAt).toLocaleString()} by @{uploadSettings.updatedByUsername}
                {uploadSettings.updateReason && (
                  <div className="mt-1">
                    <strong>Reason:</strong> {uploadSettings.updateReason}
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Action Buttons */}
          <div className="flex justify-between pt-6 border-t border-gray-200">
            <button
              onClick={resetUploadToDefaults}
              disabled={uploadSaving}
              className="inline-flex items-center px-4 py-2 border border-gray-300 rounded-md text-gray-700 bg-white hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              <RotateCcw className="w-4 h-4 mr-2" />
              Reset to Defaults
            </button>
            <button
              onClick={saveUploadSettings}
              disabled={uploadSaving}
              className="inline-flex items-center px-6 py-2 bg-green-600 text-white rounded-md hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {uploadSaving ? (
                <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
              ) : (
                <Save className="w-4 h-4 mr-2" />
              )}
              {uploadSaving ? 'Saving...' : 'Save Upload Settings'}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
