'use client';

import React from 'react';

export interface ToggleProps {
  checked: boolean;
  onChange: (checked: boolean) => void;
  disabled?: boolean;
  size?: 'sm' | 'md' | 'lg';
  color?: 'blue' | 'purple' | 'green' | 'red';
  className?: string;
  'aria-label'?: string;
}

export function Toggle({
  checked,
  onChange,
  disabled = false,
  size = 'md',
  color = 'blue',
  className = '',
  'aria-label': ariaLabel,
}: ToggleProps) {
  const sizeClasses = {
    sm: 'w-9 h-5',
    md: 'w-11 h-6',
    lg: 'w-14 h-7',
  };

  const thumbSizeClasses = {
    sm: 'after:h-4 after:w-4 after:top-[2px] after:left-[2px]',
    md: 'after:h-5 after:w-5 after:top-[2px] after:left-[2px]',
    lg: 'after:h-6 after:w-6 after:top-[2px] after:left-[2px]',
  };

  const colorClasses = {
    blue: 'peer-checked:bg-blue-600 peer-focus:ring-blue-300',
    purple: 'peer-checked:bg-purple-600 peer-focus:ring-purple-300',
    green: 'peer-checked:bg-green-600 peer-focus:ring-green-300',
    red: 'peer-checked:bg-red-600 peer-focus:ring-red-300',
  };

  return (
    <label className={`relative inline-flex items-center cursor-pointer ${disabled ? 'opacity-50 cursor-not-allowed' : ''} ${className}`}>
      <input
        type="checkbox"
        checked={checked}
        onChange={(e) => !disabled && onChange(e.target.checked)}
        disabled={disabled}
        className="sr-only peer"
        aria-label={ariaLabel}
      />
      <div 
        className={`
          ${sizeClasses[size]} 
          bg-gray-200 
          peer-focus:outline-none 
          peer-focus:ring-4 
          ${colorClasses[color]}
          rounded-full 
          peer 
          peer-checked:after:translate-x-full 
          peer-checked:after:border-white 
          after:content-[''] 
          after:absolute 
          ${thumbSizeClasses[size]}
          after:bg-white 
          after:border-gray-300 
          after:border 
          after:rounded-full 
          after:transition-all
          ${colorClasses[color]}
        `}
      />
    </label>
  );
}
