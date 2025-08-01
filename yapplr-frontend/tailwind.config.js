/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './src/pages/**/*.{js,ts,jsx,tsx,mdx}',
    './src/components/**/*.{js,ts,jsx,tsx,mdx}',
    './src/app/**/*.{js,ts,jsx,tsx,mdx}',
  ],
  darkMode: 'class', // Enable class-based dark mode
  theme: {
    extend: {
      colors: {
        // Semantic color system using CSS custom properties
        background: 'var(--background)',
        foreground: 'var(--foreground)',
        surface: 'var(--surface)',
        border: 'var(--border)',
        'text-secondary': 'var(--text-secondary)',
        'text-muted': 'var(--text-muted)',
        primary: 'var(--primary)',
        'primary-text': 'var(--primary-text)',
        success: 'var(--success)',
        error: 'var(--error)',
        warning: 'var(--warning)',
        card: 'var(--card)',
        input: 'var(--input)',
        'input-border': 'var(--input-border)',

        // Override common gray colors to use our semantic system
        gray: {
          50: 'var(--surface)',
          100: 'var(--surface)',
          200: 'var(--border)',
          300: 'var(--input-border)',
          400: 'var(--text-muted)',
          500: 'var(--text-secondary)',
          600: 'var(--text-secondary)',
          700: 'var(--foreground)',
          800: 'var(--foreground)',
          900: 'var(--foreground)',
        },

        // Keep original colors for specific use cases
        white: '#ffffff',
        black: '#000000',
        blue: {
          50: '#eff6ff',
          100: '#dbeafe',
          200: '#bfdbfe',
          300: '#93c5fd',
          400: '#60a5fa',
          500: '#3b82f6',
          600: '#2563eb',
          700: '#1d4ed8',
          800: '#1e40af',
          900: '#1e3a8a',
        },
        red: {
          400: '#f87171',
          500: '#ef4444',
          600: '#dc2626',
        },
        green: {
          400: '#4ade80',
          500: '#10b981',
          600: '#059669',
        },
        yellow: {
          400: '#facc15',
          500: '#f59e0b',
          600: '#d97706',
        },
      },
    },
  },
  plugins: [
    require('@tailwindcss/line-clamp'),
  ],
}
