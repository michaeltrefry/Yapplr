using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLinkPreviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LinkPreviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    SiteName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinkPreviews", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PostLinkPreviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostId = table.Column<int>(type: "integer", nullable: false),
                    LinkPreviewId = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostLinkPreviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostLinkPreviews_LinkPreviews_LinkPreviewId",
                        column: x => x.LinkPreviewId,
                        principalTable: "LinkPreviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostLinkPreviews_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LinkPreviews_CreatedAt",
                table: "LinkPreviews",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LinkPreviews_Status",
                table: "LinkPreviews",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LinkPreviews_Url",
                table: "LinkPreviews",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostLinkPreviews_LinkPreviewId",
                table: "PostLinkPreviews",
                column: "LinkPreviewId");

            migrationBuilder.CreateIndex(
                name: "IX_PostLinkPreviews_PostId",
                table: "PostLinkPreviews",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_PostLinkPreviews_PostId_LinkPreviewId",
                table: "PostLinkPreviews",
                columns: new[] { "PostId", "LinkPreviewId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostLinkPreviews");

            migrationBuilder.DropTable(
                name: "LinkPreviews");
        }
    }
}
