using Microsoft.EntityFrameworkCore;
using Yapplr.Api.Data;
using Yapplr.Api.Models;
using Yapplr.Api.DTOs;

namespace Yapplr.Api.Services;

public class StagingSeedService
{
    private readonly YapplrDbContext _context;
    private readonly ILogger<StagingSeedService> _logger;
    private readonly IPostService _postService;

    public StagingSeedService(YapplrDbContext context, ILogger<StagingSeedService> logger, IPostService postService)
    {
        _context = context;
        _logger = logger;
        _postService = postService;
    }

    public async Task SeedStagingDataAsync()
    {
        try
        {
            _logger.LogInformation("🌱 Starting test data seeding...");
            
            // Check if any non-system users exist (exclude system user which is created by EssentialUserSeedService)
            if (await _context.Users.AnyAsync(u => u.Username != "system"))
            {
                _logger.LogInformation("⚠️ Other users already exist in database, skipping seeding");
                return;
            }
            
            // Create admin user
            await CreateAdminUserAsync();

            // Create 20 test users
            await CreateTestUsersAsync();

            // Save users to database before creating content
            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Users created and saved to database");

            // Create some sample content
            await CreateSampleContentAsync();

            // Save content to database
            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Test data seeding completed successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during test data seeding");
            throw;
        }
    }

    private async Task CreateAdminUserAsync()
    {
        _logger.LogInformation("👑 Creating admin user...");

        var adminUser = new User
        {
            Username = "admin",
            Email = "admin@yapplr.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@$$w0rd!"),
            Bio = "System Administrator",
            Pronouns = "they/them",
            Tagline = "Keeping Yapplr running smoothly",
            Role = UserRole.Admin,
            EmailVerified = true, // Pre-verified for testing
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow
        };

        _context.Users.Add(adminUser);
        await _context.SaveChangesAsync(); // Save admin user first to get the ID
        _logger.LogInformation("✅ Admin user created: {Username} ({Email})", adminUser.Username, adminUser.Email);

        // Create notification preferences for admin user with email notifications disabled
        var adminPreferences = new NotificationPreferences
        {
            UserId = adminUser.Id, // Now this has a valid ID
            // Disable email notifications for admin user
            EnableEmailNotifications = false,
            EnableEmailDigest = false,
            EnableInstantEmailNotifications = false,
            // Keep other notification types enabled for admin testing
            EnableMessageNotifications = true,
            EnableMentionNotifications = true,
            EnableReplyNotifications = true,
            EnableCommentNotifications = true,
            EnableFollowNotifications = true,
            EnableLikeNotifications = true,
            EnableRepostNotifications = true,
            EnableFollowRequestNotifications = true,
            PreferredMethod = NotificationDeliveryMethod.SignalROnly,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.NotificationPreferences.Add(adminPreferences);
        await _context.SaveChangesAsync(); // Save admin preferences
        _logger.LogInformation("✅ Admin user notification preferences created (email notifications disabled)");

        // Suppress async warning since we're not actually using await in this method
        await Task.CompletedTask;
    }

