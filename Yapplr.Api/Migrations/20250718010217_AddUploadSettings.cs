using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UploadSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MaxImageSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    MaxVideoSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    MaxVideoDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    MaxMediaFilesPerPost = table.Column<int>(type: "integer", nullable: false),
                    AllowedImageExtensions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AllowedVideoExtensions = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeleteOriginalAfterProcessing = table.Column<bool>(type: "boolean", nullable: false),
                    VideoTargetBitrate = table.Column<int>(type: "integer", nullable: false),
                    VideoMaxWidth = table.Column<int>(type: "integer", nullable: false),
                    VideoMaxHeight = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "integer", nullable: true),
                    UpdateReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadSettings_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UploadSettings_UpdatedAt",
                table: "UploadSettings",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UploadSettings_UpdatedByUserId",
                table: "UploadSettings",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UploadSettings");
        }
    }
}
