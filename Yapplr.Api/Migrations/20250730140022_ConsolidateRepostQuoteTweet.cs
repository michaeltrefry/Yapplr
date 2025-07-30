using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateRepostQuoteTweet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Posts_QuotedPostId",
                table: "Posts");

            migrationBuilder.RenameColumn(
                name: "QuotedPostId",
                table: "Posts",
                newName: "RepostedPostId");

            migrationBuilder.RenameIndex(
                name: "IX_Posts_QuotedPostId_PostType_CreatedAt",
                table: "Posts",
                newName: "IX_Posts_RepostedPostId_PostType_CreatedAt");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Posts_RepostedPostId",
                table: "Posts",
                column: "RepostedPostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Posts_RepostedPostId",
                table: "Posts");

            migrationBuilder.RenameColumn(
                name: "RepostedPostId",
                table: "Posts",
                newName: "QuotedPostId");

            migrationBuilder.RenameIndex(
                name: "IX_Posts_RepostedPostId_PostType_CreatedAt",
                table: "Posts",
                newName: "IX_Posts_QuotedPostId_PostType_CreatedAt");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Posts_QuotedPostId",
                table: "Posts",
                column: "QuotedPostId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
