using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public interface IFirebaseService
{
    Task<bool> SendNotificationAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null);
    Task<bool> SendMessageNotificationAsync(int userId, string senderUsername, string messageContent, int conversationId);
    Task<bool> SendMentionNotificationAsync(int userId, string mentionerUsername, int postId, int? commentId = null);
    Task<bool> SendReplyNotificationAsync(int userId, string replierUsername, int postId, int commentId);
    Task<bool> SendCommentNotificationAsync(int userId, string commenterUsername, int postId, int commentId);
    Task<bool> SendFollowNotificationAsync(int userId, string followerUsername);
    Task<bool> SendFollowRequestNotificationAsync(int userId, string requesterUsername);
    Task<bool> SendFollowRequestApprovedNotificationAsync(int userId, string approverUsername);
    Task<bool> SendLikeNotificationAsync(int userId, string likerUsername, int postId);
    Task<bool> SendRepostNotificationAsync(int userId, string reposterUsername, int postId);
    Task<bool> SendMulticastNotificationAsync(List<string> fcmTokens, string title, string body, Dictionary<string, string>? data = null);
}
