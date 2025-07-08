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



  const handleApproveAiSuggestion = async (tagId: number, reason?: string) => {
    try {
      await adminApi.approveAiSuggestedTag(tagId, reason);
      // Refresh the queue data
      const queueData = await adminApi.getContentQueue();
      setQueue(queueData);
    } catch (error) {
      console.error('Failed to approve AI suggestion:', error);
      alert('Failed to approve AI suggestion');
    }
  };

  const handleRejectAiSuggestion = async (tagId: number, reason?: string) => {
    try {
      await adminApi.rejectAiSuggestedTag(tagId, reason);
      // Refresh the queue data
      const queueData = await adminApi.getContentQueue();
      setQueue(queueData);
    } catch (error) {
      console.error('Failed to reject AI suggestion:', error);
      alert('Failed to reject AI suggestion');
    }
  };

  const handleBulkApproveAiSuggestions = async (tagIds: number[], reason?: string) => {
    try {
      await adminApi.bulkApproveAiSuggestedTags(tagIds, reason);
      // Refresh the queue data
      const queueData = await adminApi.getContentQueue();
      setQueue(queueData);
    } catch (error) {
      console.error('Failed to bulk approve AI suggestions:', error);
      alert('Failed to bulk approve AI suggestions');
    }
  };

  const handleBulkRejectAiSuggestions = async (tagIds: number[], reason?: string) => {
    try {
      await adminApi.bulkRejectAiSuggestedTags(tagIds, reason);
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
          <div className="space-y-4">
            {queue?.flaggedPosts.length === 0 ? (
              <div className="text-center py-12">
                <FileText className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <p className="text-gray-500">No flagged posts to review</p>
              </div>
            ) : (
              queue?.flaggedPosts.map((post) => (
                <div key={post.id} className="bg-white rounded-lg shadow p-6">
                  <div className="flex justify-between items-start mb-4">
                    <div className="flex items-center space-x-3">
                      <User className="h-8 w-8 text-gray-400" />
                      <div>
                        <a
                          href={`/profile/${post.user.username}`}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="font-medium text-blue-600 hover:text-blue-800 flex items-center gap-1"
                        >
                          @{post.user.username}
                          <ExternalLink className="h-3 w-3" />
                        </a>
                        <p className="text-sm text-gray-500 flex items-center">
                          <Calendar className="h-4 w-4 mr-1" />
                          {new Date(post.createdAt).toLocaleDateString()}
                        </p>
                      </div>
                    </div>
                    <div className="flex space-x-2">
                      <button
                        onClick={() => handleShowActionForm('hide', 'post', post.id)}
                        className="flex items-center px-3 py-1 bg-yellow-100 text-yellow-800 rounded-md hover:bg-yellow-200 transition-colors"
                      >
                        <EyeOff className="h-4 w-4 mr-1" />
                        Hide
                      </button>

                    </div>
                  </div>

                  <div className="mb-4">
                    <a
                      href={`/yap/${post.id}`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-gray-900 hover:text-blue-600 block group"
                    >
                      <div className="flex items-start gap-2">
                        <span className="flex-1">{post.content}</span>
                        <ExternalLink className="h-4 w-4 text-gray-400 group-hover:text-blue-600 flex-shrink-0 mt-0.5" />
                      </div>
                    </a>
                    {post.imageFileName && (
                      <div className="mt-2">
                        <img
                          src={`${process.env.NEXT_PUBLIC_API_URL}/uploads/${post.imageFileName}`}
                          alt="Post image"
                          className="max-w-md rounded-lg"
                        />
                      </div>
                    )}
                  </div>

                  {post.systemTags.length > 0 && (
                    <div className="flex flex-wrap gap-2 mb-4">
                      {post.systemTags.map((tag) => (
                        <span
                          key={tag.id}
                          className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium"
                          style={{ backgroundColor: `${tag.color}20`, color: tag.color }}
                        >
                          <Tag className="h-3 w-3 mr-1" />
                          {tag.name}
                        </span>
                      ))}
                    </div>
                  )}

                  {/* AI Suggested Tags */}
                  {post.aiSuggestedTags && post.aiSuggestedTags.length > 0 && (
                    <div className="mb-4">
                      <button
                        onClick={() => toggleAiSuggestions(post.id)}
                        className="flex items-center text-sm text-blue-600 hover:text-blue-800 mb-2"
                      >
                        <Brain className="h-4 w-4 mr-1" />
                        AI Suggestions ({post.aiSuggestedTags.filter(tag => !tag.isApproved && !tag.isRejected).length} pending)
                        <span className="ml-1">
                          {expandedAiSuggestions.has(post.id) ? '▼' : '▶'}
                        </span>
                      </button>
                      {expandedAiSuggestions.has(post.id) && (
                        <AiSuggestedTags
                          tags={post.aiSuggestedTags}
                          onApprove={handleApproveAiSuggestion}
                          onReject={handleRejectAiSuggestion}
                          onBulkApprove={handleBulkApproveAiSuggestions}
                          onBulkReject={handleBulkRejectAiSuggestions}
                        />
                      )}
                    </div>
                  )}

                  <div className="flex items-center justify-between text-sm text-gray-500">
                    <div className="flex space-x-4">
                      <span>{post.likeCount} likes</span>
                      <span>{post.commentCount} comments</span>
                      <span>{post.repostCount} reposts</span>
                    </div>
                    {post.isHidden && (
                      <span className="bg-red-100 text-red-800 px-2 py-1 rounded-full text-xs">
                        Hidden
                      </span>
                    )}
                  </div>

                  {/* Inline Action Form */}
                  {activeAction && activeAction.contentType === 'post' && activeAction.contentId === post.id && (
                    <div className="mt-4 p-4 bg-gray-50 rounded-lg border-t">
                      <div className="mb-3">
                        <h4 className="text-sm font-medium text-gray-900 mb-2">
                          Hide Post
                        </h4>
                        <label htmlFor="reason" className="block text-sm text-gray-700 mb-1">
                          Reason for hiding this post:
                        </label>
                        <textarea
                          id="reason"
                          value={activeAction.reason}
                          onChange={(e) => handleReasonChange(e.target.value)}
                          rows={3}
                          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                          placeholder="Enter your reason..."
                        />
                      </div>
                      <div className="flex space-x-2">
                        <button
                          onClick={handleSaveAction}
                          disabled={!activeAction.reason.trim()}
                          className={`px-4 py-2 rounded-md text-sm font-medium ${
                            activeAction.reason.trim()
                              ? 'bg-yellow-600 text-white hover:bg-yellow-700'
                              : 'bg-gray-300 text-gray-500 cursor-not-allowed'
                          } transition-colors`}
                        >
                          Hide Post
                        </button>
                        <button
                          onClick={handleCancelAction}
                          className="px-4 py-2 bg-gray-200 text-gray-700 rounded-md text-sm font-medium hover:bg-gray-300 transition-colors"
                        >
                          Cancel
                        </button>
                      </div>
                    </div>
                  )}
                </div>
              ))
            )}
          </div>
        )}

        {activeTab === 'comments' && (
          <div className="space-y-4">
            {queue?.flaggedComments.length === 0 ? (
              <div className="text-center py-12">
                <MessageSquare className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <p className="text-gray-500">No flagged comments to review</p>
              </div>
            ) : (
              queue?.flaggedComments.map((comment) => (
                <div key={comment.id} className="bg-white rounded-lg shadow p-6">
                  <div className="flex justify-between items-start mb-4">
                    <div className="flex items-center space-x-3">
                      <User className="h-8 w-8 text-gray-400" />
                      <div>
                        <a
                          href={`/profile/${comment.user.username}`}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="font-medium text-blue-600 hover:text-blue-800 flex items-center gap-1"
                        >
                          @{comment.user.username}
                          <ExternalLink className="h-3 w-3" />
                        </a>
                        <p className="text-sm text-gray-500 flex items-center">
                          <Calendar className="h-4 w-4 mr-1" />
                          {new Date(comment.createdAt).toLocaleDateString()}
                        </p>
                      </div>
                    </div>
                    <div className="flex space-x-2">
                      <button
                        onClick={() => handleShowActionForm('hide', 'comment', comment.id)}
                        className="flex items-center px-3 py-1 bg-yellow-100 text-yellow-800 rounded-md hover:bg-yellow-200 transition-colors"
                      >
                        <EyeOff className="h-4 w-4 mr-1" />
                        Hide
                      </button>

                    </div>
                  </div>

                  <div className="mb-4">
                    <a
                      href={`/yap/${comment.postId}#comment-${comment.id}`}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-gray-900 hover:text-blue-600 block group"
                    >
                      <div className="flex items-start gap-2">
                        <span className="flex-1">{comment.content}</span>
                        <ExternalLink className="h-4 w-4 text-gray-400 group-hover:text-blue-600 flex-shrink-0 mt-0.5" />
                      </div>
                    </a>
                    <p className="text-xs text-gray-500 mt-2">
                      Comment on post #{comment.postId}
                    </p>
                  </div>

                  {comment.systemTags.length > 0 && (
                    <div className="flex flex-wrap gap-2 mb-4">
                      {comment.systemTags.map((tag) => (
                        <span
                          key={tag.id}
                          className="inline-flex items-center px-2 py-1 rounded-full text-xs font-medium"
                          style={{ backgroundColor: `${tag.color}20`, color: tag.color }}
                        >
                          <Tag className="h-3 w-3 mr-1" />
                          {tag.name}
                        </span>
                      ))}
                    </div>
                  )}

                  {comment.isHidden && (
                    <span className="bg-red-100 text-red-800 px-2 py-1 rounded-full text-xs">
                      Hidden
                    </span>
                  )}

                  {/* Inline Action Form */}
                  {activeAction && activeAction.contentType === 'comment' && activeAction.contentId === comment.id && (
                    <div className="mt-4 p-4 bg-gray-50 rounded-lg border-t">
                      <div className="mb-3">
                        <h4 className="text-sm font-medium text-gray-900 mb-2">
                          Hide Comment
                        </h4>
                        <label htmlFor="reason" className="block text-sm text-gray-700 mb-1">
                          Reason for hiding this comment:
                        </label>
                        <textarea
                          id="reason"
                          value={activeAction.reason}
                          onChange={(e) => handleReasonChange(e.target.value)}
                          rows={3}
                          className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
                          placeholder="Enter your reason..."
                        />
                      </div>
                      <div className="flex space-x-2">
                        <button
                          onClick={handleSaveAction}
                          disabled={!activeAction.reason.trim()}
                          className={`px-4 py-2 rounded-md text-sm font-medium ${
                            activeAction.reason.trim()
                              ? 'bg-yellow-600 text-white hover:bg-yellow-700'
                              : 'bg-gray-300 text-gray-500 cursor-not-allowed'
                          } transition-colors`}
                        >
                          Hide Comment
                        </button>
                        <button
                          onClick={handleCancelAction}
                          className="px-4 py-2 bg-gray-200 text-gray-700 rounded-md text-sm font-medium hover:bg-gray-300 transition-colors"
                        >
                          Cancel
                        </button>
                      </div>
                    </div>
                  )}
                </div>
              ))
            )}
          </div>
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
          <div className="space-y-4">
            {queue?.userReports.length === 0 ? (
              <div className="text-center py-12">
                <Flag className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                <p className="text-gray-500">No user reports to review</p>
              </div>
            ) : (
              queue?.userReports.map((report) => (
                <div key={report.id} className="bg-white border border-gray-200 rounded-lg p-6">
                  <div className="flex items-start justify-between mb-4">
                    <div className="flex items-center space-x-3">
                      <Flag className="w-5 h-5 text-red-500" />
                      <div>
                        <h3 className="font-medium text-gray-900">
                          Report #{report.id}
                        </h3>
                        <p className="text-sm text-gray-500">
                          Reported by @{report.reportedByUsername} on{' '}
                          {new Date(report.createdAt).toLocaleDateString()}
                        </p>
                      </div>
                    </div>
                    <span className={`px-2 py-1 text-xs font-medium rounded-full ${
                      report.status === 0 ? 'bg-yellow-100 text-yellow-800' :
                      report.status === 1 ? 'bg-blue-100 text-blue-800' :
                      report.status === 2 ? 'bg-gray-100 text-gray-800' :
                      'bg-green-100 text-green-800'
                    }`}>
                      {report.status === 0 ? 'Pending' :
                       report.status === 1 ? 'Reviewed' :
                       report.status === 2 ? 'Dismissed' :
                       'Action Taken'}
                    </span>
                  </div>

                  {/* Reported Content */}
                  <div className="bg-gray-50 p-4 rounded-lg mb-4">
                    <div className="flex items-center justify-between mb-2">
                      <span className="text-sm font-medium text-gray-700">
                        Reported {report.post ? 'Post' : 'Comment'}:
                      </span>
                      {report.post && (
                        <a
                          href={`/post/${report.post.id}`}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-blue-600 hover:text-blue-800 text-sm flex items-center space-x-1"
                        >
                          <span>View Post</span>
                          <ExternalLink className="w-3 h-3" />
                        </a>
                      )}
                    </div>
                    <p className="text-gray-900 mb-2">
                      {report.post?.content || report.comment?.content}
                    </p>
                    <div className="flex items-center space-x-2 text-sm text-gray-500">
                      <User className="w-4 h-4" />
                      <span>
                        @{report.post?.user.username || report.comment?.user.username}
                      </span>
                    </div>
                  </div>

                  {/* Report Details */}
                  <div className="space-y-3">
                    <div>
                      <span className="text-sm font-medium text-gray-700">Reason:</span>
                      <p className="text-gray-900 mt-1">{report.reason}</p>
                    </div>

                    {report.systemTags.length > 0 && (
                      <div>
                        <span className="text-sm font-medium text-gray-700">Selected Categories:</span>
                        <div className="flex flex-wrap gap-2 mt-1">
                          {report.systemTags.map((tag) => (
                            <span
                              key={tag.id}
                              className="px-2 py-1 text-xs font-medium rounded-full"
                              style={{ backgroundColor: tag.color + '20', color: tag.color }}
                            >
                              {tag.name}
                            </span>
                          ))}
                        </div>
                      </div>
                    )}

                    {report.reviewNotes && (
                      <div>
                        <span className="text-sm font-medium text-gray-700">Review Notes:</span>
                        <p className="text-gray-900 mt-1">{report.reviewNotes}</p>
                        <p className="text-sm text-gray-500 mt-1">
                          Reviewed by @{report.reviewedByUsername} on{' '}
                          {report.reviewedAt && new Date(report.reviewedAt).toLocaleDateString()}
                        </p>
                      </div>
                    )}

                    {/* Action Buttons - only show for pending reports */}
                    {report.status === 0 && (
                      <div className="mt-4 pt-4 border-t border-gray-200">
                        <div className="flex space-x-3">
                          {report.post && !report.post.isHidden && (
                            <button
                              onClick={() => handleShowActionForm('hide', 'post', report.post!.id, report.id)}
                              className="inline-flex items-center px-3 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500"
                            >
                              <EyeOff className="w-4 h-4 mr-2" />
                              Hide Post
                            </button>
                          )}
                          {report.comment && !report.comment.isHidden && (
                            <button
                              onClick={() => handleShowActionForm('hide', 'comment', report.comment!.id, report.id)}
                              className="inline-flex items-center px-3 py-2 text-sm font-medium text-white bg-red-600 border border-transparent rounded-md hover:bg-red-700 focus:outline-none focus:ring-2 focus:ring-red-500"
                            >
                              <EyeOff className="w-4 h-4 mr-2" />
                              Hide Comment
                            </button>
                          )}
                          <button
                            onClick={() => {
                              // Mark as reviewed without action
                              adminApi.reviewUserReport(report.id, {
                                status: 2, // Dismissed
                                reviewNotes: 'No action needed'
                              }).then(() => {
                                // Refresh queue
                                adminApi.getContentQueue().then(setQueue);
                              });
                            }}
                            className="inline-flex items-center px-3 py-2 text-sm font-medium text-gray-700 bg-gray-100 border border-gray-300 rounded-md hover:bg-gray-200 focus:outline-none focus:ring-2 focus:ring-gray-500"
                          >
                            Dismiss Report
                          </button>
                        </div>
                      </div>
                    )}
                  </div>
                </div>
              ))
            )}
          </div>
        )}
      </div>
    </div>
  );
}
