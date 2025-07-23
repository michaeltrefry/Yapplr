import React from 'react';
import { render, screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import SubscriptionSystemGuard from '../SubscriptionSystemGuard';
import { subscriptionApi } from '@/lib/api';

// Mock the API
jest.mock('@/lib/api', () => ({
  subscriptionApi: {
    getActiveSubscriptionTiers: jest.fn(),
  },
}));

// Mock Next.js Link component
jest.mock('next/link', () => {
  return function MockLink({ children, href }: { children: React.ReactNode; href: string }) {
    return <a href={href}>{children}</a>;
  };
});

const mockSubscriptionApi = subscriptionApi as jest.Mocked<typeof subscriptionApi>;

describe('SubscriptionSystemGuard', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('shows loading state initially', () => {
    mockSubscriptionApi.getActiveSubscriptionTiers.mockImplementation(
      () => new Promise(() => {}) // Never resolves
    );

    render(
      <SubscriptionSystemGuard>
        <div>Protected Content</div>
      </SubscriptionSystemGuard>
    );

    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  it('renders children when subscription system is enabled', async () => {
    mockSubscriptionApi.getActiveSubscriptionTiers.mockResolvedValue([
      {
        id: 1,
        name: 'Free',
        description: 'Free tier',
        price: 0,
        currency: 'USD',
        billingCycleMonths: 1,
        isActive: true,
        isDefault: true,
        sortOrder: 0,
        showAdvertisements: true,
        hasVerifiedBadge: false,
        createdAt: '2023-01-01T00:00:00Z',
        updatedAt: '2023-01-01T00:00:00Z',
      },
    ]);

    render(
      <SubscriptionSystemGuard>
        <div>Protected Content</div>
      </SubscriptionSystemGuard>
    );

    await waitFor(() => {
      expect(screen.getByText('Protected Content')).toBeInTheDocument();
    });
  });

  it('shows unavailable message when subscription system is disabled (404 error)', async () => {
    const error = new Error('Not Found');
    (error as any).response = { status: 404 };
    mockSubscriptionApi.getActiveSubscriptionTiers.mockRejectedValue(error);

    render(
      <SubscriptionSystemGuard>
        <div>Protected Content</div>
      </SubscriptionSystemGuard>
    );

    await waitFor(() => {
      expect(screen.getByText('Subscriptions Unavailable')).toBeInTheDocument();
    });

    expect(screen.getByText(/The subscription system is currently disabled/)).toBeInTheDocument();
    expect(screen.getByText('This feature is temporarily unavailable')).toBeInTheDocument();
  });

  it('renders children for non-404 errors (network, auth, etc.)', async () => {
    const error = new Error('Network Error');
    (error as any).response = { status: 500 };
    mockSubscriptionApi.getActiveSubscriptionTiers.mockRejectedValue(error);

    render(
      <SubscriptionSystemGuard>
        <div>Protected Content</div>
      </SubscriptionSystemGuard>
    );

    await waitFor(() => {
      expect(screen.getByText('Protected Content')).toBeInTheDocument();
    });
  });

  it('renders children for errors without response status', async () => {
    const error = new Error('Network Error');
    mockSubscriptionApi.getActiveSubscriptionTiers.mockRejectedValue(error);

    render(
      <SubscriptionSystemGuard>
        <div>Protected Content</div>
      </SubscriptionSystemGuard>
    );

    await waitFor(() => {
      expect(screen.getByText('Protected Content')).toBeInTheDocument();
    });
  });

  it('uses custom fallback path and text', async () => {
    const error = new Error('Not Found');
    (error as any).response = { status: 404 };
    mockSubscriptionApi.getActiveSubscriptionTiers.mockRejectedValue(error);

    render(
      <SubscriptionSystemGuard fallbackPath="/custom" fallbackText="Custom Back">
        <div>Protected Content</div>
      </SubscriptionSystemGuard>
    );

    await waitFor(() => {
      expect(screen.getByText('Subscriptions Unavailable')).toBeInTheDocument();
    });

    const backLink = screen.getByText('Custom Back');
    expect(backLink).toBeInTheDocument();
    expect(backLink.closest('a')).toHaveAttribute('href', '/custom');
  });

  it('uses default fallback path and text when not provided', async () => {
    const error = new Error('Not Found');
    (error as any).response = { status: 404 };
    mockSubscriptionApi.getActiveSubscriptionTiers.mockRejectedValue(error);

    render(
      <SubscriptionSystemGuard>
        <div>Protected Content</div>
      </SubscriptionSystemGuard>
    );

    await waitFor(() => {
      expect(screen.getByText('Subscriptions Unavailable')).toBeInTheDocument();
    });

    const backLink = screen.getByText('Back to Settings');
    expect(backLink).toBeInTheDocument();
    expect(backLink.closest('a')).toHaveAttribute('href', '/settings');
  });

  it('displays warning icon and message in unavailable state', async () => {
    const error = new Error('Not Found');
    (error as any).response = { status: 404 };
    mockSubscriptionApi.getActiveSubscriptionTiers.mockRejectedValue(error);

    render(
      <SubscriptionSystemGuard>
        <div>Protected Content</div>
      </SubscriptionSystemGuard>
    );

    await waitFor(() => {
      expect(screen.getByText('Subscriptions Unavailable')).toBeInTheDocument();
    });

    // Check for the warning message
    expect(screen.getByText('This feature is temporarily unavailable')).toBeInTheDocument();
    
    // Check for the main description
    expect(screen.getByText(/The subscription system is currently disabled/)).toBeInTheDocument();
  });

  it('handles multiple renders correctly', async () => {
    mockSubscriptionApi.getActiveSubscriptionTiers.mockResolvedValue([]);

    const { rerender } = render(
      <SubscriptionSystemGuard>
        <div>Content 1</div>
      </SubscriptionSystemGuard>
    );

    await waitFor(() => {
      expect(screen.getByText('Content 1')).toBeInTheDocument();
    });

    rerender(
      <SubscriptionSystemGuard>
        <div>Content 2</div>
      </SubscriptionSystemGuard>
    );

    await waitFor(() => {
      expect(screen.getByText('Content 2')).toBeInTheDocument();
    });

    // API should be called for each render
    expect(mockSubscriptionApi.getActiveSubscriptionTiers).toHaveBeenCalledTimes(2);
  });
});
