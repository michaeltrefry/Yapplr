using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedPersonalizationEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContentEmbeddings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ContentId = table.Column<int>(type: "integer", nullable: false),
                    EmbeddingVector = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    Dimensions = table.Column<int>(type: "integer", nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QualityScore = table.Column<float>(type: "real", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentEmbeddings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersonalizationExperiments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Configuration = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    TrafficAllocation = table.Column<float>(type: "real", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalizationExperiments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserInteractionEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    InteractionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetEntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TargetEntityId = table.Column<int>(type: "integer", nullable: true),
                    InteractionStrength = table.Column<float>(type: "real", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: true),
                    Context = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DeviceInfo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SessionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsImplicit = table.Column<bool>(type: "boolean", nullable: false),
                    Sentiment = table.Column<float>(type: "real", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInteractionEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserInteractionEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPersonalizationProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    InterestScores = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    ContentTypePreferences = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    EngagementPatterns = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SimilarUsers = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    PersonalizationConfidence = table.Column<float>(type: "real", nullable: false),
                    DiversityPreference = table.Column<float>(type: "real", nullable: false),
                    NoveltyPreference = table.Column<float>(type: "real", nullable: false),
                    SocialInfluenceFactor = table.Column<float>(type: "real", nullable: false),
                    QualityThreshold = table.Column<float>(type: "real", nullable: false),
                    LastMLUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataPointCount = table.Column<int>(type: "integer", nullable: false),
                    AlgorithmVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPersonalizationProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPersonalizationProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserExperimentParticipations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ExperimentId = table.Column<int>(type: "integer", nullable: false),
                    Variant = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserExperimentParticipations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserExperimentParticipations_PersonalizationExperiments_Exp~",
                        column: x => x.ExperimentId,
                        principalTable: "PersonalizationExperiments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserExperimentParticipations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentEmbeddings_ContentType_ContentId",
                table: "ContentEmbeddings",
                columns: new[] { "ContentType", "ContentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContentEmbeddings_ModelVersion",
                table: "ContentEmbeddings",
                column: "ModelVersion");

            migrationBuilder.CreateIndex(
                name: "IX_ContentEmbeddings_QualityScore",
                table: "ContentEmbeddings",
                column: "QualityScore");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalizationExperiments_IsActive",
                table: "PersonalizationExperiments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalizationExperiments_Name",
                table: "PersonalizationExperiments",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonalizationExperiments_StartDate_EndDate",
                table: "PersonalizationExperiments",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UserExperimentParticipations_ExperimentId",
                table: "UserExperimentParticipations",
                column: "ExperimentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserExperimentParticipations_UserId_ExperimentId",
                table: "UserExperimentParticipations",
                columns: new[] { "UserId", "ExperimentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserExperimentParticipations_Variant",
                table: "UserExperimentParticipations",
                column: "Variant");

            migrationBuilder.CreateIndex(
                name: "IX_UserInteractionEvents_CreatedAt",
                table: "UserInteractionEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserInteractionEvents_InteractionType",
                table: "UserInteractionEvents",
                column: "InteractionType");

            migrationBuilder.CreateIndex(
                name: "IX_UserInteractionEvents_SessionId",
                table: "UserInteractionEvents",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInteractionEvents_TargetEntityType_TargetEntityId",
                table: "UserInteractionEvents",
                columns: new[] { "TargetEntityType", "TargetEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserInteractionEvents_UserId",
                table: "UserInteractionEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPersonalizationProfiles_LastMLUpdate",
                table: "UserPersonalizationProfiles",
                column: "LastMLUpdate");

            migrationBuilder.CreateIndex(
                name: "IX_UserPersonalizationProfiles_PersonalizationConfidence",
                table: "UserPersonalizationProfiles",
                column: "PersonalizationConfidence");

            migrationBuilder.CreateIndex(
                name: "IX_UserPersonalizationProfiles_UserId",
                table: "UserPersonalizationProfiles",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentEmbeddings");

            migrationBuilder.DropTable(
                name: "UserExperimentParticipations");

            migrationBuilder.DropTable(
                name: "UserInteractionEvents");

            migrationBuilder.DropTable(
                name: "UserPersonalizationProfiles");

            migrationBuilder.DropTable(
                name: "PersonalizationExperiments");
        }
    }
}
