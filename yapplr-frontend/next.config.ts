import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  eslint: {
    // Disable ESLint during builds
    ignoreDuringBuilds: true,
  },
  images: {
    remotePatterns: [
      {
        protocol: 'http',
        hostname: 'localhost',
        port: '5161',
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
