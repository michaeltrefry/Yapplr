using MassTransit;

namespace Yapplr.Api.CQRS;

/// <summary>
/// Base class for command handlers with common functionality
/// </summary>
/// <typeparam name="TCommand">The type of command to handle</typeparam>
public abstract class BaseCommandHandler<TCommand> : ICommandHandler<TCommand>
    where TCommand : class, ICommand
{
    protected readonly ILogger<BaseCommandHandler<TCommand>> Logger;

    protected BaseCommandHandler(ILogger<BaseCommandHandler<TCommand>> logger)
    {
        Logger = logger;
    }

    public async Task Consume(ConsumeContext<TCommand> context)
    {
        var command = context.Message;
        
        Logger.LogInformation("Processing command {CommandType} with ID {CommandId} created at {CreatedAt}",
            typeof(TCommand).Name, command.CommandId, command.CreatedAt);

        try
        {
            await HandleAsync(command, context);
            
            Logger.LogInformation("Successfully processed command {CommandType} with ID {CommandId}",
                typeof(TCommand).Name, command.CommandId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process command {CommandType} with ID {CommandId}: {Error}",
                typeof(TCommand).Name, command.CommandId, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Handle the command implementation
    /// </summary>
    /// <param name="command">The command to handle</param>
    /// <param name="context">The consume context</param>
    protected abstract Task HandleAsync(TCommand command, ConsumeContext<TCommand> context);
}