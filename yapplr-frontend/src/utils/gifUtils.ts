// Utility functions for handling GIF embeds in text content

export interface ParsedGif {
  id: string;
  url: string;
  previewUrl: string;
  width: number;
  height: number;
  title: string;
}

export interface ContentPart {
  type: 'text' | 'gif';
  content: string;
  gif?: ParsedGif;
}

// Parse GIF embed from text content
// Format: [GIF|id|url|previewUrl|width|height|title]
export function parseGifEmbed(text: string): ParsedGif | null {
  // Use pipe separator to avoid conflicts with colons in URLs
  const gifRegex = /\[GIF\|([^\|]+)\|([^\|]+)\|([^\|]+)\|(\d+)\|(\d+)\|([^\]]*)\]/;
  const match = text.match(gifRegex);

  if (!match) return null;

  return {
    id: match[1],
    url: match[2],
    previewUrl: match[3],
    width: parseInt(match[4], 10),
    height: parseInt(match[5], 10),
    title: match[6]?.trim() || 'GIF',
  };
}

// Parse content into text and GIF parts
export function parseContentParts(content: string): ContentPart[] {
  const parts: ContentPart[] = [];
  // Use pipe separator to avoid conflicts with colons in URLs
  const gifRegex = /\[GIF\|([^\|]+)\|([^\|]+)\|([^\|]+)\|(\d+)\|(\d+)\|([^\]]*)\]/g;

  // Debug logging for GIF parsing
  if (content.includes('[GIF:')) {
    console.log('parseContentParts - Input content:', JSON.stringify(content));
    console.log('parseContentParts - Content length:', content.length);
    console.log('parseContentParts - Last 20 chars:', JSON.stringify(content.slice(-20)));

    // Test the regex directly
    const testMatches = [...content.matchAll(gifRegex)];
    console.log('parseContentParts - Regex matches found:', testMatches.length);
    if (testMatches.length === 0) {
      console.log('parseContentParts - No matches! Debugging...');
      console.log('parseContentParts - Contains [GIF:?', content.includes('[GIF:'));
      console.log('parseContentParts - Contains ]?', content.includes(']'));

      // Try a simpler regex to see what's happening
      const simpleRegex = /\[GIF:[^\]]+\]/g;
      const simpleMatches = [...content.matchAll(simpleRegex)];
      console.log('parseContentParts - Simple regex matches:', simpleMatches.length);
      if (simpleMatches.length > 0) {
        console.log('parseContentParts - Simple match found:', simpleMatches[0][0]);
      }
    } else {
      testMatches.forEach((match, i) => {
        console.log(`parseContentParts - Match ${i + 1}:`, match[0]);
      });
    }
  }

  // Debug logging for GIF parsing
  if (content.includes('[GIF:')) {
    console.log('parseContentParts - Input content:', JSON.stringify(content));
    console.log('parseContentParts - Testing regex against content...');

    // Test the regex directly
    const testMatches = [...content.matchAll(gifRegex)];
    console.log('parseContentParts - Regex matches found:', testMatches.length);
    testMatches.forEach((match, i) => {
      console.log(`parseContentParts - Match ${i + 1}:`, match);
    });
  }
  
  let lastIndex = 0;
  let match;
  
  while ((match = gifRegex.exec(content)) !== null) {
    // Add text before the GIF (if any)
    if (match.index > lastIndex) {
      const textContent = content.slice(lastIndex, match.index).trim();
      if (textContent) {
        parts.push({
          type: 'text',
          content: textContent,
        });
      }
    }
    
    // Add the GIF
    parts.push({
      type: 'gif',
      content: match[0],
      gif: {
        id: match[1],
        url: match[2],
        previewUrl: match[3],
        width: parseInt(match[4], 10),
        height: parseInt(match[5], 10),
        title: match[6]?.trim() || 'GIF',
      },
    });
    
    lastIndex = match.index + match[0].length;
  }
  
  // Add remaining text (if any)
  if (lastIndex < content.length) {
    const textContent = content.slice(lastIndex).trim();
    if (textContent) {
      parts.push({
        type: 'text',
        content: textContent,
      });
    }
  }
  
  // If no GIFs found, return the original content as text
  if (parts.length === 0) {
    parts.push({
      type: 'text',
      content: content,
    });
  }
  
  return parts;
}

// Remove GIF embeds from text (useful for character counting)
export function stripGifEmbeds(content: string): string {
  const gifRegex = /\[GIF:([^:]+):([^:]+):([^:]+):(\d+):(\d+):([^\]]*)\]/g;
  return content.replace(gifRegex, '').trim();
}

// Check if content contains GIF embeds
export function containsGifEmbeds(content: string): boolean {
  const gifRegex = /\[GIF:([^:]+):([^:]+):([^:]+):(\d+):(\d+):([^\]]*)\]/;
  return gifRegex.test(content);
}
