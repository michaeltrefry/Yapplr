namespace Yapplr.Api.Common;

/// <summary>
/// Interface for soft-deletable entities
/// </summary>
public interface ISoftDeletableEntity : IEntity
{
    bool IsDeletedByUser { get; set; }
    DateTime? DeletedByUserAt { get; set; }
}