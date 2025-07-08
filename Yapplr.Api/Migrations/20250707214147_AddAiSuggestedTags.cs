using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAiSuggestedTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiSuggestedTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostId = table.Column<int>(type: "integer", nullable: false),
                    CommentId = table.Column<int>(type: "integer", nullable: true),
                    TagName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    RiskLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RequiresReview = table.Column<bool>(type: "boolean", nullable: false),
                    SuggestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    IsRejected = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedByUserId = table.Column<int>(type: "integer", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovalReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AnalysisDetails = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SentimentScore = table.Column<double>(type: "double precision", nullable: true),
                    SentimentLabel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiSuggestedTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiSuggestedTags_Comments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "Comments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AiSuggestedTags_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AiSuggestedTags_Users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiSuggestedTags_ApprovalStatus",
                table: "AiSuggestedTags",
                columns: new[] { "IsApproved", "IsRejected" });

            migrationBuilder.CreateIndex(
                name: "IX_AiSuggestedTags_ApprovedByUserId",
                table: "AiSuggestedTags",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiSuggestedTags_CommentId_TagName",
                table: "AiSuggestedTags",
                columns: new[] { "CommentId", "TagName" });

            migrationBuilder.CreateIndex(
                name: "IX_AiSuggestedTags_PostId_TagName",
                table: "AiSuggestedTags",
                columns: new[] { "PostId", "TagName" });

            migrationBuilder.CreateIndex(
                name: "IX_AiSuggestedTags_SuggestedAt",
                table: "AiSuggestedTags",
                column: "SuggestedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiSuggestedTags");
        }
    }
}
