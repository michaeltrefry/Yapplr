# Test Data Seeding for Non-Production Environments

This document explains the automatic test data seeding that occurs in non-production environments.

## Overview

When the Yapplr API starts in any **non-production** environment (Development, Test, Staging, etc.), it automatically creates test data to provide a realistic dataset for testing and demonstration purposes. This eliminates the need to manually create users and content for testing environments.

**Note**: Seeding is **disabled in Production** for security reasons.

## What Gets Created

### 1. Admin User
- **Username**: `admin`
- **Email**: `admin@yapplr.com`
- **Password**: `P@$$w0rd!`
- **Role**: Admin
- **Status**: Pre-verified email (no verification required)

### 2. Test Users (20 users)
All test users have:
- **Password**: `P@$$w0rd!`
- **Status**: Pre-verified email (no verification required)
- **Realistic profiles**: Names, bios, pronouns, taglines
- **Varied creation dates**: Spread over the last 30 days

#### Test User List
| Username | Email | Name | Pronouns |
|----------|-------|------|----------|
| alice_j | alice_j@example.com | Alice Johnson | she/her |
| bob_smith | bob_smith@example.com | Bob Smith | he/him |
| charlie_b | charlie_b@example.com | Charlie Brown | they/them |
| diana_w | diana_w@example.com | Diana Wilson | she/her |
| ethan_d | ethan_d@example.com | Ethan Davis | he/him |
| fiona_m | fiona_m@example.com | Fiona Miller | she/her |
| george_g | george_g@example.com | George Garcia | he/him |
| hannah_m | hannah_m@example.com | Hannah Martinez | she/her |
| ian_a | ian_a@example.com | Ian Anderson | he/him |
| julia_t | julia_t@example.com | Julia Taylor | she/her |
| kevin_t | kevin_t@example.com | Kevin Thomas | he/him |
| luna_r | luna_r@example.com | Luna Rodriguez | she/her |
| marcus_l | marcus_l@example.com | Marcus Lee | he/him |
| nina_w | nina_w@example.com | Nina White | she/her |
| oscar_c | oscar_c@example.com | Oscar Clark | he/him |
| priya_p | priya_p@example.com | Priya Patel | she/her |
| quinn_t | quinn_t@example.com | Quinn Thompson | they/them |
| rachel_m | rachel_m@example.com | Rachel Moore | she/her |
| sam_j | sam_j@example.com | Sam Jackson | they/them |
| tara_w | tara_w@example.com | Tara Williams | she/her |

### 3. Sample Content
- **30 Posts Total**: Mix of regular and moderation test content
  - **15 Regular Posts**: Safe, realistic content for normal testing
  - **15 Moderation Test Posts**: Content designed to trigger AI moderation
- **30 Follow Relationships**: Random follow connections between users
- **40 Likes**: Distributed across posts from different users
- **20 Comments**: Engaging comments on various posts

#### Moderation Test Content Categories
The seeding includes posts that test various moderation scenarios:
- **NSFW Content**: Adult and sexual content detection
- **Violence**: Aggressive language and violent content
- **Harassment**: Personal attacks and bullying behavior
- **Hate Speech**: Discriminatory and xenophobic content
- **Misinformation**: Conspiracy theories and false health claims
- **Sensitive Content**: Mental health discussions and spoilers
- **Spam**: Promotional and attention-seeking content

## Technical Implementation

### Service: `StagingSeedService`
- **Location**: `Yapplr.Api/Services/StagingSeedService.cs`
- **Registration**: Only registered in Staging environment
- **Execution**: Runs automatically after database migrations and system tag seeding

### Environment Detection
```csharp
if (builder.Environment.IsEnvironment("Staging"))
{
    builder.Services.AddScoped<StagingSeedService>();
}
```

### Seeding Process
1. **Check Admin User**: If admin user exists, skip all seeding
2. **Check Existing Data**: If any users exist, skip seeding
3. **Run Migrations**: Database schema is created/updated
4. **Seed System Tags**: Default system tags are created
5. **Create Admin User**: Single admin with full permissions
6. **Create Test Users**: 20 users with realistic profiles
7. **Generate Content**: Posts, follows, likes, and comments
8. **Save Changes**: Commits all data to database

