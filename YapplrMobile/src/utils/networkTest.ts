// Network connectivity test utility
export const testNetworkConnectivity = async (baseUrl: string): Promise<boolean> => {
  try {
    console.log('Testing network connectivity to:', baseUrl);
    
    // Simple fetch test to check if the server is reachable
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 5000); // 5 second timeout
    
    const response = await fetch(`${baseUrl}/api/auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ email: 'test', password: 'test' }),
      signal: controller.signal,
    });
    
    clearTimeout(timeoutId);
    
    // We expect a 400 or 401 response, which means the server is reachable
    console.log('Network test response status:', response.status);
    return response.status >= 200 && response.status < 500;
    
  } catch (error) {
    console.error('Network test failed:', error);
    return false;
  }
};

export const testMultipleUrls = async (urls: string[]): Promise<string | null> => {
  for (const url of urls) {
    console.log('Testing URL:', url);
    const isReachable = await testNetworkConnectivity(url);
    if (isReachable) {
      console.log('Successfully connected to:', url);
      return url;
    }
  }
  console.error('All URLs failed connectivity test');
  return null;
};
