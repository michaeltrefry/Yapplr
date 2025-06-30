import React from 'react';
import Link from 'next/link';

// Regex pattern to match @username mentions
// Matches @ followed by 3-50 alphanumeric characters, underscores, or hyphens
const MENTION_REGEX = /@([a-zA-Z0-9_-]{3,50})/g;

/**
 * Extracts all unique usernames mentioned in the given content
 */
export function extractMentions(content: string): string[] {
  if (!content) return [];
  
  const matches = content.match(MENTION_REGEX);
  if (!matches) return [];
  
  const usernames = new Set<string>();
  matches.forEach(match => {
    const username = match.substring(1); // Remove the @ symbol
    usernames.add(username);
  });
  
  return Array.from(usernames);
}

/**
 * Checks if the given content contains any mentions
 */
export function hasMentions(content: string): boolean {
  if (!content) return false;
  return MENTION_REGEX.test(content);
}

/**
 * Replaces @username mentions in content with clickable React components
 */
export function highlightMentions(content: string): React.ReactNode[] {
  if (!content) return [content];
  
  const parts: React.ReactNode[] = [];
  let lastIndex = 0;
  let match;
  
  // Reset regex lastIndex to ensure we start from the beginning
  MENTION_REGEX.lastIndex = 0;
  
  while ((match = MENTION_REGEX.exec(content)) !== null) {
    // Add text before the mention
    if (match.index > lastIndex) {
      parts.push(content.substring(lastIndex, match.index));
    }
    
    // Add the mention as a clickable link
    const username = match[1];
    parts.push(
      <Link
        key={`mention-${match.index}-${username}`}
        href={`/profile/${username}`}
        className="text-blue-600 hover:text-blue-800 hover:underline font-medium"
        onClick={(e) => e.stopPropagation()} // Prevent parent click handlers
      >
        @{username}
      </Link>
    );
    
    lastIndex = MENTION_REGEX.lastIndex;
  }
  
  // Add remaining text after the last mention
  if (lastIndex < content.length) {
    parts.push(content.substring(lastIndex));
  }
  
  return parts.length > 0 ? parts : [content];
}

/**
 * Gets all mention positions in the content for highlighting purposes
 */
export interface MentionPosition {
  startIndex: number;
  length: number;
  username: string;
  fullMatch: string;
}

export function getMentionPositions(content: string): MentionPosition[] {
  if (!content) return [];
  
  const positions: MentionPosition[] = [];
  let match;
  
  // Reset regex lastIndex
  MENTION_REGEX.lastIndex = 0;
  
  while ((match = MENTION_REGEX.exec(content)) !== null) {
    positions.push({
      startIndex: match.index,
      length: match[0].length,
      username: match[1],
      fullMatch: match[0]
    });
  }
  
  return positions;
}

/**
 * Component to render content with highlighted mentions
 */
interface MentionHighlightProps {
  content: string;
  className?: string;
}

export function MentionHighlight({ content, className = '' }: MentionHighlightProps) {
  const highlightedContent = highlightMentions(content);
  
  return (
    <span className={className}>
      {highlightedContent}
    </span>
  );
}
