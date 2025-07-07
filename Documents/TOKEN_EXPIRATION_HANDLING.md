# Token Expiration Handling

This document explains how automatic token expiration handling and login redirection is implemented in the Yapplr application.

## Problem

When a user's JWT authentication token expires, API requests start returning 401 Unauthorized errors without any clear indication to the user that they need to re-authenticate. This leads to a poor user experience where users see generic error messages instead of being automatically redirected to the login page.

## Solution

The solution implements automatic detection of token expiration and redirects users to the login page when their tokens are no longer valid.

### Frontend Web App (Next.js)

#### 1. API Response Interceptor

Added an Axios response interceptor in `yapplr-frontend/src/lib/api.ts` that:

- Detects 401 Unauthorized responses
- Automatically clears the expired token from localStorage
- Redirects the user to the login page with the current page as a redirect parameter
- Prevents infinite redirects by checking if already on the login page

```typescript
// Handle auth errors and redirect to login
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Token is expired or invalid, clear it and redirect to login
      localStorage.removeItem('token');
      
      // Only redirect if we're in a browser environment
      if (typeof window !== 'undefined') {
        const currentPath = window.location.pathname + window.location.search;
        // Avoid infinite redirects by checking if we're already on login page
        if (!window.location.pathname.startsWith('/login')) {
          window.location.href = `/login?redirect=${encodeURIComponent(currentPath)}`;
        }
      }
    }
    return Promise.reject(error);
  }
);
```

#### 2. AuthContext Storage Listener

Enhanced the AuthContext in `yapplr-frontend/src/contexts/AuthContext.tsx` to listen for localStorage changes:

- Detects when the token is removed by the API interceptor
- Automatically updates the user state to null when token is cleared
- Ensures the UI reflects the logged-out state immediately

```typescript
// Listen for storage changes to handle token removal by API interceptor
useEffect(() => {
  const handleStorageChange = (e: StorageEvent) => {
    if (e.key === 'token' && e.newValue === null) {
      // Token was removed, clear user state
      setUser(null);
    }
  };

  window.addEventListener('storage', handleStorageChange);
  return () => window.removeEventListener('storage', handleStorageChange);
}, []);
```

### Mobile App (React Native)

The mobile app already has proper token expiration handling implemented:

- Uses an `onUnauthorized` callback in the API client configuration
- Automatically calls `logout()` when 401 responses are received
- Clears tokens and navigates back to the authentication flow

### API Server (.NET)

The API server properly validates JWT tokens and returns 401 status codes when tokens are:

- Expired (past the expiration time)
- Invalid (malformed or using wrong signing key)
- Missing (for protected endpoints)

JWT configuration in `Yapplr.Api/Program.cs`:
- `ValidateLifetime = true` ensures expired tokens are rejected
- Token expiration is set to 60 minutes by default in `appsettings.json`

## Testing

A test page has been created at `/debug/token-expiration` that allows you to:

1. Test API calls with a valid token
2. Simulate token expiration by setting an invalid token
3. Verify automatic redirection to login page
4. Check token state and clear tokens manually

### How to Test

1. Navigate to `http://localhost:3000/debug/token-expiration`
2. Log in if not already authenticated
3. Click "Test with Valid Token" to verify normal operation
4. Click "Test with Expired Token" to simulate expiration
5. Verify you're automatically redirected to the login page
6. After logging in, verify you're redirected back to the test page

## Benefits

1. **Better User Experience**: Users are automatically redirected to login instead of seeing confusing error messages
2. **Seamless Re-authentication**: After login, users are returned to their original page
3. **Consistent Behavior**: Both web and mobile apps handle token expiration gracefully
4. **Security**: Expired tokens are immediately cleared from storage
5. **No Manual Intervention**: The process is completely automatic

## Implementation Notes

- The solution uses `window.location.href` for redirection to ensure a full page reload and proper state reset
- Browser environment checks prevent SSR issues in Next.js
- The redirect parameter preserves the user's intended destination
- Storage event listeners ensure immediate UI updates when tokens are cleared
- Infinite redirect protection prevents loops when already on the login page

## Future Enhancements

Potential improvements could include:

1. **Token Refresh**: Implement automatic token refresh before expiration
2. **Warning Messages**: Show a warning before tokens expire
3. **Background Refresh**: Refresh tokens silently in the background
4. **Retry Logic**: Automatically retry failed requests after token refresh
