using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitializeExistingUserTrustScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Initialize trust scores for existing users who have NULL trust scores
            // This calculates a reasonable starting trust score based on their existing activity
            
            migrationBuilder.Sql(@"
                UPDATE ""Users"" 
                SET ""TrustScore"" = CASE
                    -- Users with email verified and some activity get higher scores
                    WHEN ""EmailVerified"" = true AND (
                        SELECT COUNT(*) FROM ""Posts"" WHERE ""UserId"" = ""Users"".""Id""
                    ) > 0 THEN 1.0
                    
                    -- Users with email verified but no posts
                    WHEN ""EmailVerified"" = true THEN 0.9
                    
                    -- Users with posts but no email verification
                    WHEN (
                        SELECT COUNT(*) FROM ""Posts"" WHERE ""UserId"" = ""Users"".""Id""
                    ) > 0 THEN 0.8
                    
                    -- Users with comments but no posts
                    WHEN (
                        SELECT COUNT(*) FROM ""Comments"" WHERE ""UserId"" = ""Users"".""Id""
                    ) > 0 THEN 0.7
                    
                    -- Users with likes but no other activity
                    WHEN (
                        SELECT COUNT(*) FROM ""Likes"" WHERE ""UserId"" = ""Users"".""Id""
                    ) > 0 THEN 0.6
                    
                    -- New users with no activity
                    ELSE 0.5
                END
                WHERE ""TrustScore"" IS NULL;
            ");

            // Create initial trust score history entries for users who got scores assigned
            migrationBuilder.Sql(@"
                INSERT INTO ""UserTrustScoreHistories"" (
                    ""UserId"", 
                    ""PreviousScore"", 
                    ""NewScore"", 
                    ""ScoreChange"", 
                    ""Reason"", 
                    ""Details"", 
                    ""CreatedAt"", 
                    ""IsAutomatic"", 
                    ""CalculatedBy""
                )
                SELECT 
                    ""Id"" as ""UserId"",
                    0.0 as ""PreviousScore"",
                    ""TrustScore"" as ""NewScore"",
                    ""TrustScore"" as ""ScoreChange"",
                    0 as ""Reason"", -- InitialCalculation
                    'Initial trust score calculated during migration based on existing user activity' as ""Details"",
                    NOW() as ""CreatedAt"",
                    true as ""IsAutomatic"",
                    'Migration' as ""CalculatedBy""
                FROM ""Users""
                WHERE ""TrustScore"" IS NOT NULL
                AND NOT EXISTS (
                    SELECT 1 FROM ""UserTrustScoreHistories"" 
                    WHERE ""UserId"" = ""Users"".""Id""
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the trust score history entries created by this migration
            migrationBuilder.Sql(@"
                DELETE FROM ""UserTrustScoreHistories""
                WHERE ""CalculatedBy"" = 'Migration'
                AND ""Details"" LIKE '%Initial trust score calculated during migration%';
            ");

            // Reset trust scores to NULL for users (optional - could be destructive)
            // Commented out to preserve data
            // migrationBuilder.Sql(@"UPDATE ""Users"" SET ""TrustScore"" = NULL;");
        }
    }
}
