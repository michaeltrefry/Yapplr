namespace Yapplr.Api.CQRS;

/// <summary>
/// Base implementation of ICommand with common properties
/// </summary>
public abstract record BaseCommand : ICommand
{
    public Guid CommandId { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public int? UserId { get; init; }
}