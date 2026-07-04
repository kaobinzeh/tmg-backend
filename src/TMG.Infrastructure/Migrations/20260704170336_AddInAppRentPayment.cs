using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInAppRentPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PaymentTransactionId",
                schema: "tenancies",
                table: "RentPayments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenancyId",
                schema: "payments",
                table: "PaymentTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RentPayments_PaymentTransactionId",
                schema: "tenancies",
                table: "RentPayments",
                column: "PaymentTransactionId",
                unique: true,
                filter: "\"PaymentTransactionId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RentPayments_PaymentTransactionId",
                schema: "tenancies",
                table: "RentPayments");

            migrationBuilder.DropColumn(
                name: "PaymentTransactionId",
                schema: "tenancies",
                table: "RentPayments");

            migrationBuilder.DropColumn(
                name: "TenancyId",
                schema: "payments",
                table: "PaymentTransactions");
        }
    }
}
