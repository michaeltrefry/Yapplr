# Staging Environment Test Data Seeding

This document explains the automatic test data seeding that occurs in the staging environment.

## Overview

When the Yapplr API starts in the **Staging** environment, it automatically creates test data to provide a realistic dataset for testing and demonstration purposes. This eliminates the need to manually create users and content for every staging deployment.

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
- **25 Posts**: Realistic posts with varied content and creation dates
- **30 Follow Relationships**: Random follow connections between users
- **40 Likes**: Distributed across posts from different users
- **20 Comments**: Engaging comments on various posts

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
1. **Check Existing Data**: Skips seeding if users already exist
2. **Create Admin User**: Single admin with full permissions
3. **Create Test Users**: 20 users with realistic profiles
4. **Generate Content**: Posts, follows, likes, and comments
5. **Save Changes**: Commits all data to database

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
1. The application starts in Staging environment
2. The database is empty (no existing users)
3. Database migrations complete successfully

### Manual Testing
After deployment, you can immediately:
1. **Login as Admin**: Use `admin@yapplr.com` / `P@$$w0rd!`
2. **Test User Features**: Login as any test user with `P@$$w0rd!`
3. **Explore Content**: View posts, likes, comments, and follows
4. **Test Moderation**: Use admin account to moderate content

### Deployment Integration
The seeding is integrated into the staging deployment process:
- **Docker Compose**: `ASPNETCORE_ENVIRONMENT=Staging` triggers seeding
- **Deploy Script**: `./deploy-stage.sh` automatically includes seeding
- **Health Checks**: Seeding completes before application becomes ready

## Environment Isolation

### Staging Only
- Seeding **ONLY** runs in Staging environment
- Production and Development environments are unaffected
- No risk of test data appearing in production

### Fresh Deployments
- Each staging deployment can start with fresh data
- Existing data prevents duplicate seeding
- Database can be reset for clean testing

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
