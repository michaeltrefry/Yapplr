using Yapplr.Api.Data;
using Yapplr.Api.Models;

namespace Yapplr.Api.Common;

/// <summary>
/// Specialized query builders for specific entities
/// </summary>
public static class SpecializedQueryBuilders
{
    /// <summary>
    /// Create a post query builder with common includes
    /// </summary>
    public static QueryBuilder<Post> PostsWithIncludes(this YapplrDbContext context)
    {
        return new QueryBuilder<Post>(context.GetPostsWithIncludes(), context);
    }

    /// <summary>
    /// Create a user query builder with admin includes
    /// </summary>
    public static QueryBuilder<User> UsersWithAdminIncludes(this YapplrDbContext context)
    {
        return new QueryBuilder<User>(context.GetUsersWithAdminIncludes(), context);
    }

    /// <summary>
    /// Create a message query builder with includes
    /// </summary>
    public static QueryBuilder<Message> MessagesWithIncludes(this YapplrDbContext context)
    {
        return new QueryBuilder<Message>(context.GetMessagesWithIncludes(), context);
    }

    /// <summary>
    /// Create a conversation query builder with includes
    /// </summary>
    public static QueryBuilder<Conversation> ConversationsWithIncludes(this YapplrDbContext context)
    {
        return new QueryBuilder<Conversation>(context.GetConversationsWithIncludes(), context);
    }
}