import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatDate(dateString: string): string {
  const date = new Date(dateString);
  const now = new Date();
  const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);

  if (diffInSeconds < 60) {
    return `${diffInSeconds}s`;
  }

  const diffInMinutes = Math.floor(diffInSeconds / 60);
  if (diffInMinutes < 60) {
    return `${diffInMinutes}m`;
  }

  const diffInHours = Math.floor(diffInMinutes / 60);
  if (diffInHours < 24) {
    return `${diffInHours}h`;
  }

  const diffInDays = Math.floor(diffInHours / 24);
  if (diffInDays < 7) {
    return `${diffInDays}d`;
  }

  // For older posts, show the actual date
  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: date.getFullYear() !== now.getFullYear() ? 'numeric' : undefined,
  });
}

export function formatNumber(num: number | undefined | null): string {
  // Handle undefined, null, or NaN values
  if (num == null || isNaN(num)) {
    return '0';
  }

  // Ensure we have a valid number
  const validNum = Number(num);
  if (isNaN(validNum)) {
    return '0';
  }

  if (validNum < 1000) {
    return validNum.toString();
  }

  if (validNum < 1000000) {
    const formatted = (validNum / 1000).toFixed(1);
    return `${formatted.endsWith('.0') ? formatted.slice(0, -2) : formatted}K`;
  }

  const formatted = (validNum / 1000000).toFixed(1);
  return `${formatted.endsWith('.0') ? formatted.slice(0, -2) : formatted}M`;
}
