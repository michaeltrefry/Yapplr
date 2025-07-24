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
  FileText,
  CreditCard,
  CheckCircle,
  XCircle,
  Loader2
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

interface SystemConfiguration {
  id: number;
  key: string;
  value: string;
  description: string;
  category: string;
  isVisible: boolean;
  isEditable: boolean;
  createdAt: string;
  updatedAt: string;
}

interface SubscriptionSystemStatus {
  enabled: boolean;
}

interface PaymentProviderConfiguration {
  id: number;
  providerName: string;
  isEnabled: boolean;
  environment: string;
  priority: number;
  timeoutSeconds: number;
  maxRetries: number;
  supportedCurrencies: string[];
  settings: PaymentProviderSetting[];
  createdAt: string;
  updatedAt: string;
}

interface PaymentProviderSetting {
  id: number;
  key: string;
  value: string;
  isSensitive: boolean;
  description: string;
  category: string;
  isRequired: boolean;
  createdAt: string;
  updatedAt: string;
}

interface PaymentGlobalConfiguration {
  id: number;
  defaultProvider: string;
  defaultCurrency: string;
  gracePeriodDays: number;
  maxPaymentRetries: number;
  retryIntervalDays: number;
  enableTrialPeriods: boolean;
  defaultTrialDays: number;
  enableProration: boolean;
  webhookTimeoutSeconds: number;
  verifyWebhookSignatures: boolean;
  createdAt: string;
  updatedAt: string;
}

interface PaymentConfigurationSummary {
  globalConfiguration: PaymentGlobalConfiguration;
  providers: PaymentProviderConfiguration[];
  activeProvider: string;
  providerPriority: string[];
  totalProviders: number;
  enabledProviders: number;
  lastUpdated: string;
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

  // System configuration state
  const [configurations, setConfigurations] = useState<SystemConfiguration[]>([]);
  const [subscriptionEnabled, setSubscriptionEnabled] = useState(true);
  const [configLoading, setConfigLoading] = useState(true);
  const [configSaving, setConfigSaving] = useState(false);

  // Payment configuration state
  const [paymentConfig, setPaymentConfig] = useState<PaymentConfigurationSummary | null>(null);
  const [paymentLoading, setPaymentLoading] = useState(true);
  const [paymentSaving, setPaymentSaving] = useState(false);
  const [showPaymentConfig, setShowPaymentConfig] = useState(false);
  const [providerSettings, setProviderSettings] = useState<Record<string, Record<string, string>>>({});

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
      fetchSystemConfigurations();
      fetchPaymentConfiguration();
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

  // System configuration functions
  const fetchSystemConfigurations = async () => {
    try {
      setConfigLoading(true);
      const [configResponse, statusResponse] = await Promise.all([
        api.get('/admin/system-configurations'),
        api.get('/admin/subscription-system/status')
      ]);
      setConfigurations(configResponse.data);
      setSubscriptionEnabled(statusResponse.data.enabled);
    } catch (error) {
      console.error('Failed to fetch system configuration:', error);
      setMessage({ type: 'error', text: 'Failed to load system configuration. Please try again.' });
    } finally {
      setConfigLoading(false);
    }
  };

  const toggleSubscriptionSystem = async () => {
    try {
      setConfigSaving(true);
      const newEnabled = !subscriptionEnabled;
      await api.put('/admin/subscription-system/toggle', { enabled: newEnabled });
      setSubscriptionEnabled(newEnabled);

      // Update the configurations list to keep it in sync
      setConfigurations(prev => prev.map(c =>
        c.key === 'subscription_system_enabled'
          ? { ...c, value: newEnabled.toString().toLowerCase(), updatedAt: new Date().toISOString() }
          : c
      ));

      setMessage({
        type: 'success',
        text: `Subscription system ${newEnabled ? 'enabled' : 'disabled'} successfully!`
      });
      setTimeout(() => setMessage(null), 5000);
    } catch (error) {
      console.error('Error toggling subscription system:', error);
      setMessage({ type: 'error', text: 'Failed to toggle subscription system. Please try again.' });
      setTimeout(() => setMessage(null), 5000);
    } finally {
      setConfigSaving(false);
    }
  };

