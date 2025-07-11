using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.Authorization;
using Yapplr.Api.DTOs;
using Yapplr.Api.Extensions;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class UserReportEndpoints
{
    public static void MapUserReportEndpoints(this WebApplication app)
    {
        var reports = app.MapGroup("/api/reports").WithTags("User Reports");

        // Create a user report
        reports.MapPost("/", [RequireActiveUser] async (
            CreateUserReportDto dto,
            ClaimsPrincipal user,
            IUserReportService userReportService) =>
        {
            var userId = user.GetUserId(true);

            // Validate that either PostId or CommentId is provided, but not both
            if ((dto.PostId == null && dto.CommentId == null) ||
                (dto.PostId != null && dto.CommentId != null))
            {
                return Results.BadRequest(new { message = "Either PostId or CommentId must be provided, but not both." });
            }

            if (string.IsNullOrWhiteSpace(dto.Reason))
            {
                return Results.BadRequest(new { message = "Reason is required." });
            }

            var report = await userReportService.CreateReportAsync(userId, dto);
            return report == null
                ? Results.BadRequest(new { message = "Failed to create report. Content may not exist." })
                : Results.Created($"/api/reports/{report.Id}", report);
        })
        .WithName("CreateUserReport")
        .WithSummary("Create a user report for objectionable content")
        .Produces<UserReportDto>(201)
        .Produces(400)
        .Produces(401);

        // Get user's own reports
        reports.MapGet("/my-reports", [RequireActiveUser] async (
            ClaimsPrincipal user,
            IUserReportService userReportService,
            int page = 1,
            int pageSize = 25) =>
        {
            var userId = user.GetUserId(true);
            var reports = await userReportService.GetUserReportsAsync(userId, page, pageSize);
            return Results.Ok(reports);
        })
        .WithName("GetMyReports")
        .WithSummary("Get current user's reports")
        .Produces<IEnumerable<UserReportDto>>(200)
        .Produces(401);
    }
}
