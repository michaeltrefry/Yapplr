'use client';

import React, { useState, useEffect } from 'react';
import { useParams, useRouter } from 'next/navigation';
import { ContentPage, ContentPageVersion, CreateContentPageVersionDto } from '@/types';
import { adminApi } from '@/lib/api';
import { ArrowLeft, Save, Eye, EyeOff, CheckCircle, Clock } from 'lucide-react';
import Link from 'next/link';

export default function ContentEditorPage() {
  const params = useParams();
  const router = useRouter();
  const contentPageId = parseInt(params.id as string);

  const [contentPage, setContentPage] = useState<ContentPage | null>(null);
  const [content, setContent] = useState('');
  const [changeNotes, setChangeNotes] = useState('');
  const [showPreview, setShowPreview] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    fetchContentPage();
  }, [contentPageId]);

  const fetchContentPage = async () => {
    try {
      const page = await adminApi.getContentPage(contentPageId);
      setContentPage(page);

      // Load the published version content if available
      if (page.publishedVersion) {
        setContent(page.publishedVersion.content);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    if (!content.trim()) {
      setError('Content cannot be empty');
      return;
    }

    setSaving(true);
    setError(null);
    setSuccess(null);

    try {
      const createDto: CreateContentPageVersionDto = {
        content: content.trim(),
        changeNotes: changeNotes.trim() || undefined,
      };

      const newVersion = await adminApi.createContentPageVersion(contentPageId, createDto);
      setSuccess(`Version ${newVersion.versionNumber} saved successfully`);
      setChangeNotes('');

      // Refresh the content page data
      await fetchContentPage();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setSaving(false);
    }
  };

  const renderMarkdownPreview = (markdown: string) => {
    // Simple markdown rendering for preview
    // In a real app, you'd use a proper markdown parser like react-markdown
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
            href="/admin/content"
            className="inline-flex items-center text-gray-600 hover:text-gray-900"
          >
            <ArrowLeft className="h-4 w-4 mr-1" />
            Back
          </Link>
          <div>
            <h1 className="text-2xl font-bold text-gray-900">{contentPage.title}</h1>
            <div className="flex items-center space-x-4 text-sm text-gray-500">
              <span>Slug: /{contentPage.slug}</span>
              <span>Versions: {contentPage.totalVersions}</span>
              {contentPage.publishedVersion && (
                <div className="flex items-center space-x-1">
                  <CheckCircle className="h-4 w-4 text-green-600" />
                  <span>Published: v{contentPage.publishedVersion.versionNumber}</span>
                </div>
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
          <Link
            href={`/admin/content/${contentPageId}/versions`}
            className="inline-flex items-center px-3 py-2 border border-gray-300 shadow-sm text-sm leading-4 font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
          >
            <Clock className="h-4 w-4 mr-1" />
            Version History
          </Link>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-md p-4">
          <div className="text-sm text-red-700">{error}</div>
        </div>
      )}

      {success && (
        <div className="bg-green-50 border border-green-200 rounded-md p-4">
          <div className="text-sm text-green-700">{success}</div>
        </div>
      )}

      <div className="bg-white shadow rounded-lg">
        <div className="px-4 py-5 sm:p-6">
          <div className="space-y-4">
            <div>
              <label htmlFor="changeNotes" className="block text-sm font-medium text-gray-700">
                Change Notes (Optional)
              </label>
              <input
                type="text"
                id="changeNotes"
                value={changeNotes}
                onChange={(e) => setChangeNotes(e.target.value)}
                placeholder="Describe what you changed in this version..."
                className="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
              />
            </div>

            <div className={`grid ${showPreview ? 'grid-cols-2' : 'grid-cols-1'} gap-4`}>
              <div>
                <label htmlFor="content" className="block text-sm font-medium text-gray-700">
                  Content (Markdown)
                </label>
                <textarea
                  id="content"
                  value={content}
                  onChange={(e) => setContent(e.target.value)}
                  rows={20}
                  className="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm font-mono"
                  placeholder="Enter your content in Markdown format..."
                />
              </div>

              {showPreview && (
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    Preview
                  </label>
                  <div className="mt-1 border border-gray-300 rounded-md p-4 bg-gray-50 h-96 overflow-y-auto">
                    <div className="prose prose-sm max-w-none">
                      {content ? renderMarkdownPreview(content) : (
                        <p className="text-gray-500 italic">Preview will appear here...</p>
                      )}
                    </div>
                  </div>
                </div>
              )}
            </div>

            <div className="flex justify-end">
              <button
                onClick={handleSave}
                disabled={saving || !content.trim()}
                className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {saving ? (
                  <>
                    <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                    Saving...
                  </>
                ) : (
                  <>
                    <Save className="h-4 w-4 mr-2" />
                    Save New Version
                  </>
                )}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
