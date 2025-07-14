namespace Yapplr.Api.Common;

/// <summary>
/// Interface for entities that are owned by users
/// </summary>
public interface IUserOwnedEntity : IEntity
{
    int UserId { get; set; }
}