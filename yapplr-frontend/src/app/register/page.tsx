'use client';

import { useState, Suspense } from 'react';
import { useAuth } from '@/contexts/AuthContext';

import Link from 'next/link';

function RegisterForm() {
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    username: '',
    bio: '',
    pronouns: '',
    tagline: '',
  });
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [showVerificationMessage, setShowVerificationMessage] = useState(false);
  const [registeredEmail, setRegisteredEmail] = useState('');

  const { register } = useAuth();

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    setFormData(prev => ({
      ...prev,
      [e.target.name]: e.target.value
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError('');

    try {
      await register(formData);
      // After successful registration, show verification message instead of redirecting
      setRegisteredEmail(formData.email);
      setShowVerificationMessage(true);
    } catch (err: unknown) {
      setError((err as { response?: { data?: { message?: string } } })?.response?.data?.message || 'Registration failed');
    } finally {
      setIsLoading(false);
    }
  };

  if (showVerificationMessage) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-md w-full space-y-8">
          <div>
            <h2 className="mt-6 text-center text-3xl font-bold text-gray-900">
              Check Your Email
            </h2>
            <p className="mt-2 text-center text-sm text-gray-600">
              We&apos;ve sent a verification email to <strong>{registeredEmail}</strong>
            </p>
          </div>

          <div className="bg-blue-50 border border-blue-200 rounded-md p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-blue-400" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clipRule="evenodd" />
                </svg>
              </div>
              <div className="ml-3">
                <h3 className="text-sm font-medium text-blue-800">
                  Verify your email to complete registration
                </h3>
                <div className="mt-2 text-sm text-blue-700">
                  <p>Please check your email and click the verification link or enter the 6-digit code to activate your account.</p>
                  <p className="mt-2">You won&apos;t be able to log in until your email is verified.</p>
                </div>
              </div>
            </div>
          </div>

          <div className="text-center space-y-4">
            <Link
              href="/verify-email"
              className="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
            >
              Enter Verification Code
            </Link>

            <Link
              href="/resend-verification"
              className="font-medium text-blue-600 hover:text-blue-500"
            >
              Didn&apos;t receive the email? Resend verification
            </Link>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-bold text-gray-900">
            Join Yapplr
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            Or{' '}
            <Link
              href="/login"
              className="font-medium text-blue-600 hover:text-blue-500"
            >
              sign in to your account
            </Link>
          </p>
        </div>
        <form className="mt-8 space-y-4" onSubmit={handleSubmit}>
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
              className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 bg-white text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
              placeholder="Enter your email"
              value={formData.email}
              onChange={handleChange}
            />
          </div>

          <div>
            <label htmlFor="username" className="block text-sm font-medium text-gray-700">
              Username
            </label>
            <input
              id="username"
              name="username"
              type="text"
              required
              className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 bg-white text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
              placeholder="Choose a username"
              value={formData.username}
              onChange={handleChange}
            />
          </div>

          <div>
            <label htmlFor="password" className="block text-sm font-medium text-gray-700">
              Password
            </label>
            <input
              id="password"
              name="password"
              type="password"
              autoComplete="new-password"
              required
              className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 bg-white text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
              placeholder="Create a password"
              value={formData.password}
              onChange={handleChange}
            />
          </div>

          <div>
            <label htmlFor="bio" className="block text-sm font-medium text-gray-700">
              Bio (optional)
            </label>
            <textarea
              id="bio"
              name="bio"
              rows={3}
              className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 bg-white text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
              placeholder="Tell us about yourself"
              value={formData.bio}
              onChange={handleChange}
            />
          </div>

          <div>
            <label htmlFor="pronouns" className="block text-sm font-medium text-gray-700">
              Pronouns (optional)
            </label>
            <input
              id="pronouns"
              name="pronouns"
              type="text"
              className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 bg-white text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
              placeholder="e.g., they/them, she/her, he/him"
              value={formData.pronouns}
              onChange={handleChange}
            />
          </div>

          <div>
            <label htmlFor="tagline" className="block text-sm font-medium text-gray-700">
              Tagline (optional)
            </label>
            <input
              id="tagline"
              name="tagline"
              type="text"
              className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm placeholder-gray-400 bg-white text-gray-900 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
              placeholder="Your personal tagline"
              value={formData.tagline}
              onChange={handleChange}
            />
          </div>

          {error && (
            <div className="text-red-600 text-sm text-center">{error}</div>
          )}

          <div>
            <button
              type="submit"
              disabled={isLoading}
              className="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isLoading ? 'Creating account...' : 'Create account'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default function RegisterPage() {
  return (
    <Suspense fallback={
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-2 text-gray-600">Loading...</p>
        </div>
      </div>
    }>
      <RegisterForm />
    </Suspense>
  );
}
