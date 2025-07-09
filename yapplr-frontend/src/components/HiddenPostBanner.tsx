'use client';

import React, { useState } from 'react';
import { PostModerationInfo } from '@/types';
import { AlertTriangle, Eye, EyeOff, Flag, MessageSquare, Shield, Tag, Clock, User } from 'lucide-react';
import { formatDate } from '@/lib/utils';

interface HiddenPostBannerProps {
  moderationInfo: PostModerationInfo;
  className?: string;
}

export default function HiddenPostBanner({ moderationInfo, className = '' }: HiddenPostBannerProps) {
  const [showDetails, setShowDetails] = useState(false);

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

              {/* Appeal Option */}
              <div className="bg-blue-50 rounded-lg p-3 border border-blue-200">
                <h4 className="text-sm font-medium text-blue-900 mb-2 flex items-center">
                  <MessageSquare className="h-4 w-4 mr-1" />
                  Disagree with this decision?
                </h4>
                <p className="text-sm text-blue-700 mb-3">
                  If you believe this content was hidden in error, you can submit an appeal for review.
                </p>
                <button className="inline-flex items-center px-3 py-2 border border-blue-300 shadow-sm text-sm leading-4 font-medium rounded-md text-blue-700 bg-white hover:bg-blue-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">
                  Submit Appeal
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
