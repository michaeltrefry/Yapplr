using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public class ContentSeedService
{
    private readonly YapplrDbContext _context;
    private readonly ILogger<ContentSeedService> _logger;

    public ContentSeedService(YapplrDbContext context, ILogger<ContentSeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedContentPagesAsync()
    {
        _logger.LogInformation("🌱 Starting content pages seeding...");

        try
        {
            // Get or create system user for content creation
            var systemUser = await GetOrCreateSystemUserAsync();

            // Seed Terms of Service
            await SeedTermsOfServiceAsync(systemUser.Id);

            // Seed Privacy Policy
            await SeedPrivacyPolicyAsync(systemUser.Id);

            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Content pages seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during content pages seeding");
            throw;
        }
    }

    private async Task<User> GetOrCreateSystemUserAsync()
    {
        var systemUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Role == UserRole.System);

        if (systemUser == null)
        {
            systemUser = new User
            {
                Username = "system",
                Email = "system@yapplr.com",
                PasswordHash = "system", // Not used for system user
                Role = UserRole.System,
                Status = UserStatus.Active,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow
            };

            _context.Users.Add(systemUser);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Created system user for content seeding");
        }

        return systemUser;
    }

    private async Task SeedTermsOfServiceAsync(int systemUserId)
    {
        var existingPage = await _context.ContentPages
            .FirstOrDefaultAsync(cp => cp.Type == ContentPageType.TermsOfService);

        if (existingPage != null)
        {
            _logger.LogInformation("Terms of Service page already exists, skipping...");
            return;
        }

        var termsPage = new ContentPage
        {
            Title = "Terms of Service",
            Slug = "terms",
            Type = ContentPageType.TermsOfService,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ContentPages.Add(termsPage);
        await _context.SaveChangesAsync();

        var termsContent = GetTermsOfServiceContent();
        var termsVersion = new ContentPageVersion
        {
            ContentPageId = termsPage.Id,
            Content = termsContent,
            ChangeNotes = "Initial version",
            VersionNumber = 1,
            IsPublished = true,
            PublishedAt = DateTime.UtcNow,
            PublishedByUserId = systemUserId,
            CreatedByUserId = systemUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ContentPageVersions.Add(termsVersion);
        await _context.SaveChangesAsync();

        // Update the published version reference
        termsPage.PublishedVersionId = termsVersion.Id;
        termsPage.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Created Terms of Service page with initial content");
    }

    private async Task SeedPrivacyPolicyAsync(int systemUserId)
    {
        var existingPage = await _context.ContentPages
            .FirstOrDefaultAsync(cp => cp.Type == ContentPageType.PrivacyPolicy);

        if (existingPage != null)
        {
            _logger.LogInformation("Privacy Policy page already exists, skipping...");
            return;
        }

        var privacyPage = new ContentPage
        {
            Title = "Privacy Policy",
            Slug = "privacy",
            Type = ContentPageType.PrivacyPolicy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ContentPages.Add(privacyPage);
        await _context.SaveChangesAsync();

        var privacyContent = GetPrivacyPolicyContent();
        var privacyVersion = new ContentPageVersion
        {
            ContentPageId = privacyPage.Id,
            Content = privacyContent,
            ChangeNotes = "Initial version",
            VersionNumber = 1,
            IsPublished = true,
            PublishedAt = DateTime.UtcNow,
            PublishedByUserId = systemUserId,
            CreatedByUserId = systemUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.ContentPageVersions.Add(privacyVersion);
        await _context.SaveChangesAsync();

        // Update the published version reference
        privacyPage.PublishedVersionId = privacyVersion.Id;
        privacyPage.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Created Privacy Policy page with initial content");
    }

    private string GetTermsOfServiceContent()
    {
        return @"# Terms of Service

## 1. Acceptance of Terms

By creating an account or using Yapplr, you agree to be bound by these Terms of Service and our Privacy Policy. If you do not agree to these terms, please do not use our service.

## 2. Description of Service

Yapplr is a social media platform that allows users to share short messages (""yaps""), follow other users, and engage with content through likes, comments, and reposts.

## 3. User Accounts

To use Yapplr, you must:

- Be at least 13 years old
- Provide accurate and complete information
- Maintain the security of your account credentials
- Verify your email address
- Accept responsibility for all activity under your account

## 4. Content Guidelines

You are responsible for the content you post. You agree not to post content that:

- Is illegal, harmful, threatening, or abusive
- Harasses, bullies, or intimidates others
- Contains hate speech or discriminatory language
- Violates intellectual property rights
- Contains spam, malware, or phishing attempts
- Impersonates another person or entity
- Contains explicit sexual content involving minors

## 5. Privacy and Content Visibility

Yapplr offers three privacy levels for your content:

- **Public:** Visible to everyone on the platform
- **Followers:** Visible only to your approved followers
- **Private:** Visible only to you

You are responsible for setting appropriate privacy levels for your content.

## 6. Intellectual Property

You retain ownership of content you create and post on Yapplr. By posting content, you grant Yapplr a non-exclusive, royalty-free license to use, display, and distribute your content on the platform.

## 7. Prohibited Activities

You agree not to:

- Use automated tools to access or interact with the service
- Attempt to gain unauthorized access to other accounts
- Interfere with the proper functioning of the service
- Create multiple accounts to evade restrictions
- Sell or transfer your account to others

## 8. Moderation and Enforcement

We reserve the right to:

- Remove content that violates these terms
- Suspend or terminate accounts for violations
- Investigate reported content and user behavior
- Cooperate with law enforcement when required

## 9. Disclaimers

Yapplr is provided ""as is"" without warranties of any kind. We do not guarantee uninterrupted service or the accuracy of user-generated content.

## 10. Limitation of Liability

Yapplr shall not be liable for any indirect, incidental, special, or consequential damages arising from your use of the service.

## 11. Changes to Terms

We may update these Terms of Service from time to time. Continued use of the service after changes constitutes acceptance of the new terms.

## 12. Contact Information

If you have questions about these Terms of Service, please contact us at:

Email: legal@yapplr.com";
    }

    private string GetPrivacyPolicyContent()
    {
        return @"# Privacy Policy

## 1. Information We Collect

When you create an account on Yapplr, we collect:

- Email address (required for account creation and verification)
- Username (your unique identifier on the platform)
- Password (stored securely using industry-standard encryption)
- Profile information (bio, pronouns, tagline, birthday - all optional)
- Posts, comments, and other content you create
- Usage data and interactions with other users

## 2. How We Use Your Information

We use your information to:

- Provide and maintain the Yapplr service
- Verify your identity and secure your account
- Enable you to connect and communicate with other users
- Send important account notifications and updates
- Improve our service and develop new features
- Ensure compliance with our Terms of Service

## 3. Information Sharing

We do not sell, trade, or rent your personal information to third parties. We may share your information only in the following circumstances:

- With your explicit consent
- To comply with legal obligations or court orders
- To protect the rights, property, or safety of Yapplr, our users, or others
- In connection with a business transfer or acquisition

## 4. Data Security

We implement appropriate technical and organizational measures to protect your personal information against unauthorized access, alteration, disclosure, or destruction. This includes:

- Encryption of sensitive data in transit and at rest
- Regular security assessments and updates
- Access controls and authentication measures
- Secure hosting infrastructure

## 5. Your Rights

You have the right to:

- Access and update your personal information
- Delete your account and associated data
- Control your privacy settings and content visibility
- Opt out of non-essential communications
- Request a copy of your data

## 6. Cookies and Tracking

We use essential cookies to maintain your login session and provide core functionality. We do not use tracking cookies for advertising purposes.

## 7. Children's Privacy

Yapplr is not intended for children under 13 years of age. We do not knowingly collect personal information from children under 13.

## 8. Changes to This Policy

We may update this Privacy Policy from time to time. We will notify you of any material changes by posting the new policy on this page and updating the ""Last updated"" date.

## 9. Contact Us

If you have any questions about this Privacy Policy, please contact us at:

Email: privacy@yapplr.com";
    }
}