  // Payment configuration functions
  const fetchPaymentConfiguration = async () => {
    try {
      setPaymentLoading(true);
      const response = await api.get('/admin/payment-configuration/summary');
      setPaymentConfig(response.data);
    } catch (error) {
      console.error('Failed to fetch payment configuration:', error);
      setMessage({ type: 'error', text: 'Failed to load payment configuration. Please try again.' });
    } finally {
      setPaymentLoading(false);
    }
  };

  const updateProviderSetting = (providerName: string, settingKey: string, value: string) => {
    setProviderSettings(prev => ({
      ...prev,
      [providerName]: {
        ...prev[providerName],
        [settingKey]: value
      }
    }));
  };

  const getRequiredSettings = (provider: PaymentProviderConfiguration) => {
    return provider.settings.filter(s => s.isRequired);
  };

  const areRequiredSettingsFilled = (provider: PaymentProviderConfiguration) => {
    const requiredSettings = getRequiredSettings(provider);
    const currentSettings = providerSettings[provider.providerName] || {};

    return requiredSettings.every(setting => {
      const currentValue = currentSettings[setting.key];

      // For sensitive fields, if no new value is entered, check if there's an existing value (indicated by ***)
      if (setting.isSensitive) {
        return currentValue !== undefined && currentValue.trim() !== '' ||
               (setting.value && setting.value !== '' && setting.value !== '***');
      }

      // For non-sensitive fields, use the current value or fallback to existing value
      const valueToCheck = currentValue || setting.value;
      return valueToCheck && valueToCheck.trim() !== '';
    });
  };

  const enablePaymentProvider = async (providerName: string) => {
    try {
      setPaymentSaving(true);
      const provider = paymentConfig?.providers.find(p => p.providerName === providerName);
      if (!provider) return;

      const currentSettings = providerSettings[providerName] || {};
      const updatedSettings = provider.settings.map(s => {
        const newValue = currentSettings[s.key];

        // For sensitive fields: only update if user entered a new value, otherwise keep existing
        if (s.isSensitive) {
          return {
            key: s.key,
            value: newValue && newValue.trim() !== '' ? newValue : s.value,
            isSensitive: s.isSensitive,
            description: s.description,
            category: s.category,
            isRequired: s.isRequired
          };
        }

        // For non-sensitive fields: use new value or fallback to existing
        return {
          key: s.key,
          value: newValue || s.value,
          isSensitive: s.isSensitive,
          description: s.description,
          category: s.category,
          isRequired: s.isRequired
        };
      });

      const updateData = {
        providerName: provider.providerName,
        isEnabled: true,
        environment: provider.environment,
        priority: provider.priority,
        timeoutSeconds: provider.timeoutSeconds,
        maxRetries: provider.maxRetries,
        supportedCurrencies: provider.supportedCurrencies,
        settings: updatedSettings
      };

      await api.put(`/admin/payment-configuration/providers/${providerName}`, updateData);

      // Refresh payment configuration
      await fetchPaymentConfiguration();

      setMessage({
        type: 'success',
        text: `${providerName} enabled successfully!`
      });
      setTimeout(() => setMessage(null), 5000);
    } catch (error) {
      console.error('Error enabling payment provider:', error);
      setMessage({ type: 'error', text: 'Failed to enable payment provider. Please try again.' });
      setTimeout(() => setMessage(null), 5000);
    } finally {
      setPaymentSaving(false);
    }
  };

  const disablePaymentProvider = async (providerName: string) => {
    try {
      setPaymentSaving(true);
      const provider = paymentConfig?.providers.find(p => p.providerName === providerName);
      if (!provider) return;

      const updateData = {
        providerName: provider.providerName,
        isEnabled: false,
        environment: provider.environment,
        priority: provider.priority,
        timeoutSeconds: provider.timeoutSeconds,
        maxRetries: provider.maxRetries,
        supportedCurrencies: provider.supportedCurrencies,
        settings: provider.settings.map(s => ({
          key: s.key,
          value: s.value,
          isSensitive: s.isSensitive,
          description: s.description,
          category: s.category,
          isRequired: s.isRequired
        }))
      };

      await api.put(`/admin/payment-configuration/providers/${providerName}`, updateData);

      // Refresh payment configuration
      await fetchPaymentConfiguration();

      setMessage({
        type: 'success',
        text: `${providerName} disabled successfully!`
      });
      setTimeout(() => setMessage(null), 5000);
    } catch (error) {
      console.error('Error disabling payment provider:', error);
      setMessage({ type: 'error', text: 'Failed to disable payment provider. Please try again.' });
      setTimeout(() => setMessage(null), 5000);
    } finally {
      setPaymentSaving(false);
    }
  };

