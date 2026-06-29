using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenamePaymentTransactionTenantIdToClientId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The RenameTenantToClient migration renamed TenantId -> ClientId across the domain
            // but omitted payments.PaymentTransactions, leaving the column out of sync with the model.
            migrationBuilder.RenameColumn(
                name: "TenantId",
                schema: "payments",
                table: "PaymentTransactions",
                newName: "ClientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ClientId",
                schema: "payments",
                table: "PaymentTransactions",
                newName: "TenantId");
        }
    }
}
