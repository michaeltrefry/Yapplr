'use client';

import { useEffect, useState } from 'react';
import { adminApi } from '@/lib/api';
import { ContentQueue, AdminPost, AdminComment, SystemTag } from '@/types';
import {
  AlertTriangle,
  Eye,
  EyeOff,
  Tag,
  User,
  Calendar,
  MessageSquare,
  FileText,
  ExternalLink,
  Brain,
  Flag,
} from 'lucide-react';
import { AdminQueueList, AdminUserReportList } from '@/components/admin';
import AiSuggestedTags from '@/components/admin/AiSuggestedTags';

export default function ContentQueuePage() {
  const [queue, setQueue] = useState<ContentQueue | null>(null);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<'posts' | 'comments' | 'appeals' | 'reports'>('posts');
  const [systemTags, setSystemTags] = useState<SystemTag[]>([]);
  const [expandedAiSuggestions, setExpandedAiSuggestions] = useState<Set<number>>(new Set());
  const [activeAction, setActiveAction] = useState<{
    type: 'hide';
    contentType: 'post' | 'comment';
    contentId: number;
    reason: string;
    reportId?: number;
  } | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const [queueData, tagsData] = await Promise.all([
          adminApi.getContentQueue(),
          adminApi.getSystemTags(),
        ]);
        setQueue(queueData);
        setSystemTags(tagsData);
      } catch (error) {
        console.error('Failed to fetch queue data:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  const handleShowActionForm = (type: 'hide', contentType: 'post' | 'comment', contentId: number, reportId?: number) => {
    setActiveAction({
      type,
      contentType,
      contentId,
      reason: '',
      reportId
    });
  };

  const handleCancelAction = () => {
    setActiveAction(null);
  };

  const handleSaveAction = async () => {
    if (!activeAction || !activeAction.reason.trim()) return;

    try {
      if (activeAction.contentType === 'post') {
        if (activeAction.type === 'hide') {
          await adminApi.hidePost(activeAction.contentId, { reason: activeAction.reason });
        }
        // Delete functionality removed - only hide is available
      } else {
        if (activeAction.type === 'hide') {
          await adminApi.hideComment(activeAction.contentId, { reason: activeAction.reason });
        }
        // Delete functionality removed - only hide is available
      }

      // If this was from a user report, use the special endpoint that handles both hiding and messaging
      if (activeAction.reportId) {
        try {
          await adminApi.hideContentFromReport(activeAction.reportId, {
            reason: activeAction.reason
          });
        } catch (error) {
          console.error('Failed to hide content from report:', error);
        }
      }

      // Refresh queue
      const queueData = await adminApi.getContentQueue();
      setQueue(queueData);
      setActiveAction(null);
    } catch (error) {
      console.error(`Failed to ${activeAction.type} ${activeAction.contentType}:`, error);
      alert(`Failed to ${activeAction.type} ${activeAction.contentType}`);
    }
  };

  const handleReasonChange = (reason: string) => {
    if (activeAction) {
      setActiveAction({ ...activeAction, reason });
    }
  };

  // Simplified handlers for the new components
  const handleHidePost = async (postId: number, reason: string) => {
    try {
      await adminApi.hidePost(postId, { reason });
      const queueData = await adminApi.getContentQueue();
      setQueue(queueData);
    } catch (error) {
      console.error('Failed to hide post:', error);
    }
  };

  const handleHideComment = async (commentId: number, reason: string) => {
    try {
      await adminApi.hideComment(commentId, { reason });
      const queueData = await adminApi.getContentQueue();
      setQueue(queueData);
    } catch (error) {
      console.error('Failed to hide comment:', error);
    }
  };

  // User Report handlers
  const handleHideContentFromReport = async (contentId: number, contentType: 'post' | 'comment', reason: string) => {
    try {
      if (contentType === 'post') {
        await adminApi.hidePost(contentId, { reason });
      } else {
        await adminApi.hideComment(contentId, { reason });
      }
      const queueData = await adminApi.getContentQueue();
      setQueue(queueData);
    } catch (error) {
      console.error('Failed to hide content from report:', error);
    }
  };

  const handleDismissReport = async (reportId: number, notes: string) => {
    try {
      await adminApi.reviewUserReport(reportId, {
        status: 2, // Dismissed
        reviewNotes: notes
      });
      const queueData = await adminApi.getContentQueue();
      setQueue(queueData);
    } catch (error) {
      console.error('Failed to dismiss report:', error);
    }
  };



  const handleApproveAiSuggestion = async (postId: number, tagId: number) => {
    try {
      await adminApi.approveAiSuggestedTag(tagId);
      // Refresh the queue data
      const queueData = await adminApi.getContentQueue();
      setQueue(queueData);
    } catch (error) {
      console.error('Failed to approve AI suggestion:', error);
      alert('Failed to approve AI suggestion');
    }
  };

  const handleRejectAiSuggestion = async (postId: number, tagId: number) => {
    try {
      await adminApi.rejectAiSuggestedTag(tagId);
      // Refresh the queue data
      const queueData = await adminApi.getContentQueue();
      setQueue(queueData);
    } catch (error) {
      console.error('Failed to reject AI suggestion:', error);
      alert('Failed to reject AI suggestion');
    }
  };

  const handleBulkApproveAiSuggestions = async (postId: number, tagIds: number[]) => {
    try {
      await adminApi.bulkApproveAiSuggestedTags(tagIds);
      // Refresh the queue data
      const queueData = await adminApi.getContentQueue();
      setQueue(queueData);
    } catch (error) {
      console.error('Failed to bulk approve AI suggestions:', error);
      alert('Failed to bulk approve AI suggestions');
    }
  };

  const handleBulkRejectAiSuggestions = async (postId: number, tagIds: number[]) => {
    try {
      await adminApi.bulkRejectAiSuggestedTags(tagIds);
      // Refresh the queue data
      const queueData = await adminApi.getContentQueue();
      setQueue(queueData);
    } catch (error) {
      console.error('Failed to bulk reject AI suggestions:', error);
      alert('Failed to bulk reject AI suggestions');
    }
  };

  const toggleAiSuggestions = (postId: number) => {
    const newExpanded = new Set(expandedAiSuggestions);
    if (newExpanded.has(postId)) {
      newExpanded.delete(postId);
    } else {
      newExpanded.add(postId);
    }
    setExpandedAiSuggestions(newExpanded);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  const tabs = [
    { id: 'posts', name: 'Flagged Posts', count: queue?.flaggedPosts.length || 0, icon: FileText },
    { id: 'comments', name: 'Flagged Comments', count: queue?.flaggedComments.length || 0, icon: MessageSquare },
    { id: 'appeals', name: 'Pending Appeals', count: queue?.pendingAppeals.length || 0, icon: AlertTriangle },
    { id: 'reports', name: 'User Reported', count: queue?.userReports.length || 0, icon: Flag },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Content Moderation Queue</h1>
        <p className="text-gray-600">Review and moderate flagged content</p>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200">
        <nav className="-mb-px flex space-x-8">
          {tabs.map((tab) => {
            const Icon = tab.icon;
            return (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id as any)}
                className={`flex items-center py-2 px-1 border-b-2 font-medium text-sm ${
                  activeTab === tab.id
                    ? 'border-blue-500 text-blue-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                }`}
              >
                <Icon className="h-5 w-5 mr-2" />
                {tab.name}
                {tab.count > 0 && (
                  <span className="ml-2 bg-red-100 text-red-600 py-0.5 px-2 rounded-full text-xs">
                    {tab.count}
                  </span>
                )}
              </button>
            );
          })}
        </nav>
      </div>

      {/* Content */}
      <div className="space-y-4">
        {activeTab === 'posts' && (
          <AdminQueueList
            items={queue?.flaggedPosts || []}
            contentType="post"
            loading={loading}
            onHide={handleHidePost}
            onApproveAiSuggestion={handleApproveAiSuggestion}
            onRejectAiSuggestion={handleRejectAiSuggestion}
            onBulkApproveAiSuggestions={handleBulkApproveAiSuggestions}
            onBulkRejectAiSuggestions={handleBulkRejectAiSuggestions}
            showAiSuggestions={true}
          />
        )}

        {activeTab === 'comments' && (
          <AdminQueueList
            items={queue?.flaggedComments || []}
            contentType="comment"
            loading={loading}
            onHide={handleHideComment}
            showAiSuggestions={false}
          />
        )}

        {activeTab === 'appeals' && (
          <div className="space-y-4">
            {queue?.pendingAppeals.length === 0 ? (
              <div className="text-center py-12">
                <AlertTriangle className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <p className="text-gray-500">No pending appeals to review</p>
              </div>
            ) : (
              queue?.pendingAppeals.map((appeal) => (
                <div key={appeal.id} className="bg-white rounded-lg shadow p-6">
                  <div className="flex justify-between items-start mb-4">
                    <div>
                      <p className="font-medium text-gray-900">@{appeal.username}</p>
                      <p className="text-sm text-gray-500">{appeal.type} Appeal</p>
                    </div>
                    <span className="bg-yellow-100 text-yellow-800 px-2 py-1 rounded-full text-xs">
                      {appeal.status}
                    </span>
                  </div>
                  <div className="mb-4">
                    <p className="text-gray-900">{appeal.reason}</p>
                    {appeal.additionalInfo && (
                      <p className="text-gray-600 mt-2">{appeal.additionalInfo}</p>
                    )}
                  </div>
                  <p className="text-sm text-gray-500">
                    Submitted {new Date(appeal.createdAt).toLocaleDateString()}
                  </p>
                </div>
              ))
            )}
          </div>
        )}

        {activeTab === 'reports' && (
          <AdminUserReportList
            reports={queue?.userReports || []}
            loading={loading}
            onHideContent={handleHideContentFromReport}
            onDismissReport={handleDismissReport}
          />
        )}
      </div>
    </div>
  );
}
