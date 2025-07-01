import { NextApiRequest, NextApiResponse } from 'next';
import fs from 'fs';
import path from 'path';

export default function handler(req: NextApiRequest, res: NextApiResponse) {
  if (req.method !== 'GET') {
    return res.status(405).json({ message: 'Method not allowed' });
  }

  try {
    // Read the service worker template
    const swPath = path.join(process.cwd(), 'public', 'firebase-messaging-sw.js');
    let swContent = fs.readFileSync(swPath, 'utf8');

    // Replace placeholders with actual environment variables
    swContent = swContent
      .replace('PLACEHOLDER_API_KEY', process.env.NEXT_PUBLIC_FIREBASE_API_KEY || '')
      .replace('PLACEHOLDER_AUTH_DOMAIN', process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN || '')
      .replace('PLACEHOLDER_DATABASE_URL', process.env.NEXT_PUBLIC_FIREBASE_DATABASE_URL || '')
      .replace('PLACEHOLDER_PROJECT_ID', process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID || '')
      .replace('PLACEHOLDER_STORAGE_BUCKET', process.env.NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET || '')
      .replace('PLACEHOLDER_MESSAGING_SENDER_ID', process.env.NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID || '')
      .replace('PLACEHOLDER_APP_ID', process.env.NEXT_PUBLIC_FIREBASE_APP_ID || '')
      .replace('PLACEHOLDER_MEASUREMENT_ID', process.env.NEXT_PUBLIC_FIREBASE_MEASUREMENT_ID || '');

    // Set appropriate headers for service worker
    res.setHeader('Content-Type', 'application/javascript');
    res.setHeader('Cache-Control', 'no-cache, no-store, must-revalidate');
    res.setHeader('Pragma', 'no-cache');
    res.setHeader('Expires', '0');

    res.status(200).send(swContent);
  } catch (error) {
    console.error('Error serving service worker:', error);
    res.status(500).json({ message: 'Internal server error' });
  }
}
