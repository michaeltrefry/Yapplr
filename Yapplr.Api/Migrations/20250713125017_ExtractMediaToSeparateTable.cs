using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExtractMediaToSeparateTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageFileName",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ProcessedVideoFileName",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoBitrate",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoCompressionRatio",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoDuration",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoFileName",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoFileSizeBytes",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoFormat",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoHeight",
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

            migrationBuilder.DropColumn(
                name: "VideoWidth",
                table: "Posts");

            migrationBuilder.CreateTable(
                name: "PostMedia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostId = table.Column<int>(type: "integer", nullable: false),
                    MediaType = table.Column<int>(type: "integer", nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ImageFileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    VideoFileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ProcessedVideoFileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    VideoThumbnailFileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    VideoProcessingStatus = table.Column<int>(type: "integer", nullable: false),
                    VideoProcessingStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VideoProcessingCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VideoProcessingError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VideoWidth = table.Column<int>(type: "integer", nullable: true),
                    VideoHeight = table.Column<int>(type: "integer", nullable: true),
                    VideoDuration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    VideoFileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    VideoFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    VideoBitrate = table.Column<double>(type: "double precision", nullable: true),
                    VideoCompressionRatio = table.Column<double>(type: "double precision", nullable: true),
                    OriginalVideoWidth = table.Column<int>(type: "integer", nullable: true),
                    OriginalVideoHeight = table.Column<int>(type: "integer", nullable: true),
                    OriginalVideoDuration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    OriginalVideoFileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    OriginalVideoFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OriginalVideoBitrate = table.Column<double>(type: "double precision", nullable: true),
                    ImageWidth = table.Column<int>(type: "integer", nullable: true),
                    ImageHeight = table.Column<int>(type: "integer", nullable: true),
                    ImageFileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    ImageFormat = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostMedia_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostMedia_CreatedAt",
                table: "PostMedia",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PostMedia_MediaType",
                table: "PostMedia",
                column: "MediaType");

            migrationBuilder.CreateIndex(
                name: "IX_PostMedia_PostId",
                table: "PostMedia",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_PostMedia_PostId_MediaType",
                table: "PostMedia",
                columns: new[] { "PostId", "MediaType" });

            migrationBuilder.CreateIndex(
                name: "IX_PostMedia_VideoProcessingStatus",
                table: "PostMedia",
                column: "VideoProcessingStatus");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostMedia");

            migrationBuilder.AddColumn<string>(
                name: "ImageFileName",
                table: "Posts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessedVideoFileName",
                table: "Posts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "VideoBitrate",
                table: "Posts",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "VideoCompressionRatio",
                table: "Posts",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "VideoDuration",
                table: "Posts",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoFileName",
                table: "Posts",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VideoFileSizeBytes",
                table: "Posts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoFormat",
                table: "Posts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VideoHeight",
                table: "Posts",
                type: "integer",
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

            migrationBuilder.AddColumn<int>(
                name: "VideoWidth",
                table: "Posts",
                type: "integer",
                nullable: true);
        }
    }
}
