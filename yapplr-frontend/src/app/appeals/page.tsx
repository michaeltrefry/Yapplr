'use client';

import { useState } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { adminApi } from '@/lib/api';
import { AppealType, CreateAppealDto } from '@/types';
import { useRouter } from 'next/navigation';
import {
  Scale,
  ArrowLeft,
  AlertTriangle,
  FileText,
  MessageSquare,
  Tag,
  HelpCircle,
} from 'lucide-react';
import Link from 'next/link';

export default function AppealsPage() {
  const { user } = useAuth();
  const router = useRouter();
  const [loading, setLoading] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [formData, setFormData] = useState<CreateAppealDto>({
    type: AppealType.Other,
    reason: '',
    additionalInfo: '',
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!user) return;

    try {
      setLoading(true);
      await adminApi.createUserAppeal(user.id, formData);
      setSubmitted(true);
    } catch (error) {
      console.error('Failed to submit appeal:', error);
      alert('Failed to submit appeal. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const getAppealTypeIcon = (type: AppealType) => {
    switch (type) {
      case AppealType.Suspension:
        return <AlertTriangle className="h-5 w-5 text-yellow-500" />;
      case AppealType.Ban:
        return <AlertTriangle className="h-5 w-5 text-red-500" />;
      case AppealType.ContentRemoval:
        return <FileText className="h-5 w-5 text-orange-500" />;
      case AppealType.SystemTag:
        return <Tag className="h-5 w-5 text-purple-500" />;
      case AppealType.Other:
        return <HelpCircle className="h-5 w-5 text-blue-500" />;
      default:
        return <Scale className="h-5 w-5 text-gray-500" />;
    }
  };

  const getAppealTypeName = (type: AppealType) => {
    switch (type) {
      case AppealType.Suspension:
        return 'Account Suspension';
      case AppealType.Ban:
        return 'Account Ban';
      case AppealType.ContentRemoval:
        return 'Content Removal';
      case AppealType.SystemTag:
        return 'Content Tag';
      case AppealType.Other:
        return 'Other';
      default:
        return 'Unknown';
    }
  };

  if (submitted) {
    return (
      <div className="min-h-screen bg-gray-50 py-8">
        <div className="max-w-2xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="bg-white rounded-lg shadow p-8 text-center">
            <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-green-100 mb-4">
              <Scale className="h-6 w-6 text-green-600" />
            </div>
            <h1 className="text-2xl font-bold text-gray-900 mb-4">Appeal Submitted</h1>
            <p className="text-gray-600 mb-6">
              Your appeal has been submitted successfully. Our moderation team will review it and respond within 24-48 hours.
              You will receive a notification when your appeal has been reviewed.
            </p>
            <div className="space-y-3">
              <Link
                href="/"
                className="block w-full bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 transition-colors"
              >
                Return to Home
              </Link>
              <Link
                href="/appeals/status"
                className="block w-full border border-gray-300 text-gray-700 px-4 py-2 rounded-md hover:bg-gray-50 transition-colors"
              >
                View My Appeals
              </Link>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="max-w-2xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mb-8">
          <Link
            href="/"
            className="inline-flex items-center text-blue-600 hover:text-blue-800 mb-4"
          >
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Home
          </Link>
          <div className="flex items-center mb-4">
            <Scale className="h-8 w-8 text-blue-600 mr-3" />
            <h1 className="text-3xl font-bold text-gray-900">Submit an Appeal</h1>
          </div>
          <p className="text-gray-600">
            If you believe a moderation action was taken in error, you can submit an appeal for review.
            Please provide as much detail as possible to help us understand your situation.
          </p>
        </div>

        {/* Appeal Form */}
        <div className="bg-white rounded-lg shadow p-6">
          <form onSubmit={handleSubmit} className="space-y-6">
            {/* Appeal Type */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-3">
                What are you appealing?
              </label>
              <div className="grid grid-cols-1 gap-3">
                {Object.values(AppealType)
                  .filter(value => typeof value === 'number')
                  .map((type) => (
                    <label
                      key={type}
                      className={`flex items-center p-4 border rounded-lg cursor-pointer transition-colors ${
                        formData.type === type
                          ? 'border-blue-500 bg-blue-50'
                          : 'border-gray-200 hover:bg-gray-50'
                      }`}
                    >
                      <input
                        type="radio"
                        name="appealType"
                        value={type}
                        checked={formData.type === type}
                        onChange={(e) => setFormData({ ...formData, type: parseInt(e.target.value) as AppealType })}
                        className="sr-only"
                      />
                      <div className="flex items-center">
                        {getAppealTypeIcon(type as AppealType)}
                        <span className="ml-3 font-medium text-gray-900">
                          {getAppealTypeName(type as AppealType)}
                        </span>
                      </div>
                    </label>
                  ))}
              </div>
            </div>

            {/* Reason */}
            <div>
              <label htmlFor="reason" className="block text-sm font-medium text-gray-700 mb-2">
                Reason for Appeal *
              </label>
              <textarea
                id="reason"
                required
                rows={4}
                value={formData.reason}
                onChange={(e) => setFormData({ ...formData, reason: e.target.value })}
                placeholder="Please explain why you believe the moderation action was incorrect..."
                className="w-full border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
              <p className="text-sm text-gray-500 mt-1">
                Be specific about what happened and why you think it was a mistake.
              </p>
            </div>

            {/* Additional Information */}
            <div>
              <label htmlFor="additionalInfo" className="block text-sm font-medium text-gray-700 mb-2">
                Additional Information
              </label>
              <textarea
                id="additionalInfo"
                rows={3}
                value={formData.additionalInfo}
                onChange={(e) => setFormData({ ...formData, additionalInfo: e.target.value })}
                placeholder="Any additional context or information that might help with your appeal..."
                className="w-full border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              />
            </div>

            {/* Content ID Fields */}
            {(formData.type === AppealType.ContentRemoval || formData.type === AppealType.SystemTag) && (
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label htmlFor="targetPostId" className="block text-sm font-medium text-gray-700 mb-2">
                    Post ID (if applicable)
                  </label>
                  <input
                    type="number"
                    id="targetPostId"
                    value={formData.targetPostId || ''}
                    onChange={(e) => setFormData({ ...formData, targetPostId: e.target.value ? parseInt(e.target.value) : undefined })}
                    placeholder="Enter post ID"
                    className="w-full border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  />
                </div>
                <div>
                  <label htmlFor="targetCommentId" className="block text-sm font-medium text-gray-700 mb-2">
                    Comment ID (if applicable)
                  </label>
                  <input
                    type="number"
                    id="targetCommentId"
                    value={formData.targetCommentId || ''}
                    onChange={(e) => setFormData({ ...formData, targetCommentId: e.target.value ? parseInt(e.target.value) : undefined })}
                    placeholder="Enter comment ID"
                    className="w-full border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                  />
                </div>
              </div>
            )}

            {/* Guidelines */}
            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
              <h3 className="font-medium text-blue-900 mb-2">Appeal Guidelines</h3>
              <ul className="text-sm text-blue-800 space-y-1">
                <li>• Be respectful and professional in your appeal</li>
                <li>• Provide specific details about the situation</li>
                <li>• Include any relevant context or evidence</li>
                <li>• Appeals are typically reviewed within 24-48 hours</li>
                <li>• Submitting false or misleading appeals may result in additional penalties</li>
              </ul>
            </div>

            {/* Submit Button */}
            <div className="flex justify-end space-x-3">
              <Link
                href="/"
                className="px-4 py-2 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50 transition-colors"
              >
                Cancel
              </Link>
              <button
                type="submit"
                disabled={loading || !formData.reason.trim()}
                className="px-6 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {loading ? 'Submitting...' : 'Submit Appeal'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
