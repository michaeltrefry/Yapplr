# CORS Configuration Guide

This guide explains how to configure Cross-Origin Resource Sharing (CORS) policies for the Yapplr API in different environments.

## Overview

The CORS configuration has been made environment-specific and configurable through appsettings files. This allows you to:

- Set different allowed origins for Development, Staging, and Production
- Configure specific policies for different use cases (Frontend, SignalR, Mobile)
- Override settings using environment variables for deployment

## Configuration Structure

The CORS configuration is defined in the `Cors` section of appsettings files:

```json
{
  "Cors": {
    "AllowFrontend": {
      "AllowedOrigins": ["https://example.com"],
      "AllowAnyHeader": true,
      "AllowAnyMethod": true,
      "AllowCredentials": true
    },
    "AllowSignalR": {
      "AllowedOrigins": ["https://example.com"],
      "AllowAnyHeader": true,
      "AllowAnyMethod": true,
      "AllowCredentials": true
    },
    "AllowAll": {
      "AllowAnyOrigin": true,
      "AllowAnyHeader": true,
      "AllowAnyMethod": true,
      "AllowCredentials": false
    }
  }
}
```

## Policy Configuration Options

Each CORS policy supports the following configuration options:

### Origins
- `AllowedOrigins`: Array of specific origins to allow
- `AllowAnyOrigin`: Boolean to allow all origins (cannot be used with AllowCredentials)

### Headers
- `AllowedHeaders`: Array of specific headers to allow
- `AllowAnyHeader`: Boolean to allow all headers

### Methods
- `AllowedMethods`: Array of specific HTTP methods to allow
- `AllowAnyMethod`: Boolean to allow all HTTP methods

### Credentials
- `AllowCredentials`: Boolean to allow credentials (cookies, authorization headers)

## Environment-Specific Configurations

### Development (appsettings.Development.json)
```json
{
  "Cors": {
    "AllowFrontend": {
      "AllowedOrigins": [
        "http://localhost:3000",
        "http://localhost:3001",
        "http://localhost:5173",
        "http://192.168.254.181:3000",
        "http://192.168.254.181:3001"
      ],
      "AllowAnyHeader": true,
      "AllowAnyMethod": true,
      "AllowCredentials": true
    }
  }
}
```

### Staging (appsettings.Staging.json)
```json
{
  "Cors": {
    "AllowFrontend": {
      "AllowedOrigins": [
        "https://stg.yapplr.com",
        "https://stg-api.yapplr.com",
        "http://localhost:3000",
        "http://localhost:3001"
      ],
      "AllowAnyHeader": true,
      "AllowAnyMethod": true,
      "AllowCredentials": true
    }
  }
}
```

### Production (appsettings.Production.json)
```json
{
  "Cors": {
    "AllowFrontend": {
      "AllowedOrigins": [
        "https://yapplr.com",
        "https://www.yapplr.com",
        "https://app.yapplr.com"
      ],
      "AllowAnyHeader": true,
      "AllowAnyMethod": true,
      "AllowCredentials": true
    },
    "AllowAll": {
      "AllowAnyOrigin": false,
      "AllowAnyHeader": false,
      "AllowAnyMethod": false,
      "AllowCredentials": false
    }
  }
}
```

## Environment Variables

You can override CORS settings using environment variables:

```bash
# Frontend policy
Cors__AllowFrontend__AllowedOrigins__0=https://your-domain.com
Cors__AllowFrontend__AllowedOrigins__1=https://www.your-domain.com
Cors__AllowFrontend__AllowCredentials=true

# SignalR policy
Cors__AllowSignalR__AllowedOrigins__0=https://your-domain.com
Cors__AllowSignalR__AllowCredentials=true

# Disable AllowAll in production
Cors__AllowAll__AllowAnyOrigin=false
```

## CORS Policies

The API defines three CORS policies:

### 1. AllowFrontend
- Used for general API endpoints
- Supports credentials for authentication
- Configured with specific allowed origins per environment

### 2. AllowSignalR
- Used specifically for SignalR hub connections
- Supports credentials for real-time authentication
- Usually mirrors AllowFrontend configuration

### 3. AllowAll
- Used for mobile development and testing
- Does not support credentials
- Should be disabled or restricted in production

## Current Policy Usage

The API currently uses the `AllowSignalR` policy by default:

```csharp
app.UseCors("AllowSignalR");
```

## Admin Endpoint

Administrators can view the current CORS configuration via:

```
GET /api/cors-config
```

This endpoint requires Admin authorization and returns the complete CORS configuration.

## Security Considerations

1. **Production Security**: Always specify exact origins in production, never use `AllowAnyOrigin`
2. **Credentials**: Only enable `AllowCredentials` when necessary and with specific origins
3. **Mobile Apps**: Use the `AllowAll` policy for mobile development but disable in production
4. **Environment Variables**: Use environment variables to override settings in deployed environments

## Migration from Hardcoded Configuration

The previous hardcoded CORS configuration has been replaced with this configurable system. The default configurations maintain the same behavior as before but can now be customized per environment.
