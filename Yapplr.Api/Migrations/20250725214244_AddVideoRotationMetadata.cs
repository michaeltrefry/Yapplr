using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoRotationMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayVideoHeight",
                table: "PostMedia",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayVideoWidth",
                table: "PostMedia",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginalVideoRotation",
                table: "PostMedia",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProcessedVideoRotation",
                table: "PostMedia",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayVideoHeight",
                table: "PostMedia");

            migrationBuilder.DropColumn(
                name: "DisplayVideoWidth",
                table: "PostMedia");

            migrationBuilder.DropColumn(
                name: "OriginalVideoRotation",
                table: "PostMedia");

            migrationBuilder.DropColumn(
                name: "ProcessedVideoRotation",
                table: "PostMedia");
        }
    }
}