### Password Hashing
All passwords are properly hashed using BCrypt before storage:
```csharp
PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@$$w0rd!")
```

### Email Verification Bypass
All users are created with `EmailVerified = true` to skip the email verification process in staging.

## Usage

### Automatic Seeding
Seeding happens automatically when:
1. The application starts in any non-production environment (Development, Test, Staging, etc.)
2. Database migrations complete successfully
3. No admin user exists in the database
4. No other users exist in the database

**Note**: If an admin user already exists, seeding is completely skipped to avoid conflicts.

### Manual Testing
After deployment, you can immediately:
1. **Login as Admin**: Use `admin@yapplr.com` / `P@$$w0rd!`
2. **Test User Features**: Login as any test user with `P@$$w0rd!`
3. **Explore Content**: View posts, likes, comments, and follows
4. **Test Moderation**: Use admin account to moderate content

### AI Moderation Testing
The seeded content includes specific posts designed to test the AI moderation system:

#### **Admin Dashboard Testing**
1. **Login as Admin**: `admin@yapplr.com` / `P@$$w0rd!`
2. **Navigate to Content Queue**: Check for flagged posts
3. **Review AI Suggestions**: See AI-generated tag recommendations
4. **Test Approval Workflow**: Approve or reject AI suggestions
5. **Monitor System Performance**: Check moderation effectiveness

#### **Expected Moderation Results**
The test posts should trigger various AI responses:
- **High Risk Posts**: NSFW, violence, hate speech content
- **Medium Risk Posts**: Harassment, misinformation content
- **Low Risk Posts**: Sensitive topics, spoilers
- **Tag Suggestions**: ContentWarning and Violation tags
- **Risk Scores**: Range from MINIMAL to HIGH

#### **Testing Scenarios**
- **Auto-flagging**: Posts should appear in moderation queue
- **Tag Application**: AI should suggest appropriate system tags
- **Risk Assessment**: Different risk levels for different content types
- **Admin Override**: Test manual approval/rejection of AI decisions

### Deployment Integration
The seeding is integrated into the staging deployment process:
- **Docker Compose**: `ASPNETCORE_ENVIRONMENT=Staging` triggers seeding
- **Deploy Script**: `./deploy-stage.sh` automatically includes seeding
- **Health Checks**: Seeding completes before application becomes ready

## Environment Isolation

### Non-Production Only
- Seeding **ONLY** runs in non-production environments (Development, Test, Staging, etc.)
- Production environment is completely protected
- No risk of test data appearing in production

### Smart Seeding
- Checks for existing admin user before seeding
- Skips seeding if data already exists
- Safe to run multiple times without conflicts
- Perfect for development and testing scenarios

## Security Considerations

### Test Passwords
- All test accounts use the same password: `P@$$w0rd!`
- **Never use these credentials in production**
- Passwords are properly hashed even in staging

### Email Domains
- Test users use `@example.com` domain
- Admin uses `@yapplr.com` domain
- No real email addresses are used

### Data Cleanup
- Staging data should be considered temporary
- Regular staging environment resets are recommended
- No sensitive or real user data is included

## Troubleshooting

### Seeding Not Running
1. Check environment is set to "Staging"
2. Verify `StagingSeedService` is registered
3. Check application startup logs for seeding messages

### Duplicate Data Errors
- Seeding skips if users already exist
- Clear database to force fresh seeding
- Check for unique constraint violations

### Login Issues
- Verify email verification is bypassed (`EmailVerified = true`)
- Confirm password hashing is working correctly
- Check user role assignments

## Logs

Look for these log messages during startup:
```
🌱 Starting staging data seeding...
👑 Creating admin user...
👥 Creating 20 test users...
📝 Creating sample posts and interactions...
✅ Staging data seeding completed successfully!
```
