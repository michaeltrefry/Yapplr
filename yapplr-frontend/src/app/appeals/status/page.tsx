'use client';

import { useEffect, useState } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { adminApi } from '@/lib/api';
import { UserAppeal, AppealStatus, AppealType } from '@/types';
import { useRouter } from 'next/navigation';
import {
  Scale,
  ArrowLeft,
  Clock,
  Check,
  X,
  AlertTriangle,
  FileText,
  Tag,
  HelpCircle,
  Calendar,
  MessageSquare,
  Plus,
} from 'lucide-react';
import Link from 'next/link';

export default function AppealsStatusPage() {
  const { user } = useAuth();
  const router = useRouter();
  const [appeals, setAppeals] = useState<UserAppeal[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!user) {
      router.push('/login');
      return;
    }
    fetchUserAppeals();
  }, [user, router]);

  const fetchUserAppeals = async () => {
    if (!user) return;
    
    try {
      setLoading(true);
      // Get appeals for the current user
      const userAppeals = await adminApi.getAppeals(1, 50, undefined, undefined, user.id);
      setAppeals(userAppeals);
    } catch (error) {
      console.error('Failed to fetch user appeals:', error);
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

  const getStatusBadge = (status: AppealStatus) => {
    switch (status) {
      case AppealStatus.Pending:
        return (
          <span className="inline-flex items-center bg-yellow-100 text-yellow-800 px-3 py-1 rounded-full text-sm">
            <Clock className="h-4 w-4 mr-1" />
            Pending Review
          </span>
        );
      case AppealStatus.Approved:
        return (
          <span className="inline-flex items-center bg-green-100 text-green-800 px-3 py-1 rounded-full text-sm">
            <Check className="h-4 w-4 mr-1" />
            Approved
          </span>
        );
      case AppealStatus.Denied:
        return (
          <span className="inline-flex items-center bg-red-100 text-red-800 px-3 py-1 rounded-full text-sm">
            <X className="h-4 w-4 mr-1" />
            Denied
          </span>
        );
      default:
        return null;
    }
  };

  const getStatusMessage = (status: AppealStatus) => {
    switch (status) {
      case AppealStatus.Pending:
        return 'Your appeal is being reviewed by our moderation team. You will receive a notification when a decision is made.';
      case AppealStatus.Approved:
        return 'Your appeal has been approved. The moderation action has been reversed.';
      case AppealStatus.Denied:
        return 'Your appeal has been reviewed and denied. The original moderation action remains in place.';
      default:
        return '';
    }
  };

  if (!user) {
    return null;
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mb-8">
          <Link
            href="/"
            className="inline-flex items-center text-blue-600 hover:text-blue-800 mb-4"
          >
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Home
          </Link>
          <div className="flex items-center justify-between">
            <div className="flex items-center mb-4">
              <Scale className="h-8 w-8 text-blue-600 mr-3" />
              <h1 className="text-3xl font-bold text-gray-900">My Appeals</h1>
            </div>
            <Link
              href="/appeals"
              className="inline-flex items-center px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
            >
              <Plus className="h-4 w-4 mr-2" />
              Submit New Appeal
            </Link>
          </div>
          <p className="text-gray-600">
            Track the status of your submitted appeals and view responses from our moderation team.
          </p>
        </div>

        {loading ? (
          <div className="flex items-center justify-center h-64">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
          </div>
        ) : appeals.length === 0 ? (
          <div className="bg-white rounded-lg shadow p-8 text-center">
            <Scale className="h-12 w-12 text-gray-400 mx-auto mb-4" />
            <h2 className="text-xl font-semibold text-gray-900 mb-2">No Appeals Submitted</h2>
            <p className="text-gray-600 mb-6">
              You haven't submitted any appeals yet. If you believe a moderation action was taken in error,
              you can submit an appeal for review.
            </p>
            <Link
              href="/appeals"
              className="inline-flex items-center px-6 py-3 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
            >
              <Plus className="h-4 w-4 mr-2" />
              Submit an Appeal
            </Link>
          </div>
        ) : (
          <div className="space-y-6">
            {appeals.map((appeal) => (
              <div key={appeal.id} className="bg-white rounded-lg shadow overflow-hidden">
                <div className="p-6">
                  <div className="flex justify-between items-start mb-4">
                    <div className="flex items-center space-x-3">
                      {getAppealTypeIcon(appeal.type)}
                      <div>
                        <h3 className="text-lg font-semibold text-gray-900">
                          {getAppealTypeName(appeal.type)} Appeal
                        </h3>
                        <p className="text-sm text-gray-500 flex items-center">
                          <Calendar className="h-4 w-4 mr-1" />
                          Submitted {new Date(appeal.createdAt).toLocaleDateString()}
                          {appeal.reviewedAt && (
                            <>
                              <span className="mx-2">•</span>
                              Reviewed {new Date(appeal.reviewedAt).toLocaleDateString()}
                            </>
                          )}
                        </p>
                      </div>
                    </div>
                    {getStatusBadge(appeal.status)}
                  </div>

                  <div className="mb-4">
                    <p className="text-sm text-gray-600 mb-2">
                      {getStatusMessage(appeal.status)}
                    </p>
                  </div>

                  <div className="border-t border-gray-200 pt-4">
                    <div className="mb-4">
                      <h4 className="font-medium text-gray-900 mb-2">Your Appeal Reason:</h4>
                      <p className="text-gray-700 bg-gray-50 p-3 rounded">{appeal.reason}</p>
                    </div>

                    {appeal.additionalInfo && (
                      <div className="mb-4">
                        <h4 className="font-medium text-gray-900 mb-2">Additional Information:</h4>
                        <p className="text-gray-700 bg-gray-50 p-3 rounded">{appeal.additionalInfo}</p>
                      </div>
                    )}

                    {appeal.reviewNotes && (
                      <div className="mb-4">
                        <h4 className="font-medium text-gray-900 mb-2 flex items-center">
                          <MessageSquare className="h-4 w-4 mr-1" />
                          Moderator Response:
                        </h4>
                        <div className={`p-3 rounded border-l-4 ${
                          appeal.status === AppealStatus.Approved 
                            ? 'bg-green-50 border-green-400' 
                            : appeal.status === AppealStatus.Denied
                            ? 'bg-red-50 border-red-400'
                            : 'bg-blue-50 border-blue-400'
                        }`}>
                          <p className="text-gray-700">{appeal.reviewNotes}</p>
                        </div>
                      </div>
                    )}

                    {(appeal.targetPostId || appeal.targetCommentId) && (
                      <div className="text-sm text-gray-500">
                        <strong>Related Content:</strong>
                        {appeal.targetPostId && <span> Post #{appeal.targetPostId}</span>}
                        {appeal.targetCommentId && <span> Comment #{appeal.targetCommentId}</span>}
                      </div>
                    )}
                  </div>
                </div>

                {appeal.status === AppealStatus.Pending && (
                  <div className="bg-yellow-50 border-t border-yellow-200 px-6 py-3">
                    <div className="flex items-center">
                      <Clock className="h-5 w-5 text-yellow-600 mr-2" />
                      <p className="text-sm text-yellow-800">
                        <strong>Status:</strong> Your appeal is in the review queue. 
                        Appeals are typically reviewed within 24-48 hours.
                      </p>
                    </div>
                  </div>
                )}

                {appeal.status === AppealStatus.Approved && (
                  <div className="bg-green-50 border-t border-green-200 px-6 py-3">
                    <div className="flex items-center">
                      <Check className="h-5 w-5 text-green-600 mr-2" />
                      <p className="text-sm text-green-800">
                        <strong>Good news!</strong> Your appeal was approved and the moderation action has been reversed.
                      </p>
                    </div>
                  </div>
                )}

                {appeal.status === AppealStatus.Denied && (
                  <div className="bg-red-50 border-t border-red-200 px-6 py-3">
                    <div className="flex items-center">
                      <X className="h-5 w-5 text-red-600 mr-2" />
                      <p className="text-sm text-red-800">
                        <strong>Appeal Denied:</strong> After review, the original moderation action will remain in place.
                        Please review our community guidelines to avoid future violations.
                      </p>
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}

        {/* Help Section */}
        <div className="mt-8 bg-blue-50 border border-blue-200 rounded-lg p-6">
          <h3 className="font-medium text-blue-900 mb-2">Need Help?</h3>
          <p className="text-sm text-blue-800 mb-3">
            If you have questions about the appeals process or need additional assistance:
          </p>
          <ul className="text-sm text-blue-800 space-y-1">
            <li>• Review our community guidelines to understand our policies</li>
            <li>• Appeals are reviewed in the order they are received</li>
            <li>• Submitting multiple appeals for the same issue may delay the review process</li>
            <li>• Be respectful and provide detailed information to help us understand your situation</li>
          </ul>
        </div>
      </div>
    </div>
  );
}
