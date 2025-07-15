# Dark Mode Implementation

This document explains how dark mode is implemented in the Yapplr frontend using a clean CSS-only approach.

## Overview

The dark mode implementation uses CSS custom properties (CSS variables) combined with Tailwind's class-based dark mode to provide automatic theme switching without requiring changes to individual components.

## How It Works

### 1. Theme Context
- `ThemeContext` manages the dark mode state
- Automatically adds/removes the `dark` class to `document.documentElement`
- Syncs with user preferences stored in the backend
- Falls back to localStorage for unauthenticated users

### 2. CSS Custom Properties
All theme colors are defined as CSS custom properties in `globals.css`:

```css
:root {
  --background: #ffffff;
  --foreground: #1f2937;
  --surface: #f9fafb;
  --border: #e5e7eb;
  /* ... more colors */
}

.dark {
  --background: #111827;
  --foreground: #f9fafb;
  --surface: #1f2937;
  --border: #374151;
  /* ... more colors */
}
```

### 3. Automatic Class Mapping
Common Tailwind classes are automatically mapped to use our CSS custom properties:

```css
.bg-white {
  background-color: var(--background) !important;
}

.text-gray-900 {
  color: var(--foreground) !important;
}

.border-gray-200 {
  border-color: var(--border) !important;
}
```

This means existing components automatically adapt to dark mode without code changes.

### 4. Tailwind Configuration
- `darkMode: 'class'` enables class-based dark mode
- Extended color palette maps common gray colors to our semantic system
- Preserves original colors for specific use cases (blue, red, green, etc.)

## Benefits

1. **No HTML Changes Required**: Existing components work automatically
2. **Consistent Theming**: All colors use the same semantic system
3. **Easy Maintenance**: Theme changes only require updating CSS custom properties
4. **Performance**: No JavaScript color calculations at runtime
5. **Accessibility**: Proper contrast ratios maintained in both themes

## Usage

### For Users
- Toggle dark mode in Settings → Appearance → Dark Mode
- Preference is automatically saved and synced across devices
- Works for both authenticated and guest users (localStorage fallback)

### For Developers
- Use semantic color classes when possible: `bg-card`, `text-secondary`, etc.
- For new components, prefer CSS custom properties: `var(--background)`
- Existing components using standard Tailwind classes work automatically
- Test both themes using the `/theme-test` page

## Color System

| CSS Variable | Light Theme | Dark Theme | Usage |
|--------------|-------------|------------|-------|
| `--background` | #ffffff | #111827 | Main page background |
| `--foreground` | #1f2937 | #f9fafb | Primary text color |
| `--surface` | #f9fafb | #1f2937 | Cards, panels, hover states |
| `--border` | #e5e7eb | #374151 | Borders, dividers |
| `--text-secondary` | #6b7280 | #d1d5db | Secondary text |
| `--text-muted` | #9ca3af | #9ca3af | Muted text, placeholders |
| `--primary` | #3b82f6 | #3b82f6 | Brand color, links, buttons |
| `--card` | #ffffff | #1f2937 | Card backgrounds |
| `--input` | #ffffff | #374151 | Input field backgrounds |
| `--input-border` | #d1d5db | #4b5563 | Input field borders |

## Testing

Visit `/theme-test` to:
- Toggle between light and dark modes
- See all color swatches in both themes
- Test interactive elements
- Verify CSS custom properties are working

## Implementation Notes

- Uses `!important` declarations to override existing Tailwind classes
- Handles transparency with specific dark mode overrides (especially for headers)
- Maintains backward compatibility with existing components
- Smooth transitions applied to all color changes
- Scrollbar colors adapt to theme
- Special handling for sticky headers with `bg-white/80` backgrounds
- Comprehensive CSS selectors to ensure all header variations are covered
