'use client';

import React, { useState } from 'react';
import { PostModerationInfo, AppealStatus } from '@/types';
import { AlertTriangle, Eye, EyeOff, Flag, MessageSquare, Shield, Tag, Clock, User, CheckCircle, XCircle, AlertCircle } from 'lucide-react';
import { formatDate } from '@/lib/utils';
import AppealModal from './AppealModal';

interface HiddenPostBannerProps {
  moderationInfo: PostModerationInfo;
  postId: number;
  className?: string;
  onAppealSubmitted?: () => void;
}

export default function HiddenPostBanner({ moderationInfo, postId, className = '', onAppealSubmitted }: HiddenPostBannerProps) {
  const [showDetails, setShowDetails] = useState(false);
  const [showAppealModal, setShowAppealModal] = useState(false);

  const getRiskLevelColor = (riskLevel?: string) => {
    switch (riskLevel?.toLowerCase()) {
      case 'low':
        return 'text-yellow-600 bg-yellow-50 border-yellow-200';
      case 'medium':
        return 'text-orange-600 bg-orange-50 border-orange-200';
      case 'high':
        return 'text-red-600 bg-red-50 border-red-200';
      case 'critical':
        return 'text-red-800 bg-red-100 border-red-300';
      default:
        return 'text-gray-600 bg-gray-50 border-gray-200';
    }
  };

  const getCategoryIcon = (category: string) => {
    switch (category.toLowerCase()) {
      case 'contentwarning':
        return AlertTriangle;
      case 'violation':
        return Flag;
      case 'safety':
        return Shield;
      case 'quality':
        return Tag;
      default:
        return Tag;
    }
  };

  const getCategoryColor = (category: string) => {
    switch (category.toLowerCase()) {
      case 'contentwarning':
        return 'text-yellow-700 bg-yellow-100';
      case 'violation':
        return 'text-red-700 bg-red-100';
      case 'safety':
        return 'text-red-700 bg-red-100';
      case 'quality':
        return 'text-orange-700 bg-orange-100';
      case 'moderationstatus':
        return 'text-blue-700 bg-blue-100';
      case 'legal':
        return 'text-purple-700 bg-purple-100';
      default:
        return 'text-gray-700 bg-gray-100';
    }
  };

  const getAppealStatusIcon = (status: AppealStatus) => {
    switch (status) {
      case AppealStatus.Pending:
        return <Clock className="h-4 w-4 text-yellow-600" />;
      case AppealStatus.UnderReview:
        return <AlertCircle className="h-4 w-4 text-blue-600" />;
      case AppealStatus.Approved:
        return <CheckCircle className="h-4 w-4 text-green-600" />;
      case AppealStatus.Denied:
        return <XCircle className="h-4 w-4 text-red-600" />;
      case AppealStatus.Escalated:
        return <AlertTriangle className="h-4 w-4 text-orange-600" />;
      default:
        return <Clock className="h-4 w-4 text-gray-600" />;
    }
  };

  const getAppealStatusColor = (status: AppealStatus) => {
    switch (status) {
      case AppealStatus.Pending:
        return 'text-yellow-800 bg-yellow-100 border-yellow-200';
      case AppealStatus.UnderReview:
        return 'text-blue-800 bg-blue-100 border-blue-200';
      case AppealStatus.Approved:
        return 'text-green-800 bg-green-100 border-green-200';
      case AppealStatus.Denied:
        return 'text-red-800 bg-red-100 border-red-200';
      case AppealStatus.Escalated:
        return 'text-orange-800 bg-orange-100 border-orange-200';
      default:
        return 'text-gray-800 bg-gray-100 border-gray-200';
    }
  };

  const getAppealStatusText = (status: AppealStatus) => {
    switch (status) {
      case AppealStatus.Pending:
        return 'Pending Review';
      case AppealStatus.UnderReview:
        return 'Under Review';
      case AppealStatus.Approved:
        return 'Approved';
      case AppealStatus.Denied:
        return 'Denied';
      case AppealStatus.Escalated:
        return 'Escalated';
      default:
        return 'Unknown';
    }
  };

  return (
    <div className={`border-l-4 border-red-500 bg-red-50 p-4 mb-4 ${className}`}>
      <div className="flex items-start">
        <div className="flex-shrink-0">
          <AlertTriangle className="h-5 w-5 text-red-400" />
        </div>
        <div className="ml-3 flex-1">
          <h3 className="text-sm font-medium text-red-800">
            Content Hidden Due to Community Guidelines Violation
          </h3>
          <div className="mt-2 text-sm text-red-700">
            <p>
              This post has been hidden because it violates our community guidelines.
              {moderationInfo.hiddenReason && (
                <span className="block mt-1 font-medium">
                  Reason: {moderationInfo.hiddenReason}
                </span>
              )}
            </p>
          </div>

          {/* Toggle Details Button */}
          <div className="mt-3">
            <button
              onClick={() => setShowDetails(!showDetails)}
              className="inline-flex items-center text-sm text-red-600 hover:text-red-500 font-medium"
            >
              {showDetails ? (
                <>
                  <EyeOff className="h-4 w-4 mr-1" />
                  Hide Details
                </>
              ) : (
                <>
                  <Eye className="h-4 w-4 mr-1" />
                  Show Details
                </>
              )}
            </button>
          </div>

          {/* Detailed Information */}
          {showDetails && (
            <div className="mt-4 space-y-4">
              {/* Moderation Details */}
              <div className="bg-white rounded-lg p-3 border border-red-200">
                <h4 className="text-sm font-medium text-gray-900 mb-2 flex items-center">
                  <Shield className="h-4 w-4 mr-1" />
                  Moderation Details
                </h4>
                <div className="space-y-2 text-sm text-gray-600">
                  {moderationInfo.hiddenAt && (
                    <div className="flex items-center">
                      <Clock className="h-4 w-4 mr-2 text-gray-400" />
                      <span>Hidden on {formatDate(moderationInfo.hiddenAt)}</span>
                    </div>
                  )}
                  {moderationInfo.hiddenByUser && (
                    <div className="flex items-center">
                      <User className="h-4 w-4 mr-2 text-gray-400" />
                      <span>Hidden by @{moderationInfo.hiddenByUser.username}</span>
                    </div>
                  )}
                  {moderationInfo.riskScore !== undefined && (
                    <div className="flex items-center">
                      <AlertTriangle className="h-4 w-4 mr-2 text-gray-400" />
                      <span>Risk Score: {(moderationInfo.riskScore * 100).toFixed(1)}%</span>
                      {moderationInfo.riskLevel && (
                        <span className={`ml-2 px-2 py-1 rounded-full text-xs font-medium ${getRiskLevelColor(moderationInfo.riskLevel)}`}>
                          {moderationInfo.riskLevel}
                        </span>
                      )}
                    </div>
                  )}
                </div>
              </div>

              {/* System Flags */}
              {moderationInfo.systemTags.length > 0 && (
                <div className="bg-white rounded-lg p-3 border border-red-200">
                  <h4 className="text-sm font-medium text-gray-900 mb-2 flex items-center">
                    <Flag className="h-4 w-4 mr-1" />
                    System Flags ({moderationInfo.systemTags.length})
                  </h4>
                  <div className="space-y-2">
                    {moderationInfo.systemTags.map((tag) => {
                      const IconComponent = getCategoryIcon(tag.category);
                      return (
                        <div key={tag.id} className="flex items-start space-x-2">
                          <div className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${getCategoryColor(tag.category)}`}>
                            <IconComponent className="h-3 w-3 mr-1" />
                            {tag.name}
                          </div>
                          <div className="flex-1 text-xs text-gray-600">
                            <div>{tag.description}</div>
                            {tag.reason && (
                              <div className="mt-1 text-gray-500">Reason: {tag.reason}</div>
                            )}
                            <div className="mt-1 text-gray-400">
                              Applied on {formatDate(tag.appliedAt)} by @{tag.appliedByUser.username}
                            </div>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                </div>
              )}

              {/* Appeal Status or Submit Appeal */}
              {moderationInfo.appealInfo ? (
                <div className={`rounded-lg p-3 border ${getAppealStatusColor(moderationInfo.appealInfo.status)}`}>
                  <h4 className="text-sm font-medium mb-2 flex items-center">
                    {getAppealStatusIcon(moderationInfo.appealInfo.status)}
                    <span className="ml-2">Appeal Status: {getAppealStatusText(moderationInfo.appealInfo.status)}</span>
                  </h4>
                  <div className="space-y-2 text-sm">
                    <div>
                      <span className="font-medium">Submitted:</span> {formatDate(moderationInfo.appealInfo.createdAt)}
                    </div>
                    {moderationInfo.appealInfo.reviewedAt && (
                      <div>
                        <span className="font-medium">Reviewed:</span> {formatDate(moderationInfo.appealInfo.reviewedAt)}
                        {moderationInfo.appealInfo.reviewedByUsername && (
                          <span className="ml-1">by @{moderationInfo.appealInfo.reviewedByUsername}</span>
                        )}
                      </div>
                    )}
                    {moderationInfo.appealInfo.status === AppealStatus.Pending && (
                      <p className="text-sm opacity-75">
                        Your appeal is being reviewed by our moderation team. You will receive a notification when a decision is made.
                      </p>
                    )}
                    {moderationInfo.appealInfo.status === AppealStatus.UnderReview && (
                      <p className="text-sm opacity-75">
                        Your appeal is currently under review by our moderation team.
                      </p>
                    )}
                    {moderationInfo.appealInfo.status === AppealStatus.Approved && (
                      <p className="text-sm opacity-75">
                        Your appeal has been approved. The moderation action should be reversed shortly.
                      </p>
                    )}
                    {moderationInfo.appealInfo.status === AppealStatus.Denied && (
                      <>
                        <p className="text-sm opacity-75">
                          Your appeal has been reviewed and denied. The original moderation action remains in place.
                        </p>
                        {moderationInfo.appealInfo.reviewNotes && (
                          <div className="mt-2 p-2 bg-white bg-opacity-50 rounded text-sm">
                            <span className="font-medium">Review Notes:</span> {moderationInfo.appealInfo.reviewNotes}
                          </div>
                        )}
                      </>
                    )}
                  </div>
                </div>
              ) : (
                <div className="bg-blue-50 rounded-lg p-3 border border-blue-200">
                  <h4 className="text-sm font-medium text-blue-900 mb-2 flex items-center">
                    <MessageSquare className="h-4 w-4 mr-1" />
                    Disagree with this decision?
                  </h4>
                  <p className="text-sm text-blue-700 mb-3">
                    If you believe this content was hidden in error, you can submit an appeal for review.
                  </p>
                  <button
                    onClick={() => setShowAppealModal(true)}
                    className="inline-flex items-center px-3 py-2 border border-blue-300 shadow-sm text-sm leading-4 font-medium rounded-md text-blue-700 bg-white hover:bg-blue-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                  >
                    Submit Appeal
                  </button>
                </div>
              )}
            </div>
          )}
        </div>
      </div>

      {/* Appeal Modal */}
      <AppealModal
        isOpen={showAppealModal}
        onClose={() => setShowAppealModal(false)}
        postId={postId}
        contentType="post"
        hiddenReason={moderationInfo.hiddenReason}
        onSuccess={() => {
          onAppealSubmitted?.();
          setShowAppealModal(false);
        }}
      />
    </div>
  );
}
