'use client';

import { useState } from 'react';
import { X, Clock, AlertTriangle } from 'lucide-react';

interface SuspendUserModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (reason: string, days: number | null) => Promise<void>;
  username: string;
  isLoading?: boolean;
}

export default function SuspendUserModal({
  isOpen,
  onClose,
  onSubmit,
  username,
  isLoading = false
}: SuspendUserModalProps) {
  const [reason, setReason] = useState('');
  const [durationType, setDurationType] = useState<'temporary' | 'permanent'>('temporary');
  const [days, setDays] = useState<string>('7');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!reason.trim()) {
      alert('Please provide a reason for suspension.');
      return;
    }

    if (durationType === 'temporary' && (!days || parseInt(days) <= 0)) {
      alert('Please provide a valid number of days for temporary suspension.');
      return;
    }

    try {
      setIsSubmitting(true);
      const suspensionDays = durationType === 'temporary' ? parseInt(days) : null;
      await onSubmit(reason.trim(), suspensionDays);
      handleClose();
    } catch (error) {
      console.error('Failed to suspend user:', error);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    if (!isSubmitting && !isLoading) {
      onClose();
      setReason('');
      setDurationType('temporary');
      setDays('7');
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg max-w-md w-full max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200">
          <div className="flex items-center space-x-2">
            <Clock className="w-5 h-5 text-yellow-500" />
            <h2 className="text-lg font-semibold text-gray-900">
              Suspend User
            </h2>
          </div>
          <button
            onClick={handleClose}
            disabled={isSubmitting || isLoading}
            className="text-gray-400 hover:text-gray-600 disabled:opacity-50"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="p-4 space-y-4">
          {/* User Info */}
          <div className="bg-gray-50 p-3 rounded-lg">
            <p className="text-sm text-gray-600 mb-1">Suspending user:</p>
            <p className="text-sm font-medium text-gray-900">@{username}</p>
          </div>

          {/* Suspension Duration */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Suspension Duration *
            </label>
            <div className="space-y-3">
              <label className="flex items-center space-x-2 cursor-pointer">
                <input
                  type="radio"
                  name="durationType"
                  value="temporary"
                  checked={durationType === 'temporary'}
                  onChange={(e) => setDurationType(e.target.value as 'temporary')}
                  className="text-yellow-600 focus:ring-yellow-500"
                />
                <span className="text-sm text-gray-700">Temporary suspension</span>
              </label>
              
              {durationType === 'temporary' && (
                <div className="ml-6">
                  <div className="flex items-center space-x-2">
                    <input
                      type="number"
                      value={days}
                      onChange={(e) => setDays(e.target.value)}
                      min="1"
                      max="365"
                      className="w-20 px-2 py-1 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-yellow-500 focus:border-transparent"
                      required={durationType === 'temporary'}
                    />
                    <span className="text-sm text-gray-600">days</span>
                  </div>
                  <p className="text-xs text-gray-500 mt-1">
                    User will be automatically unsuspended after this period
                  </p>
                </div>
              )}

              <label className="flex items-center space-x-2 cursor-pointer">
                <input
                  type="radio"
                  name="durationType"
                  value="permanent"
                  checked={durationType === 'permanent'}
                  onChange={(e) => setDurationType(e.target.value as 'permanent')}
                  className="text-yellow-600 focus:ring-yellow-500"
                />
                <span className="text-sm text-gray-700">Permanent suspension</span>
              </label>
              
              {durationType === 'permanent' && (
                <p className="ml-6 text-xs text-gray-500">
                  User will remain suspended until manually unsuspended
                </p>
              )}
            </div>
          </div>

          {/* Reason Text Area */}
          <div>
            <label htmlFor="reason" className="block text-sm font-medium text-gray-700 mb-2">
              Reason for Suspension *
            </label>
            <textarea
              id="reason"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Explain why this user is being suspended..."
              rows={4}
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-yellow-500 focus:border-transparent resize-none"
              required
            />
          </div>

          {/* Warning Message */}
          <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-3">
            <div className="flex items-start space-x-2">
              <AlertTriangle className="w-4 h-4 text-yellow-600 mt-0.5 flex-shrink-0" />
              <div className="text-sm text-yellow-800">
                <p className="font-medium">Important:</p>
                <ul className="mt-1 list-disc list-inside space-y-1">
                  <li>The user will be notified of their suspension</li>
                  <li>They will not be able to post or interact until unsuspended</li>
                  <li>This action will be logged in the audit trail</li>
                </ul>
              </div>
            </div>
          </div>

          {/* Action Buttons */}
          <div className="flex space-x-3 pt-2">
            <button
              type="button"
              onClick={handleClose}
              disabled={isSubmitting || isLoading}
              className="flex-1 px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 border border-gray-300 rounded-md hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-500 disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isSubmitting || isLoading || !reason.trim()}
              className="flex-1 px-4 py-2 text-sm font-medium text-white bg-yellow-600 border border-transparent rounded-md hover:bg-yellow-700 focus:outline-none focus:ring-2 focus:ring-yellow-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isSubmitting ? 'Suspending...' : 'Suspend User'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
