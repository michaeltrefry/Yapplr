import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  eslint: {
    // Disable ESLint during builds
    ignoreDuringBuilds: true,
  },
  // Add cache-busting headers for development
  async headers() {
    if (process.env.NODE_ENV === 'development') {
      return [
        {
          source: '/(.*)',
          headers: [
            {
              key: 'Cache-Control',
              value: 'no-cache, no-store, must-revalidate',
            },
            {
              key: 'Pragma',
              value: 'no-cache',
            },
            {
              key: 'Expires',
              value: '0',
            },
          ],
        },
      ];
    }
    return [];
  },
  images: {
    // Disable image optimization for local development to avoid Docker networking issues
    unoptimized: true, // Force disable optimization for all environments
    remotePatterns: [
      {
        protocol: 'http',
        hostname: 'localhost',
        port: '5161',
        pathname: '/api/images/**',
      },
      {
        protocol: 'http',
        hostname: 'localhost',
        port: '8080',
        pathname: '/api/images/**',
      },
      {
        protocol: 'http',
        hostname: 'yapplr-api',
        port: '8080',
        pathname: '/api/images/**',
      },
      {
        protocol: 'https',
        hostname: '**', // Allow all HTTPS hostnames for link preview images
      },
      {
        protocol: 'http',
        hostname: '**', // Allow all HTTP hostnames for link preview images
      },
    ],
  },
};

export default nextConfig;
