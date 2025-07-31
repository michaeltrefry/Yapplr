using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCommentIdFromMention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mentions_Posts_CommentId",
                table: "Mentions");

            migrationBuilder.DropIndex(
                name: "IX_Mentions_CommentId",
                table: "Mentions");

            migrationBuilder.DropIndex(
                name: "IX_Mentions_MentionedUserId_CommentId",
                table: "Mentions");

            migrationBuilder.DropColumn(
                name: "CommentId",
                table: "Mentions");

            migrationBuilder.AlterColumn<int>(
                name: "PostId",
                table: "Mentions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "PostId",
                table: "Mentions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "CommentId",
                table: "Mentions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mentions_CommentId",
                table: "Mentions",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Mentions_MentionedUserId_CommentId",
                table: "Mentions",
                columns: new[] { "MentionedUserId", "CommentId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Mentions_Posts_CommentId",
                table: "Mentions",
                column: "CommentId",
                principalTable: "Posts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
