// Utility functions for handling GIF embeds in text content (React Native)

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
// Format: [GIF:id:url:previewUrl:width:height:title]
export function parseGifEmbed(text: string): ParsedGif | null {
  const gifRegex = /\[GIF:([^:]+):([^:]+):([^:]+):(\d+):(\d+):([^\]]*)\]/;
  const match = text.match(gifRegex);
  
  if (!match) return null;
  
  return {
    id: match[1],
    url: match[2],
    previewUrl: match[3],
    width: parseInt(match[4], 10),
    height: parseInt(match[5], 10),
    title: match[6] || 'GIF',
  };
}

// Parse content into text and GIF parts
export function parseContentParts(content: string): ContentPart[] {
  const parts: ContentPart[] = [];
  const gifRegex = /\[GIF:([^:]+):([^:]+):([^:]+):(\d+):(\d+):([^\]]*)\]/g;
  
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
        title: match[6] || 'GIF',
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
