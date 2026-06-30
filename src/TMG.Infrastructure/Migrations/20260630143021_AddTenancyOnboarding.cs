using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenancyOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tenancies");

            migrationBuilder.CreateTable(
                name: "Tenancies",
                schema: "tenancies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    PropertyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantStakeholderId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TermsAcceptedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AcceptedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_Tenancies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenancyDocuments",
                schema: "tenancies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenancyId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UploadedByStakeholderId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_TenancyDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenancyDocuments_Tenancies_TenancyId",
                        column: x => x.TenancyId,
                        principalSchema: "tenancies",
                        principalTable: "Tenancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tenancies_ClientId",
                schema: "tenancies",
                table: "Tenancies",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenancies_TenantStakeholderId",
                schema: "tenancies",
                table: "Tenancies",
                column: "TenantStakeholderId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenancies_UnitId",
                schema: "tenancies",
                table: "Tenancies",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_TenancyDocuments_ClientId",
                schema: "tenancies",
                table: "TenancyDocuments",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_TenancyDocuments_TenancyId",
                schema: "tenancies",
                table: "TenancyDocuments",
                column: "TenancyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenancyDocuments",
                schema: "tenancies");

            migrationBuilder.DropTable(
                name: "Tenancies",
                schema: "tenancies");
        }
    }
}
