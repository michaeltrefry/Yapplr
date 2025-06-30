using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public interface IFirebaseService
{
    Task<bool> SendNotificationAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null);
    Task<bool> SendMessageNotificationAsync(int userId, string senderUsername, string messageContent, int conversationId);
    Task<bool> SendMentionNotificationAsync(int userId, string mentionerUsername, int postId, int? commentId = null);
    Task<bool> SendReplyNotificationAsync(int userId, string replierUsername, int postId, int commentId);
    Task<bool> SendFollowNotificationAsync(int userId, string followerUsername);
    Task<bool> SendMulticastNotificationAsync(List<string> fcmTokens, string title, string body, Dictionary<string, string>? data = null);
}
