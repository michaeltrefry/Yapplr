namespace Yapplr.Api.Common;

/// <summary>
/// Interface for entities that can be used with BaseCrudService
/// </summary>
public interface IEntity
{
    int Id { get; set; }
}