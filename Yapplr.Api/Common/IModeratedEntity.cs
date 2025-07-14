namespace Yapplr.Api.Common;

/// <summary>
/// Interface for entities that can be hidden by moderators
/// </summary>
public interface IModeratedEntity : IEntity
{
    bool IsHidden { get; set; }
    int? HiddenByUserId { get; set; }
    DateTime? HiddenAt { get; set; }
    string? HiddenReason { get; set; }
}