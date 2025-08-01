using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTopicBasedFeedSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RelatedHashtags = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    FollowerCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TopicAnalytics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TopicId = table.Column<int>(type: "integer", nullable: false),
                    TopicName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AnalyticsDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PostCount = table.Column<int>(type: "integer", nullable: false),
                    TotalEngagement = table.Column<int>(type: "integer", nullable: false),
                    UniquePosters = table.Column<int>(type: "integer", nullable: false),
                    AvgEngagementRate = table.Column<float>(type: "real", nullable: false),
                    TrendingScore = table.Column<float>(type: "real", nullable: false),
                    GrowthRate = table.Column<float>(type: "real", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicAnalytics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopicAnalytics_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TopicFollows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TopicName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TopicDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RelatedHashtags = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    InterestLevel = table.Column<float>(type: "real", nullable: false),
                    IncludeInMainFeed = table.Column<bool>(type: "boolean", nullable: false),
                    EnableNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationThreshold = table.Column<float>(type: "real", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TopicId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicFollows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopicFollows_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TopicFollows_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TopicAnalytics_AnalyticsDate",
                table: "TopicAnalytics",
                column: "AnalyticsDate");

            migrationBuilder.CreateIndex(
                name: "IX_TopicAnalytics_TopicId",
                table: "TopicAnalytics",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicAnalytics_TopicName_AnalyticsDate",
                table: "TopicAnalytics",
                columns: new[] { "TopicName", "AnalyticsDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TopicFollows_Category",
                table: "TopicFollows",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_TopicFollows_TopicId",
                table: "TopicFollows",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicFollows_TopicName",
                table: "TopicFollows",
                column: "TopicName");

            migrationBuilder.CreateIndex(
                name: "IX_TopicFollows_UserId",
                table: "TopicFollows",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicFollows_UserId_TopicName",
                table: "TopicFollows",
                columns: new[] { "UserId", "TopicName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Topics_Category",
                table: "Topics",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_IsFeatured",
                table: "Topics",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_Name",
                table: "Topics",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Topics_Slug",
                table: "Topics",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TopicAnalytics");

            migrationBuilder.DropTable(
                name: "TopicFollows");

            migrationBuilder.DropTable(
                name: "Topics");
        }
    }
}
