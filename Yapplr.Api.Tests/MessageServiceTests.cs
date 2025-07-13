using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.Services;
using Yapplr.Api.DTOs;

namespace Yapplr.Api.Tests;

public class MessageServiceTests : IDisposable
{
    private readonly TestYapplrDbContext _context;
    private readonly MessageService _messageService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ICompositeNotificationService> _mockNotificationService;
    private readonly Mock<ICountCacheService> _mockCountCache;

    public MessageServiceTests()
    {
        var options = new DbContextOptionsBuilder<YapplrDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestYapplrDbContext(options);
        _mockUserService = new Mock<IUserService>();
        _mockNotificationService = new Mock<ICompositeNotificationService>();
        _mockCountCache = new Mock<ICountCacheService>();

        _messageService = new MessageService(_context, _mockUserService.Object, _mockNotificationService.Object, _mockCountCache.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task SendMessageAsync_WithValidData_CreatesMessageAndReturnsDto()
    {
        // Arrange
        var sender = new User { Id = 1, Username = "sender", Email = "sender@test.com" };
        var recipient = new User { Id = 2, Username = "recipient", Email = "recipient@test.com" };
        
        _context.Users.AddRange(sender, recipient);
        await _context.SaveChangesAsync();

        var createDto = new CreateMessageDto(
            RecipientId: 2,
            Content: "Hello, this is a test message!"
        );

        _mockNotificationService.Setup(n => n.SendMessageNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                                .ReturnsAsync(true);

        // Act
        var result = await _messageService.SendMessageAsync(1, createDto);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Be("Hello, this is a test message!");
        result.Sender.Username.Should().Be("sender");
        result.ConversationId.Should().BeGreaterThan(0);

        // Verify message was created in database
        var messageInDb = await _context.Messages.FirstOrDefaultAsync();
        messageInDb.Should().NotBeNull();
        messageInDb!.Content.Should().Be("Hello, this is a test message!");
        messageInDb.SenderId.Should().Be(1);

        // Verify conversation was created
        var conversationInDb = await _context.Conversations.FirstOrDefaultAsync();
        conversationInDb.Should().NotBeNull();

        // Verify participants were added
        var participants = await _context.ConversationParticipants.ToListAsync();
        participants.Should().HaveCount(2);
        participants.Should().Contain(p => p.UserId == 1);
        participants.Should().Contain(p => p.UserId == 2);

        // Verify message statuses were created
        var messageStatuses = await _context.MessageStatuses.ToListAsync();
        messageStatuses.Should().HaveCount(2);
        messageStatuses.Should().Contain(ms => ms.UserId == 1 && ms.Status == MessageStatusType.Sent);
        messageStatuses.Should().Contain(ms => ms.UserId == 2 && ms.Status == MessageStatusType.Delivered);
    }

    [Fact]
    public async Task SendMessageAsync_WhenUserIsBlocked_ReturnsNull()
    {
        // Arrange
        var sender = new User { Id = 1, Username = "sender", Email = "sender@test.com" };
        var recipient = new User { Id = 2, Username = "recipient", Email = "recipient@test.com" };

        _context.Users.AddRange(sender, recipient);

        // Create a block relationship - sender blocks recipient
        var block = new Block { BlockerId = 1, BlockedId = 2, CreatedAt = DateTime.UtcNow };
        _context.Blocks.Add(block);

        await _context.SaveChangesAsync();

        var createDto = new CreateMessageDto(
            RecipientId: 2,
            Content: "This message should not be sent"
        );

        // Act
        var result = await _messageService.SendMessageAsync(1, createDto);

        // Assert
        result.Should().BeNull();

        // Verify no message was created
        var messageCount = await _context.Messages.CountAsync();
        messageCount.Should().Be(0);
    }

    [Fact]
    public async Task SendMessageAsync_WhenSenderIsBlockedByRecipient_ReturnsNull()
    {
        // Arrange
        var sender = new User { Id = 1, Username = "sender", Email = "sender@test.com" };
        var recipient = new User { Id = 2, Username = "recipient", Email = "recipient@test.com" };

        _context.Users.AddRange(sender, recipient);

        // Create a block relationship - recipient blocks sender
        var block = new Block { BlockerId = 2, BlockedId = 1, CreatedAt = DateTime.UtcNow };
        _context.Blocks.Add(block);

        await _context.SaveChangesAsync();

        var createDto = new CreateMessageDto(
            RecipientId: 2,
            Content: "This message should not be sent"
        );

        // Act
        var result = await _messageService.SendMessageAsync(1, createDto);

        // Assert
        result.Should().BeNull();

        // Verify no message was created
        var messageCount = await _context.Messages.CountAsync();
        messageCount.Should().Be(0);
    }

    [Fact]
    public async Task SendMessageAsync_WithEmptyContentAndNoImage_ReturnsNull()
    {
        // Arrange
        var sender = new User { Id = 1, Username = "sender", Email = "sender@test.com" };
        var recipient = new User { Id = 2, Username = "recipient", Email = "recipient@test.com" };
        
        _context.Users.AddRange(sender, recipient);
        await _context.SaveChangesAsync();

        var createDto = new CreateMessageDto(
            RecipientId: 2,
            Content: ""
        );



        // Act
        var result = await _messageService.SendMessageAsync(1, createDto);

        // Assert
        result.Should().BeNull();
        
        // Verify no message was created
        var messageCount = await _context.Messages.CountAsync();
        messageCount.Should().Be(0);
    }

    [Fact]
    public async Task SendMessageAsync_WithImageButNoContent_CreatesMessage()
    {
        // Arrange
        var sender = new User { Id = 1, Username = "sender", Email = "sender@test.com" };
        var recipient = new User { Id = 2, Username = "recipient", Email = "recipient@test.com" };
        
        _context.Users.AddRange(sender, recipient);
        await _context.SaveChangesAsync();

        var createDto = new CreateMessageDto(
            RecipientId: 2,
            Content: "",
            ImageFileName: "test-image.jpg"
        );

        _mockNotificationService.Setup(n => n.SendMessageNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                                .ReturnsAsync(true);

        // Act
        var result = await _messageService.SendMessageAsync(1, createDto);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Be("");
        result.ImageUrl.Should().Be("/api/images/test-image.jpg");
        
        // Verify message was created in database
        var messageInDb = await _context.Messages.FirstOrDefaultAsync();
        messageInDb.Should().NotBeNull();
        messageInDb!.ImageFileName.Should().Be("test-image.jpg");
    }

    [Fact]
    public async Task SendMessageToConversationAsync_WithValidData_CreatesMessage()
    {
        // Arrange
        var sender = new User { Id = 1, Username = "sender", Email = "sender@test.com" };
        var recipient = new User { Id = 2, Username = "recipient", Email = "recipient@test.com" };

        _context.Users.AddRange(sender, recipient);

        var conversation = new Conversation { Id = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.Conversations.Add(conversation);

        var participant1 = new ConversationParticipant { ConversationId = 1, UserId = 1, JoinedAt = DateTime.UtcNow };
        var participant2 = new ConversationParticipant { ConversationId = 1, UserId = 2, JoinedAt = DateTime.UtcNow };
        _context.ConversationParticipants.AddRange(participant1, participant2);

        await _context.SaveChangesAsync();

        var sendDto = new SendMessageDto(
            ConversationId: 1,
            Content: "Hello from conversation!"
        );

        _mockNotificationService.Setup(n => n.SendMessageNotificationAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                                .ReturnsAsync(true);

        // Act
        var result = await _messageService.SendMessageToConversationAsync(1, sendDto);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Be("Hello from conversation!");
        result.ConversationId.Should().Be(1);

        // Verify message was created in database
        var messageInDb = await _context.Messages.FirstOrDefaultAsync();
        messageInDb.Should().NotBeNull();
        messageInDb!.Content.Should().Be("Hello from conversation!");
        messageInDb.ConversationId.Should().Be(1);
    }

    [Fact]
    public async Task SendMessageToConversationAsync_WhenUserNotParticipant_ReturnsNull()
    {
        // Arrange
        var sender = new User { Id = 1, Username = "sender", Email = "sender@test.com" };
        var recipient = new User { Id = 2, Username = "recipient", Email = "recipient@test.com" };

        _context.Users.AddRange(sender, recipient);

        var conversation = new Conversation { Id = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.Conversations.Add(conversation);

        // Only add recipient as participant, not sender
        var participant = new ConversationParticipant { ConversationId = 1, UserId = 2, JoinedAt = DateTime.UtcNow };
        _context.ConversationParticipants.Add(participant);

        await _context.SaveChangesAsync();

        var sendDto = new SendMessageDto(
            ConversationId: 1,
            Content: "This should not be sent"
        );

        // Act
        var result = await _messageService.SendMessageToConversationAsync(1, sendDto);

        // Assert
        result.Should().BeNull();

        // Verify no message was created
        var messageCount = await _context.Messages.CountAsync();
        messageCount.Should().Be(0);
    }

    [Fact]
    public async Task GetConversationAsync_WithValidData_ReturnsConversationDto()
    {
        // Arrange
        var user1 = new User { Id = 1, Username = "user1", Email = "user1@test.com" };
        var user2 = new User { Id = 2, Username = "user2", Email = "user2@test.com" };

        _context.Users.AddRange(user1, user2);

        var conversation = new Conversation { Id = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.Conversations.Add(conversation);

        var participant1 = new ConversationParticipant { ConversationId = 1, UserId = 1, JoinedAt = DateTime.UtcNow };
        var participant2 = new ConversationParticipant { ConversationId = 1, UserId = 2, JoinedAt = DateTime.UtcNow };
        _context.ConversationParticipants.AddRange(participant1, participant2);

        await _context.SaveChangesAsync();

        // Act
        var result = await _messageService.GetConversationAsync(1, 1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Participants.Should().HaveCount(2);
        result.Participants.Should().Contain(p => p.Username == "user1");
        result.Participants.Should().Contain(p => p.Username == "user2");
    }

    [Fact]
    public async Task GetConversationAsync_WhenUserNotParticipant_ReturnsNull()
    {
        // Arrange
        var user1 = new User { Id = 1, Username = "user1", Email = "user1@test.com" };
        var user2 = new User { Id = 2, Username = "user2", Email = "user2@test.com" };
        var user3 = new User { Id = 3, Username = "user3", Email = "user3@test.com" };

        _context.Users.AddRange(user1, user2, user3);

        var conversation = new Conversation { Id = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.Conversations.Add(conversation);

        var participant1 = new ConversationParticipant { ConversationId = 1, UserId = 1, JoinedAt = DateTime.UtcNow };
        var participant2 = new ConversationParticipant { ConversationId = 1, UserId = 2, JoinedAt = DateTime.UtcNow };
        _context.ConversationParticipants.AddRange(participant1, participant2);

        await _context.SaveChangesAsync();

        // Act - user3 tries to access conversation they're not part of
        var result = await _messageService.GetConversationAsync(1, 3);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CanUserMessageAsync_WhenUsersCanMessage_ReturnsTrue()
    {
        // Arrange
        var sender = new User { Id = 1, Username = "sender", Email = "sender@test.com" };
        var recipient = new User { Id = 2, Username = "recipient", Email = "recipient@test.com" };

        _context.Users.AddRange(sender, recipient);
        await _context.SaveChangesAsync();



        // Act
        var result = await _messageService.CanUserMessageAsync(1, 2);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanUserMessageAsync_WhenUserIsBlocked_ReturnsFalse()
    {
        // Arrange
        var sender = new User { Id = 1, Username = "sender", Email = "sender@test.com" };
        var recipient = new User { Id = 2, Username = "recipient", Email = "recipient@test.com" };

        _context.Users.AddRange(sender, recipient);

        // Create a block relationship - sender blocks recipient
        var block = new Block { BlockerId = 1, BlockedId = 2, CreatedAt = DateTime.UtcNow };
        _context.Blocks.Add(block);

        await _context.SaveChangesAsync();

        // Act
        var result = await _messageService.CanUserMessageAsync(1, 2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanUserMessageAsync_WhenSenderIsBlockedByRecipient_ReturnsFalse()
    {
        // Arrange
        var sender = new User { Id = 1, Username = "sender", Email = "sender@test.com" };
        var recipient = new User { Id = 2, Username = "recipient", Email = "recipient@test.com" };

        _context.Users.AddRange(sender, recipient);

        // Create a block relationship - recipient blocks sender
        var block = new Block { BlockerId = 2, BlockedId = 1, CreatedAt = DateTime.UtcNow };
        _context.Blocks.Add(block);

        await _context.SaveChangesAsync();

        // Act
        var result = await _messageService.CanUserMessageAsync(1, 2);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetOrCreateConversationAsync_WithExistingConversation_ReturnsExistingConversation()
    {
        // Arrange
        var user1 = new User { Id = 1, Username = "user1", Email = "user1@test.com" };
        var user2 = new User { Id = 2, Username = "user2", Email = "user2@test.com" };

        _context.Users.AddRange(user1, user2);

        var existingConversation = new Conversation { Id = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.Conversations.Add(existingConversation);

        var participant1 = new ConversationParticipant { ConversationId = 1, UserId = 1, JoinedAt = DateTime.UtcNow };
        var participant2 = new ConversationParticipant { ConversationId = 1, UserId = 2, JoinedAt = DateTime.UtcNow };
        _context.ConversationParticipants.AddRange(participant1, participant2);

        await _context.SaveChangesAsync();

        // Act
        var result = await _messageService.GetOrCreateConversationAsync(1, 2);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);

        // Verify no new conversation was created
        var conversationCount = await _context.Conversations.CountAsync();
        conversationCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateConversationAsync_WithNoExistingConversation_CreatesNewConversation()
    {
        // Arrange
        var user1 = new User { Id = 1, Username = "user1", Email = "user1@test.com" };
        var user2 = new User { Id = 2, Username = "user2", Email = "user2@test.com" };

        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _messageService.GetOrCreateConversationAsync(1, 2);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().BeGreaterThan(0);
        result.Participants.Should().HaveCount(2);
        result.Participants.Should().Contain(p => p.Username == "user1");
        result.Participants.Should().Contain(p => p.Username == "user2");

        // Verify new conversation was created
        var conversationCount = await _context.Conversations.CountAsync();
        conversationCount.Should().Be(1);

        // Verify participants were added
        var participantCount = await _context.ConversationParticipants.CountAsync();
        participantCount.Should().Be(2);
    }

    [Fact]
    public async Task GetMessagesAsync_WithValidConversation_ReturnsMessages()
    {
        // Arrange
        var user1 = new User { Id = 1, Username = "user1", Email = "user1@test.com" };
        var user2 = new User { Id = 2, Username = "user2", Email = "user2@test.com" };

        _context.Users.AddRange(user1, user2);

        var conversation = new Conversation { Id = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.Conversations.Add(conversation);

        var participant1 = new ConversationParticipant { ConversationId = 1, UserId = 1, JoinedAt = DateTime.UtcNow };
        var participant2 = new ConversationParticipant { ConversationId = 1, UserId = 2, JoinedAt = DateTime.UtcNow };
        _context.ConversationParticipants.AddRange(participant1, participant2);

        var message1 = new Message
        {
            Id = 1,
            ConversationId = 1,
            SenderId = 1,
            Content = "First message",
            CreatedAt = DateTime.UtcNow.AddMinutes(-2),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-2)
        };
        var message2 = new Message
        {
            Id = 2,
            ConversationId = 1,
            SenderId = 2,
            Content = "Second message",
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        _context.Messages.AddRange(message1, message2);

        await _context.SaveChangesAsync();

        // Act
        var result = await _messageService.GetMessagesAsync(1, 1, 1, 25);

        // Assert
        result.Should().HaveCount(2);
        var messagesList = result.ToList();
        messagesList[0].Content.Should().Be("First message"); // Should be ordered by CreatedAt ascending
        messagesList[1].Content.Should().Be("Second message");
    }

    [Fact]
    public async Task GetMessagesAsync_WhenUserNotParticipant_ReturnsEmptyList()
    {
        // Arrange
        var user1 = new User { Id = 1, Username = "user1", Email = "user1@test.com" };
        var user2 = new User { Id = 2, Username = "user2", Email = "user2@test.com" };
        var user3 = new User { Id = 3, Username = "user3", Email = "user3@test.com" };

        _context.Users.AddRange(user1, user2, user3);

        var conversation = new Conversation { Id = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.Conversations.Add(conversation);

        var participant1 = new ConversationParticipant { ConversationId = 1, UserId = 1, JoinedAt = DateTime.UtcNow };
        var participant2 = new ConversationParticipant { ConversationId = 1, UserId = 2, JoinedAt = DateTime.UtcNow };
        _context.ConversationParticipants.AddRange(participant1, participant2);

        var message = new Message
        {
            Id = 1,
            ConversationId = 1,
            SenderId = 1,
            Content = "Secret message",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Messages.Add(message);

        await _context.SaveChangesAsync();

        // Act - user3 tries to access messages from conversation they're not part of
        var result = await _messageService.GetMessagesAsync(1, 3, 1, 25);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task MarkConversationAsReadAsync_WithValidData_ReturnsTrue()
    {
        // Arrange
        var user1 = new User { Id = 1, Username = "user1", Email = "user1@test.com" };
        var user2 = new User { Id = 2, Username = "user2", Email = "user2@test.com" };

        _context.Users.AddRange(user1, user2);

        var conversation = new Conversation { Id = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.Conversations.Add(conversation);

        var participant1 = new ConversationParticipant { ConversationId = 1, UserId = 1, JoinedAt = DateTime.UtcNow };
        var participant2 = new ConversationParticipant { ConversationId = 1, UserId = 2, JoinedAt = DateTime.UtcNow };
        _context.ConversationParticipants.AddRange(participant1, participant2);

        var message = new Message
        {
            Id = 1,
            ConversationId = 1,
            SenderId = 2,
            Content = "Unread message",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Messages.Add(message);

        await _context.SaveChangesAsync();

        // Act
        var result = await _messageService.MarkConversationAsReadAsync(1, 1);

        // Assert
        result.Should().BeTrue();

        // Verify participant's LastReadAt was updated
        var updatedParticipant = await _context.ConversationParticipants
            .FirstOrDefaultAsync(cp => cp.ConversationId == 1 && cp.UserId == 1);
        updatedParticipant.Should().NotBeNull();
        updatedParticipant!.LastReadAt.Should().NotBeNull();
        updatedParticipant.LastReadAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetTotalUnreadMessageCountAsync_ReturnsCorrectCount()
    {
        // Arrange
        var user1 = new User { Id = 1, Username = "user1", Email = "user1@test.com" };
        var user2 = new User { Id = 2, Username = "user2", Email = "user2@test.com" };

        _context.Users.AddRange(user1, user2);

        var conversation = new Conversation { Id = 1, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.Conversations.Add(conversation);

        var participant1 = new ConversationParticipant { ConversationId = 1, UserId = 1, JoinedAt = DateTime.UtcNow };
        var participant2 = new ConversationParticipant { ConversationId = 1, UserId = 2, JoinedAt = DateTime.UtcNow };
        _context.ConversationParticipants.AddRange(participant1, participant2);

        var message1 = new Message { Id = 1, ConversationId = 1, SenderId = 2, Content = "Message 1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var message2 = new Message { Id = 2, ConversationId = 1, SenderId = 2, Content = "Message 2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _context.Messages.AddRange(message1, message2);

        // Create message statuses - both delivered (unread) for user1
        var status1 = new MessageStatus { MessageId = 1, UserId = 1, Status = MessageStatusType.Delivered, CreatedAt = DateTime.UtcNow };
        var status2 = new MessageStatus { MessageId = 2, UserId = 1, Status = MessageStatusType.Delivered, CreatedAt = DateTime.UtcNow };
        _context.MessageStatuses.AddRange(status1, status2);

        await _context.SaveChangesAsync();

        // Setup mock count cache to return expected count
        _mockCountCache.Setup(x => x.GetUnreadMessageCountAsync(1))
            .ReturnsAsync(2);

        // Act
        var result = await _messageService.GetTotalUnreadMessageCountAsync(1);

        // Assert
        result.Should().Be(2);
    }
}
