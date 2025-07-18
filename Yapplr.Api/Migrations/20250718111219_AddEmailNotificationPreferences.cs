using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmailDigestFrequencyHours",
                table: "NotificationPreferences",
                type: "integer",
                nullable: false,
                defaultValue: 24);

            migrationBuilder.AddColumn<bool>(
                name: "EnableEmailDigest",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableEmailNotifications",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableInstantEmailNotifications",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailDigestFrequencyHours",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "EnableEmailDigest",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "EnableEmailNotifications",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "EnableInstantEmailNotifications",
                table: "NotificationPreferences");
        }
    }
}
