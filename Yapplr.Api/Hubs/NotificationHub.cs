using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Yapplr.Api.Data;
using Yapplr.Api.Services;

namespace Yapplr.Api.Hubs;

/// <summary>
/// SignalR hub for real-time notifications
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;
    private readonly YapplrDbContext _context;
    private readonly IUserService _userService;
    private readonly ISignalRConnectionPool _connectionPool;
    private readonly IActiveConversationTracker _conversationTracker;

    public NotificationHub(
        ILogger<NotificationHub> logger,
        YapplrDbContext context,
        IUserService userService,
        ISignalRConnectionPool connectionPool,
        IActiveConversationTracker conversationTracker)
    {
        _logger = logger;
        _context = context;
        _userService = userService;
        _connectionPool = connectionPool;
        _conversationTracker = conversationTracker;
    }

    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            // Add to connection pool
            await _connectionPool.AddUserConnectionAsync(userId.Value, Context.ConnectionId);

            // Add user to their personal group for notifications
            var userGroup = $"user_{userId.Value}";
            await Groups.AddToGroupAsync(Context.ConnectionId, userGroup);

            _logger.LogInformation("User {UserId} connected to SignalR with connection {ConnectionId} and added to group {UserGroup}",
                userId.Value, Context.ConnectionId, userGroup);

            // Notify the user that SignalR is connected
            await Clients.Caller.SendAsync("Connected", new {
                message = "SignalR connected successfully",
                connectionId = Context.ConnectionId,
                userId = userId.Value
            });
        }
        else
        {
            _logger.LogWarning("Unauthorized connection attempt to SignalR hub");
            Context.Abort();
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            // Remove from connection pool
            await _connectionPool.RemoveUserConnectionAsync(userId.Value, Context.ConnectionId);

            // Remove user from all active conversations when they disconnect
            await _conversationTracker.RemoveAllUserConversationsAsync(userId.Value);

            // Remove user from their personal group
            var userGroup = $"user_{userId.Value}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userGroup);

            _logger.LogInformation("User {UserId} disconnected from SignalR with connection {ConnectionId} and removed from group {UserGroup}",
                userId.Value, Context.ConnectionId, userGroup);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a conversation group for real-time messaging
    /// </summary>
    public async Task JoinConversation(int conversationId)
    {
        _logger.LogInformation("JoinConversation called for conversation {ConversationId} from connection {ConnectionId}",
            conversationId, Context.ConnectionId);

        var userId = GetUserId();
        if (!userId.HasValue)
        {
            _logger.LogWarning("JoinConversation called but no userId found in context for connection {ConnectionId}", Context.ConnectionId);
            await Clients.Caller.SendAsync("Error", "Unauthorized");
            return;
        }

        // Verify user is part of this conversation
        var isParticipant = await _context.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId.Value);

        if (!isParticipant)
        {
            await Clients.Caller.SendAsync("Error", "Not authorized for this conversation");
            return;
        }

        var conversationGroup = $"conversation_{conversationId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationGroup);

        // Track that user is actively viewing this conversation
        await _conversationTracker.SetUserActiveInConversationAsync(userId.Value, conversationId);

        _logger.LogInformation("User {UserId} joined conversation {ConversationId}",
            userId.Value, conversationId);

        await Clients.Caller.SendAsync("JoinedConversation", conversationId);
    }

    /// <summary>
    /// Leave a conversation group
    /// </summary>
    public async Task LeaveConversation(int conversationId)
    {
        _logger.LogInformation("LeaveConversation called for conversation {ConversationId} from connection {ConnectionId}",
            conversationId, Context.ConnectionId);

        var conversationGroup = $"conversation_{conversationId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationGroup);

        var userId = GetUserId();
        if (userId.HasValue)
        {
            // Remove user from active conversation tracking
            await _conversationTracker.RemoveUserFromConversationAsync(userId.Value, conversationId);
            _logger.LogInformation("User {UserId} left conversation {ConversationId}", userId.Value, conversationId);
        }
        else
        {
            _logger.LogWarning("LeaveConversation called but no userId found in context for connection {ConnectionId}", Context.ConnectionId);
        }

        await Clients.Caller.SendAsync("LeftConversation", conversationId);
    }

    /// <summary>
    /// Notify other participants that user is typing in a conversation
    /// </summary>
    public async Task StartTyping(int conversationId)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized");
            return;
        }

        // Verify user is part of this conversation
        var isParticipant = await _context.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId.Value);

        if (!isParticipant)
        {
            await Clients.Caller.SendAsync("Error", "Not authorized for this conversation");
            return;
        }

        var conversationGroup = $"conversation_{conversationId}";

        // Get user info for the typing indicator
        var user = await _context.Users.FindAsync(userId.Value);
        if (user != null)
        {
            // Notify other participants in the conversation (exclude the sender)
            await Clients.GroupExcept(conversationGroup, Context.ConnectionId)
                .SendAsync("UserStartedTyping", new {
                    conversationId = conversationId,
                    userId = userId.Value,
                    username = user.Username,
                    timestamp = DateTime.UtcNow
                });

            _logger.LogDebug("User {UserId} started typing in conversation {ConversationId}",
                userId.Value, conversationId);
        }
    }

    /// <summary>
    /// Notify other participants that user stopped typing in a conversation
    /// </summary>
    public async Task StopTyping(int conversationId)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized");
            return;
        }

        // Verify user is part of this conversation
        var isParticipant = await _context.ConversationParticipants
            .AnyAsync(cp => cp.ConversationId == conversationId && cp.UserId == userId.Value);

        if (!isParticipant)
        {
            await Clients.Caller.SendAsync("Error", "Not authorized for this conversation");
            return;
        }

        var conversationGroup = $"conversation_{conversationId}";

        // Notify other participants in the conversation (exclude the sender)
        await Clients.GroupExcept(conversationGroup, Context.ConnectionId)
            .SendAsync("UserStoppedTyping", new {
                conversationId = conversationId,
                userId = userId.Value,
                timestamp = DateTime.UtcNow
            });

        _logger.LogDebug("User {UserId} stopped typing in conversation {ConversationId}",
            userId.Value, conversationId);
    }

    /// <summary>
    /// Test method to verify SignalR connectivity
    /// </summary>
    public async Task Ping()
    {
        var userId = GetUserId();
        _logger.LogInformation("Ping received from user {UserId}", userId);

        await Clients.Caller.SendAsync("Pong", new {
            timestamp = DateTime.UtcNow,
            userId = userId,
            connectionId = Context.ConnectionId
        });
    }

    /// <summary>
    /// Get the current user's ID from the JWT token
    /// </summary>
    private int? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Get the current user's username from the JWT token
    /// </summary>
    private string? GetUsername()
    {
        return Context.User?.FindFirst(ClaimTypes.Name)?.Value;
    }
}