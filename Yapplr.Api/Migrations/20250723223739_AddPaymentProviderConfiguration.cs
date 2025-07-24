using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentProviderConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentGlobalConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DefaultProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DefaultCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    GracePeriodDays = table.Column<int>(type: "integer", nullable: false),
                    MaxPaymentRetries = table.Column<int>(type: "integer", nullable: false),
                    RetryIntervalDays = table.Column<int>(type: "integer", nullable: false),
                    EnableTrialPeriods = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultTrialDays = table.Column<int>(type: "integer", nullable: false),
                    EnableProration = table.Column<bool>(type: "boolean", nullable: false),
                    WebhookTimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                    VerifyWebhookSignatures = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentGlobalConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentProviderConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Environment = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    TimeoutSeconds = table.Column<int>(type: "integer", nullable: false),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false),
                    SupportedCurrencies = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentProviderConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentProviderSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PaymentProviderConfigurationId = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    IsSensitive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentProviderSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentProviderSettings_PaymentProviderConfigurations_Payme~",
                        column: x => x.PaymentProviderConfigurationId,
                        principalTable: "PaymentProviderConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderConfigurations_ProviderName",
                table: "PaymentProviderConfigurations",
                column: "ProviderName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentProviderSettings_PaymentProviderConfigurationId_Key",
                table: "PaymentProviderSettings",
                columns: new[] { "PaymentProviderConfigurationId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentGlobalConfigurations");

            migrationBuilder.DropTable(
                name: "PaymentProviderSettings");

            migrationBuilder.DropTable(
                name: "PaymentProviderConfigurations");
        }
    }
}
