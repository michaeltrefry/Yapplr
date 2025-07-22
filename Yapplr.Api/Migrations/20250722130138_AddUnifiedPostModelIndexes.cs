using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUnifiedPostModelIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Posts_ParentId",
                table: "Posts");

            migrationBuilder.AddColumn<int>(
                name: "PostId",
                table: "Posts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ParentId_PostType_CreatedAt",
                table: "Posts",
                columns: new[] { "ParentId", "PostType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_PostId",
                table: "Posts",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_PostType_CreatedAt",
                table: "Posts",
                columns: new[] { "PostType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_UserId_PostType_CreatedAt",
                table: "Posts",
                columns: new[] { "UserId", "PostType", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Posts_PostId",
                table: "Posts",
                column: "PostId",
                principalTable: "Posts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Posts_PostId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_ParentId_PostType_CreatedAt",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_PostId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_PostType_CreatedAt",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_UserId_PostType_CreatedAt",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "PostId",
                table: "Posts");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ParentId",
                table: "Posts",
                column: "ParentId");
        }
    }
}
