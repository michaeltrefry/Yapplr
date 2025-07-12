using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Services;
using Yapplr.Api.Models;

namespace Yapplr.Api.Extensions;

public static class CommandLineExtensions
{
    public static async Task<bool> HandleCommandLineArgumentsAsync(this WebApplication app, string[] args)
    {
        if (args.Length > 0 && args[0] == "create-admin")
        {
            await CreateAdminUser(app.Services, args);
            return true;
        }

        if (args.Length > 0 && args[0] == "promote-user")
        {
            await PromoteUser(app.Services, args);
            return true;
        }

        return false;
    }

    private static async Task CreateAdminUser(IServiceProvider services, string[] args)
    {
        if (args.Length < 4)
        {
            Console.WriteLine("Usage: dotnet run create-admin <username> <email> <password>");
            Console.WriteLine("Example: dotnet run create-admin admin admin@yapplr.com SecurePassword123!");
            return;
        }

        var username = args[1];
        var email = args[2];
        var password = args[3];

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<YapplrDbContext>();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        try
        {
            // Check if user already exists
            var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Username == username || u.Email == email);
            if (existingUser != null)
            {
                Console.WriteLine($"❌ User with username '{username}' or email '{email}' already exists.");
                return;
            }

            // Create admin user
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = authService.HashPassword(password),
                Role = UserRole.Admin,
                Status = UserStatus.Active,
                EmailVerified = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            // Log the admin creation
            await auditService.LogActionAsync(AuditAction.UserRoleChanged, user.Id, $"Admin user created via command line");

            Console.WriteLine($"✅ Admin user created successfully!");
            Console.WriteLine($"   Username: {username}");
            Console.WriteLine($"   Email: {email}");
            Console.WriteLine($"   Role: Admin");
            Console.WriteLine($"   Status: Active");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating admin user: {ex.Message}");
        }
    }

    private static async Task PromoteUser(IServiceProvider services, string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: dotnet run promote-user <username-or-email> <role>");
            Console.WriteLine("Roles: User, Moderator, Admin");
            Console.WriteLine("Example: dotnet run promote-user john@example.com Admin");
            return;
        }

        var usernameOrEmail = args[1];
        var roleString = args[2];

        if (!Enum.TryParse<UserRole>(roleString, true, out var role))
        {
            Console.WriteLine("❌ Invalid role. Valid roles are: User, Moderator, Admin");
            return;
        }

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<YapplrDbContext>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        try
        {
            // Find user
            var user = await context.Users.FirstOrDefaultAsync(u =>
                u.Username == usernameOrEmail || u.Email == usernameOrEmail);

            if (user == null)
            {
                Console.WriteLine($"❌ User not found: {usernameOrEmail}");
                return;
            }

            var oldRole = user.Role;
            user.Role = role;
            user.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            // Log the role change
            await auditService.LogActionAsync(AuditAction.UserRoleChanged, user.Id,
                $"Role changed from {oldRole} to {role} via command line");

            Console.WriteLine($"✅ User role updated successfully!");
            Console.WriteLine($"   Username: {user.Username}");
            Console.WriteLine($"   Email: {user.Email}");
            Console.WriteLine($"   Old Role: {oldRole}");
            Console.WriteLine($"   New Role: {role}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error promoting user: {ex.Message}");
        }
    }
}
