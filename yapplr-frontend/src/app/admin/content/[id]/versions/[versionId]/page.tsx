'use client';

import React, { useState, useEffect } from 'react';
import { useParams } from 'next/navigation';
import { ContentPageVersion, PublishContentVersionDto } from '@/types';
import { adminApi } from '@/lib/api';
import { ArrowLeft, Globe, CheckCircle, AlertCircle, Eye, EyeOff } from 'lucide-react';
import Link from 'next/link';

export default function VersionDetailPage() {
  const params = useParams();
  const contentPageId = parseInt(params.id as string);
  const versionId = parseInt(params.versionId as string);

  const [version, setVersion] = useState<ContentPageVersion | null>(null);
  const [showPreview, setShowPreview] = useState(true);
  const [loading, setLoading] = useState(true);
  const [publishing, setPublishing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    fetchVersion();
  }, [versionId]);

  const fetchVersion = async () => {
    try {
      const versionData = await adminApi.getContentPageVersion(versionId);
      setVersion(versionData);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  const handlePublish = async () => {
    if (!version) return;

    setPublishing(true);
    setError(null);
    setSuccess(null);

    try {
      await adminApi.publishContentPageVersion(contentPageId, version.id);
      setSuccess('Version published successfully');
      await fetchVersion(); // Refresh data
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setPublishing(false);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  const renderMarkdownPreview = (markdown: string) => {
    // Simple markdown rendering for preview
    return markdown
      .split('\n')
      .map((line, index) => {
        if (line.startsWith('# ')) {
          return <h1 key={index} className="text-3xl font-bold mb-4">{line.slice(2)}</h1>;
        }
        if (line.startsWith('## ')) {
          return <h2 key={index} className="text-2xl font-semibold mb-3">{line.slice(3)}</h2>;
        }
        if (line.startsWith('### ')) {
          return <h3 key={index} className="text-xl font-medium mb-2">{line.slice(4)}</h3>;
        }
        if (line.trim() === '') {
          return <br key={index} />;
        }
        return <p key={index} className="mb-2">{line}</p>;
      });
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (!version) {
    return (
      <div className="text-center py-12">
        <h3 className="text-lg font-medium text-gray-900">Version not found</h3>
        <Link
          href={`/admin/content/${contentPageId}/versions`}
          className="mt-2 inline-flex items-center text-blue-600 hover:text-blue-500"
        >
          <ArrowLeft className="h-4 w-4 mr-1" />
          Back to Version History
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <Link
            href={`/admin/content/${contentPageId}/versions`}
            className="inline-flex items-center text-gray-600 hover:text-gray-900"
          >
            <ArrowLeft className="h-4 w-4 mr-1" />
            Back to History
          </Link>
          <div>
            <div className="flex items-center space-x-3">
              <h1 className="text-2xl font-bold text-gray-900">
                Version {version.versionNumber}
              </h1>
              {version.isPublished && (
                <div className="flex items-center space-x-1">
                  <Globe className="h-4 w-4 text-green-600" />
                  <span className="text-sm font-medium text-green-600">Published</span>
                </div>
              )}
            </div>
            <div className="mt-1 space-y-1 text-sm text-gray-600">
              <div>Created: {formatDate(version.createdAt)} by {version.createdByUsername}</div>
              {version.isPublished && version.publishedAt && (
                <div>Published: {formatDate(version.publishedAt)} by {version.publishedByUsername}</div>
              )}
              {version.changeNotes && (
                <div>Notes: {version.changeNotes}</div>
              )}
            </div>
          </div>
        </div>
        <div className="flex items-center space-x-2">
          <button
            onClick={() => setShowPreview(!showPreview)}
            className="inline-flex items-center px-3 py-2 border border-gray-300 shadow-sm text-sm leading-4 font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
          >
            {showPreview ? (
              <>
                <EyeOff className="h-4 w-4 mr-1" />
                Hide Preview
              </>
            ) : (
              <>
                <Eye className="h-4 w-4 mr-1" />
                Show Preview
              </>
            )}
          </button>
          {!version.isPublished && (
            <button
              onClick={handlePublish}
              disabled={publishing}
              className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {publishing ? (
                <>
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                  Publishing...
                </>
              ) : (
                <>
                  <Globe className="h-4 w-4 mr-2" />
                  Publish This Version
                </>
              )}
            </button>
          )}
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
          <div className={`grid ${showPreview ? 'grid-cols-2' : 'grid-cols-1'} gap-6`}>
            <div>
              <h3 className="text-lg font-medium text-gray-900 mb-3">Markdown Source</h3>
              <div className="border border-gray-300 rounded-md p-4 bg-gray-50">
                <pre className="whitespace-pre-wrap text-sm font-mono text-gray-800">
                  {version.content}
                </pre>
              </div>
            </div>

            {showPreview && (
              <div>
                <h3 className="text-lg font-medium text-gray-900 mb-3">Preview</h3>
                <div className="border border-gray-300 rounded-md p-4 bg-white">
                  <div className="prose prose-sm max-w-none">
                    {renderMarkdownPreview(version.content)}
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
