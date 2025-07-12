'use client';

import React, { useState, useEffect } from 'react';
import { useParams } from 'next/navigation';
import { ContentPage, ContentPageVersion, PublishContentVersionDto } from '@/types';
import { adminApi } from '@/lib/api';
import { ArrowLeft, CheckCircle, Clock, Eye, Globe, AlertCircle } from 'lucide-react';
import Link from 'next/link';

export default function VersionHistoryPage() {
  const params = useParams();
  const contentPageId = parseInt(params?.id as string);

  const [contentPage, setContentPage] = useState<ContentPage | null>(null);
  const [versions, setVersions] = useState<ContentPageVersion[]>([]);
  const [loading, setLoading] = useState(true);
  const [publishing, setPublishing] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    fetchData();
  }, [contentPageId]);

  const fetchData = async () => {
    try {
      const [page, versionsList] = await Promise.all([
        adminApi.getContentPage(contentPageId),
        adminApi.getContentPageVersions(contentPageId)
      ]);

      setContentPage(page);
      setVersions(versionsList);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  const handlePublish = async (versionId: number) => {
    setPublishing(versionId);
    setError(null);
    setSuccess(null);

    try {
      await adminApi.publishContentPageVersion(contentPageId, versionId);
      setSuccess('Version published successfully');
      await fetchData(); // Refresh data
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setPublishing(null);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  const truncateContent = (content: string, maxLength: number = 100) => {
    if (content.length <= maxLength) return content;
    return content.substring(0, maxLength) + '...';
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (!contentPage) {
    return (
      <div className="text-center py-12">
        <h3 className="text-lg font-medium text-gray-900">Content page not found</h3>
        <Link
          href="/admin/content"
          className="mt-2 inline-flex items-center text-blue-600 hover:text-blue-500"
        >
          <ArrowLeft className="h-4 w-4 mr-1" />
          Back to Content Management
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <Link
            href={`/admin/content/${contentPageId}`}
            className="inline-flex items-center text-gray-600 hover:text-gray-900"
          >
            <ArrowLeft className="h-4 w-4 mr-1" />
            Back to Editor
          </Link>
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Version History</h1>
            <p className="text-sm text-gray-600">{contentPage.title}</p>
          </div>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-md p-4">
          <div className="flex">
            <AlertCircle className="h-5 w-5 text-red-400" />
            <div className="ml-3 text-sm text-red-700">{error}</div>
          </div>
        </div>
      )}

      {success && (
        <div className="bg-green-50 border border-green-200 rounded-md p-4">
          <div className="flex">
            <CheckCircle className="h-5 w-5 text-green-400" />
            <div className="ml-3 text-sm text-green-700">{success}</div>
          </div>
        </div>
      )}

      <div className="bg-white shadow rounded-lg">
        <div className="px-4 py-5 sm:p-6">
          {versions.length === 0 ? (
            <div className="text-center py-12">
              <Clock className="mx-auto h-12 w-12 text-gray-400" />
              <h3 className="mt-2 text-sm font-medium text-gray-900">No versions yet</h3>
              <p className="mt-1 text-sm text-gray-500">
                Create your first version by editing the content.
              </p>
              <Link
                href={`/admin/content/${contentPageId}`}
                className="mt-4 inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700"
              >
                Start Editing
              </Link>
            </div>
          ) : (
            <div className="space-y-4">
              {versions.map((version) => (
                <div
                  key={version.id}
                  className={`border rounded-lg p-4 ${
                    version.isPublished
                      ? 'border-green-200 bg-green-50'
                      : 'border-gray-200'
                  }`}
                >
                  <div className="flex items-start justify-between">
                    <div className="flex-1">
                      <div className="flex items-center space-x-3">
                        <h3 className="text-lg font-medium text-gray-900">
                          Version {version.versionNumber}
                        </h3>
                        {version.isPublished && (
                          <div className="flex items-center space-x-1">
                            <Globe className="h-4 w-4 text-green-600" />
                            <span className="text-sm font-medium text-green-600">
                              Published
                            </span>
                          </div>
                        )}
                      </div>
                      
                      <div className="mt-2 space-y-1">
                        <div className="text-sm text-gray-600">
                          <strong>Created:</strong> {formatDate(version.createdAt)} by {version.createdByUsername}
                        </div>
                        {version.isPublished && version.publishedAt && (
                          <div className="text-sm text-gray-600">
                            <strong>Published:</strong> {formatDate(version.publishedAt)} by {version.publishedByUsername}
                          </div>
                        )}
                        {version.changeNotes && (
                          <div className="text-sm text-gray-600">
                            <strong>Notes:</strong> {version.changeNotes}
                          </div>
                        )}
                      </div>

                      <div className="mt-3 p-3 bg-gray-50 rounded border text-sm">
                        <div className="font-medium text-gray-700 mb-1">Content Preview:</div>
                        <div className="text-gray-600 font-mono whitespace-pre-wrap">
                          {truncateContent(version.content, 200)}
                        </div>
                      </div>
                    </div>

                    <div className="ml-4 flex flex-col space-y-2">
                      <Link
                        href={`/admin/content/${contentPageId}/versions/${version.id}`}
                        className="inline-flex items-center px-3 py-2 border border-gray-300 shadow-sm text-sm leading-4 font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
                      >
                        <Eye className="h-4 w-4 mr-1" />
                        View
                      </Link>
                      
                      {!version.isPublished && (
                        <button
                          onClick={() => handlePublish(version.id)}
                          disabled={publishing === version.id}
                          className="inline-flex items-center px-3 py-2 border border-transparent shadow-sm text-sm leading-4 font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                          {publishing === version.id ? (
                            <>
                              <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-1"></div>
                              Publishing...
                            </>
                          ) : (
                            <>
                              <Globe className="h-4 w-4 mr-1" />
                              Publish
                            </>
                          )}
                        </button>
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
