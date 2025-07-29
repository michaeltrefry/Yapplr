using System.Collections.Concurrent;

namespace Yapplr.Api.Services;

/// <summary>
/// Service to track which conversations users are currently viewing
/// Used to suppress notifications when user is actively in a conversation
/// </summary>
public interface IActiveConversationTracker
{
    /// <summary>
    /// Mark a user as actively viewing a conversation
    /// </summary>
    Task SetUserActiveInConversationAsync(int userId, int conversationId);
    
    /// <summary>
    /// Mark a user as no longer viewing a conversation
    /// </summary>
    Task RemoveUserFromConversationAsync(int userId, int conversationId);
    
    /// <summary>
    /// Check if a user is currently viewing a specific conversation
    /// </summary>
    Task<bool> IsUserActiveInConversationAsync(int userId, int conversationId);
    
    /// <summary>
    /// Remove all active conversations for a user (when they disconnect)
    /// </summary>
    Task RemoveAllUserConversationsAsync(int userId);
    
    /// <summary>
    /// Get all users currently viewing a conversation
    /// </summary>
    Task<IEnumerable<int>> GetActiveUsersInConversationAsync(int conversationId);
}

public class ActiveConversationTracker : IActiveConversationTracker
{
    private readonly ILogger<ActiveConversationTracker> _logger;
    
    // Track which conversation each user is currently viewing
    private readonly ConcurrentDictionary<int, int> _userActiveConversations = new();
    
    // Track which users are viewing each conversation (for efficiency)
    private readonly ConcurrentDictionary<int, ConcurrentBag<int>> _conversationActiveUsers = new();

    public ActiveConversationTracker(ILogger<ActiveConversationTracker> logger)
    {
        _logger = logger;
    }

    public async Task SetUserActiveInConversationAsync(int userId, int conversationId)
    {
        await Task.CompletedTask;
        
        // Remove user from any previous conversation
        if (_userActiveConversations.TryGetValue(userId, out var previousConversationId))
        {
            await RemoveUserFromConversationAsync(userId, previousConversationId);
        }
        
        // Set user's active conversation
        _userActiveConversations[userId] = conversationId;
        
        // Add user to conversation's active users
        _conversationActiveUsers.AddOrUpdate(
            conversationId,
            new ConcurrentBag<int> { userId },
            (key, existingBag) =>
            {
                if (!existingBag.Contains(userId))
                {
                    existingBag.Add(userId);
                }
                return existingBag;
            });
        
        _logger.LogInformation("User {UserId} is now active in conversation {ConversationId}",
            userId, conversationId);
    }

    public async Task RemoveUserFromConversationAsync(int userId, int conversationId)
    {
        await Task.CompletedTask;
        
        // Remove from user's active conversation if it matches
        if (_userActiveConversations.TryGetValue(userId, out var activeConversationId) && 
            activeConversationId == conversationId)
        {
            _userActiveConversations.TryRemove(userId, out _);
        }
        
        // Remove user from conversation's active users
        if (_conversationActiveUsers.TryGetValue(conversationId, out var activeUsers))
        {
            var newActiveUsers = new ConcurrentBag<int>(
                activeUsers.Where(u => u != userId));
            
            if (newActiveUsers.IsEmpty)
            {
                _conversationActiveUsers.TryRemove(conversationId, out _);
            }
            else
            {
                _conversationActiveUsers[conversationId] = newActiveUsers;
            }
        }
        
        _logger.LogDebug("User {UserId} is no longer active in conversation {ConversationId}", 
            userId, conversationId);
    }

    public async Task<bool> IsUserActiveInConversationAsync(int userId, int conversationId)
    {
        await Task.CompletedTask;

        var isActive = _userActiveConversations.TryGetValue(userId, out var activeConversationId) &&
                       activeConversationId == conversationId;

        _logger.LogInformation("Checking if user {UserId} is active in conversation {ConversationId}: {IsActive}",
            userId, conversationId, isActive);

        return isActive;
    }

    public async Task RemoveAllUserConversationsAsync(int userId)
    {
        await Task.CompletedTask;
        
        if (_userActiveConversations.TryRemove(userId, out var conversationId))
        {
            await RemoveUserFromConversationAsync(userId, conversationId);
            _logger.LogDebug("Removed user {UserId} from all active conversations", userId);
        }
    }

    public async Task<IEnumerable<int>> GetActiveUsersInConversationAsync(int conversationId)
    {
        await Task.CompletedTask;
        
        if (_conversationActiveUsers.TryGetValue(conversationId, out var activeUsers))
        {
            return activeUsers.ToList();
        }
        
        return Enumerable.Empty<int>();
    }
}
