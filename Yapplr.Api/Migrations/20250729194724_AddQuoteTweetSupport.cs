using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteTweetSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "QuotedPostId",
                table: "Posts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_QuotedPostId_PostType_CreatedAt",
                table: "Posts",
                columns: new[] { "QuotedPostId", "PostType", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Posts_QuotedPostId",
                table: "Posts",
                column: "QuotedPostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Posts_QuotedPostId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_QuotedPostId_PostType_CreatedAt",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "QuotedPostId",
                table: "Posts");
        }
    }
}
