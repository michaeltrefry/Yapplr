namespace Yapplr.Api.Models.Analytics;

public enum TrustScoreChangeReason
{
    InitialScore = 0,
    PositiveEngagement = 1,
    NegativeEngagement = 2,
    ContentModeration = 3,
    UserReport = 4,
    AdminAdjustment = 5,
    AutomaticDecay = 6,
    SuccessfulAppeal = 7,
    VerificationComplete = 8,
    SuspensionLifted = 9,
    QualityContent = 10,
    SpamDetection = 11,
    CommunityFeedback = 12
}