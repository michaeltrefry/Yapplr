using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Yapplr.Api.DTOs;
using Yapplr.Api.Extensions;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class MessageEndpoints
{
    public static void MapMessageEndpoints(this WebApplication app)
    {
        var messages = app.MapGroup("/api/messages").WithTags("Messages");

        // Send a new message (creates conversation if needed)
        messages.MapPost("/", async ([FromBody] CreateMessageDto createDto, ClaimsPrincipal user, IMessageService messageService) =>
        {
            var senderId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var message = await messageService.SendMessageAsync(senderId, createDto);

            return message == null ? Results.BadRequest(new { message = "Unable to send message. User may be blocked or message is invalid." }) : Results.Created($"/api/messages/{message.Id}", message);
        })
        .WithName("SendMessage")
        .WithSummary("Send a new message")
        .RequireAuthorization("ActiveUser")
        .Produces<MessageDto>(201)
        .Produces(400)
        .Produces(401);

        // Send message to existing conversation
        messages.MapPost("/conversation", async ([FromBody] SendMessageDto sendDto, ClaimsPrincipal user, IMessageService messageService) =>
        {
            var senderId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var message = await messageService.SendMessageToConversationAsync(senderId, sendDto);

            return message == null ? Results.BadRequest(new { message = "Unable to send message to conversation." }) : Results.Ok(message);
        })
        .WithName("SendMessageToConversation")
        .WithSummary("Send message to existing conversation")
        .RequireAuthorization("ActiveUser")
        .Produces<MessageDto>(200)
        .Produces(400)
        .Produces(401);

        // Get conversations list
        messages.MapGet("/conversations", async (ClaimsPrincipal user, IMessageService messageService, int page = 1, int pageSize = 25) =>
        {
            var userId = user.GetUserId(true);
            var conversations = await messageService.GetConversationsAsync(userId, page, pageSize);

            return Results.Ok(conversations);
        })
        .WithName("GetConversations")
        .WithSummary("Get user's conversations")
        .RequireAuthorization("ActiveUser")
        .Produces<IEnumerable<ConversationListDto>>(200)
        .Produces(401);

        // Get specific conversation
        messages.MapGet("/conversations/{conversationId:int}", async (int conversationId, ClaimsPrincipal user, IMessageService messageService) =>
        {
            var userId = user.GetUserId(true);
            var conversation = await messageService.GetConversationAsync(conversationId, userId);

            return conversation == null ? Results.NotFound() : Results.Ok(conversation);
        })
        .WithName("GetConversation")
        .WithSummary("Get specific conversation")
        .RequireAuthorization("ActiveUser")
        .Produces<ConversationDto>(200)
        .Produces(404)
        .Produces(401);

        // Get messages in conversation
        messages.MapGet("/conversations/{conversationId:int}/messages", async (int conversationId, ClaimsPrincipal user, IMessageService messageService, int page = 1, int pageSize = 25) =>
        {
            var userId = user.GetUserId(true);
            var messages = await messageService.GetMessagesAsync(conversationId, userId, page, pageSize);
            
            return Results.Ok(messages);
        })
        .WithName("GetMessages")
        .WithSummary("Get messages in conversation")
        .RequireAuthorization("ActiveUser")
        .Produces<IEnumerable<MessageDto>>(200)
        .Produces(401);

        // Mark conversation as read
        messages.MapPost("/conversations/{conversationId:int}/read", async (int conversationId, ClaimsPrincipal user, IMessageService messageService) =>
        {
            var userId = user.GetUserId(true);
            var success = await messageService.MarkConversationAsReadAsync(conversationId, userId);
            
            return success ? Results.Ok(new { message = "Conversation marked as read" }) : Results.BadRequest(new { message = "Unable to mark conversation as read" });
        })
        .WithName("MarkConversationAsRead")
        .WithSummary("Mark conversation as read")
        .RequireAuthorization("ActiveUser")
        .Produces(200)
        .Produces(400)
        .Produces(401);

        // Check if user can message another user
        messages.MapGet("/can-message/{userId:int}", async (int userId, ClaimsPrincipal user, IMessageService messageService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var canMessage = await messageService.CanUserMessageAsync(currentUserId, userId);
            
            return Results.Ok(new { canMessage });
        })
        .WithName("CanMessage")
        .WithSummary("Check if current user can message another user")
        .RequireAuthorization("ActiveUser")
        .Produces(200)
        .Produces(401);

        // Get or create conversation with another user
        messages.MapPost("/conversations/with/{userId:int}", async (int userId, ClaimsPrincipal user, IMessageService messageService) =>
        {
            var currentUserId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            // Check if user can message the other user
            if (!await messageService.CanUserMessageAsync(currentUserId, userId))
            {
                return Results.BadRequest(new { message = "Cannot message this user" });
            }

            var conversation = await messageService.GetOrCreateConversationAsync(currentUserId, userId);

            return conversation == null ? Results.BadRequest() : Results.Ok(conversation);
        })
        .WithName("GetOrCreateConversation")
        .WithSummary("Get or create conversation with another user")
        .RequireAuthorization("ActiveUser")
        .Produces<ConversationDto>(200)
        .Produces(400)
        .Produces(401);

        // Get total unread message count
        messages.MapGet("/unread-count", async (ClaimsPrincipal user, IMessageService messageService) =>
        {
            var userId = user.GetUserId(true);
            var unreadCount = await messageService.GetTotalUnreadMessageCountAsync(userId);

            return Results.Ok(new { unreadCount });
        })
        .WithName("GetUnreadMessageCount")
        .WithSummary("Get total unread message count for user")
        .RequireAuthorization("ActiveUser")
        .Produces(200)
        .Produces(401);
    }
}
