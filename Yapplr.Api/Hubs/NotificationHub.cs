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

    public NotificationHub(
        ILogger<NotificationHub> logger,
        YapplrDbContext context,
        IUserService userService,
        ISignalRConnectionPool connectionPool)
    {
        _logger = logger;
        _context = context;
        _userService = userService;
        _connectionPool = connectionPool;
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
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationGroup);
        
        _logger.LogInformation("User {UserId} joined conversation {ConversationId}", 
            userId.Value, conversationId);

        await Clients.Caller.SendAsync("JoinedConversation", conversationId);
    }

    /// <summary>
    /// Leave a conversation group
    /// </summary>
    public async Task LeaveConversation(int conversationId)
    {
        var conversationGroup = $"conversation_{conversationId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationGroup);
        
        var userId = GetUserId();
        _logger.LogInformation("User {UserId} left conversation {ConversationId}", 
            userId, conversationId);

        await Clients.Caller.SendAsync("LeftConversation", conversationId);
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