using Microsoft.AspNetCore.Mvc;
using Yapplr.Api.DTOs;
using Yapplr.Api.Services;

namespace Yapplr.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Authentication");

        auth.MapPost("/register", async ([FromBody] RegisterUserDto registerDto, IAuthService authService) =>
        {
            var result = await authService.RegisterAsync(registerDto);
            
            if (result == null)
            {
                return Results.BadRequest(new { message = "User already exists or username is taken" });
            }

            return Results.Ok(result);
        })
        .WithName("Register")
        .WithSummary("Register a new user")
        .Produces<AuthResponseDto>(200)
        .Produces(400);

        auth.MapPost("/login", async ([FromBody] LoginUserDto loginDto, IAuthService authService) =>
        {
            var result = await authService.LoginAsync(loginDto);
            
            if (result == null)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(result);
        })
        .WithName("Login")
        .WithSummary("Login with email and password")
        .Produces<AuthResponseDto>(200)
        .Produces(401);

        auth.MapPost("/forgot-password", async ([FromBody] ForgotPasswordDto forgotPasswordDto, IAuthService authService, HttpContext context) =>
        {
            var resetBaseUrl = $"{context.Request.Scheme}://{context.Request.Host}/reset-password";
            var success = await authService.RequestPasswordResetAsync(forgotPasswordDto.Email, resetBaseUrl);

            // Always return success for security (don't reveal if email exists)
            return Results.Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
        })
        .WithName("ForgotPassword")
        .WithSummary("Request password reset email")
        .Produces<object>(200);

        auth.MapPost("/reset-password", async ([FromBody] ResetPasswordDto resetPasswordDto, IAuthService authService) =>
        {
            var success = await authService.ResetPasswordAsync(resetPasswordDto.Token, resetPasswordDto.NewPassword);

            if (!success)
            {
                return Results.BadRequest(new { message = "Invalid or expired reset token" });
            }

            return Results.Ok(new { message = "Password has been reset successfully" });
        })
        .WithName("ResetPassword")
        .WithSummary("Reset password with token")
        .Produces<object>(200)
        .Produces(400);
    }
}
