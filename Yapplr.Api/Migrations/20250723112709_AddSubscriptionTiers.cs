using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubscriptionTierId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SubscriptionTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    BillingCycleMonths = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ShowAdvertisements = table.Column<bool>(type: "boolean", nullable: false),
                    HasVerifiedBadge = table.Column<bool>(type: "boolean", nullable: false),
                    Features = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionTiers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_SubscriptionTierId",
                table: "Users",
                column: "SubscriptionTierId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTiers_IsActive",
                table: "SubscriptionTiers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTiers_Name",
                table: "SubscriptionTiers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTiers_SortOrder",
                table: "SubscriptionTiers",
                column: "SortOrder");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_SubscriptionTiers_SubscriptionTierId",
                table: "Users",
                column: "SubscriptionTierId",
                principalTable: "SubscriptionTiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_SubscriptionTiers_SubscriptionTierId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "SubscriptionTiers");

            migrationBuilder.DropIndex(
                name: "IX_Users_SubscriptionTierId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SubscriptionTierId",
                table: "Users");
        }
    }
}
