using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.Authorization;
using Yapplr.Api.DTOs;
using Yapplr.Api.Extensions;
using Yapplr.Api.Services;
using Yapplr.Api.Common;

namespace Yapplr.Api.Endpoints;

public static class PostEndpoints
{
    public static void MapPostEndpoints(this WebApplication app)
    {
        var posts = app.MapGroup("/api/posts").WithTags("Posts");

        // Create post
        posts.MapPost("/", async ([FromBody] CreatePostDto createDto, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = user.GetUserId(true);
            return await EndpointUtilities.HandleAsync(
                async () => await postService.CreatePostAsync(userId, createDto),
                $"/api/posts/{{id}}"
            );
        })
        .WithName("CreatePost")
        .WithSummary("Create a new post")
        .RequireAuthorization("ActiveUser")
        .Produces<PostDto>(201)
        .Produces(400)
        .Produces(401);

        // Get post by ID
        posts.MapGet("/{id:int}", async (int id, ClaimsPrincipal? user, IPostService postService) =>
        {
            var currentUserId = user?.GetUserIdOrNull();
            return await EndpointUtilities.HandleAsync(
                async () => await postService.GetPostByIdAsync(id, currentUserId)
            );
        })
        .WithName("GetPost")
        .WithSummary("Get post by ID")
        .Produces<PostDto>(200)
        .Produces(404);

        // Get timeline (authenticated)
        posts.MapGet("/timeline", async (ClaimsPrincipal user, IPostService postService, int page = 1, int pageSize = 25) =>
        {
            var userId = user.GetUserId(true);
            var (validPage, validPageSize) = EndpointUtilities.GetPaginationParams(page, pageSize);

            return await EndpointUtilities.HandleAsync(
                async () => await postService.GetTimelineWithRepostsAsync(userId, validPage, validPageSize)
            );
        })
        .WithName("GetTimeline")
        .WithSummary("Get timeline feed with reposts")
        .RequireAuthorization("User")
        .Produces<IEnumerable<TimelineItemDto>>(200)
        .Produces(401);

        // Get public timeline (no authentication required)
        posts.MapGet("/public", async (ClaimsPrincipal? user, IPostService postService, int page = 1, int pageSize = 25) =>
        {
            var currentUserId = user?.Identity?.IsAuthenticated == true
                ? int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value)
                : (int?)null;

            var timeline = await postService.GetPublicTimelineAsync(currentUserId, page, pageSize);

            return Results.Ok(timeline);
        })
        .WithName("GetPublicTimeline")
        .WithSummary("Get public timeline feed (no authentication required)")
        .Produces<IEnumerable<TimelineItemDto>>(200);

