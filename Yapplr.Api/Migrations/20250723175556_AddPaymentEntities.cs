using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    PaymentProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalPaymentMethodId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Last4 = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    ExpiryMonth = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    ExpiryYear = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    HolderName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BillingEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BillingAddress = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    BillingCity = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BillingState = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BillingPostalCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BillingCountry = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProviderData = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentMethods_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionTierId = table.Column<int>(type: "integer", nullable: false),
                    PaymentProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalSubscriptionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextBillingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsTrialPeriod = table.Column<bool>(type: "boolean", nullable: false),
                    TrialEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentMethodId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PaymentMethodType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PaymentMethodLast4 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    BillingCycleCount = table.Column<int>(type: "integer", nullable: false),
                    GracePeriodEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentMethodId1 = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_PaymentMethods_PaymentMethodId1",
                        column: x => x.PaymentMethodId1,
                        principalTable: "PaymentMethods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_SubscriptionTiers_SubscriptionTierId",
                        column: x => x.SubscriptionTierId,
                        principalTable: "SubscriptionTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    SubscriptionTierId = table.Column<int>(type: "integer", nullable: false),
                    PaymentProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExternalTransactionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ExternalSubscriptionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BillingPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BillingPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProviderResponse = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    WebhookEventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserSubscriptionId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_SubscriptionTiers_SubscriptionTierId",
                        column: x => x.SubscriptionTierId,
                        principalTable: "SubscriptionTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_UserSubscriptions_UserSubscriptionId",
                        column: x => x.UserSubscriptionId,
                        principalTable: "UserSubscriptions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_ExternalPaymentMethodId",
                table: "PaymentMethods",
                column: "ExternalPaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_IsActive",
                table: "PaymentMethods",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_PaymentProvider_ExternalPaymentMethodId",
                table: "PaymentMethods",
                columns: new[] { "PaymentProvider", "ExternalPaymentMethodId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_UserId",
                table: "PaymentMethods",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_UserId_IsDefault",
                table: "PaymentMethods",
                columns: new[] { "UserId", "IsDefault" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_BillingPeriodStart_BillingPeriodEnd",
                table: "PaymentTransactions",
                columns: new[] { "BillingPeriodStart", "BillingPeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_CreatedAt",
                table: "PaymentTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_ExternalSubscriptionId",
                table: "PaymentTransactions",
                column: "ExternalSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_ExternalTransactionId",
                table: "PaymentTransactions",
                column: "ExternalTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PaymentProvider_ExternalTransactionId",
                table: "PaymentTransactions",
                columns: new[] { "PaymentProvider", "ExternalTransactionId" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_Status",
                table: "PaymentTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_SubscriptionTierId",
                table: "PaymentTransactions",
                column: "SubscriptionTierId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_UserId",
                table: "PaymentTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_UserId_Status",
                table: "PaymentTransactions",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_UserSubscriptionId",
                table: "PaymentTransactions",
                column: "UserSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_ExternalSubscriptionId",
                table: "UserSubscriptions",
                column: "ExternalSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_NextBillingDate",
                table: "UserSubscriptions",
                column: "NextBillingDate");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_PaymentMethodId1",
                table: "UserSubscriptions",
                column: "PaymentMethodId1");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_PaymentProvider_ExternalSubscriptionId",
                table: "UserSubscriptions",
                columns: new[] { "PaymentProvider", "ExternalSubscriptionId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_SubscriptionTierId",
                table: "UserSubscriptions",
                column: "SubscriptionTierId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId",
                table: "UserSubscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId_Status",
                table: "UserSubscriptions",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "PaymentMethods");
        }
    }
}
