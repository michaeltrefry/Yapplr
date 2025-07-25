'use client';

import React from 'react';
import { AdminPost, AdminComment } from '@/types';
import { ExternalLink, Tag } from 'lucide-react';
import { SystemTagDisplay } from './SystemTagDisplay';

export interface ContentBodyProps {
  content: AdminPost | AdminComment;
  contentType: 'post' | 'comment';
  showPostContext?: boolean;
}

export function ContentBody({
  content,
  contentType,
  showPostContext = false,
}: ContentBodyProps) {
  const isPost = contentType === 'post';
  const post = content as AdminPost;
  const comment = content as AdminComment;

  // Generate the appropriate link for the content
  const getContentLink = () => {
    if (isPost) {
      // If post is in a group, link to group post view
      if (post.group) {
        return `/groups/${post.group.id}/posts/${content.id}`;
      }
      return `/yap/${content.id}`;
    } else {
      // If comment is in a group, link to group post view
      if (comment.group) {
        return `/groups/${comment.group.id}/posts/${comment.postId}#comment-${content.id}`;
      }
      return `/yap/${comment.postId}#comment-${content.id}`;
    }
  };

  return (
    <div className="space-y-4">
      {/* Group Context */}
      {(isPost ? post.group : comment.group) && (
        <div className="text-sm text-purple-700 bg-purple-50 p-3 rounded-lg border border-purple-200">
          <span className="font-medium">
            {isPost ? 'Group Post' : 'Group Comment'} in:
          </span>
          <a
            href={`/groups/${(isPost ? post.group : comment.group)?.id}`}
            target="_blank"
            rel="noopener noreferrer"
            className="ml-2 text-purple-800 hover:text-purple-900 inline-flex items-center gap-1 font-medium"
          >
            {(isPost ? post.group : comment.group)?.name}
            <ExternalLink className="h-3 w-3" />
          </a>
        </div>
      )}

      {/* Main Content */}
      <div>
        <a
          href={getContentLink()}
          target="_blank"
          rel="noopener noreferrer"
          className="text-gray-900 hover:text-blue-600 block group"
        >
          <div className="flex items-start gap-2">
            <span className="flex-1 text-sm leading-relaxed">
              {content.content}
            </span>
            <ExternalLink className="h-4 w-4 text-gray-400 group-hover:text-blue-600 flex-shrink-0 mt-0.5" />
          </div>
        </a>

        {/* Image for posts */}
        {isPost && post.imageFileName && (
          <div className="mt-3">
            <img
              src={`${process.env.NEXT_PUBLIC_API_URL}/uploads/${post.imageFileName}`}
              alt="Post image"
              className="max-w-md rounded-lg border border-gray-200"
            />
          </div>
        )}
      </div>

      {/* Post Context for Comments */}
      {!isPost && showPostContext && (
        <div className="text-sm text-gray-600 bg-gray-50 p-3 rounded-lg">
          <span className="font-medium">Comment on:</span>
          <a
            href={comment.group ? `/groups/${comment.group.id}/posts/${comment.postId}` : `/yap/${comment.postId}`}
            target="_blank"
            rel="noopener noreferrer"
            className="ml-2 text-blue-600 hover:text-blue-800 inline-flex items-center gap-1"
          >
            {comment.group ? `Group Post #${comment.postId}` : `Post #${comment.postId}`}
            <ExternalLink className="h-3 w-3" />
          </a>
        </div>
      )}

      {/* System Tags */}
      {content.systemTags && content.systemTags.length > 0 && (
        <SystemTagDisplay tags={content.systemTags} />
      )}

      {/* AI Suggested Tags for Posts */}
      {isPost && post.aiSuggestedTags && post.aiSuggestedTags.length > 0 && (
        <div className="border-t border-gray-200 pt-3">
          <div className="text-sm text-blue-600 font-medium mb-2">
            AI Suggestions ({post.aiSuggestedTags.filter(tag => !tag.isApproved && !tag.isRejected).length} pending)
          </div>
          <div className="flex flex-wrap gap-2">
            {post.aiSuggestedTags.map((tag) => (
              <span
                key={tag.id}
                className={`inline-flex items-center px-2 py-1 rounded-full text-xs font-medium ${
                  tag.isApproved 
                    ? 'bg-green-100 text-green-800' 
                    : tag.isRejected 
                    ? 'bg-red-100 text-red-800'
                    : 'bg-blue-100 text-blue-800'
                }`}
              >
                <Tag className="h-3 w-3 mr-1" />
                {tag.tagName}
              </span>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
