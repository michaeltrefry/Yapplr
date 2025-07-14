using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:hstore", ",,");

            migrationBuilder.CreateTable(
                name: "NotificationAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    Username = table.Column<string>(type: "text", nullable: true),
                    NotificationType = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Body = table.Column<string>(type: "text", nullable: true),
                    DeliveryMethod = table.Column<string>(type: "text", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    AdditionalDataJson = table.Column<string>(type: "text", nullable: true),
                    ProcessingTime = table.Column<TimeSpan>(type: "interval", nullable: true),
                    SecurityFlags = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationDeliveryConfirmations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    NotificationId = table.Column<string>(type: "text", nullable: false),
                    NotificationType = table.Column<string>(type: "text", nullable: false),
                    DeliveryMethod = table.Column<int>(type: "integer", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDelivered = table.Column<bool>(type: "boolean", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    DeliveryError = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationDeliveryConfirmations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationDeliveryConfirmations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    NotificationId = table.Column<string>(type: "text", nullable: false),
                    NotificationType = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    DataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    WasDelivered = table.Column<bool>(type: "boolean", nullable: false),
                    WasReplayed = table.Column<bool>(type: "boolean", nullable: false),
                    ReplayedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Data = table.Column<Dictionary<string, string>>(type: "hstore", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationHistory_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PreferredMethod = table.Column<int>(type: "integer", nullable: false),
                    EnableMessageNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    EnableMentionNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    EnableReplyNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    EnableCommentNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    EnableFollowNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    EnableLikeNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    EnableRepostNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    EnableFollowRequestNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    MessageDeliveryMethod = table.Column<int>(type: "integer", nullable: false),
                    MentionDeliveryMethod = table.Column<int>(type: "integer", nullable: false),
                    ReplyDeliveryMethod = table.Column<int>(type: "integer", nullable: false),
                    CommentDeliveryMethod = table.Column<int>(type: "integer", nullable: false),
                    FollowDeliveryMethod = table.Column<int>(type: "integer", nullable: false),
                    LikeDeliveryMethod = table.Column<int>(type: "integer", nullable: false),
                    RepostDeliveryMethod = table.Column<int>(type: "integer", nullable: false),
                    FollowRequestDeliveryMethod = table.Column<int>(type: "integer", nullable: false),
                    EnableQuietHours = table.Column<bool>(type: "boolean", nullable: false),
                    QuietHoursStart = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    QuietHoursEnd = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    QuietHoursTimezone = table.Column<string>(type: "text", nullable: false),
                    EnableFrequencyLimits = table.Column<bool>(type: "boolean", nullable: false),
                    MaxNotificationsPerHour = table.Column<int>(type: "integer", nullable: false),
                    MaxNotificationsPerDay = table.Column<int>(type: "integer", nullable: false),
                    RequireDeliveryConfirmation = table.Column<bool>(type: "boolean", nullable: false),
                    EnableReadReceipts = table.Column<bool>(type: "boolean", nullable: false),
                    EnableMessageHistory = table.Column<bool>(type: "boolean", nullable: false),
                    MessageHistoryDays = table.Column<int>(type: "integer", nullable: false),
                    EnableOfflineReplay = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationDeliveryConfirmations_UserId",
                table: "NotificationDeliveryConfirmations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationHistory_UserId",
                table: "NotificationHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId",
                table: "NotificationPreferences",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationAuditLogs");

            migrationBuilder.DropTable(
                name: "NotificationDeliveryConfirmations");

            migrationBuilder.DropTable(
                name: "NotificationHistory");

            migrationBuilder.DropTable(
                name: "NotificationPreferences");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:hstore", ",,");
        }
    }
}
