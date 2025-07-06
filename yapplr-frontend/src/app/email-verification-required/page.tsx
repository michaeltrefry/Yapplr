'use client';

import { useState, useEffect, Suspense } from 'react';
import { useSearchParams } from 'next/navigation';
import { authApi } from '@/lib/api';
import Link from 'next/link';

function EmailVerificationRequiredForm() {
  const [email, setEmail] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [isSuccess, setIsSuccess] = useState(false);

  const searchParams = useSearchParams();

  useEffect(() => {
    const urlEmail = searchParams?.get('email');
    if (urlEmail) {
      setEmail(urlEmail);
    }
  }, [searchParams]);

  const handleResendVerification = async () => {
    if (!email.trim()) {
      setError('Please enter your email address');
      return;
    }

    setIsLoading(true);
    setError('');
    setMessage('');

    try {
      const result = await authApi.resendVerification(email);
      setMessage(result.message);
      setIsSuccess(true);
    } catch (err: unknown) {
      setError((err as { response?: { data?: { message?: string } } })?.response?.data?.message || 'Failed to resend verification email');
      setIsSuccess(false);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-bold text-gray-900">
            Email Verification Required
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            You need to verify your email address before you can log in
          </p>
        </div>

        <div className="bg-yellow-50 border border-yellow-200 rounded-md p-4">
          <div className="flex">
            <div className="flex-shrink-0">
              <svg className="h-5 w-5 text-yellow-400" viewBox="0 0 20 20" fill="currentColor">
                <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
              </svg>
            </div>
            <div className="ml-3">
              <h3 className="text-sm font-medium text-yellow-800">
                Account Not Verified
              </h3>
              <div className="mt-2 text-sm text-yellow-700">
                <p>Your account was created successfully, but you haven&apos;t verified your email address yet. Please check your email for a verification code or request a new one below.</p>
              </div>
            </div>
          </div>
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
                  Verification Email Sent!
                </h3>
                <div className="mt-2 text-sm text-green-700">
                  <p>{message}</p>
                  <p className="mt-2">
                    <Link
                      href="/verify-email"
                      className="font-medium text-green-600 hover:text-green-500"
                    >
                      Go to verification page →
                    </Link>
                  </p>
                </div>
              </div>
            </div>
          </div>
        ) : (
          <form className="mt-8 space-y-6" onSubmit={(e) => { e.preventDefault(); handleResendVerification(); }}>
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
                      Error
                    </h3>
                    <div className="mt-2 text-sm text-red-700">
                      <p>{error}</p>
                    </div>
                  </div>
                </div>
              </div>
            )}

            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-700">
                Email address
              </label>
              <input
                id="email"
                name="email"
                type="email"
                autoComplete="email"
                required
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-blue-500 focus:border-blue-500 focus:z-10 sm:text-sm"
                placeholder="Enter your email address"
              />
            </div>

            <div>
              <button
                type="submit"
                disabled={isLoading}
                className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isLoading ? 'Sending...' : 'Resend Verification Email'}
              </button>
            </div>
          </form>
        )}

        <div className="text-center space-y-2">
          <Link
            href="/verify-email"
            className="block font-medium text-blue-600 hover:text-blue-500"
          >
            Already have a verification code? Verify email
          </Link>
          <Link
            href="/login"
            className="block font-medium text-blue-600 hover:text-blue-500"
          >
            Back to login
          </Link>
        </div>
      </div>
    </div>
  );
}

export default function EmailVerificationRequiredPage() {
  return (
    <Suspense fallback={<div>Loading...</div>}>
      <EmailVerificationRequiredForm />
    </Suspense>
  );
}
