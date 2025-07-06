import React from 'react';
import { Text, TouchableOpacity } from 'react-native';

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

interface ContentHighlightProps {
  content: string;
  style?: any;
  onMentionPress?: (username: string) => void;
  onHashtagPress?: (hashtag: string) => void;
  linkColor?: string;
}

/**
 * Component to render content with highlighted mentions and hashtags for React Native
 */
export function ContentHighlight({ 
  content, 
  style, 
  onMentionPress, 
  onHashtagPress,
  linkColor = '#1D4ED8' // Default blue color
}: ContentHighlightProps) {
  const matches = parseContent(content);
  
  return (
    <Text style={style}>
      {matches.map((match, index) => {
        switch (match.type) {
          case 'mention':
            return (
              <TouchableOpacity
                key={`mention-${index}-${match.value}`}
                onPress={() => onMentionPress?.(match.value!)}
                activeOpacity={0.7}
              >
                <Text style={{ color: linkColor, fontWeight: '500' }}>
                  @{match.value}
                </Text>
              </TouchableOpacity>
            );
          
          case 'hashtag':
            return (
              <TouchableOpacity
                key={`hashtag-${index}-${match.value}`}
                onPress={() => onHashtagPress?.(match.value!)}
                activeOpacity={0.7}
              >
                <Text style={{ color: linkColor, fontWeight: '500' }}>
                  #{match.value}
                </Text>
              </TouchableOpacity>
            );
          
          case 'text':
          default:
            return (
              <Text key={`text-${index}`}>
                {match.content}
              </Text>
            );
        }
      })}
    </Text>
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
