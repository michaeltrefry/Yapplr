using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Postr.Api.DTOs;
using Postr.Api.Services;

namespace Postr.Api.Endpoints;

public static class PostEndpoints
{
    public static void MapPostEndpoints(this WebApplication app)
    {
        var posts = app.MapGroup("/api/posts").WithTags("Posts");

        // Create post
        posts.MapPost("/", [Authorize] async ([FromBody] CreatePostDto createDto, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var post = await postService.CreatePostAsync(userId, createDto);
            
            return post == null ? Results.BadRequest() : Results.Created($"/api/posts/{post.Id}", post);
        })
        .WithName("CreatePost")
        .WithSummary("Create a new post")
        .Produces<PostDto>(201)
        .Produces(400)
        .Produces(401);

        // Get post by ID
        posts.MapGet("/{id:int}", async (int id, ClaimsPrincipal? user, IPostService postService) =>
        {
            var currentUserId = user?.Identity?.IsAuthenticated == true 
                ? int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value) 
                : (int?)null;
            
            var post = await postService.GetPostByIdAsync(id, currentUserId);
            
            return post == null ? Results.NotFound() : Results.Ok(post);
        })
        .WithName("GetPost")
        .WithSummary("Get post by ID")
        .Produces<PostDto>(200)
        .Produces(404);

        // Get timeline
        posts.MapGet("/timeline", [Authorize] async (ClaimsPrincipal user, IPostService postService, int page = 1, int pageSize = 20) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var timeline = await postService.GetTimelineWithRepostsAsync(userId, page, pageSize);

            return Results.Ok(timeline);
        })
        .WithName("GetTimeline")
        .WithSummary("Get timeline feed with reposts")
        .Produces<IEnumerable<TimelineItemDto>>(200)
        .Produces(401);

        // Get user posts
        posts.MapGet("/user/{userId:int}", async (int userId, ClaimsPrincipal? user, IPostService postService, int page = 1, int pageSize = 20) =>
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

        // Delete post
        posts.MapDelete("/{id:int}", [Authorize] async (int id, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await postService.DeletePostAsync(id, userId);
            
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeletePost")
        .WithSummary("Delete a post")
        .Produces(204)
        .Produces(401)
        .Produces(404);

        // Like/Unlike post
        posts.MapPost("/{id:int}/like", [Authorize] async (int id, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await postService.LikePostAsync(id, userId);
            
            return success ? Results.Ok() : Results.BadRequest(new { message = "Already liked" });
        })
        .WithName("LikePost")
        .WithSummary("Like a post")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        posts.MapDelete("/{id:int}/like", [Authorize] async (int id, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await postService.UnlikePostAsync(id, userId);
            
            return success ? Results.Ok() : Results.BadRequest(new { message = "Not liked" });
        })
        .WithName("UnlikePost")
        .WithSummary("Unlike a post")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        // Repost/Unrepost
        posts.MapPost("/{id:int}/repost", [Authorize] async (int id, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await postService.RepostAsync(id, userId);
            
            return success ? Results.Ok() : Results.BadRequest(new { message = "Already reposted" });
        })
        .WithName("RepostPost")
        .WithSummary("Repost a post")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        posts.MapDelete("/{id:int}/repost", [Authorize] async (int id, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await postService.UnrepostAsync(id, userId);
            
            return success ? Results.Ok() : Results.BadRequest(new { message = "Not reposted" });
        })
        .WithName("UnrepostPost")
        .WithSummary("Remove repost")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        // Comments
        posts.MapPost("/{id:int}/comments", [Authorize] async (int id, [FromBody] CreateCommentDto createDto, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var comment = await postService.AddCommentAsync(id, userId, createDto);
            
            return comment == null ? Results.BadRequest() : Results.Created($"/api/posts/{id}/comments/{comment.Id}", comment);
        })
        .WithName("AddComment")
        .WithSummary("Add comment to post")
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

        posts.MapDelete("/comments/{commentId:int}", [Authorize] async (int commentId, ClaimsPrincipal user, IPostService postService) =>
        {
            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var success = await postService.DeleteCommentAsync(commentId, userId);
            
            return success ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteComment")
        .WithSummary("Delete a comment")
        .Produces(204)
        .Produces(401)
        .Produces(404);
    }
}
