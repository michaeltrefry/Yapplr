'use client';

import Link from 'next/link';

export default function TermsOfServicePage() {
  return (
    <div className="min-h-screen bg-gray-50">
      <div className="max-w-4xl mx-auto py-12 px-4 sm:px-6 lg:px-8">
        <div className="bg-white rounded-lg shadow-sm border border-gray-200 p-8">
          <div className="mb-8">
            <Link
              href="/"
              className="text-blue-600 hover:text-blue-700 font-medium"
            >
              ← Back to Yapplr
            </Link>
          </div>

          <h1 className="text-3xl font-bold text-gray-900 mb-8">Terms of Service</h1>
          
          <div className="prose prose-gray max-w-none">
            <p className="text-sm text-gray-600 mb-6">
              <strong>Last updated:</strong> {new Date().toLocaleDateString()}
            </p>

            <section className="mb-8">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">1. Acceptance of Terms</h2>
              <p className="mb-4">
                By creating an account or using Yapplr, you agree to be bound by these Terms of Service and our Privacy Policy. If you do not agree to these terms, please do not use our service.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">2. Description of Service</h2>
              <p className="mb-4">
                Yapplr is a social media platform that allows users to share short messages ("yaps"), follow other users, and engage with content through likes, comments, and reposts.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">3. User Accounts</h2>
              <p className="mb-4">
                To use Yapplr, you must:
              </p>
              <ul className="list-disc pl-6 mb-4">
                <li>Be at least 13 years old</li>
                <li>Provide accurate and complete information</li>
                <li>Maintain the security of your account credentials</li>
                <li>Verify your email address</li>
                <li>Accept responsibility for all activity under your account</li>
              </ul>
            </section>

            <section className="mb-8">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">4. Content Guidelines</h2>
              <p className="mb-4">
                You are responsible for the content you post. You agree not to post content that:
              </p>
              <ul className="list-disc pl-6 mb-4">
                <li>Is illegal, harmful, threatening, or abusive</li>
                <li>Harasses, bullies, or intimidates others</li>
                <li>Contains hate speech or discriminatory language</li>
                <li>Violates intellectual property rights</li>
                <li>Contains spam, malware, or phishing attempts</li>
                <li>Impersonates another person or entity</li>
                <li>Contains explicit sexual content involving minors</li>
              </ul>
            </section>

            <section className="mb-8">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">5. Privacy and Content Visibility</h2>
              <p className="mb-4">
                Yapplr offers three privacy levels for your content:
              </p>
              <ul className="list-disc pl-6 mb-4">
                <li><strong>Public:</strong> Visible to everyone on the platform</li>
                <li><strong>Followers:</strong> Visible only to your approved followers</li>
                <li><strong>Private:</strong> Visible only to you</li>
              </ul>
              <p className="mb-4">
                You are responsible for setting appropriate privacy levels for your content.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">6. Intellectual Property</h2>
              <p className="mb-4">
                You retain ownership of content you create and post on Yapplr. By posting content, you grant Yapplr a non-exclusive, royalty-free license to use, display, and distribute your content on the platform.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">7. Prohibited Activities</h2>
              <p className="mb-4">
                You agree not to:
              </p>
              <ul className="list-disc pl-6 mb-4">
                <li>Use automated tools to access or interact with the service</li>
                <li>Attempt to gain unauthorized access to other accounts</li>
                <li>Interfere with the proper functioning of the service</li>
                <li>Create multiple accounts to evade restrictions</li>
                <li>Sell or transfer your account to others</li>
              </ul>
            </section>

            <section className="mb-8">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">8. Moderation and Enforcement</h2>
              <p className="mb-4">
                We reserve the right to:
              </p>
              <ul className="list-disc pl-6 mb-4">
                <li>Remove content that violates these terms</li>
                <li>Suspend or terminate accounts for violations</li>
                <li>Investigate reported content and user behavior</li>
                <li>Cooperate with law enforcement when required</li>
              </ul>
            </section>

            <section className="mb-8">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">9. Disclaimers</h2>
              <p className="mb-4">
                Yapplr is provided "as is" without warranties of any kind. We do not guarantee uninterrupted service or the accuracy of user-generated content.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">10. Limitation of Liability</h2>
              <p className="mb-4">
                Yapplr shall not be liable for any indirect, incidental, special, or consequential damages arising from your use of the service.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">11. Changes to Terms</h2>
              <p className="mb-4">
                We may update these Terms of Service from time to time. Continued use of the service after changes constitutes acceptance of the new terms.
              </p>
            </section>

            <section className="mb-8">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">12. Contact Information</h2>
              <p className="mb-4">
                If you have questions about these Terms of Service, please contact us at:
              </p>
              <p className="mb-4">
                Email: legal@yapplr.com
              </p>
            </section>
          </div>
        </div>
      </div>
    </div>
  );
}
