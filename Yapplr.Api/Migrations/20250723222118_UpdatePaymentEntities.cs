using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePaymentEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastRetryDate",
                table: "UserSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSyncDate",
                table: "UserSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "UserSubscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "TrialProcessed",
                table: "UserSubscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastRetryDate",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "LastSyncDate",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "TrialProcessed",
                table: "UserSubscriptions");
        }
    }
}