  const updateConfiguration = async (key: string, value: string) => {
    try {
      const config = configurations.find(c => c.key === key);
      if (!config) return;

      await api.put(`/admin/system-configurations/${key}`, {
        value,
        description: config.description,
        category: config.category,
        isVisible: config.isVisible,
        isEditable: config.isEditable
      });

      setConfigurations(prev => prev.map(c =>
        c.key === key ? { ...c, value, updatedAt: new Date().toISOString() } : c
      ));

      setMessage({ type: 'success', text: 'Configuration updated successfully!' });
      setTimeout(() => setMessage(null), 5000);
    } catch (error) {
      console.error('Error updating configuration:', error);
      setMessage({ type: 'error', text: 'Failed to update configuration. Please try again.' });
      setTimeout(() => setMessage(null), 5000);
    }
  };

  // Helper functions for unit conversion
  const bytesToMB = (bytes: number) => Math.round(bytes / (1024 * 1024));
  const bytesToGB = (bytes: number) => Math.round(bytes / (1024 * 1024 * 1024) * 10) / 10;
  const mbToBytes = (mb: number) => mb * 1024 * 1024;
  const gbToBytes = (gb: number) => gb * 1024 * 1024 * 1024;
  const secondsToMinutes = (seconds: number) => Math.round(seconds / 60);
  const minutesToSeconds = (minutes: number) => minutes * 60;

