using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNotificationSystemModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalDataJson",
                table: "NotificationAuditLogs");

            migrationBuilder.DropColumn(
                name: "Body",
                table: "NotificationAuditLogs");

            migrationBuilder.DropColumn(
                name: "DeliveryMethod",
                table: "NotificationAuditLogs");

            migrationBuilder.DropColumn(
                name: "ErrorMessage",
                table: "NotificationAuditLogs");

            migrationBuilder.DropColumn(
                name: "NotificationType",
                table: "NotificationAuditLogs");

            migrationBuilder.DropColumn(
                name: "ProcessingTime",
                table: "NotificationAuditLogs");

            migrationBuilder.DropColumn(
                name: "SecurityFlags",
                table: "NotificationAuditLogs");

            migrationBuilder.DropColumn(
                name: "Success",
                table: "NotificationAuditLogs");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "NotificationAuditLogs");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "NotificationAuditLogs");

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "NotificationAuditLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "NotificationAuditLogs",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "NotificationAuditLogs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalData",
                table: "NotificationAuditLogs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "NotificationAuditLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "NotificationAuditLogs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationAuditLogs_UserId",
                table: "NotificationAuditLogs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationAuditLogs_Users_UserId",
                table: "NotificationAuditLogs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationAuditLogs_Users_UserId",
                table: "NotificationAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_NotificationAuditLogs_UserId",
                table: "NotificationAuditLogs");

            migrationBuilder.DropColumn(
                name: "AdditionalData",
                table: "NotificationAuditLogs");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "NotificationAuditLogs");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "NotificationAuditLogs");

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "NotificationAuditLogs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "NotificationAuditLogs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EventType",
                table: "NotificationAuditLogs",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalDataJson",
                table: "NotificationAuditLogs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Body",
                table: "NotificationAuditLogs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryMethod",
                table: "NotificationAuditLogs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ErrorMessage",
                table: "NotificationAuditLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotificationType",
                table: "NotificationAuditLogs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ProcessingTime",
                table: "NotificationAuditLogs",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityFlags",
                table: "NotificationAuditLogs",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Success",
                table: "NotificationAuditLogs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "NotificationAuditLogs",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "NotificationAuditLogs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
