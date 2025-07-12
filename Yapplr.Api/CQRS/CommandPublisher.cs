using MassTransit;

namespace Yapplr.Api.CQRS;

/// <summary>
/// Implementation of command publisher using MassTransit
/// </summary>
public class CommandPublisher : ICommandPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CommandPublisher> _logger;

    public CommandPublisher(IPublishEndpoint publishEndpoint, ILogger<CommandPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class, ICommand
    {
        try
        {
            _logger.LogInformation("Publishing command {CommandType} with ID {CommandId}",
                typeof(TCommand).Name, command.CommandId);

            await _publishEndpoint.Publish(command, cancellationToken);

            _logger.LogInformation("Successfully published command {CommandType} with ID {CommandId}",
                typeof(TCommand).Name, command.CommandId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish command {CommandType} with ID {CommandId}: {Error}",
                typeof(TCommand).Name, command.CommandId, ex.Message);
            throw;
        }
    }

    public async Task PublishDelayedAsync<TCommand>(TCommand command, TimeSpan delay, CancellationToken cancellationToken = default)
        where TCommand : class, ICommand
    {
        try
        {
            _logger.LogInformation("Publishing delayed command {CommandType} with ID {CommandId}, delay: {Delay}",
                typeof(TCommand).Name, command.CommandId, delay);

            await _publishEndpoint.Publish(command, context =>
            {
                context.Delay = delay;
            }, cancellationToken);

            _logger.LogInformation("Successfully published delayed command {CommandType} with ID {CommandId}",
                typeof(TCommand).Name, command.CommandId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish delayed command {CommandType} with ID {CommandId}: {Error}",
                typeof(TCommand).Name, command.CommandId, ex.Message);
            throw;
        }
    }
}
