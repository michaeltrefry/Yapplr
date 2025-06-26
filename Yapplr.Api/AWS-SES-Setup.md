# AWS SES Setup Guide for Yapplr

This guide will help you set up Amazon Simple Email Service (SES) for sending password reset emails in your Yapplr application.

## Prerequisites

- AWS Account
- Domain name (recommended for production)
- AWS CLI installed (optional but recommended)

## Step 1: AWS SES Console Setup

### 1.1 Access AWS SES
1. Log into AWS Console
2. Navigate to **Simple Email Service (SES)**
3. Select your preferred region (e.g., `us-east-1`)

### 1.2 Verify Email Address or Domain

#### For Development (Email Verification):
1. Go to **Verified identities**
2. Click **Create identity**
3. Select **Email address**
4. Enter your email address (e.g., `noreply@yourdomain.com`)
5. Click **Create identity**
6. Check your email and click the verification link

#### For Production (Domain Verification):
1. Go to **Verified identities**
2. Click **Create identity**
3. Select **Domain**
4. Enter your domain (e.g., `yourdomain.com`)
5. Choose **Easy DKIM** (recommended)
6. Click **Create identity**
7. Add the provided DNS records to your domain

### 1.3 Request Production Access (Important!)
By default, SES is in **sandbox mode** and can only send to verified addresses.

1. Go to **Account dashboard**
2. Click **Request production access**
3. Fill out the form:
   - **Mail type**: Transactional
   - **Website URL**: Your application URL
   - **Use case description**: "Password reset emails for social media application"
   - **Additional contact addresses**: Your support email
4. Submit the request (usually approved within 24 hours)

## Step 2: Create IAM User for SES

### 2.1 Create IAM Policy
1. Go to **IAM Console**
2. Click **Policies** → **Create policy**
3. Use JSON editor and paste:

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Effect": "Allow",
            "Action": [
                "ses:SendEmail",
                "ses:SendRawEmail"
            ],
            "Resource": "*"
        }
    ]
}
```

4. Name it `YapplrSESPolicy`
5. Click **Create policy**

### 2.2 Create IAM User
1. Go to **Users** → **Create user**
2. Username: `yapplr-ses-user`
3. Select **Programmatic access**
4. Attach the `YapplrSESPolicy`
5. Click **Create user**
6. **Save the Access Key ID and Secret Access Key**

## Step 3: Configure Yapplr Application

### 3.1 Update appsettings.json

```json
{
  "AwsSesSettings": {
    "Region": "us-east-1",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "Yapplr",
    "AccessKey": "YOUR_ACCESS_KEY_ID",
    "SecretKey": "YOUR_SECRET_ACCESS_KEY"
  },
  "EmailProvider": "AwsSes"
}
```

### 3.2 Environment Variables (Recommended for Production)

Instead of storing credentials in appsettings.json, use environment variables:

```bash
export AWS_ACCESS_KEY_ID=your_access_key_id
export AWS_SECRET_ACCESS_KEY=your_secret_access_key
export AWS_DEFAULT_REGION=us-east-1
```

Or use appsettings.Production.json:

```json
{
  "AwsSesSettings": {
    "Region": "us-east-1",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "Yapplr"
  }
}
```

## Step 4: Testing

### 4.1 Test Email Sending
```bash
curl -X POST "http://localhost:5161/api/auth/forgot-password" \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com"}'
```

### 4.2 Check SES Metrics
1. Go to SES Console
2. Click **Reputation metrics**
3. Monitor bounce and complaint rates

## Step 5: Production Considerations

### 5.1 Domain Authentication
- Set up SPF record: `"v=spf1 include:amazonses.com ~all"`
- Configure DKIM (automatically done with Easy DKIM)
- Set up DMARC policy

### 5.2 Monitoring
- Set up CloudWatch alarms for bounce/complaint rates
- Monitor sending quotas
- Set up SNS notifications for bounces/complaints

### 5.3 Security
- Use IAM roles instead of access keys when possible
- Rotate access keys regularly
- Use least privilege principle

## Troubleshooting

### Common Issues

#### 1. "Email address not verified"
- Verify your sender email address in SES console
- Check spam folder for verification email

#### 2. "Account is in sandbox mode"
- Request production access in SES console
- Wait for approval (usually 24 hours)

#### 3. "Access denied"
- Check IAM permissions
- Verify access keys are correct
- Ensure region matches

#### 4. "Daily sending quota exceeded"
- Check sending limits in SES console
- Request quota increase if needed

### Logs and Debugging
Check application logs for detailed error messages:
```bash
dotnet run --environment Development
```

## Cost Estimation

AWS SES Pricing (as of 2025):
- First 62,000 emails per month: $0.10 per 1,000 emails
- Additional emails: $0.10 per 1,000 emails
- No monthly fees

For a typical social media app:
- 1,000 password resets/month: ~$0.10
- 10,000 password resets/month: ~$1.00

## Support

- AWS SES Documentation: https://docs.aws.amazon.com/ses/
- AWS Support: Available through AWS Console
- Yapplr Support: Check application logs and error messages
