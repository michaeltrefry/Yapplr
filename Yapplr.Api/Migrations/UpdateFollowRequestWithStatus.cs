using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFollowRequestWithStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FollowRequests_RequesterId_RequestedId",
                table: "FollowRequests");

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "FollowRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "FollowRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_FollowRequests_RequesterId_RequestedId_Status",
                table: "FollowRequests",
                columns: new[] { "RequesterId", "RequestedId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FollowRequests_RequesterId_RequestedId_Status",
                table: "FollowRequests");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "FollowRequests");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "FollowRequests");

            migrationBuilder.CreateIndex(
                name: "IX_FollowRequests_RequesterId_RequestedId",
                table: "FollowRequests",
                columns: new[] { "RequesterId", "RequestedId" },
                unique: true);
        }
    }
}
