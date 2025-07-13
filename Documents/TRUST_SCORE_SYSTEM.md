# User Trust Score System

The Yapplr platform features an advanced user trust score system that provides intelligent, behavior-based moderation and content management. This system automatically calculates trust scores for all users based on their activity, behavior, and community standing.

## Overview

The trust score system is designed to:
- **Promote positive community behavior** by rewarding good actors
- **Automatically moderate problematic content** without manual intervention
- **Provide fair and transparent scoring** based on objective criteria
- **Scale moderation efforts** efficiently across the platform
- **Protect users** from spam, harassment, and low-quality content

## Trust Score Calculation

### Score Range
- **Range**: 0.0 to 1.0 (floating point)
- **Default**: New users start with 1.0 trust score
- **Updates**: Real-time updates based on user actions
- **Persistence**: Scores are stored in the database with full audit trail

### Positive Factors (Increase Trust)

#### Profile Completeness (+0.1 to +0.3)
- **Email Verification**: +0.1 (required for account security)
- **Profile Image**: +0.1 (shows user investment in their profile)
- **Bio Completion**: +0.1 (provides context about the user)

#### Positive Activity (+0.05 to +0.15 per action)
- **Creating Posts**: +0.05 (contributing content to the platform)
- **Creating Comments**: +0.02 (engaging in discussions)
- **Receiving Likes**: +0.01 (content appreciated by community)
- **Receiving Follows**: +0.02 (trusted by other users)

#### Community Standing (+0.02 to +0.1)
- **Account Age**: +0.05 to +0.2 (established accounts are more trustworthy)
- **Consistent Activity**: +0.02 to +0.1 (regular engagement shows genuine use)
- **Positive Interactions**: +0.01 to +0.05 (helpful comments, quality posts)

### Negative Factors (Decrease Trust)

#### Moderation Actions (-0.1 to -0.5)
- **Content Hidden**: -0.1 (content violated community guidelines)
- **User Suspended**: -0.3 (serious policy violation)
- **User Banned**: -0.5 (severe or repeated violations)
- **Multiple Violations**: Exponential penalty increase

#### Reported Content (-0.05 to -0.2)
- **Post Reported**: -0.05 (community flagged content as problematic)
- **Comment Reported**: -0.02 (inappropriate comment behavior)
- **Multiple Reports**: Cumulative penalty based on report frequency

#### Spam Behavior (-0.1 to -0.3)
- **Excessive Posting**: -0.1 (posting too frequently)
- **Repetitive Content**: -0.2 (copy-paste or duplicate content)
- **Bot-like Behavior**: -0.3 (automated or scripted activity)

#### Inactivity Decay (-0.01 per week)
- **Gradual Reduction**: Trust scores slowly decrease for inactive accounts
- **Prevents Abuse**: Stops dormant accounts from maintaining high trust
- **Encourages Engagement**: Motivates users to stay active

## Trust-Based Features

### Content Visibility Levels

#### Hidden (< 0.1)
- Content completely hidden from all feeds and searches
- User cannot create new posts or comments
- Existing content becomes invisible to other users
- Requires manual admin review to restore access

#### Limited (0.1-0.3)
- Content requires user action to view (click to expand)
- Reduced visibility in feeds and search results
- Warning messages displayed to viewers
- Limited engagement capabilities

#### Reduced (0.3-0.5)
- Lower priority in algorithmic feeds
- Reduced reach and engagement opportunities
- Content appears with subtle visual indicators
- Normal functionality but limited amplification

#### Normal (0.5-0.8)
- Standard visibility and engagement
- Full platform functionality available
- No restrictions on content creation or interaction
- Typical user experience

#### Full (0.8+)
- Maximum visibility and engagement opportunities
- Priority placement in feeds and recommendations
- Enhanced reach and amplification
- Trusted user benefits and features

### Action Thresholds

#### Content Creation
- **Create Posts**: 0.1 minimum trust score
- **Create Comments**: 0.1 minimum trust score
- **Upload Images**: 0.1 minimum trust score
- **Upload Videos**: 0.2 minimum trust score

#### Social Interactions
- **Like Content**: 0.05 minimum trust score
- **Follow Users**: 0.1 minimum trust score
- **Send Messages**: 0.3 minimum trust score
- **Share Content**: 0.2 minimum trust score

#### Community Actions
- **Report Content**: 0.2 minimum trust score
- **Submit Appeals**: 0.1 minimum trust score
- **Participate in Polls**: 0.1 minimum trust score

### Rate Limiting Multipliers

#### Very Low Trust (< 0.2)
- **Rate Limit**: 0.25x normal limits
- **Posts per hour**: 2 (vs 8 normal)
- **Comments per hour**: 5 (vs 20 normal)
- **Messages per hour**: 1 (vs 4 normal)

