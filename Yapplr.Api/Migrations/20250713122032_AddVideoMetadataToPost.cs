using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoMetadataToPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<int>(
                name: "VideoWidth",
                table: "Posts",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                name: "VideoFileSizeBytes",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoFormat",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoHeight",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "VideoWidth",
                table: "Posts");
        }
    }
}
