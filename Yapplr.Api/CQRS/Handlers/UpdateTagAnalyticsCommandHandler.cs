using MassTransit;
using Yapplr.Api.CQRS.Commands;
using Yapplr.Api.Services;

namespace Yapplr.Api.CQRS.Handlers;

/// <summary>
/// Handler for tag analytics updates
/// </summary>
public class UpdateTagAnalyticsCommandHandler : BaseCommandHandler<UpdateTagAnalyticsCommand>
{
    private readonly ITagAnalyticsService _tagAnalyticsService;

    public UpdateTagAnalyticsCommandHandler(
        ITagAnalyticsService tagAnalyticsService,
        ILogger<UpdateTagAnalyticsCommandHandler> logger) : base(logger)
    {
        _tagAnalyticsService = tagAnalyticsService;
    }

    protected override async Task HandleAsync(UpdateTagAnalyticsCommand command, ConsumeContext<UpdateTagAnalyticsCommand> context)
    {
        try
        {
            await _tagAnalyticsService.UpdateTagMetricsAsync(
                command.TagId,
                command.Action,
                command.Timestamp);

            Logger.LogInformation("Updated tag analytics for tag {TagId}: {Action}",
                command.TagId, command.Action);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update tag analytics for tag {TagId}: {Error}",
                command.TagId, ex.Message);
            // Don't throw - analytics failures shouldn't break the main flow
        }
    }
}