        // Get user posts
        posts.MapGet("/user/{userId:int}", async (int userId, ClaimsPrincipal? user, IPostService postService, int page = 1, int pageSize = 25) =>
        {
            var currentUserId = user?.Identity?.IsAuthenticated == true
                ? int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value)
                : (int?)null;

            var posts = await postService.GetUserPostsAsync(userId, currentUserId, page, pageSize);

            return Results.Ok(posts);
        })
        .WithName("GetUserPosts")
        .WithSummary("Get posts by user")
        .Produces<IEnumerable<PostDto>>(200);

        // Get user timeline (posts + reposts)
        posts.MapGet("/user/{userId}/timeline", async (int userId, ClaimsPrincipal? user, IPostService postService, int page = 1, int pageSize = 25) =>
        {
            var currentUserId = user?.Identity?.IsAuthenticated == true
                ? int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value)
                : (int?)null;

            var userTimeline = await postService.GetUserTimelineAsync(userId, currentUserId, page, pageSize);

            return Results.Ok(userTimeline);
        })
        .WithName("GetUserTimeline")
        .WithSummary("Get user timeline (posts and reposts)")
        .Produces<IEnumerable<TimelineItemDto>>(200);

        // Update post
        posts.MapPut("/{id:int}", async (int id, [FromBody] UpdatePostDto updateDto, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = user.GetUserId(true);
            var post = await postService.UpdatePostAsync(id, userId, updateDto);

            return post == null ? Results.NotFound() : Results.Ok(post);
        })
        .WithName("UpdatePost")
        .WithSummary("Update a post")
        .RequireAuthorization("ActiveUser")
        .Produces<PostDto>(200)
        .Produces(400)
        .Produces(401)
        .Produces(404);

        // Delete post
        posts.MapDelete("/{id:int}", async (int id, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = user.GetUserId(true);
            var success = await postService.DeletePostAsync(id, userId);

            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeletePost")
        .WithSummary("Delete a post")
        .RequireAuthorization("ActiveUser")
        .Produces(204)
        .Produces(401)
        .Produces(404);

        // Like/Unlike post
        posts.MapPost("/{id:int}/like", async (int id, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = user.GetUserId(true);
            var success = await postService.LikePostAsync(id, userId);

            return success ? Results.Ok() : Results.BadRequest(new { message = "Already liked" });
        })
        .WithName("LikePost")
        .WithSummary("Like a post")
        .RequireAuthorization("ActiveUser")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        posts.MapDelete("/{id:int}/like", async (int id, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = user.GetUserId(true);
            var success = await postService.UnlikePostAsync(id, userId);
            
            return success ? Results.Ok() : Results.BadRequest(new { message = "Not liked" });
        })
        .WithName("UnlikePost")
        .WithSummary("Unlike a post")
        .RequireAuthorization("ActiveUser")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        // Repost/Unrepost
        posts.MapPost("/{id:int}/repost", async (int id, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = user.GetUserId(true);
            var success = await postService.RepostAsync(id, userId);

            return success ? Results.Ok() : Results.BadRequest(new { message = "Already reposted" });
        })
        .WithName("RepostPost")
        .WithSummary("Repost a post")
        .RequireAuthorization("ActiveUser")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        posts.MapDelete("/{id:int}/repost", async (int id, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = user.GetUserId(true);
            var success = await postService.UnrepostAsync(id, userId);
            
            return success ? Results.Ok() : Results.BadRequest(new { message = "Not reposted" });
        })
        .WithName("UnrepostPost")
        .WithSummary("Remove repost")
        .RequireAuthorization("ActiveUser")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        // Comments
        posts.MapPost("/{id:int}/comments", async (int id, [FromBody] CreateCommentDto createDto, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = user.GetUserId(true);
            var comment = await postService.AddCommentAsync(id, userId, createDto);
            
            return comment == null ? Results.BadRequest() : Results.Created($"/api/posts/{id}/comments/{comment.Id}", comment);
        })
        .WithName("AddComment")
        .WithSummary("Add comment to post")
        .RequireAuthorization("ActiveUser")
        .Produces<CommentDto>(201)
        .Produces(400)
        .Produces(401);

        posts.MapGet("/{id:int}/comments", async (int id, IPostService postService) =>
        {
            var comments = await postService.GetPostCommentsAsync(id);
            return Results.Ok(comments);
        })
        .WithName("GetPostComments")
        .WithSummary("Get post comments")
        .Produces<IEnumerable<CommentDto>>(200);

        posts.MapPut("/comments/{commentId:int}", async (int commentId, [FromBody] UpdateCommentDto updateDto, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = user.GetUserId(true);
            var comment = await postService.UpdateCommentAsync(commentId, userId, updateDto);

            return comment == null ? Results.NotFound() : Results.Ok(comment);
        })
        .WithName("UpdateComment")
        .WithSummary("Update a comment")
        .RequireAuthorization("ActiveUser")
        .Produces<CommentDto>(200)
        .Produces(400)
        .Produces(401)
        .Produces(404);

        posts.MapDelete("/comments/{commentId:int}", async (int commentId, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = user.GetUserId(true);
            var success = await postService.DeleteCommentAsync(commentId, userId);

            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteComment")
        .WithSummary("Delete a comment")
        .RequireAuthorization("ActiveUser")
        .Produces(204)
        .Produces(401)
        .Produces(404);
    }
}
