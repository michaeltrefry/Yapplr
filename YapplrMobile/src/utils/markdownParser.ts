import { ReactElement } from 'react';

export interface MarkdownElement {
  type: 'heading1' | 'heading2' | 'heading3' | 'paragraph' | 'bulletPoint' | 'break';
  content: string;
  level?: number;
}

export function parseMarkdown(markdown: string): MarkdownElement[] {
  const lines = markdown.split('\n');
  const elements: MarkdownElement[] = [];

  for (const line of lines) {
    const trimmedLine = line.trim();
    
    if (trimmedLine === '') {
      elements.push({ type: 'break', content: '' });
      continue;
    }

    if (trimmedLine.startsWith('# ')) {
      elements.push({
        type: 'heading1',
        content: trimmedLine.slice(2),
        level: 1
      });
    } else if (trimmedLine.startsWith('## ')) {
      elements.push({
        type: 'heading2',
        content: trimmedLine.slice(3),
        level: 2
      });
    } else if (trimmedLine.startsWith('### ')) {
      elements.push({
        type: 'heading3',
        content: trimmedLine.slice(4),
        level: 3
      });
    } else if (trimmedLine.startsWith('- ')) {
      elements.push({
        type: 'bulletPoint',
        content: trimmedLine.slice(2)
      });
    } else {
      elements.push({
        type: 'paragraph',
        content: trimmedLine
      });
    }
  }

  return elements;
}

export function formatLastUpdated(dateString: string): string {
  try {
    const date = new Date(dateString);
    return date.toLocaleDateString();
  } catch {
    return new Date().toLocaleDateString();
  }
}
