using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenancyRentCycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UploadedByStakeholderId",
                schema: "tenancies",
                table: "TenancyDocuments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CycleEndUtc",
                schema: "tenancies",
                table: "Tenancies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CycleStartUtc",
                schema: "tenancies",
                table: "Tenancies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextRentDueUtc",
                schema: "tenancies",
                table: "Tenancies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProposedLeaseStartUtc",
                schema: "tenancies",
                table: "Tenancies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProposedTermMonths",
                schema: "tenancies",
                table: "Tenancies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Reminder1MonthSentAtUtc",
                schema: "tenancies",
                table: "Tenancies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "Reminder3MonthsSentAtUtc",
                schema: "tenancies",
                table: "Tenancies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenancies_Status_NextRentDueUtc",
                schema: "tenancies",
                table: "Tenancies",
                columns: new[] { "Status", "NextRentDueUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenancies_Status_NextRentDueUtc",
                schema: "tenancies",
                table: "Tenancies");

            migrationBuilder.DropColumn(
                name: "CycleEndUtc",
                schema: "tenancies",
                table: "Tenancies");

            migrationBuilder.DropColumn(
                name: "CycleStartUtc",
                schema: "tenancies",
                table: "Tenancies");

            migrationBuilder.DropColumn(
                name: "NextRentDueUtc",
                schema: "tenancies",
                table: "Tenancies");

            migrationBuilder.DropColumn(
                name: "ProposedLeaseStartUtc",
                schema: "tenancies",
                table: "Tenancies");

            migrationBuilder.DropColumn(
                name: "ProposedTermMonths",
                schema: "tenancies",
                table: "Tenancies");

            migrationBuilder.DropColumn(
                name: "Reminder1MonthSentAtUtc",
                schema: "tenancies",
                table: "Tenancies");

            migrationBuilder.DropColumn(
                name: "Reminder3MonthsSentAtUtc",
                schema: "tenancies",
                table: "Tenancies");

            migrationBuilder.AlterColumn<Guid>(
                name: "UploadedByStakeholderId",
                schema: "tenancies",
                table: "TenancyDocuments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