    private async Task CreateTestUsersAsync()
    {
        _logger.LogInformation("👥 Creating 20 test users...");

        var testUsers = new List<(string firstName, string lastName, string username, string bio, string pronouns, string tagline)>
        {
            ("Alice", "Johnson", "alice_j", "Love coffee and coding ☕", "she/her", "Building the future, one line at a time"),
            ("Bob", "Smith", "bob_smith", "Tech enthusiast and gamer 🎮", "he/him", "Level up every day"),
            ("Charlie", "Brown", "charlie_b", "Designer with a passion for UX", "they/them", "Making the web beautiful"),
            ("Diana", "Wilson", "diana_w", "Marketing guru and social media expert", "she/her", "Connecting brands with people"),
            ("Ethan", "Davis", "ethan_d", "Full-stack developer and mentor", "he/him", "Teaching through code"),
            ("Fiona", "Miller", "fiona_m", "Data scientist and AI researcher", "she/her", "Finding patterns in chaos"),
            ("George", "Garcia", "george_g", "Product manager and startup founder", "he/him", "Turning ideas into reality"),
            ("Hannah", "Martinez", "hannah_m", "Content creator and photographer", "she/her", "Capturing life's moments"),
            ("Ian", "Anderson", "ian_a", "DevOps engineer and cloud architect", "he/him", "Scaling to infinity"),
            ("Julia", "Taylor", "julia_t", "Frontend developer and design systems advocate", "she/her", "Consistency is key"),
            ("Kevin", "Thomas", "kevin_t", "Backend engineer and API specialist", "he/him", "Building robust foundations"),
            ("Luna", "Rodriguez", "luna_r", "Mobile developer and accessibility champion", "she/her", "Apps for everyone"),
            ("Marcus", "Lee", "marcus_l", "Security researcher and ethical hacker", "he/him", "Breaking things to fix them"),
            ("Nina", "White", "nina_w", "Technical writer and documentation expert", "she/her", "Making complex simple"),
            ("Oscar", "Clark", "oscar_c", "Machine learning engineer", "he/him", "Teaching machines to think"),
            ("Priya", "Patel", "priya_p", "Quality assurance and testing specialist", "she/her", "Perfection through iteration"),
            ("Quinn", "Thompson", "quinn_t", "Open source contributor and community builder", "they/them", "Code together, grow together"),
            ("Rachel", "Moore", "rachel_m", "Database administrator and performance tuner", "she/her", "Optimizing every query"),
            ("Sam", "Jackson", "sam_j", "Systems architect and technology strategist", "they/them", "Designing tomorrow's systems"),
            ("Tara", "Williams", "tara_w", "Cybersecurity analyst and privacy advocate", "she/her", "Protecting digital rights")
        };

        foreach (var (firstName, lastName, username, bio, pronouns, tagline) in testUsers)
        {
            var email = $"{username}@example.com";
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@$$w0rd!"),
                Bio = bio,
                Pronouns = pronouns,
                Tagline = tagline,
                Role = UserRole.User,
                EmailVerified = true, // Pre-verified for testing
                CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)), // Spread creation dates
                UpdatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 1440)) // Random last seen within 24 hours
            };

            _context.Users.Add(user);
        }

        // Save all test users first
        await _context.SaveChangesAsync();
        _logger.LogInformation("✅ Created 20 test users with pre-verified emails");

        // Create notification preferences for all users with email notifications disabled
        await CreateNotificationPreferencesAsync();

        // Suppress async warning since we're not actually using await in this method
        await Task.CompletedTask;
    }

    private async Task CreateNotificationPreferencesAsync()
    {
        _logger.LogInformation("🔔 Creating notification preferences for all users (email notifications disabled)...");

        // Get all users that need notification preferences
        var users = await _context.Users.ToListAsync();
        var existingPreferences = await _context.NotificationPreferences
            .Select(np => np.UserId)
            .ToListAsync();

        var usersNeedingPreferences = users
            .Where(u => !existingPreferences.Contains(u.Id))
            .ToList();

        foreach (var user in usersNeedingPreferences)
        {
            var preferences = new NotificationPreferences
            {
                UserId = user.Id,
                // Disable email notifications for all seed users
                EnableEmailNotifications = false,
                EnableEmailDigest = false,
                EnableInstantEmailNotifications = false,
                // Keep other notification types enabled for testing
                EnableMessageNotifications = true,
                EnableMentionNotifications = true,
                EnableReplyNotifications = true,
                EnableCommentNotifications = true,
                EnableFollowNotifications = true,
                EnableLikeNotifications = true,
                EnableRepostNotifications = true,
                EnableFollowRequestNotifications = true,
                // Use SignalR for real-time notifications
                PreferredMethod = NotificationDeliveryMethod.SignalROnly,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.NotificationPreferences.Add(preferences);
        }

        if (usersNeedingPreferences.Any())
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Created notification preferences for {Count} users with email notifications disabled",
                usersNeedingPreferences.Count);
        }
        else
        {
            _logger.LogInformation("ℹ️ All users already have notification preferences");
        }
    }

    private async Task CreateSampleContentAsync()
    {
        _logger.LogInformation("📝 Creating sample posts using PostService (with full content moderation)...");

        // Get all users for creating content
        var users = await _context.Users.ToListAsync();
        var random = new Random();

        // Regular posts (safe content)
        var regularPosts = new List<string>
        {
            "Welcome to Yapplr! Excited to be part of this community 🎉",
            "Just shipped a new feature at work. The feeling never gets old! #coding #webdev",
            "Coffee shop vibes today ☕ Perfect for some deep work",
            "Anyone else think dark mode makes everything better? 🌙",
            "Working on a new project with #react and loving the developer experience",
            "Beautiful sunset today 🌅 Sometimes you need to step away from the screen",
            "Hot take: Documentation is just as important as the code itself 📚",
            "Debugging is like being a detective in a crime movie where you're also the murderer 🕵️",
            "Just discovered this amazing #opensource library that saves so much time!",
            "Team lunch was great today. Good food, better conversations 🍕",
            "Weekend project: Building a small app to track my reading habits 📖",
            "The best part about being a developer? There's always something new to learn",
            "Pair programming session went really well today. Two minds are better than one! 🧠",
            "Finally fixed that bug that's been haunting me for days. Victory! 🎯",
            "Attending a tech meetup tonight. Love connecting with the community 🤝"
        };

        // Content that should trigger moderation (for testing AI moderation)
        var moderationTestPosts = new List<(string content, string description)>
        {
            // NSFW Content
            ("This adult content is definitely not safe for work and contains explicit material", "NSFW - Adult content"),
            ("Check out this sexy photo I took last night 🔥", "NSFW - Sexual content"),

            // Violence
            ("I'm so angry I could punch someone right now. Violence is the answer!", "Violence - Aggressive language"),
            ("That movie had so much blood and gore, loved the brutal fight scenes", "Violence - Graphic content"),
            ("Someone should beat some sense into these idiots", "Violence - Threatening language"),

            // Harassment
            ("You're such an idiot, nobody likes you and you should just disappear", "Harassment - Personal attack"),
            ("Stop posting stupid content, you're ruining this platform for everyone", "Harassment - Bullying"),

            // Hate Speech
            ("I hate all people from that country, they're all the same", "Hate Speech - Discrimination"),
            ("Those people don't belong here and should go back where they came from", "Hate Speech - Xenophobia"),

            // Misinformation
            ("The earth is flat and the government is hiding the truth from us all", "Misinformation - Conspiracy theory"),
            ("Vaccines contain microchips to track and control the population", "Misinformation - Health misinformation"),

            // Sensitive Content
            ("I've been struggling with depression and having thoughts of self-harm lately", "Sensitive - Mental health"),
            ("Spoiler alert: The main character dies at the end of the movie!", "Spoiler - Entertainment spoiler"),

            // Spam-like content
            ("Buy now! Amazing deal! Click here! Limited time offer! Don't miss out!", "Spam - Promotional content"),
            ("Follow me follow me follow me! Like and subscribe! Check my profile!", "Spam - Attention seeking")
        };

        // Create regular posts (15 posts) using PostService
        _logger.LogInformation("📝 Creating 15 regular posts...");
        for (int i = 0; i < 15; i++)
        {
            var randomUser = users[random.Next(users.Count)];
            var randomContent = regularPosts[random.Next(regularPosts.Count)];

            var createPostDto = new CreatePostDto(
                randomContent,
                null, // No image
                null, // No video
                (PostPrivacy)random.Next(0, 3) // Random privacy level
            );

            try
            {
                var createdPost = await _postService.CreatePostAsync(randomUser.Id, createPostDto);
                if (createdPost != null)
                {
                    _logger.LogInformation("✅ Created regular post by {Username}: {Content}",
                        randomUser.Username, randomContent.Substring(0, Math.Min(50, randomContent.Length)) + "...");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to create regular post for user {Username}", randomUser.Username);
            }
        }

        // Create moderation test posts (10-15 posts from specific users) using PostService
        var moderationTestUsers = users.Take(8).ToList(); // Use first 8 users for moderation tests
        _logger.LogInformation("📝 Creating 15 moderation test posts (will trigger AI analysis)...");

        for (int i = 0; i < Math.Min(15, moderationTestPosts.Count); i++)
        {
            var testUser = moderationTestUsers[i % moderationTestUsers.Count];
            var (content, description) = moderationTestPosts[i];

            var createPostDto = new CreatePostDto(
                content,
                null, // No image
                null, // No video
                PostPrivacy.Public // Make moderation test posts public for visibility
            );

            try
            {
                var createdPost = await _postService.CreatePostAsync(testUser.Id, createPostDto);
                if (createdPost != null)
                {
                    _logger.LogInformation("📝 Created moderation test post by {Username}: {Description}",
                        testUser.Username, description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to create moderation test post for user {Username}: {Description}",
                    testUser.Username, description);
            }
        }

        // Create some follow relationships
        await CreateSampleFollowsAsync(users);

        // Create some comments using PostService
        await CreateSampleCommentsAsync();

        _logger.LogInformation("✅ Sample content created successfully (15 regular posts + 15 moderation test posts + comments)");
    }

    private async Task CreateSampleFollowsAsync(List<User> users)
    {
        var random = new Random();
        var followsToCreate = new HashSet<(int FollowerId, int FollowingId)>();

        // Create some random follow relationships (avoid duplicates)
        var attempts = 0;
        while (followsToCreate.Count < 30 && attempts < 100) // Limit attempts to avoid infinite loop
        {
            attempts++;
            var follower = users[random.Next(users.Count)];
            var following = users[random.Next(users.Count)];

            // Don't follow yourself and avoid duplicates
            if (follower.Id != following.Id &&
                !followsToCreate.Contains((follower.Id, following.Id)) &&
                !await _context.Follows.AnyAsync(f => f.FollowerId == follower.Id && f.FollowingId == following.Id))
            {
                followsToCreate.Add((follower.Id, following.Id));
                _context.Follows.Add(new Follow
                {
                    FollowerId = follower.Id,
                    FollowingId = following.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 14))
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task CreateSampleCommentsAsync()
    {
        _logger.LogInformation("💬 Creating sample comments using PostService (with content moderation)...");

        var posts = await _context.Posts.Include(p => p.User).ToListAsync();
        var users = await _context.Users.ToListAsync();
        var random = new Random();

        // Regular comment texts (safe content)
        var regularCommentTexts = new[]
        {
            "Great post! Thanks for sharing 👍",
            "I totally agree with this",
            "Interesting perspective!",
            "This is really helpful, thank you",
            "Love this! 💯",
            "Thanks for the insight",
            "Well said!",
            "This made my day 😊",
            "Couldn't agree more",
            "Awesome work!"
        };

        // Problematic comment texts (for moderation testing)
        var moderationTestComments = new[]
        {
            "This is stupid and you're an idiot for posting it",
            "Nobody cares about your opinion, shut up",
            "This contains some really inappropriate adult content",
            "I hate people like you, you should disappear",
            "This is fake news and misinformation"
        };

        // Create regular comments (15 comments)
        for (int i = 0; i < 15; i++)
        {
            var randomPost = posts[random.Next(posts.Count)];
            var randomUser = users[random.Next(users.Count)];
            var randomComment = regularCommentTexts[random.Next(regularCommentTexts.Length)];

            // Don't comment on your own post
            if (randomPost.UserId != randomUser.Id)
            {
                var createCommentDto = new CreateCommentDto(randomComment);

                try
                {
                    var createdComment = await _postService.AddCommentAsync(randomPost.Id, randomUser.Id, createCommentDto);
                    if (createdComment != null)
                    {
                        _logger.LogInformation("💬 Created regular comment by {Username} on post by {PostAuthor}",
                            randomUser.Username, randomPost.User.Username);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Failed to create regular comment for user {Username}", randomUser.Username);
                }
            }
        }

        // Create moderation test comments (5 comments)
        for (int i = 0; i < 5; i++)
        {
            var randomPost = posts[random.Next(posts.Count)];
            var randomUser = users[random.Next(users.Count)];
            var testComment = moderationTestComments[random.Next(moderationTestComments.Length)];

            // Don't comment on your own post
            if (randomPost.UserId != randomUser.Id)
            {
                var createCommentDto = new CreateCommentDto(testComment);

                try
                {
                    var createdComment = await _postService.AddCommentAsync(randomPost.Id, randomUser.Id, createCommentDto);
                    if (createdComment != null)
                    {
                        _logger.LogInformation("💬 Created moderation test comment by {Username}: {Content}",
                            randomUser.Username, testComment.Substring(0, Math.Min(30, testComment.Length)) + "...");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Failed to create moderation test comment for user {Username}", randomUser.Username);
                }
            }
        }
    }
}
