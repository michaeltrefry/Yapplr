using MassTransit;

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

/// <summary>
/// Base implementation of ICommand with common properties
/// </summary>
public abstract record BaseCommand : ICommand
{
    public Guid CommandId { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public int? UserId { get; init; }
}
