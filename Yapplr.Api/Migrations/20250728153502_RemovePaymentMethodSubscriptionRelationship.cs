using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Yapplr.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemovePaymentMethodSubscriptionRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptions_PaymentMethods_PaymentMethodId1",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_PaymentMethodId1",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "PaymentMethodId1",
                table: "UserSubscriptions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaymentMethodId1",
                table: "UserSubscriptions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_PaymentMethodId1",
                table: "UserSubscriptions",
                column: "PaymentMethodId1");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscriptions_PaymentMethods_PaymentMethodId1",
                table: "UserSubscriptions",
                column: "PaymentMethodId1",
                principalTable: "PaymentMethods",
                principalColumn: "Id");
        }
    }
}
