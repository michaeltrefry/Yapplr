'use client';

import { useState, useEffect, useCallback, Suspense } from 'react';
import { useSearchParams, useRouter } from 'next/navigation';
import { authApi } from '@/lib/api';
import Link from 'next/link';

function VerifyEmailForm() {
  const [token, setToken] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [isSuccess, setIsSuccess] = useState(false);

  const searchParams = useSearchParams();
  const router = useRouter();

  const handleVerification = useCallback(async (verificationToken: string) => {
    setIsLoading(true);
    setError('');
    setMessage('');

    try {
      const result = await authApi.verifyEmail(verificationToken);
      setMessage(result.message);
      setIsSuccess(true);

      // Redirect to login after successful verification
      setTimeout(() => {
        router.push('/login?message=Email verified successfully! You can now log in.');
      }, 3000);
    } catch (err: unknown) {
      setError((err as { response?: { data?: { message?: string } } })?.response?.data?.message || 'Verification failed');
      setIsSuccess(false);
    } finally {
      setIsLoading(false);
    }
  }, [router]);

  useEffect(() => {
    const urlToken = searchParams?.get('token');
    if (urlToken) {
      setToken(urlToken);
      handleVerification(urlToken);
    }
  }, [searchParams, handleVerification]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token.trim()) {
      setError('Please enter a verification code');
      return;
    }
    await handleVerification(token);
  };

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-bold text-gray-900">
            Verify Your Email
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            Enter the 6-digit code sent to your email address
          </p>
        </div>

        {isSuccess ? (
          <div className="bg-green-50 border border-green-200 rounded-md p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-green-400" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clipRule="evenodd" />
                </svg>
              </div>
              <div className="ml-3">
                <h3 className="text-sm font-medium text-green-800">
                  Email Verified Successfully!
                </h3>
                <div className="mt-2 text-sm text-green-700">
                  <p>{message}</p>
                  <p className="mt-1">Redirecting to login page...</p>
                </div>
              </div>
            </div>
          </div>
        ) : (
          <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
            {error && (
              <div className="bg-red-50 border border-red-200 rounded-md p-4">
                <div className="flex">
                  <div className="flex-shrink-0">
                    <svg className="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                      <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                    </svg>
                  </div>
                  <div className="ml-3">
                    <h3 className="text-sm font-medium text-red-800">
                      Verification Failed
                    </h3>
                    <div className="mt-2 text-sm text-red-700">
                      <p>{error}</p>
                    </div>
                  </div>
                </div>
              </div>
            )}

            <div>
              <label htmlFor="token" className="block text-sm font-medium text-gray-700">
                Verification Code
              </label>
              <input
                id="token"
                name="token"
                type="text"
                required
                value={token}
                onChange={(e) => setToken(e.target.value)}
                className="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm"
                placeholder="Enter 6-digit code"
                maxLength={6}
                pattern="[0-9]{6}"
              />
            </div>

            <div>
              <button
                type="submit"
                disabled={isLoading}
                className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isLoading ? 'Verifying...' : 'Verify Email'}
              </button>
            </div>

            <div className="text-center">
              <Link
                href="/resend-verification"
                className="font-medium text-blue-600 hover:text-blue-500"
              >
                Didn&apos;t receive the code? Resend verification email
              </Link>
            </div>
          </form>
        )}

        <div className="text-center">
          <Link
            href="/login"
            className="font-medium text-blue-600 hover:text-blue-500"
          >
            Back to login
          </Link>
        </div>
      </div>
    </div>
  );
}

export default function VerifyEmailPage() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <VerifyEmailForm />
    </Suspense>
  );
}
