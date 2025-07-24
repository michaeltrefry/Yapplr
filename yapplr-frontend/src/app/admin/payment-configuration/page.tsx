'use client';

import { useEffect, useState } from 'react';
import api from '@/lib/api';
import {
  CreditCard,
  Settings,
  Save,
  RefreshCw,
  AlertTriangle,
  CheckCircle,
  XCircle,
  Eye,
  EyeOff,
  TestTube,
  ArrowLeft,
  Loader2,
} from 'lucide-react';
import Link from 'next/link';

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

interface Message {
  type: 'success' | 'error';
  text: string;
}

export default function PaymentConfigurationPage() {
  const [config, setConfig] = useState<PaymentConfigurationSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState<string | null>(null);
  const [message, setMessage] = useState<Message | null>(null);
  const [showSensitive, setShowSensitive] = useState<Record<string, boolean>>({});
  const [editingProvider, setEditingProvider] = useState<string | null>(null);
  const [editingGlobal, setEditingGlobal] = useState(false);
  const [providerSettings, setProviderSettings] = useState<Record<string, Record<string, string>>>({});

  useEffect(() => {
    fetchConfiguration();
  }, []);

  const fetchConfiguration = async () => {
    try {
      setLoading(true);
      const response = await api.get('/admin/payment-configuration/summary');
      setConfig(response.data);
    } catch (error) {
      console.error('Failed to fetch payment configuration:', error);
      setMessage({ type: 'error', text: 'Failed to load payment configuration. Please try again.' });
    } finally {
      setLoading(false);
    }
  };

  const updateProviderConfiguration = async (providerName: string, updates: Partial<PaymentProviderConfiguration>) => {
    try {
      setSaving(true);
      const provider = config?.providers.find(p => p.providerName === providerName);
      if (!provider) return;

      const updateData = {
        providerName: provider.providerName,
        isEnabled: updates.isEnabled ?? provider.isEnabled,
        environment: updates.environment ?? provider.environment,
        priority: updates.priority ?? provider.priority,
        timeoutSeconds: updates.timeoutSeconds ?? provider.timeoutSeconds,
        maxRetries: updates.maxRetries ?? provider.maxRetries,
        supportedCurrencies: updates.supportedCurrencies ?? provider.supportedCurrencies,
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
      await fetchConfiguration();

      setMessage({
        type: 'success',
        text: `${providerName} configuration updated successfully!`
      });
      setTimeout(() => setMessage(null), 5000);
    } catch (error) {
      console.error('Error updating provider configuration:', error);
      setMessage({ type: 'error', text: 'Failed to update provider configuration. Please try again.' });
      setTimeout(() => setMessage(null), 5000);
    } finally {
      setSaving(false);
      setEditingProvider(null);
    }
  };

  const updateGlobalConfiguration = async (updates: Partial<PaymentGlobalConfiguration>) => {
    try {
      setSaving(true);
      if (!config?.globalConfiguration) return;

      const updateData = {
        defaultProvider: updates.defaultProvider ?? config.globalConfiguration.defaultProvider,
        defaultCurrency: updates.defaultCurrency ?? config.globalConfiguration.defaultCurrency,
        gracePeriodDays: updates.gracePeriodDays ?? config.globalConfiguration.gracePeriodDays,
        maxPaymentRetries: updates.maxPaymentRetries ?? config.globalConfiguration.maxPaymentRetries,
        retryIntervalDays: updates.retryIntervalDays ?? config.globalConfiguration.retryIntervalDays,
        enableTrialPeriods: updates.enableTrialPeriods ?? config.globalConfiguration.enableTrialPeriods,
        defaultTrialDays: updates.defaultTrialDays ?? config.globalConfiguration.defaultTrialDays,
        enableProration: updates.enableProration ?? config.globalConfiguration.enableProration,
        webhookTimeoutSeconds: updates.webhookTimeoutSeconds ?? config.globalConfiguration.webhookTimeoutSeconds,
        verifyWebhookSignatures: updates.verifyWebhookSignatures ?? config.globalConfiguration.verifyWebhookSignatures
      };

      await api.put('/admin/payment-configuration/global', updateData);
      await fetchConfiguration();

      setMessage({
        type: 'success',
        text: 'Global configuration updated successfully!'
      });
      setTimeout(() => setMessage(null), 5000);
    } catch (error) {
      console.error('Error updating global configuration:', error);
      setMessage({ type: 'error', text: 'Failed to update global configuration. Please try again.' });
      setTimeout(() => setMessage(null), 5000);
    } finally {
      setSaving(false);
      setEditingGlobal(false);
    }
  };

  const testProviderConfiguration = async (providerName: string) => {
    try {
      setTesting(providerName);
      const response = await api.post(`/admin/payment-configuration/providers/${providerName}/test`);
      
      if (response.data.isSuccessful) {
        setMessage({
          type: 'success',
          text: `${providerName} connectivity test passed!`
        });
      } else {
        setMessage({
          type: 'error',
          text: `${providerName} test failed: ${response.data.message}`
        });
      }
      setTimeout(() => setMessage(null), 5000);
    } catch (error) {
      console.error('Error testing provider configuration:', error);
      setMessage({ type: 'error', text: `Failed to test ${providerName} configuration.` });
      setTimeout(() => setMessage(null), 5000);
    } finally {
      setTesting(null);
    }
  };

  const toggleSensitiveVisibility = (settingKey: string) => {
    setShowSensitive(prev => ({
      ...prev,
      [settingKey]: !prev[settingKey]
    }));
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
      setSaving(true);
      const provider = config?.providers.find(p => p.providerName === providerName);
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
      await fetchConfiguration();

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
      setSaving(false);
    }
  };

  const disablePaymentProvider = async (providerName: string) => {
    try {
      setSaving(true);
      const provider = config?.providers.find(p => p.providerName === providerName);
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
      await fetchConfiguration();

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
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="flex items-center space-x-2">
          <Loader2 className="w-6 h-6 animate-spin text-purple-600" />
          <span className="text-gray-600">Loading payment configuration...</span>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Header */}
        <div className="mb-8">
          <div className="flex items-center space-x-4 mb-4">
            <Link
              href="/admin/settings"
              className="flex items-center text-purple-600 hover:text-purple-700"
            >
              <ArrowLeft className="w-4 h-4 mr-1" />
              Back to Settings
            </Link>
          </div>
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold text-gray-900 flex items-center">
                <CreditCard className="w-8 h-8 mr-3 text-purple-600" />
                Payment Gateway Configuration
              </h1>
              <p className="text-gray-600 mt-2">
                Configure payment providers and global payment settings
              </p>
            </div>
            <button
              onClick={fetchConfiguration}
              disabled={loading}
              className="flex items-center px-4 py-2 bg-purple-600 text-white rounded-md hover:bg-purple-700 disabled:opacity-50"
            >
              <RefreshCw className={`w-4 h-4 mr-2 ${loading ? 'animate-spin' : ''}`} />
              Refresh
            </button>
          </div>
        </div>

        {/* Message */}
        {message && (
          <div className={`mb-6 p-4 rounded-lg flex items-center ${
            message.type === 'success' 
              ? 'bg-green-50 border border-green-200 text-green-800' 
              : 'bg-red-50 border border-red-200 text-red-800'
          }`}>
            {message.type === 'success' ? (
              <CheckCircle className="w-5 h-5 mr-2" />
            ) : (
              <XCircle className="w-5 h-5 mr-2" />
            )}
            {message.text}
          </div>
        )}

        {config && (
          <div className="space-y-8">
            {/* Global Configuration */}
            <div className="bg-white rounded-lg shadow-sm border border-gray-200">
              <div className="px-6 py-4 border-b border-gray-200">
                <div className="flex items-center justify-between">
                  <h2 className="text-xl font-semibold text-gray-900 flex items-center">
                    <Settings className="w-5 h-5 mr-2" />
                    Global Configuration
                  </h2>
                  <button
                    onClick={() => setEditingGlobal(!editingGlobal)}
                    className="text-purple-600 hover:text-purple-700 text-sm font-medium"
                  >
                    {editingGlobal ? 'Cancel' : 'Edit'}
                  </button>
                </div>
              </div>
              <div className="p-6">
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Default Provider
                    </label>
                    {editingGlobal ? (
                      <select
                        value={config.globalConfiguration.defaultProvider}
                        onChange={(e) => updateGlobalConfiguration({ defaultProvider: e.target.value })}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-purple-500"
                      >
                        {config.providers.map(provider => (
                          <option key={provider.providerName} value={provider.providerName}>
                            {provider.providerName}
                          </option>
                        ))}
                      </select>
                    ) : (
                      <div className="text-gray-900">{config.globalConfiguration.defaultProvider}</div>
                    )}
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-1">
                      Default Currency
                    </label>
                    {editingGlobal ? (
                      <input
                        type="text"
                        value={config.globalConfiguration.defaultCurrency}
                        onChange={(e) => updateGlobalConfiguration({ defaultCurrency: e.target.value })}
                        className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-purple-500"
                      />
                    ) : (
                      <div className="text-gray-900">{config.globalConfiguration.defaultCurrency}</div>
                    )}
                  </div>
                </div>
              </div>
            </div>

            {/* Provider Configurations */}
            <div className="space-y-6">
              {config.providers.map(provider => (
                <div key={provider.providerName} className="bg-white rounded-lg shadow-sm border border-gray-200">
                  <div className="px-6 py-4 border-b border-gray-200">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center space-x-3">
                        <h3 className="text-lg font-semibold text-gray-900">
                          {provider.providerName}
                        </h3>
                        <span className={`px-2 py-1 text-xs font-medium rounded-full ${
                          provider.isEnabled
                            ? 'bg-green-100 text-green-800'
                            : 'bg-red-100 text-red-800'
                        }`}>
                          {provider.isEnabled ? 'Enabled' : 'Disabled'}
                        </span>
                        <span className="px-2 py-1 text-xs font-medium bg-gray-100 text-gray-800 rounded-full">
                          {provider.environment}
                        </span>
                      </div>
                      <div className="flex items-center space-x-2">
                        <button
                          onClick={() => testProviderConfiguration(provider.providerName)}
                          disabled={testing === provider.providerName || !provider.isEnabled}
                          className="flex items-center px-3 py-1 text-sm bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50"
                        >
                          {testing === provider.providerName ? (
                            <Loader2 className="w-4 h-4 mr-1 animate-spin" />
                          ) : (
                            <TestTube className="w-4 h-4 mr-1" />
                          )}
                          Test
                        </button>
                        <button
                          onClick={() => setEditingProvider(
                            editingProvider === provider.providerName ? null : provider.providerName
                          )}
                          className="text-purple-600 hover:text-purple-700 text-sm font-medium"
                        >
                          {editingProvider === provider.providerName ? 'Cancel' : 'Edit'}
                        </button>
                      </div>
                    </div>
                  </div>

                  <div className="p-6">
                    {/* Provider Basic Settings */}
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 mb-6">
                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">
                          Status
                        </label>
                        <div className="flex items-center">
                          <span className={`text-sm font-medium ${
                            provider.isEnabled ? 'text-green-600' : 'text-red-600'
                          }`}>
                            {provider.isEnabled ? 'Enabled' : 'Disabled'}
                          </span>
                        </div>
                      </div>

                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">
                          Environment
                        </label>
                        {editingProvider === provider.providerName ? (
                          <select
                            value={provider.environment}
                            onChange={(e) => updateProviderConfiguration(provider.providerName, { environment: e.target.value })}
                            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-purple-500"
                          >
                            <option value="sandbox">Sandbox</option>
                            <option value="test">Test</option>
                            <option value="live">Live</option>
                          </select>
                        ) : (
                          <div className="text-gray-900">{provider.environment}</div>
                        )}
                      </div>

                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">
                          Priority
                        </label>
                        {editingProvider === provider.providerName ? (
                          <input
                            type="number"
                            value={provider.priority}
                            onChange={(e) => updateProviderConfiguration(provider.providerName, { priority: parseInt(e.target.value) })}
                            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-purple-500"
                          />
                        ) : (
                          <div className="text-gray-900">{provider.priority}</div>
                        )}
                      </div>

                      <div>
                        <label className="block text-sm font-medium text-gray-700 mb-1">
                          Timeout (seconds)
                        </label>
                        {editingProvider === provider.providerName ? (
                          <input
                            type="number"
                            value={provider.timeoutSeconds}
                            onChange={(e) => updateProviderConfiguration(provider.providerName, { timeoutSeconds: parseInt(e.target.value) })}
                            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-purple-500"
                          />
                        ) : (
                          <div className="text-gray-900">{provider.timeoutSeconds}</div>
                        )}
                      </div>
                    </div>

                    {/* Configuration Form for Disabled Providers */}
                    {!provider.isEnabled && provider.settings.length > 0 && (
                      <div className="mt-6 pt-6 border-t border-gray-200">
                        <h4 className="text-md font-medium text-gray-900 mb-4">Provider Configuration</h4>

                        {/* Group settings by category */}
                        {['Authentication', 'Webhooks', 'General'].map(category => {
                          const categorySettings = provider.settings.filter(s => s.category === category || (!s.category && category === 'General'));
                          if (categorySettings.length === 0) return null;

                          return (
                            <div key={category} className="mb-6">
                              <h5 className="text-sm font-medium text-gray-800 mb-3 flex items-center">
                                {category === 'Authentication' && <span className="w-2 h-2 bg-red-500 rounded-full mr-2"></span>}
                                {category === 'Webhooks' && <span className="w-2 h-2 bg-blue-500 rounded-full mr-2"></span>}
                                {category === 'General' && <span className="w-2 h-2 bg-gray-500 rounded-full mr-2"></span>}
                                {category}
                                {category === 'Authentication' && <span className="text-xs text-red-600 ml-2">(Required)</span>}
                              </h5>
                              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                {categorySettings.map((setting) => (
                                  <div key={setting.key} className={setting.isRequired ? 'md:col-span-2' : ''}>
                                    <div className="flex items-center justify-between mb-1">
                                      <label className="block text-sm font-medium text-gray-700">
                                        {setting.key}
                                        {setting.isRequired && <span className="text-red-500 ml-1">*</span>}
                                      </label>
                                      {setting.isSensitive && (
                                        <button
                                          onClick={() => toggleSensitiveVisibility(setting.key)}
                                          className="text-gray-500 hover:text-gray-700"
                                        >
                                          {showSensitive[setting.key] ? (
                                            <EyeOff className="w-4 h-4" />
                                          ) : (
                                            <Eye className="w-4 h-4" />
                                          )}
                                        </button>
                                      )}
                                    </div>
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
                                        type={setting.isSensitive && !showSensitive[setting.key] ? 'password' : 'text'}
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
                            disabled={!areRequiredSettingsFilled(provider) || saving}
                            className={`px-6 py-2 rounded-md text-sm font-medium transition-colors ${
                              areRequiredSettingsFilled(provider) && !saving
                                ? 'bg-green-600 text-white hover:bg-green-700'
                                : 'bg-gray-300 text-gray-500 cursor-not-allowed'
                            }`}
                          >
                            {saving ? 'Enabling...' : 'Enable Provider'}
                          </button>
                        </div>
                      </div>
                    )}

                    {/* Configuration View for Enabled Providers */}
                    {provider.isEnabled && provider.settings.length > 0 && (
                      <div className="mt-6 pt-6 border-t border-gray-200">
                        <div className="flex justify-between items-center mb-4">
                          <h4 className="text-md font-medium text-gray-900">Current Configuration</h4>
                          <button
                            onClick={() => disablePaymentProvider(provider.providerName)}
                            disabled={saving}
                            className="px-4 py-2 bg-red-600 text-white rounded-md text-sm font-medium hover:bg-red-700 disabled:opacity-50 transition-colors"
                          >
                            {saving ? 'Disabling...' : 'Disable Provider'}
                          </button>
                        </div>

                        {/* Show current settings grouped by category */}
                        {['Authentication', 'Webhooks', 'General'].map(category => {
                          const categorySettings = provider.settings.filter(s => s.category === category || (!s.category && category === 'General'));
                          if (categorySettings.length === 0) return null;

                          return (
                            <div key={category} className="mb-4">
                              <h5 className="text-sm font-medium text-gray-800 mb-2 flex items-center">
                                {category === 'Authentication' && <span className="w-2 h-2 bg-red-500 rounded-full mr-2"></span>}
                                {category === 'Webhooks' && <span className="w-2 h-2 bg-blue-500 rounded-full mr-2"></span>}
                                {category === 'General' && <span className="w-2 h-2 bg-gray-500 rounded-full mr-2"></span>}
                                {category}
                              </h5>
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
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
