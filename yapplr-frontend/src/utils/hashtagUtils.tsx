import React from 'react';
import Link from 'next/link';

// Regex pattern to match #hashtag tags
// Matches # followed by 1-50 alphanumeric characters, underscores, or hyphens
// Excludes tags that start with numbers to follow common hashtag conventions
const HASHTAG_REGEX = /#([a-zA-Z][a-zA-Z0-9_-]{0,49})/g;

/**
 * Extracts all unique hashtags from the given content
 */
export function extractHashtags(content: string): string[] {
  if (!content) return [];
  
  const matches = content.match(HASHTAG_REGEX);
  if (!matches) return [];
  
  const hashtags = new Set<string>();
  matches.forEach(match => {
    const hashtag = match.substring(1).toLowerCase(); // Remove the # symbol and normalize to lowercase
    hashtags.add(hashtag);
  });
  
  return Array.from(hashtags);
}

/**
 * Checks if the given content contains any hashtags
 */
export function hasHashtags(content: string): boolean {
  if (!content) return false;
  return HASHTAG_REGEX.test(content);
}

/**
 * Replaces #hashtag tags in content with clickable React components
 */
export function highlightHashtags(content: string): React.ReactNode[] {
  if (!content) return [content];
  
  const parts: React.ReactNode[] = [];
  let lastIndex = 0;
  let match;
  
  // Reset regex lastIndex to ensure we start from the beginning
  HASHTAG_REGEX.lastIndex = 0;
  
  while ((match = HASHTAG_REGEX.exec(content)) !== null) {
    // Add text before the hashtag
    if (match.index > lastIndex) {
      parts.push(content.substring(lastIndex, match.index));
    }
    
    // Add the hashtag as a clickable link
    const hashtag = match[1].toLowerCase();
    parts.push(
      <Link
        key={`hashtag-${match.index}-${hashtag}`}
        href={`/hashtag/${hashtag}`}
        className="text-blue-600 hover:text-blue-800 hover:underline font-medium"
        onClick={(e) => e.stopPropagation()} // Prevent parent click handlers
      >
        #{hashtag}
      </Link>
    );
    
    lastIndex = HASHTAG_REGEX.lastIndex;
  }
  
  // Add remaining text after the last hashtag
  if (lastIndex < content.length) {
    parts.push(content.substring(lastIndex));
  }
  
  return parts.length > 0 ? parts : [content];
}

/**
 * Gets all hashtag positions in the content for highlighting purposes
 */
export interface HashtagPosition {
  startIndex: number;
  length: number;
  hashtag: string;
  fullMatch: string;
}

export function getHashtagPositions(content: string): HashtagPosition[] {
  if (!content) return [];
  
  const positions: HashtagPosition[] = [];
  let match;
  
  // Reset regex lastIndex
  HASHTAG_REGEX.lastIndex = 0;
  
  while ((match = HASHTAG_REGEX.exec(content)) !== null) {
    positions.push({
      startIndex: match.index,
      length: match[0].length,
      hashtag: match[1].toLowerCase(),
      fullMatch: match[0]
    });
  }
  
  return positions;
}

/**
 * Component to render content with highlighted hashtags
 */
interface HashtagHighlightProps {
  content: string;
  className?: string;
}

export function HashtagHighlight({ content, className = '' }: HashtagHighlightProps) {
  const highlightedContent = highlightHashtags(content);
  
  return (
    <span className={className}>
      {highlightedContent}
    </span>
  );
}

/**
 * Validates if a hashtag name is valid according to our rules
 */
export function isValidHashtag(hashtag: string): boolean {
  if (!hashtag) return false;
  
  // Remove # if present
  const cleanHashtag = hashtag.startsWith('#') ? hashtag.substring(1) : hashtag;
  
  // Check length (1-50 characters)
  if (cleanHashtag.length < 1 || cleanHashtag.length > 50) return false;
  
  // Check if it matches our pattern (starts with letter, contains only letters, numbers, underscores, hyphens)
  return /^[a-zA-Z][a-zA-Z0-9_-]*$/.test(cleanHashtag);
}