  if (isLoading || loading || uploadLoading || configLoading) {
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

      {/* System Configuration */}
      <div className="bg-white rounded-lg shadow p-6">
        <div className="flex items-center mb-6">
          <Settings className="w-6 h-6 text-purple-600 mr-3" />
          <h2 className="text-xl font-semibold text-gray-900">System Configuration</h2>
        </div>

        <div className="space-y-6">
          {/* Subscription System Toggle */}
          <div>
            <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
              <CreditCard className="w-5 h-5 mr-2" />
              Subscription System
            </h3>
            <div className="flex items-center justify-between p-4 bg-gray-50 rounded-lg">
              <div>
                <div className="font-medium text-gray-900">Enable Subscription System</div>
                <div className="text-sm text-gray-600">Allow users to subscribe to premium tiers</div>
              </div>
              <label className="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  checked={subscriptionEnabled}
                  onChange={toggleSubscriptionSystem}
                  disabled={configSaving}
                  className="sr-only peer"
                />
                <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-purple-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-purple-600"></div>
              </label>
            </div>

            {!subscriptionEnabled && (
              <div className="mt-4 p-3 bg-yellow-50 border border-yellow-200 rounded-lg">
                <div className="flex items-center space-x-2">
                  <AlertTriangle className="w-4 h-4 text-yellow-600" />
                  <span className="text-sm text-yellow-800">
                    <strong>Warning:</strong> Subscription system is currently disabled. Users cannot access subscription pages or manage their subscriptions.
                  </span>
                </div>
              </div>
            )}
          </div>

          {/* Payment Gateway Configuration */}
          <div>
            <h3 className="text-lg font-medium text-gray-900 mb-4 flex items-center">
              <CreditCard className="w-5 h-5 mr-2" />
              Payment Gateway Configuration
            </h3>

            {paymentLoading ? (
              <div className="flex items-center justify-center py-8">
                <Loader2 className="w-6 h-6 animate-spin text-purple-600 mr-2" />
                <span className="text-gray-600">Loading payment configuration...</span>
              </div>
            ) : paymentConfig ? (
              <div className="space-y-4">
                {/* Global Settings Summary */}
                <div className="bg-gray-50 rounded-lg p-4">
                  <div className="flex items-center justify-between mb-3">
                    <h4 className="font-medium text-gray-900">Global Settings</h4>
                    <button
                      onClick={() => setShowPaymentConfig(!showPaymentConfig)}
                      className="text-purple-600 hover:text-purple-700 text-sm font-medium"
                    >
                      {showPaymentConfig ? 'Hide Details' : 'Show Details'}
                    </button>
                  </div>
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
                    <div>
                      <span className="text-gray-600">Active Provider:</span>
                      <div className="font-medium">{paymentConfig.activeProvider}</div>
                    </div>
                    <div>
                      <span className="text-gray-600">Default Currency:</span>
                      <div className="font-medium">{paymentConfig.globalConfiguration.defaultCurrency}</div>
                    </div>
                    <div>
                      <span className="text-gray-600">Enabled Providers:</span>
                      <div className="font-medium">{paymentConfig.enabledProviders} / {paymentConfig.totalProviders}</div>
                    </div>
                    <div>
                      <span className="text-gray-600">Trial Periods:</span>
                      <div className="font-medium">
                        {paymentConfig.globalConfiguration.enableTrialPeriods ? 'Enabled' : 'Disabled'}
                      </div>
                    </div>
                  </div>
                </div>

                {/* Payment Providers */}
                <div className="space-y-3">
                  <h4 className="font-medium text-gray-900">Payment Providers</h4>
                  {paymentConfig.providers.map((provider) => (
                    <div key={provider.id} className="border border-gray-200 rounded-lg p-4">
                      <div className="flex items-center justify-between">
                        <div className="flex items-center space-x-3">
                          <div className="flex-shrink-0">
                            {provider.providerName === 'PayPal' ? (
                              <div className="w-8 h-8 bg-blue-600 rounded flex items-center justify-center">
                                <span className="text-white text-xs font-bold">PP</span>
                              </div>
                            ) : provider.providerName === 'Stripe' ? (
                              <div className="w-8 h-8 bg-purple-600 rounded flex items-center justify-center">
                                <span className="text-white text-xs font-bold">S</span>
                              </div>
                            ) : (
                              <div className="w-8 h-8 bg-gray-600 rounded flex items-center justify-center">
                                <CreditCard className="w-4 h-4 text-white" />
                              </div>
                            )}
                          </div>
                          <div>
                            <div className="font-medium text-gray-900">{provider.providerName}</div>
                            <div className="text-sm text-gray-600">
                              {provider.environment} • Priority: {provider.priority}
                            </div>
                          </div>
                        </div>
                        <div className="flex items-center space-x-2">
                          {provider.isEnabled ? (
                            <CheckCircle className="w-4 h-4 text-green-600" />
                          ) : (
                            <XCircle className="w-4 h-4 text-red-600" />
                          )}
                          <span className={`text-sm font-medium ${
                            provider.isEnabled ? 'text-green-600' : 'text-red-600'
                          }`}>
                            {provider.isEnabled ? 'Enabled' : 'Disabled'}
                          </span>
                        </div>
                      </div>

                      {/* Configuration Form */}
                      {!provider.isEnabled && (
                        <div className="mt-4 pt-4 border-t border-gray-200">
                          <h5 className="text-sm font-medium text-gray-900 mb-4">Provider Configuration</h5>

                          {/* Group settings by category */}
                          {['Authentication', 'Webhooks', 'General'].map(category => {
                            const categorySettings = provider.settings.filter(s => s.category === category || (!s.category && category === 'General'));
                            if (categorySettings.length === 0) return null;

                            return (
                              <div key={category} className="mb-6">
                                <h6 className="text-sm font-medium text-gray-800 mb-3 flex items-center">
                                  {category === 'Authentication' && <span className="w-2 h-2 bg-red-500 rounded-full mr-2"></span>}
                                  {category === 'Webhooks' && <span className="w-2 h-2 bg-blue-500 rounded-full mr-2"></span>}
                                  {category === 'General' && <span className="w-2 h-2 bg-gray-500 rounded-full mr-2"></span>}
                                  {category}
                                  {category === 'Authentication' && <span className="text-xs text-red-600 ml-2">(Required)</span>}
                                </h6>
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                  {categorySettings.map((setting) => (
                                    <div key={setting.key} className={setting.isRequired ? 'md:col-span-2' : ''}>
                                      <label className="block text-sm font-medium text-gray-700 mb-1">
                                        {setting.key}
                                        {setting.isRequired && <span className="text-red-500 ml-1">*</span>}
                                      </label>
                                      {setting.key === 'WebhookEnabledEvents' ? (
                                        <textarea
                                          value={providerSettings[provider.providerName]?.[setting.key] ?? (setting.isSensitive ? '' : setting.value)}
                                          onChange={(e) => updateProviderSetting(provider.providerName, setting.key, e.target.value)}
                                          placeholder={setting.description}
                                          rows={3}
                                          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent text-xs"
                                        />
                                      ) : (
                                        <input
                                          type={setting.isSensitive ? 'password' : 'text'}
                                          value={providerSettings[provider.providerName]?.[setting.key] ?? (setting.isSensitive ? '' : setting.value)}
                                          onChange={(e) => updateProviderSetting(provider.providerName, setting.key, e.target.value)}
                                          placeholder={setting.isSensitive ? 'Enter new value or leave blank to keep current' : setting.description}
                                          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent"
                                        />
                                      )}
                                      <p className="text-xs text-gray-500 mt-1">{setting.description}</p>
                                    </div>
                                  ))}
                                </div>
                              </div>
                            );
                          })}

                          <div className="mt-6 flex justify-between items-center pt-4 border-t border-gray-200">
                            <div className="text-sm text-gray-600">
                              <span className="text-red-500">*</span> Required fields must be filled to enable the provider
                            </div>
                            <button
                              onClick={() => enablePaymentProvider(provider.providerName)}
                              disabled={!areRequiredSettingsFilled(provider) || paymentSaving}
                              className={`px-6 py-2 rounded-md text-sm font-medium transition-colors ${
                                areRequiredSettingsFilled(provider) && !paymentSaving
                                  ? 'bg-green-600 text-white hover:bg-green-700'
                                  : 'bg-gray-300 text-gray-500 cursor-not-allowed'
                              }`}
                            >
                              {paymentSaving ? 'Enabling...' : 'Enable Provider'}
                            </button>
                          </div>
                        </div>
                      )}

                      {/* Configuration View for Enabled Providers */}
                      {provider.isEnabled && (
                        <div className="mt-4 pt-4 border-t border-gray-200">
                          <div className="flex justify-between items-center mb-4">
                            <div>
                              <h5 className="text-sm font-medium text-gray-900">Current Configuration</h5>
                              <p className="text-sm text-gray-600">
                                This provider is currently enabled and processing payments.
                              </p>
                            </div>
                            <button
                              onClick={() => disablePaymentProvider(provider.providerName)}
                              disabled={paymentSaving}
                              className="px-4 py-2 bg-red-600 text-white rounded-md text-sm font-medium hover:bg-red-700 disabled:opacity-50 transition-colors"
                            >
                              {paymentSaving ? 'Disabling...' : 'Disable Provider'}
                            </button>
                          </div>

                          {/* Show current settings grouped by category */}
                          {['Authentication', 'Webhooks', 'General'].map(category => {
                            const categorySettings = provider.settings.filter(s => s.category === category || (!s.category && category === 'General'));
                            if (categorySettings.length === 0) return null;

                            return (
                              <div key={category} className="mb-4">
                                <h6 className="text-sm font-medium text-gray-800 mb-2 flex items-center">
                                  {category === 'Authentication' && <span className="w-2 h-2 bg-red-500 rounded-full mr-2"></span>}
                                  {category === 'Webhooks' && <span className="w-2 h-2 bg-blue-500 rounded-full mr-2"></span>}
                                  {category === 'General' && <span className="w-2 h-2 bg-gray-500 rounded-full mr-2"></span>}
                                  {category}
                                </h6>
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                                  {categorySettings.map((setting) => (
                                    <div key={setting.key} className="bg-gray-50 p-3 rounded-md">
                                      <div className="flex justify-between items-start">
                                        <div className="flex-1">
                                          <span className="text-sm font-medium text-gray-700">{setting.key}</span>
                                          {setting.isRequired && <span className="text-red-500 ml-1 text-xs">*</span>}
                                          <div className="text-sm text-gray-900 mt-1 font-mono">
                                            {setting.isSensitive ? '••••••••' : (setting.value || 'Not set')}
                                          </div>
                                          <p className="text-xs text-gray-500 mt-1">{setting.description}</p>
                                        </div>
                                      </div>
                                    </div>
                                  ))}
                                </div>
                              </div>
                            );
                          })}

                          <div className="mt-4 pt-3 border-t border-gray-200">
                            <Link
                              href="/admin/payment-configuration"
                              className="inline-flex items-center text-sm text-purple-600 hover:text-purple-700"
                            >
                              <Settings className="w-4 h-4 mr-1" />
                              Advanced Configuration
                            </Link>
                          </div>
                        </div>
                      )}

                      {showPaymentConfig && (
                        <div className="mt-4 pt-4 border-t border-gray-200">
                          <div className="grid grid-cols-2 md:grid-cols-3 gap-4 text-sm">
                            <div>
                              <span className="text-gray-600">Timeout:</span>
                              <div className="font-medium">{provider.timeoutSeconds}s</div>
                            </div>
                            <div>
                              <span className="text-gray-600">Max Retries:</span>
                              <div className="font-medium">{provider.maxRetries}</div>
                            </div>
                            <div>
                              <span className="text-gray-600">Currencies:</span>
                              <div className="font-medium">{provider.supportedCurrencies.join(', ')}</div>
                            </div>
                          </div>
                          <div className="mt-3">
                            <span className="text-gray-600 text-sm">Settings:</span>
                            <div className="mt-1 space-y-1">
                              {provider.settings.map((setting) => (
                                <div key={setting.id} className="flex justify-between text-sm">
                                  <span className="text-gray-700">{setting.key}:</span>
                                  <span className="font-medium">
                                    {setting.isSensitive ? '***' : setting.value || 'Not set'}
                                  </span>
                                </div>
                              ))}
                            </div>
                          </div>
                        </div>
                      )}
                    </div>
                  ))}
                </div>

                <div className="flex justify-end">
                  <Link
                    href="/admin/payment-configuration"
                    className="inline-flex items-center px-4 py-2 bg-purple-600 text-white rounded-md hover:bg-purple-700 transition-colors"
                  >
                    Advanced Configuration
                    <ArrowRight className="w-4 h-4 ml-2" />
                  </Link>
                </div>
              </div>
            ) : (
              <div className="text-center py-8">
                <CreditCard className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                <h4 className="text-lg font-medium text-gray-900 mb-2">Payment configuration unavailable</h4>
                <p className="text-gray-600">Unable to load payment gateway configuration.</p>
              </div>
            )}
          </div>

          {/* System Configurations List */}
          <div>
            <h3 className="text-lg font-medium text-gray-900 mb-4">All System Configurations</h3>

            {configurations.filter(config => config.isVisible).length === 0 ? (
              <div className="text-center py-8">
                <Settings className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                <h4 className="text-lg font-medium text-gray-900 mb-2">No configurations found</h4>
                <p className="text-gray-600">System configurations will appear here once they are created.</p>
              </div>
            ) : (
              <div className="space-y-4">
                {configurations.filter(config => config.isVisible).map((config) => (
                  <div key={config.id} className="border border-gray-200 rounded-lg p-4">
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <div className="flex items-center space-x-2 mb-1">
                          <h4 className="font-medium text-gray-900">{config.key}</h4>
                          <span className="px-2 py-1 text-xs font-medium bg-gray-100 text-gray-600 rounded">
                            {config.category}
                          </span>
                          {!config.isVisible && (
                            <span className="px-2 py-1 text-xs font-medium bg-red-100 text-red-600 rounded">
                              Hidden
                            </span>
                          )}
                          {!config.isEditable && (
                            <span className="px-2 py-1 text-xs font-medium bg-orange-100 text-orange-600 rounded">
                              Read-only
                            </span>
                          )}
                        </div>
                        <p className="text-sm text-gray-600 mb-2">{config.description}</p>
                        {config.isEditable ? (
                          <div className="flex items-center space-x-2">
                            <input
                              type="text"
                              value={config.value}
                              onChange={(e) => {
                                const newValue = e.target.value;
                                setConfigurations(prev => prev.map(c =>
                                  c.id === config.id ? { ...c, value: newValue } : c
                                ));
                              }}
                              onBlur={(e) => {
                                if (e.target.value !== config.value) {
                                  updateConfiguration(config.key, e.target.value);
                                }
                              }}
                              className="flex-1 px-3 py-1 text-sm border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-purple-500"
                            />
                          </div>
                        ) : (
                          <div className="px-3 py-1 text-sm bg-gray-100 rounded font-mono">
                            {config.value}
                          </div>
                        )}
                      </div>
                    </div>
                    <div className="mt-2 text-xs text-gray-500">
                      Last updated: {new Date(config.updatedAt).toLocaleString()}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
