namespace Yapplr.Api.Services;

/// <summary>
/// API operation types for rate limiting
/// </summary>
public enum ApiOperation
{
    CreatePost,
    CreateComment,
    LikePost,
    UnlikePost,
    FollowUser,
    UnfollowUser,
    ReportContent,
    SendMessage,
    UploadMedia,
    UpdateProfile,
    Search,
    General
}