using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.DTOs;
using Yapplr.Api.Models;

namespace Yapplr.Api.Services;

public class MessageService : IMessageService
{
    private readonly YapplrDbContext _context;
    private readonly IUserService _userService;

    public MessageService(YapplrDbContext context, IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task<bool> CanUserMessageAsync(int senderId, int recipientId)
    {
        // Users cannot message themselves
        if (senderId == recipientId)
            return false;

        // Check if either user has blocked the other
        var isBlocked = await _context.Blocks
            .AnyAsync(b => (b.BlockerId == senderId && b.BlockedId == recipientId) ||
                          (b.BlockerId == recipientId && b.BlockedId == senderId));

        return !isBlocked;
    }

    public async Task<MessageDto?> SendMessageAsync(int senderId, CreateMessageDto createDto)
    {
        // Check if sender can message the recipient
        if (!await CanUserMessageAsync(senderId, createDto.RecipientId))
            return null;

        // Validate that content or image is provided
        if (string.IsNullOrWhiteSpace(createDto.Content) && string.IsNullOrWhiteSpace(createDto.ImageFileName))
            return null;

        // Get or create conversation
        var conversation = await GetOrCreateConversationAsync(senderId, createDto.RecipientId);
        if (conversation == null)
            return null;

        // Create the message
        var message = new Message
        {
            ConversationId = conversation.Id,
            SenderId = senderId,
            Content = createDto.Content?.Trim() ?? string.Empty,
            ImageFileName = createDto.ImageFileName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);

        // Update conversation timestamp
        var conversationEntity = await _context.Conversations.FindAsync(conversation.Id);
        if (conversationEntity != null)
        {
            conversationEntity.UpdatedAt = DateTime.UtcNow;
        }

        // Create message status for sender (sent)
        var senderStatus = new MessageStatus
        {
            MessageId = message.Id,
            UserId = senderId,
            Status = MessageStatusType.Sent,
            CreatedAt = DateTime.UtcNow
        };
        _context.MessageStatuses.Add(senderStatus);

        // Create message status for recipient (delivered)
        var recipientStatus = new MessageStatus
        {
            MessageId = message.Id,
            UserId = createDto.RecipientId,
            Status = MessageStatusType.Delivered,
            CreatedAt = DateTime.UtcNow
        };
        _context.MessageStatuses.Add(recipientStatus);

        await _context.SaveChangesAsync();

        return await GetMessageDtoAsync(message.Id, senderId);
    }

    public async Task<MessageDto?> SendMessageToConversationAsync(int senderId, SendMessageDto sendDto)
    {
        // Verify user is participant in conversation
        var isParticipant = await _context.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == sendDto.ConversationId && cp.UserId == senderId);

        if (!isParticipant)
            return null;

        // Get other participants to check blocking
        var otherParticipants = await _context.ConversationParticipants
            .Where(cp => cp.ConversationId == sendDto.ConversationId && cp.UserId != senderId)
            .Select(cp => cp.UserId)
            .ToListAsync();

        // Check if sender can message any of the other participants
        foreach (var participantId in otherParticipants)
        {
            if (!await CanUserMessageAsync(senderId, participantId))
                return null;
        }

        // Validate that content or image is provided
        if (string.IsNullOrWhiteSpace(sendDto.Content) && string.IsNullOrWhiteSpace(sendDto.ImageFileName))
            return null;

        // Create the message
        var message = new Message
        {
            ConversationId = sendDto.ConversationId,
            SenderId = senderId,
            Content = sendDto.Content?.Trim() ?? string.Empty,
            ImageFileName = sendDto.ImageFileName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);

        // Update conversation timestamp
        var conversation = await _context.Conversations.FindAsync(sendDto.ConversationId);
        if (conversation != null)
        {
            conversation.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return await GetMessageDtoAsync(message.Id, senderId);
    }

    public async Task<ConversationDto?> GetOrCreateConversationAsync(int userId1, int userId2)
    {
        // Check if conversation already exists between these users
        var existingConversation = await _context.Conversations
            .Where(c => c.Participants.Count == 2 &&
                       c.Participants.Any(p => p.UserId == userId1) &&
                       c.Participants.Any(p => p.UserId == userId2))
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync();

        if (existingConversation != null)
        {
            return await MapToConversationDto(existingConversation, userId1);
        }

        // Create new conversation
        var conversation = new Conversation
        {
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();

        // Add participants
        var participant1 = new ConversationParticipant
        {
            ConversationId = conversation.Id,
            UserId = userId1,
            JoinedAt = DateTime.UtcNow
        };

        var participant2 = new ConversationParticipant
        {
            ConversationId = conversation.Id,
            UserId = userId2,
            JoinedAt = DateTime.UtcNow
        };

        _context.ConversationParticipants.AddRange(participant1, participant2);
        await _context.SaveChangesAsync();

        // Reload with includes
        var newConversation = await _context.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .FirstAsync(c => c.Id == conversation.Id);

        return await MapToConversationDto(newConversation, userId1);
    }

    public async Task<ConversationDto?> GetConversationAsync(int conversationId, int userId)
    {
        var conversation = await _context.Conversations
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .ThenInclude(m => m.Sender)
            .FirstOrDefaultAsync(c => c.Id == conversationId &&
                                    c.Participants.Any(p => p.UserId == userId));

        if (conversation == null)
            return null;

        return await MapToConversationDto(conversation, userId);
    }

    public async Task<IEnumerable<ConversationListDto>> GetConversationsAsync(int userId, int page = 1, int pageSize = 25)
    {
        var conversations = await _context.Conversations
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .ThenInclude(m => m.Sender)
            .OrderByDescending(c => c.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new List<ConversationListDto>();

        foreach (var conversation in conversations)
        {
            var otherParticipant = conversation.Participants
                .FirstOrDefault(p => p.UserId != userId)?.User;

            if (otherParticipant == null) continue;

            var lastMessage = conversation.Messages.FirstOrDefault();
            var unreadCount = await GetUnreadMessageCountAsync(conversation.Id, userId);

            result.Add(new ConversationListDto(
                conversation.Id,
                conversation.CreatedAt,
                conversation.UpdatedAt,
                MapToUserDto(otherParticipant),
                lastMessage != null ? await MapToMessageDto(lastMessage, userId) : null,
                unreadCount
            ));
        }

        return result;
    }

    public async Task<IEnumerable<MessageDto>> GetMessagesAsync(int conversationId, int userId, int page = 1, int pageSize = 25)
    {
        // Verify user is participant in conversation
        var isParticipant = await _context.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

        if (!isParticipant)
            return new List<MessageDto>();

        var messages = await _context.Messages
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
            .Include(m => m.Sender)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = new List<MessageDto>();
        foreach (var message in messages)
        {
            result.Add(await MapToMessageDto(message, userId));
        }

        return result.OrderBy(m => m.CreatedAt);
    }

    public async Task<bool> MarkConversationAsReadAsync(int conversationId, int userId)
    {
        // Verify user is participant in conversation
        var isParticipant = await _context.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

        if (!isParticipant)
            return false;

        // Update last read timestamp
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

        if (participant != null)
        {
            participant.LastReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    private async Task<int> GetUnreadMessageCountAsync(int conversationId, int userId)
    {
        var participant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId);

        if (participant?.LastReadAt == null)
        {
            return await _context.Messages
                .CountAsync(m => m.ConversationId == conversationId && m.SenderId != userId && !m.IsDeleted);
        }

        return await _context.Messages
            .CountAsync(m => m.ConversationId == conversationId &&
                           m.SenderId != userId &&
                           m.CreatedAt > participant.LastReadAt &&
                           !m.IsDeleted);
    }

    private async Task<MessageDto?> GetMessageDtoAsync(int messageId, int currentUserId)
    {
        var message = await _context.Messages
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == messageId);

        if (message == null)
            return null;

        return await MapToMessageDto(message, currentUserId);
    }

    private async Task<MessageDto> MapToMessageDto(Message message, int currentUserId)
    {
        var imageUrl = !string.IsNullOrEmpty(message.ImageFileName)
            ? $"/api/images/{message.ImageFileName}"
            : null;

        // Get message status for current user
        var status = await _context.MessageStatuses
            .Where(ms => ms.MessageId == message.Id && ms.UserId == currentUserId)
            .Select(ms => ms.Status)
            .FirstOrDefaultAsync();

        return new MessageDto(
            message.Id,
            message.Content,
            imageUrl,
            message.CreatedAt,
            message.UpdatedAt,
            message.IsEdited,
            message.IsDeleted,
            message.ConversationId,
            MapToUserDto(message.Sender),
            status
        );
    }

    private async Task<ConversationDto> MapToConversationDto(Conversation conversation, int currentUserId)
    {
        var participants = conversation.Participants.Select(p => MapToUserDto(p.User)).ToList();

        var lastMessage = conversation.Messages.FirstOrDefault();
        var lastMessageDto = lastMessage != null ? await MapToMessageDto(lastMessage, currentUserId) : null;

        var unreadCount = await GetUnreadMessageCountAsync(conversation.Id, currentUserId);

        return new ConversationDto(
            conversation.Id,
            conversation.CreatedAt,
            conversation.UpdatedAt,
            participants,
            lastMessageDto,
            unreadCount
        );
    }

    private UserDto MapToUserDto(User user)
    {
        return new UserDto(
            user.Id,
            user.Email,
            user.Username,
            user.Bio,
            user.Birthday,
            user.Pronouns,
            user.Tagline,
            user.ProfileImageFileName,
            user.CreatedAt
        );
    }
}
