using MassTransit;

namespace Yapplr.Api.CQRS;

/// <summary>
/// Interface for command handlers that process commands asynchronously
/// </summary>
/// <typeparam name="TCommand">The type of command to handle</typeparam>
public interface ICommandHandler<in TCommand> : IConsumer<TCommand>
    where TCommand : class, ICommand
{
}