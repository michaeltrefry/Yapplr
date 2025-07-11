'use client';

import React, { useState, useEffect } from 'react';
import { ContentPage, ContentPageType } from '@/types';
import { adminApi } from '@/lib/api';
import { Edit, Eye, Clock, CheckCircle, AlertCircle } from 'lucide-react';
import Link from 'next/link';

export default function ContentManagementPage() {
  const [contentPages, setContentPages] = useState<ContentPage[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchContentPages();
  }, []);

  const fetchContentPages = async () => {
    try {
      const pages = await adminApi.getContentPages();
      setContentPages(pages);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'An error occurred');
    } finally {
      setLoading(false);
    }
  };

  const getContentTypeLabel = (type: ContentPageType): string => {
    switch (type) {
      case ContentPageType.TermsOfService:
        return 'Terms of Service';
      case ContentPageType.PrivacyPolicy:
        return 'Privacy Policy';
      case ContentPageType.CommunityGuidelines:
        return 'Community Guidelines';
      case ContentPageType.AboutUs:
        return 'About Us';
      case ContentPageType.Help:
        return 'Help';
      default:
        return 'Unknown';
    }
  };

  const getStatusIcon = (page: ContentPage) => {
    if (page.publishedVersion) {
      return <CheckCircle className="h-5 w-5 text-green-600" />;
    }
    return <AlertCircle className="h-5 w-5 text-yellow-600" />;
  };

  const getStatusText = (page: ContentPage) => {
    if (page.publishedVersion) {
      return 'Published';
    }
    return 'Draft';
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-64">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-md p-4">
        <div className="flex">
          <AlertCircle className="h-5 w-5 text-red-400" />
          <div className="ml-3">
            <h3 className="text-sm font-medium text-red-800">Error</h3>
            <div className="mt-2 text-sm text-red-700">{error}</div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Content Management</h1>
          <p className="mt-1 text-sm text-gray-600">
            Manage website content pages with version control and publishing
          </p>
        </div>
      </div>

      <div className="bg-white shadow rounded-lg">
        <div className="px-4 py-5 sm:p-6">
          <div className="space-y-4">
            {contentPages.length === 0 ? (
              <div className="text-center py-12">
                <Edit className="mx-auto h-12 w-12 text-gray-400" />
                <h3 className="mt-2 text-sm font-medium text-gray-900">No content pages</h3>
                <p className="mt-1 text-sm text-gray-500">
                  Get started by creating your first content page.
                </p>
              </div>
            ) : (
              <div className="grid gap-4">
                {contentPages.map((page) => (
                  <div
                    key={page.id}
                    className="border border-gray-200 rounded-lg p-4 hover:border-gray-300 transition-colors"
                  >
                    <div className="flex items-center justify-between">
                      <div className="flex-1">
                        <div className="flex items-center space-x-3">
                          <h3 className="text-lg font-medium text-gray-900">
                            {page.title}
                          </h3>
                          <div className="flex items-center space-x-1">
                            {getStatusIcon(page)}
                            <span className="text-sm text-gray-600">
                              {getStatusText(page)}
                            </span>
                          </div>
                        </div>
                        <div className="mt-1 flex items-center space-x-4 text-sm text-gray-500">
                          <span>Type: {getContentTypeLabel(page.type)}</span>
                          <span>Slug: /{page.slug}</span>
                          <span>Versions: {page.totalVersions}</span>
                          {page.publishedVersion && (
                            <span>
                              Published: v{page.publishedVersion.versionNumber}
                            </span>
                          )}
                        </div>
                      </div>
                      <div className="flex items-center space-x-2">
                        <Link
                          href={`/admin/content/${page.id}`}
                          className="inline-flex items-center px-3 py-2 border border-gray-300 shadow-sm text-sm leading-4 font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                        >
                          <Edit className="h-4 w-4 mr-1" />
                          Edit
                        </Link>
                        <Link
                          href={`/admin/content/${page.id}/versions`}
                          className="inline-flex items-center px-3 py-2 border border-gray-300 shadow-sm text-sm leading-4 font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                        >
                          <Clock className="h-4 w-4 mr-1" />
                          History
                        </Link>
                        {page.publishedVersion && (
                          <Link
                            href={`/${page.slug}`}
                            target="_blank"
                            className="inline-flex items-center px-3 py-2 border border-gray-300 shadow-sm text-sm leading-4 font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                          >
                            <Eye className="h-4 w-4 mr-1" />
                            View
                          </Link>
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
    </div>
  );
}
