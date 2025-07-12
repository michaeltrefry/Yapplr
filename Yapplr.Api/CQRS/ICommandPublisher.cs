namespace Yapplr.Api.CQRS;

/// <summary>
/// Interface for publishing commands to message queues
/// </summary>
public interface ICommandPublisher
{
    /// <summary>
    /// Publish a command to the appropriate queue
    /// </summary>
    /// <typeparam name="TCommand">The type of command to publish</typeparam>
    /// <param name="command">The command to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class, ICommand;

    /// <summary>
    /// Publish a command with a delay
    /// </summary>
    /// <typeparam name="TCommand">The type of command to publish</typeparam>
    /// <param name="command">The command to publish</param>
    /// <param name="delay">Delay before processing the command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task PublishDelayedAsync<TCommand>(TCommand command, TimeSpan delay, CancellationToken cancellationToken = default)
        where TCommand : class, ICommand;
}
