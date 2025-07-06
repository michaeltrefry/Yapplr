import React from 'react';
import Link from 'next/link';

// Regex patterns
const MENTION_REGEX = /@([a-zA-Z0-9_-]{3,50})/g;
const HASHTAG_REGEX = /#([a-zA-Z][a-zA-Z0-9_-]{0,49})/g;
const URL_REGEX = /https?:\/\/(?:[-\w.])+(?:\:[0-9]+)?(?:\/(?:[\w/_.-])*(?:\?(?:[\w&=%.~+/-])*)?(?:\#(?:[\w.-])*)?)?/g;

interface ContentMatch {
  type: 'mention' | 'hashtag' | 'url' | 'text';
  content: string;
  startIndex: number;
  endIndex: number;
  value?: string; // username for mentions, hashtag for hashtags, full URL for urls
}

/**
 * Parses content and identifies mentions, hashtags, and regular text
 */
function parseContent(content: string): ContentMatch[] {
  if (!content) return [{ type: 'text', content, startIndex: 0, endIndex: content.length }];

  const matches: ContentMatch[] = [];
  
  // Find all mentions
  let match;
  MENTION_REGEX.lastIndex = 0;
  while ((match = MENTION_REGEX.exec(content)) !== null) {
    matches.push({
      type: 'mention',
      content: match[0],
      startIndex: match.index,
      endIndex: match.index + match[0].length,
      value: match[1]
    });
  }

  // Find all hashtags
  HASHTAG_REGEX.lastIndex = 0;
  while ((match = HASHTAG_REGEX.exec(content)) !== null) {
    matches.push({
      type: 'hashtag',
      content: match[0],
      startIndex: match.index,
      endIndex: match.index + match[0].length,
      value: match[1].toLowerCase()
    });
  }

  // Find all URLs
  URL_REGEX.lastIndex = 0;
  while ((match = URL_REGEX.exec(content)) !== null) {
    matches.push({
      type: 'url',
      content: match[0],
      startIndex: match.index,
      endIndex: match.index + match[0].length,
      value: match[0]
    });
  }

  // Sort matches by start index
  matches.sort((a, b) => a.startIndex - b.startIndex);

  // Fill in text segments
  const result: ContentMatch[] = [];
  let lastIndex = 0;

  for (const match of matches) {
    // Add text before this match
    if (match.startIndex > lastIndex) {
      result.push({
        type: 'text',
        content: content.substring(lastIndex, match.startIndex),
        startIndex: lastIndex,
        endIndex: match.startIndex
      });
    }

    // Add the match
    result.push(match);
    lastIndex = match.endIndex;
  }

  // Add remaining text
  if (lastIndex < content.length) {
    result.push({
      type: 'text',
      content: content.substring(lastIndex),
      startIndex: lastIndex,
      endIndex: content.length
    });
  }

  return result.length > 0 ? result : [{ type: 'text', content, startIndex: 0, endIndex: content.length }];
}

/**
 * Renders content with highlighted mentions and hashtags
 */
export function highlightContent(content: string): React.ReactNode[] {
  const matches = parseContent(content);
  
  return matches.map((match, index) => {
    switch (match.type) {
      case 'mention':
        return (
          <Link
            key={`mention-${index}-${match.value}`}
            href={`/profile/${match.value}`}
            className="text-blue-600 hover:text-blue-800 hover:underline font-medium"
            onClick={(e) => e.stopPropagation()}
          >
            @{match.value}
          </Link>
        );
      
      case 'hashtag':
        return (
          <Link
            key={`hashtag-${index}-${match.value}`}
            href={`/hashtag/${match.value}`}
            className="text-blue-600 hover:text-blue-800 hover:underline font-medium"
            onClick={(e) => e.stopPropagation()}
          >
            #{match.value}
          </Link>
        );

      case 'url':
        return (
          <a
            key={`url-${index}-${match.value}`}
            href={match.value}
            target="_blank"
            rel="noopener noreferrer"
            className="text-blue-600 hover:text-blue-800 hover:underline font-medium"
            onClick={(e) => e.stopPropagation()}
          >
            {match.value}
          </a>
        );

      case 'text':
      default:
        return match.content;
    }
  });
}

/**
 * Component to render content with highlighted mentions and hashtags
 */
interface ContentHighlightProps {
  content: string;
  className?: string;
}

export function ContentHighlight({ content, className = '' }: ContentHighlightProps) {
  const highlightedContent = highlightContent(content);
  
  return (
    <span className={className}>
      {highlightedContent}
    </span>
  );
}

/**
 * Extract mentions from content
 */
export function extractMentions(content: string): string[] {
  if (!content) return [];
  
  const matches = content.match(MENTION_REGEX);
  if (!matches) return [];
  
  const usernames = new Set<string>();
  matches.forEach(match => {
    const username = match.substring(1);
    usernames.add(username);
  });
  
  return Array.from(usernames);
}

/**
 * Extract hashtags from content
 */
export function extractHashtags(content: string): string[] {
  if (!content) return [];
  
  const matches = content.match(HASHTAG_REGEX);
  if (!matches) return [];
  
  const hashtags = new Set<string>();
  matches.forEach(match => {
    const hashtag = match.substring(1).toLowerCase();
    hashtags.add(hashtag);
  });
  
  return Array.from(hashtags);
}

/**
 * Check if content has mentions
 */
export function hasMentions(content: string): boolean {
  if (!content) return false;
  return MENTION_REGEX.test(content);
}

/**
 * Check if content has hashtags
 */
export function hasHashtags(content: string): boolean {
  if (!content) return false;
  return HASHTAG_REGEX.test(content);
}

/**
 * Extract URLs from content
 */
export function extractUrls(content: string): string[] {
  if (!content) return [];

  const matches = content.match(URL_REGEX);
  if (!matches) return [];

  const urls = new Set<string>();
  matches.forEach(match => {
    urls.add(match);
  });

  return Array.from(urls);
}

/**
 * Check if content has URLs
 */
export function hasUrls(content: string): boolean {
  if (!content) return false;
  return URL_REGEX.test(content);
}
