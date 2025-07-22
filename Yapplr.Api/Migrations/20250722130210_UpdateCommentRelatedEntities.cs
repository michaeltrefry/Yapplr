using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCommentRelatedEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Migrate CommentReactions to PostReactions
            // First, we need to find the Post IDs for the migrated comments
            migrationBuilder.Sql(@"
                INSERT INTO ""PostReactions"" (
                    ""UserId"",
                    ""PostId"",
                    ""ReactionType"",
                    ""CreatedAt"",
                    ""UpdatedAt""
                )
                SELECT
                    cr.""UserId"",
                    p.""Id"" as ""PostId"",  -- Get the new Post ID for the migrated comment
                    cr.""ReactionType"",
                    cr.""CreatedAt"",
                    cr.""UpdatedAt""
                FROM ""CommentReactions"" cr
                INNER JOIN ""Comments"" c ON cr.""CommentId"" = c.""Id""
                INNER JOIN ""Posts"" p ON p.""ParentId"" = c.""PostId""
                    AND p.""PostType"" = 1  -- PostType.Comment
                    AND p.""UserId"" = c.""UserId""
                    AND p.""Content"" = c.""Content""
                    AND p.""CreatedAt"" = c.""CreatedAt""
            ");

            // Step 2: Migrate CommentSystemTags to PostSystemTags
            migrationBuilder.Sql(@"
                INSERT INTO ""PostSystemTags"" (
                    ""PostId"",
                    ""SystemTagId"",
                    ""AppliedByUserId"",
                    ""Reason"",
                    ""AppliedAt""
                )
                SELECT
                    p.""Id"" as ""PostId"",  -- Get the new Post ID for the migrated comment
                    cst.""SystemTagId"",
                    cst.""AppliedByUserId"",
                    cst.""Reason"",
                    cst.""AppliedAt""
                FROM ""CommentSystemTags"" cst
                INNER JOIN ""Comments"" c ON cst.""CommentId"" = c.""Id""
                INNER JOIN ""Posts"" p ON p.""ParentId"" = c.""PostId""
                    AND p.""PostType"" = 1  -- PostType.Comment
                    AND p.""UserId"" = c.""UserId""
                    AND p.""Content"" = c.""Content""
                    AND p.""CreatedAt"" = c.""CreatedAt""
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove migrated PostReactions that came from CommentReactions
            migrationBuilder.Sql(@"
                DELETE FROM ""PostReactions"" pr
                WHERE EXISTS (
                    SELECT 1 FROM ""Posts"" p
                    WHERE p.""Id"" = pr.""PostId""
                    AND p.""PostType"" = 1  -- PostType.Comment
                )
            ");

            // Remove migrated PostSystemTags that came from CommentSystemTags
            migrationBuilder.Sql(@"
                DELETE FROM ""PostSystemTags"" pst
                WHERE EXISTS (
                    SELECT 1 FROM ""Posts"" p
                    WHERE p.""Id"" = pst.""PostId""
                    AND p.""PostType"" = 1  -- PostType.Comment
                )
            ");
        }
    }
}
