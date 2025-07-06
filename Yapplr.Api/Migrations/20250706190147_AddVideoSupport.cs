using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VideoDurationSeconds",
                table: "Posts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoFileName",
                table: "Posts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoFormat",
                table: "Posts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VideoProcessingStatus",
                table: "Posts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "VideoSizeBytes",
                table: "Posts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoThumbnailFileName",
                table: "Posts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VideoDurationSeconds",
                table: "Messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoFileName",
                table: "Messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoFormat",
                table: "Messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VideoProcessingStatus",
                table: "Messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "VideoSizeBytes",
                table: "Messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoThumbnailFileName",
                table: "Messages",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VideoProcessingJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessedFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ThumbnailFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    ProcessedSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ProcessedFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Resolution = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Bitrate = table.Column<int>(type: "integer", nullable: true),
                    FrameRate = table.Column<double>(type: "double precision", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PostId = table.Column<int>(type: "integer", nullable: true),
                    MessageId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoProcessingJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoProcessingJobs_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VideoProcessingJobs_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VideoProcessingJobs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VideoProcessingJobs_CreatedAt",
                table: "VideoProcessingJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_VideoProcessingJobs_MessageId_Status",
                table: "VideoProcessingJobs",
                columns: new[] { "MessageId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoProcessingJobs_PostId_Status",
                table: "VideoProcessingJobs",
                columns: new[] { "PostId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_VideoProcessingJobs_Status",
                table: "VideoProcessingJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_VideoProcessingJobs_UserId",
                table: "VideoProcessingJobs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideoProcessingJobs");

            migrationBuilder.DropColumn(
                name: "VideoDurationSeconds",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoFileName",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoFormat",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoProcessingStatus",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoSizeBytes",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoThumbnailFileName",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoDurationSeconds",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "VideoFileName",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "VideoFormat",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "VideoProcessingStatus",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "VideoSizeBytes",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "VideoThumbnailFileName",
                table: "Messages");
        }
    }
}
