using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRentPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastRentPaidAtUtc",
                schema: "tenancies",
                table: "Tenancies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TermMonths",
                schema: "tenancies",
                table: "Tenancies",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RentPayments",
                schema: "tenancies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenancyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaidAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PeriodEndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RecordedByStakeholderId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReceiptDocumentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RentPayments_Tenancies_TenancyId",
                        column: x => x.TenancyId,
                        principalSchema: "tenancies",
                        principalTable: "Tenancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RentPayments_ClientId",
                schema: "tenancies",
                table: "RentPayments",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_RentPayments_TenancyId",
                schema: "tenancies",
                table: "RentPayments",
                column: "TenancyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RentPayments",
                schema: "tenancies");

            migrationBuilder.DropColumn(
                name: "LastRentPaidAtUtc",
                schema: "tenancies",
                table: "Tenancies");

            migrationBuilder.DropColumn(
                name: "TermMonths",
                schema: "tenancies",
                table: "Tenancies");
        }
    }
}
