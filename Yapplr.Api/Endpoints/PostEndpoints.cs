using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
        .WithDescription(@"Create a new post with optional single media file.

**For multiple media files, use the /api/posts/with-media endpoint instead.**

**Requirements:**
- Content must be 1-256 characters
- Optional single image or video file (legacy support)
- Use imageFileName OR videoFileName, not both

**Note:** This endpoint supports backward compatibility for single media files. For new implementations with multiple media support, use the /api/posts/with-media endpoint.")
        .RequireAuthorization("ActiveUser")
        .Produces<PostDto>(201)
        .Produces(400)
        .Produces(401);

        // Create post with multiple media
        posts.MapPost("/with-media", async ([FromBody] CreatePostWithMediaDto createDto, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = user.GetUserId(true);
            return await EndpointUtilities.HandleAsync(
                async () => await postService.CreatePostWithMediaAsync(userId, createDto),
                $"/api/posts/{{id}}"
            );
        })
        .WithName("CreatePostWithMedia")
        .WithSummary("Create a new post with multiple media files")
        .WithDescription(@"Create a new post with up to 10 media files (images and videos).

**Requirements:**
- Content is optional when media files are provided (0-256 characters)
- Either content or media files must be provided
- Maximum 10 media files per post
- Media files must be uploaded first using the /api/uploads/media endpoint
- Use the returned file names in the mediaFiles array

**Media File Information:**
Each media file object should include:
- fileName: Name returned from upload endpoint
- mediaType: 0 for Image, 1 for Video
- width, height: Optional dimensions
- fileSizeBytes: Optional file size
- duration: Optional video duration (ISO 8601 format)")
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
                async () => await postService.GetTimelineAsync(userId, validPage, validPageSize)
            );
        })
        .WithName("GetTimeline")
        .WithSummary("Get timeline feed")
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

        // Get user photos
        posts.MapGet("/user/{userId}/photos", async (int userId, ClaimsPrincipal? user, IPostService postService, int page = 1, int pageSize = 25) =>
        {
            var currentUserId = user?.Identity?.IsAuthenticated == true
                ? int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value)
                : (int?)null;

            var userPhotos = await postService.GetUserPhotosAsync(userId, currentUserId, page, pageSize);

            return Results.Ok(userPhotos);
        })
        .WithName("GetUserPhotos")
        .WithSummary("Get photos by user")
        .Produces<IEnumerable<PostDto>>(200);

        // Get user videos
        posts.MapGet("/user/{userId}/videos", async (int userId, ClaimsPrincipal? user, IPostService postService, int page = 1, int pageSize = 25) =>
        {
            var currentUserId = user?.Identity?.IsAuthenticated == true
                ? int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value)
                : (int?)null;

            var userVideos = await postService.GetUserVideosAsync(userId, currentUserId, page, pageSize);

            return Results.Ok(userVideos);
        })
        .WithName("GetUserVideos")
        .WithSummary("Get videos by user")
        .Produces<IEnumerable<PostDto>>(200);

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



        // React/Remove reaction from post
        posts.MapPost("/{id:int}/react", async (int id, ReactionDto dto, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = user.GetUserId(true);
            var success = await postService.ReactToPostAsync(id, userId, dto.ReactionType);

            return success ? Results.Ok() : Results.BadRequest(new { message = "Already reacted with this type" });
        })
        .WithName("ReactToPost")
        .WithSummary("React to a post with an emoji")
        .RequireAuthorization("ActiveUser")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        posts.MapDelete("/{id:int}/react", async (int id, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = user.GetUserId(true);
            var success = await postService.RemovePostReactionAsync(id, userId);

            return success ? Results.Ok() : Results.BadRequest(new { message = "No reaction to remove" });
        })
        .WithName("RemovePostReaction")
        .WithSummary("Remove reaction from a post")
        .RequireAuthorization("ActiveUser")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        // Enhanced Repost functionality (replaces simple repost and quote tweet)
        posts.MapPost("/repost", async ([FromBody] CreateRepostDto createDto, ClaimsPrincipal user, IPostService postService, ILogger<Program> logger) =>
        {
            try
            {
                var userId = user.GetUserId(true);
                logger.LogInformation("Repost endpoint called by user {UserId} with data: {@CreateDto}", userId, createDto);

                var repost = await postService.CreateRepostAsync(userId, createDto);

                if (repost == null)
                {
                    logger.LogWarning("CreateRepostAsync returned null for user {UserId}", userId);
                    return Results.BadRequest("Failed to create repost");
                }

                logger.LogInformation("Repost created successfully with ID {PostId}", repost.Id);
                return Results.Created($"/api/posts/{repost.Id}", repost);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Exception in repost endpoint: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
                return Results.BadRequest($"Error: {ex.Message}");
            }
        })
        .WithName("CreateRepost")
        .WithSummary("Create a repost with optional content")
        .RequireAuthorization("ActiveUser")
        .Produces<PostDto>(201)
        .Produces(400)
        .Produces(401);

        posts.MapPost("/repost-with-media", async ([FromBody] CreateRepostWithMediaDto createDto, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = user.GetUserId(true);
            return await EndpointUtilities.HandleAsync(
                async () => await postService.CreateRepostWithMediaAsync(userId, createDto),
                $"/api/posts/{{id}}"
            );
        })
        .WithName("CreateRepostWithMedia")
        .WithSummary("Create a repost with optional content and media attachments")
        .RequireAuthorization("ActiveUser")
        .Produces<PostDto>(201)
        .Produces(400)
        .Produces(401);

        posts.MapGet("/{id:int}/reposts", async (int id, ClaimsPrincipal? user, IPostService postService, int page = 1, int pageSize = 20) =>
        {
            var currentUserId = user?.Identity?.IsAuthenticated == true
                ? int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value)
                : (int?)null;

            var reposts = await postService.GetRepostsAsync(id, currentUserId, page, pageSize);
            return Results.Ok(reposts);
        })
        .WithName("GetReposts")
        .WithSummary("Get reposts for a specific post")
        .Produces<IEnumerable<PostDto>>(200)
        .Produces(404);









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

        posts.MapGet("/{id:int}/comments", async (int id, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = user.GetUserIdOrNull(); // Returns int? - null if not authenticated

            IEnumerable<CommentDto> comments;
            if (userId.HasValue)
            {
                // Authenticated user - include like information
                comments = await postService.GetPostCommentsAsync(id, userId.Value);
            }
            else
            {
                // Unauthenticated user - no like information
                comments = await postService.GetPostCommentsAsync(id);
            }

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



        // Comment reactions
        posts.MapPost("/{postId:int}/comments/{commentId:int}/react", async (int postId, int commentId, ReactionDto dto, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = user.GetUserId(true);
            var success = await postService.ReactToCommentAsync(commentId, userId, dto.ReactionType);

            return success ? Results.Ok() : Results.BadRequest(new { message = "Already reacted with this type" });
        })
        .WithName("ReactToComment")
        .WithSummary("React to a comment with an emoji")
        .RequireAuthorization("ActiveUser")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        posts.MapDelete("/{postId:int}/comments/{commentId:int}/react", async (int postId, int commentId, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = user.GetUserId(true);
            var success = await postService.RemoveCommentReactionAsync(commentId, userId);

            return success ? Results.Ok() : Results.BadRequest(new { message = "No reaction to remove" });
        })
        .WithName("RemoveCommentReaction")
        .WithSummary("Remove reaction from a comment")
        .RequireAuthorization("ActiveUser")
        .Produces(200)
        .Produces(400)
        .Produces(401);
    }
}