#### Low Trust (0.2-0.4)
- **Rate Limit**: 0.5x normal limits
- **Posts per hour**: 4 (vs 8 normal)
- **Comments per hour**: 10 (vs 20 normal)
- **Messages per hour**: 2 (vs 4 normal)

#### Medium Trust (0.4-0.6)
- **Rate Limit**: 1.0x normal limits (baseline)
- **Posts per hour**: 8
- **Comments per hour**: 20
- **Messages per hour**: 4

#### High Trust (0.6-0.8)
- **Rate Limit**: 1.5x normal limits
- **Posts per hour**: 12 (vs 8 normal)
- **Comments per hour**: 30 (vs 20 normal)
- **Messages per hour**: 6 (vs 4 normal)

#### Very High Trust (0.8+)
- **Rate Limit**: 2.0x normal limits
- **Posts per hour**: 16 (vs 8 normal)
- **Comments per hour**: 40 (vs 20 normal)
- **Messages per hour**: 8 (vs 4 normal)

## Technical Implementation

### Background Service
- **Automated Recalculation**: Runs every 60 minutes by default
- **Inactivity Decay**: Applied weekly to inactive accounts
- **Performance Optimized**: Efficient database queries with proper indexing
- **Error Handling**: Safe defaults prevent user blocking on system errors

### Database Schema
- **UserTrustScoreHistory**: Complete audit trail of all score changes
- **TrustScore**: Current score stored in Users table
- **Metadata**: Reason, details, and timestamp for each change
- **Indexing**: Optimized for fast queries and reporting

### API Integration
- **Real-time Updates**: Trust scores update immediately on user actions
- **Service Integration**: Integrated with PostService, UserService, AdminService
- **Admin Override**: Administrators can manually adjust scores with reasons
- **Analytics**: Comprehensive reporting and statistics

### Configuration Options
```json
{
  "TrustScore": {
    "EnableBackgroundService": true,
    "RecalculationIntervalMinutes": 60,
    "InactivityDecayDays": 7,
    "DefaultNewUserScore": 1.0,
    "MinimumActionThresholds": {
      "CreatePost": 0.1,
      "CreateComment": 0.1,
      "LikeContent": 0.05,
      "ReportContent": 0.2,
      "SendMessage": 0.3
    },
    "RateLimitMultipliers": {
      "VeryLowTrust": 0.25,
      "LowTrust": 0.5,
      "MediumTrust": 1.0,
      "HighTrust": 1.5,
      "VeryHighTrust": 2.0
    }
  }
}
```

## Admin Features

### Trust Score Dashboard
- **Platform Statistics**: Total users, average scores, distribution
- **Trend Analysis**: Score changes over time
- **Risk Assessment**: Users requiring attention
- **Performance Metrics**: System effectiveness

### User Management
- **Individual Scores**: View any user's current trust score
- **Score History**: Complete audit trail of changes
- **Factor Breakdown**: Detailed analysis of score components
- **Manual Adjustments**: Override scores with reason tracking

### Moderation Integration
- **Priority Scoring**: 1-5 levels for moderation queue
- **Automatic Actions**: Content hiding and rate limiting
- **Appeal System**: Users can appeal trust score penalties
- **Bulk Operations**: Efficient management of multiple users

## Security and Privacy

### Data Protection
- **Transparent Scoring**: Users can understand their scores
- **Appeal Process**: Fair mechanism for score disputes
- **Audit Trail**: Complete history of all changes
- **Privacy Respect**: Scores don't reveal personal information

### Abuse Prevention
- **Gaming Protection**: Multiple factors prevent score manipulation
- **Sybil Resistance**: New account penalties and verification requirements
- **Coordinated Attacks**: Detection of organized abuse attempts
- **Admin Oversight**: Human review for significant score changes

## Monitoring and Analytics

### Key Metrics
- **Score Distribution**: Platform-wide trust score statistics
- **Effectiveness**: Reduction in moderation workload
- **User Satisfaction**: Appeal rates and resolution times
- **System Performance**: Processing times and error rates

### Reporting
- **Daily Reports**: Score changes and system activity
- **Weekly Trends**: Platform health and user behavior
- **Monthly Analysis**: Long-term effectiveness and improvements
- **Alert System**: Notifications for unusual patterns or issues

## Future Enhancements

### Planned Features
- **Machine Learning**: AI-powered score prediction and optimization
- **Community Voting**: Peer review system for content quality
- **Reputation Badges**: Visual indicators of user trustworthiness
- **Cross-Platform Sync**: Trust scores across multiple services

### Research Areas
- **Behavioral Analysis**: Advanced pattern recognition
- **Social Graph**: Network effects on trust propagation
- **Temporal Dynamics**: Time-based score adjustments
- **Cultural Sensitivity**: Localized scoring criteria
