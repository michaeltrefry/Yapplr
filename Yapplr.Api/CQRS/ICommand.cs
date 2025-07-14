namespace Yapplr.Api.CQRS;

/// <summary>
/// Marker interface for commands that can be published to message queues
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Unique identifier for the command
    /// </summary>
    Guid CommandId { get; }
    
    /// <summary>
    /// Timestamp when the command was created
    /// </summary>
    DateTime CreatedAt { get; }
    
    /// <summary>
    /// User ID who initiated the command (if applicable)
    /// </summary>
    int? UserId { get; }
}