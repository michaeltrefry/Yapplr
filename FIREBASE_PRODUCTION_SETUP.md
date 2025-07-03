# Firebase Production Setup Guide

## Overview

This guide explains how to set up Firebase notifications for production deployment. The current implementation supports both development (using Application Default Credentials) and production (using Service Account Keys).

## Development Setup (Current)

For local development, you can continue using Google Application Default Credentials:

```bash
# Install Google Cloud CLI (if not already installed)
brew install google-cloud-sdk

# Authenticate with your Google account
gcloud auth application-default login

# Verify authentication
gcloud auth application-default print-access-token
```

## Production Setup

### Step 1: Create Firebase Service Account

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Select your project (yapplr)
3. Go to **Project Settings** → **Service Accounts**
4. Click **Generate New Private Key**
5. Download the JSON file (keep it secure!)

### Step 2: Prepare Service Account Key

The downloaded JSON file will look like this:
```json
{
  "type": "service_account",
  "project_id": "your-project-id",
  "private_key_id": "...",
  "private_key": "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----\n",
  "client_email": "firebase-adminsdk-...@your-project-id.iam.gserviceaccount.com",
  "client_id": "...",
  "auth_uri": "https://accounts.google.com/o/oauth2/auth",
  "token_uri": "https://oauth2.googleapis.com/token",
  "auth_provider_x509_cert_url": "https://www.googleapis.com/oauth2/v1/certs",
  "client_x509_cert_url": "https://www.googleapis.com/robot/v1/metadata/x509/firebase-adminsdk-...%40your-project-id.iam.gserviceaccount.com"
}
```

### Step 3: Set Environment Variables

For production deployment, set these environment variables:

```bash
# Firebase Configuration
Firebase__ProjectId=your-firebase-project-id
Firebase__ServiceAccountKey='{"type":"service_account","project_id":"your-project-id",...}'
```

**Important**: The `Firebase__ServiceAccountKey` should be the entire JSON content as a single-line string.

### Step 4: Docker Deployment

For Docker deployment on Linode, you can:

#### Option A: Environment File
Create a `.env.production` file:
```bash
cp .env.production.example .env.production
# Edit .env.production with your actual values
```

#### Option B: Docker Environment Variables
```bash
docker run -d \
  -e Firebase__ProjectId=your-project-id \
  -e Firebase__ServiceAccountKey='{"type":"service_account",...}' \
  -p 5161:5161 \
  yapplr-api
```

#### Option C: Docker Compose
```yaml
version: '3.8'
services:
  api:
    image: yapplr-api
    environment:
      - Firebase__ProjectId=your-project-id
      - Firebase__ServiceAccountKey={"type":"service_account",...}
    ports:
      - "5161:5161"
```

## Security Best Practices

1. **Never commit service account keys to version control**
2. **Use environment variables or secure secret management**
3. **Rotate service account keys periodically**
4. **Limit service account permissions to only what's needed**
5. **Use different service accounts for different environments**

## Verification

To verify Firebase is working in production:

1. Check application logs for: `Firebase initialized using Service Account Key`
2. Test sending a message/notification
3. Monitor logs for successful notification sends: `Successfully sent Firebase notification`

## Troubleshooting

### Common Issues:

1. **Invalid JSON format**: Ensure the service account key is valid JSON
2. **Escaped quotes**: Make sure quotes in the JSON are properly escaped in environment variables
3. **Missing permissions**: Ensure the service account has Firebase Admin SDK permissions
4. **Wrong project ID**: Verify the project ID matches your Firebase project

### Logs to Check:

- `Firebase initialized using Service Account Key` - Success
- `Failed to initialize Firebase` - Check service account key format
- `Error sending FCM notification` - Check token validity and permissions
