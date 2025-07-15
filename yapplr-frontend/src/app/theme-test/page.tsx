'use client';

import { useTheme } from '@/contexts/ThemeContext';
import { Moon, Sun } from 'lucide-react';

export default function ThemeTest() {
  const { isDarkMode, toggleDarkMode } = useTheme();

  return (
    <div className="min-h-screen p-8">
      <div className="max-w-4xl mx-auto space-y-8">
        {/* Header */}
        <div className="text-center">
          <h1 className="text-4xl font-bold text-gray-900 mb-4">Dark Mode Test Page</h1>
          <p className="text-gray-600 mb-6">
            Current theme: <strong>{isDarkMode ? 'Dark' : 'Light'}</strong>
          </p>
          
          {/* Toggle Button */}
          <button
            onClick={toggleDarkMode}
            className="inline-flex items-center space-x-2 px-6 py-3 rounded-lg transition-colors border-2"
            style={{
              backgroundColor: 'var(--primary)',
              color: 'var(--primary-text)',
              borderColor: 'var(--primary)',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.opacity = '0.9';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.opacity = '1';
            }}
          >
            {isDarkMode ? <Sun className="w-5 h-5" /> : <Moon className="w-5 h-5" />}
            <span>Switch to {isDarkMode ? 'Light' : 'Dark'} Mode</span>
          </button>
        </div>

        {/* Color Swatches */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <div className="bg-white border border-gray-200 rounded-lg p-6">
            <h3 className="font-semibold text-gray-900 mb-4">Background Colors</h3>
            <div className="space-y-3">
              <div className="bg-white p-3 border border-gray-200 rounded">
                <span className="text-gray-900">Background (bg-white)</span>
              </div>
              <div className="bg-gray-50 p-3 border border-gray-200 rounded">
                <span className="text-gray-900">Surface (bg-gray-50)</span>
              </div>
            </div>
          </div>

          <div className="bg-white border border-gray-200 rounded-lg p-6">
            <h3 className="font-semibold text-gray-900 mb-4">Text Colors</h3>
            <div className="space-y-2">
              <p className="text-gray-900">Primary text (text-gray-900)</p>
              <p className="text-gray-600">Secondary text (text-gray-600)</p>
              <p className="text-gray-500">Muted text (text-gray-500)</p>
              <p className="text-gray-400">Disabled text (text-gray-400)</p>
            </div>
          </div>

          <div className="bg-white border border-gray-200 rounded-lg p-6">
            <h3 className="font-semibold text-gray-900 mb-4">Interactive Elements</h3>
            <div className="space-y-3">
              <button
                className="w-full px-4 py-2 rounded transition-colors border"
                style={{
                  backgroundColor: 'var(--surface)',
                  color: 'var(--foreground)',
                  borderColor: 'var(--border)',
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.backgroundColor = 'var(--border)';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.backgroundColor = 'var(--surface)';
                }}
              >
                Button
              </button>
              <input 
                type="text" 
                placeholder="Input field"
                className="w-full px-3 py-2 border border-gray-300 rounded focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
              />
              <textarea 
                placeholder="Textarea"
                className="w-full px-3 py-2 border border-gray-300 rounded focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
                rows={3}
              />
            </div>
          </div>
        </div>

        {/* CSS Custom Properties Display */}
        <div className="bg-white border border-gray-200 rounded-lg p-6">
          <h3 className="font-semibold text-gray-900 mb-4">CSS Custom Properties</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm font-mono">
            <div>
              <div className="mb-2">
                <span className="text-gray-600">--background:</span>
                <div className="w-full h-8 border border-gray-300 rounded" style={{backgroundColor: 'var(--background)'}}></div>
              </div>
              <div className="mb-2">
                <span className="text-gray-600">--foreground:</span>
                <div className="w-full h-8 border border-gray-300 rounded" style={{backgroundColor: 'var(--foreground)'}}></div>
              </div>
              <div className="mb-2">
                <span className="text-gray-600">--surface:</span>
                <div className="w-full h-8 border border-gray-300 rounded" style={{backgroundColor: 'var(--surface)'}}></div>
              </div>
            </div>
            <div>
              <div className="mb-2">
                <span className="text-gray-600">--border:</span>
                <div className="w-full h-8 border border-gray-300 rounded" style={{backgroundColor: 'var(--border)'}}></div>
              </div>
              <div className="mb-2">
                <span className="text-gray-600">--primary:</span>
                <div className="w-full h-8 border border-gray-300 rounded" style={{backgroundColor: 'var(--primary)'}}></div>
              </div>
              <div className="mb-2">
                <span className="text-gray-600">--card:</span>
                <div className="w-full h-8 border border-gray-300 rounded" style={{backgroundColor: 'var(--card)'}}></div>
              </div>
            </div>
          </div>
        </div>

        {/* Header Test Section */}
        <div className="bg-white border border-gray-200 rounded-lg p-6">
          <h3 className="font-semibold text-gray-900 mb-4">Sticky Header Test</h3>
          <p className="text-gray-600 mb-4">
            This section tests the sticky header with bg-white/80 background that should adapt to dark mode:
          </p>

          {/* Test sticky header */}
          <div className="relative h-32 overflow-y-auto border border-gray-200 rounded">
            <div className="sticky top-0 bg-white/80 backdrop-blur-md border-b border-gray-200 p-4 z-20">
              <h4 className="text-lg font-semibold text-gray-900">Sticky Header</h4>
              <p className="text-sm text-gray-600">This header should have a dark background in dark mode</p>
            </div>
            <div className="p-4 space-y-4">
              <p className="text-gray-700">Scroll content below the header...</p>
              <p className="text-gray-700">More content to enable scrolling...</p>
              <p className="text-gray-700">Even more content...</p>
              <p className="text-gray-700">And more content to test the sticky behavior...</p>
              <p className="text-gray-700">Final line of content.</p>
            </div>
          </div>
        </div>

        {/* Navigation */}
        <div className="text-center">
          <a
            href="/"
            className="inline-block px-6 py-3 rounded-lg transition-colors border-2"
            style={{
              backgroundColor: 'var(--surface)',
              color: 'var(--foreground)',
              borderColor: 'var(--border)',
            }}
            onMouseEnter={(e) => {
              e.currentTarget.style.backgroundColor = 'var(--border)';
            }}
            onMouseLeave={(e) => {
              e.currentTarget.style.backgroundColor = 'var(--surface)';
            }}
          >
            ← Back to Home
          </a>
        </div>
      </div>
    </div>
  );
}
