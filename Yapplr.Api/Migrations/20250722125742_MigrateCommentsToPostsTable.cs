using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class MigrateCommentsToPostsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migrate all comments to the Posts table
            migrationBuilder.Sql(@"
                INSERT INTO ""Posts"" (
                    ""Content"",
                    ""CreatedAt"",
                    ""UpdatedAt"",
                    ""IsHidden"",
                    ""HiddenByUserId"",
                    ""HiddenAt"",
                    ""HiddenReason"",
                    ""IsFlagged"",
                    ""FlaggedReason"",
                    ""FlaggedAt"",
                    ""IsDeletedByUser"",
                    ""DeletedByUserAt"",
                    ""UserId"",
                    ""ParentId"",
                    ""PostType"",
                    ""Privacy"",
                    ""HiddenReasonType"",
                    ""IsHiddenDuringVideoProcessing""
                )
                SELECT
                    c.""Content"",
                    c.""CreatedAt"",
                    c.""UpdatedAt"",
                    c.""IsHidden"",
                    c.""HiddenByUserId"",
                    c.""HiddenAt"",
                    c.""HiddenReason"",
                    c.""IsFlagged"",
                    c.""FlaggedReason"",
                    c.""FlaggedAt"",
                    c.""IsDeletedByUser"",
                    c.""DeletedByUserAt"",
                    c.""UserId"",
                    c.""PostId"" as ""ParentId"",
                    1 as ""PostType"",  -- PostType.Comment = 1
                    0 as ""Privacy"",   -- PostPrivacy.Public = 0 (comments inherit parent privacy)
                    CASE
                        WHEN c.""IsDeletedByUser"" = true THEN 1  -- PostHiddenReasonType.DeletedByUser = 1
                        WHEN c.""IsHidden"" = true THEN 2         -- PostHiddenReasonType.ModeratorHidden = 2
                        ELSE 0                                    -- PostHiddenReasonType.None = 0
                    END as ""HiddenReasonType"",
                    false as ""IsHiddenDuringVideoProcessing""
                FROM ""Comments"" c
                ORDER BY c.""Id""
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove all migrated comments from Posts table
            migrationBuilder.Sql(@"
                DELETE FROM ""Posts""
                WHERE ""PostType"" = 1  -- PostType.Comment = 1
            ");
        }
    }
}
