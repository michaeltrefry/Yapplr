namespace Yapplr.Api.Models;

public enum AuditAction
{
    // User actions
    UserSuspended = 100,
    UserBanned = 101,
    UserShadowBanned = 102,
    UserUnsuspended = 103,
    UserUnbanned = 104,
    UserRoleChanged = 105,
    UserForcePasswordReset = 106,
    UserEmailVerificationToggled = 107,
    
    // Content actions
    PostHidden = 200,
    PostDeleted = 201,
    PostRestored = 202,
    PostSystemTagAdded = 203,
    PostSystemTagRemoved = 204,
    
    CommentHidden = 210,
    CommentDeleted = 211,
    CommentRestored = 212,
    CommentSystemTagAdded = 213,
    CommentSystemTagRemoved = 214,
    
    // System actions
    SystemTagCreated = 300,
    SystemTagUpdated = 301,
    SystemTagDeleted = 302,
    
    // Security actions
    IpBlocked = 400,
    IpUnblocked = 401,
    SecurityIncidentReported = 402,

    // User Report actions
    UserReportCreated = 450,
    UserReportReviewed = 451,

    // Appeal actions
    AppealCreated = 460,
    AppealApproved = 461,
    AppealDenied = 462,
    AppealEscalated = 463,

    // Bulk actions
    BulkContentDeleted = 500,
    BulkContentHidden = 501,
    BulkUsersActioned = 502
}