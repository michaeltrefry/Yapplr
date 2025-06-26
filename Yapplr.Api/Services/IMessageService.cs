using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

public interface IMessageService
{
    Task<MessageDto?> SendMessageAsync(int senderId, CreateMessageDto createDto);
    Task<MessageDto?> SendMessageToConversationAsync(int senderId, SendMessageDto sendDto);
    Task<ConversationDto?> GetConversationAsync(int conversationId, int userId);
    Task<IEnumerable<ConversationListDto>> GetConversationsAsync(int userId, int page = 1, int pageSize = 25);
    Task<IEnumerable<MessageDto>> GetMessagesAsync(int conversationId, int userId, int page = 1, int pageSize = 25);
    Task<bool> MarkConversationAsReadAsync(int conversationId, int userId);
    Task<bool> CanUserMessageAsync(int senderId, int recipientId);
    Task<ConversationDto?> GetOrCreateConversationAsync(int userId1, int userId2);
    Task<int> GetTotalUnreadMessageCountAsync(int userId);
}
