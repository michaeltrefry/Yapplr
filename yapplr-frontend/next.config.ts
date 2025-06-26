import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  images: {
    remotePatterns: [
      {
        protocol: 'http',
        hostname: 'localhost',
        port: '5161',
        pathname: '/api/images/**',
      },
    ],
  },
};

export default nextConfig;
