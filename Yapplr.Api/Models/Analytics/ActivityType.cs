namespace Yapplr.Api.Models.Analytics;

public enum ActivityType
{
    Login = 0,
    Logout = 1,
    PostCreated = 2,
    PostLiked = 3,
    PostUnliked = 4,
    PostCommented = 5,
    PostReposted = 6,
    PostViewed = 7,
    ProfileViewed = 8,
    UserFollowed = 9,
    UserUnfollowed = 10,
    MessageSent = 11,
    SearchPerformed = 12,
    TagClicked = 13,
    LinkClicked = 14,
    VideoWatched = 15,
    ImageViewed = 16,
    NotificationClicked = 17,
    SettingsChanged = 18,
    AppOpened = 19,
    AppClosed = 20,
    FeedScrolled = 21,
    ShareAction = 22,
    ReportSubmitted = 23,
    BlockUser = 24,
    UnblockUser = 25
}