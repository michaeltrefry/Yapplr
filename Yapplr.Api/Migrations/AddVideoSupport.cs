using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProcessedVideoFileName",
                table: "Posts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoFileName",
                table: "Posts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VideoProcessingCompletedAt",
                table: "Posts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoProcessingError",
                table: "Posts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VideoProcessingStartedAt",
                table: "Posts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VideoProcessingStatus",
                table: "Posts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VideoThumbnailFileName",
                table: "Posts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessedVideoFileName",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoFileName",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoProcessingCompletedAt",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoProcessingError",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoProcessingStartedAt",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoProcessingStatus",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoThumbnailFileName",
                table: "Posts");
        }
    }
}
