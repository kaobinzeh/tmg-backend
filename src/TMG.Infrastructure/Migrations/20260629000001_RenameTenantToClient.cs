using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameTenantToClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // -------------------------------------------------------
            // Rename tables: SaaS Tenant → Client
            // (Frees up the word "Tenant" for property tenants later)
            // -------------------------------------------------------
            migrationBuilder.RenameTable(
                name: "Tenants",
                schema: "stakeholders",
                newName: "Clients",
                newSchema: "stakeholders");

            migrationBuilder.RenameTable(
                name: "TenantEmailBaseTemplates",
                schema: "notifications",
                newName: "ClientEmailBaseTemplates",
                newSchema: "notifications");

            // -------------------------------------------------------
            // Rename TenantId → ClientId columns across all tables
            // -------------------------------------------------------
            migrationBuilder.RenameColumn(
                name: "TenantId",
                schema: "authentication",
                table: "LoginActivities",
                newName: "ClientId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                schema: "notifications",
                table: "EmailNotificationLogs",
                newName: "ClientId");

            // Table was already renamed above; reference the new name
            migrationBuilder.RenameColumn(
                name: "TenantId",
                schema: "notifications",
                table: "ClientEmailBaseTemplates",
                newName: "ClientId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                schema: "payments",
                table: "SubscriptionActivations",
                newName: "ClientId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                schema: "payments",
                table: "Wallets",
                newName: "ClientId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                schema: "stakeholders",
                table: "Stakeholders",
                newName: "ClientId");

            migrationBuilder.RenameColumn(
                name: "TenantId",
                schema: "stakeholders",
                table: "StakeholderTypes",
                newName: "ClientId");

            // -------------------------------------------------------
            // Rename indexes to match new column/table names
            // -------------------------------------------------------
            migrationBuilder.RenameIndex(
                name: "IX_LoginActivities_TenantId",
                schema: "authentication",
                table: "LoginActivities",
                newName: "IX_LoginActivities_ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_TenantEmailBaseTemplates_TenantId",
                schema: "notifications",
                table: "ClientEmailBaseTemplates",
                newName: "IX_ClientEmailBaseTemplates_ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_Tenants_BrandKey",
                schema: "stakeholders",
                table: "Clients",
                newName: "IX_Clients_BrandKey");

            migrationBuilder.RenameIndex(
                name: "IX_Stakeholders_TenantId",
                schema: "stakeholders",
                table: "Stakeholders",
                newName: "IX_Stakeholders_ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_StakeholderTypes_TenantId",
                schema: "stakeholders",
                table: "StakeholderTypes",
                newName: "IX_StakeholderTypes_ClientId");

            migrationBuilder.RenameIndex(
                name: "IX_StakeholderTypes_TenantId_Key",
                schema: "stakeholders",
                table: "StakeholderTypes",
                newName: "IX_StakeholderTypes_ClientId_Key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse in opposite order: indexes → columns → tables

            migrationBuilder.RenameIndex(
                name: "IX_StakeholderTypes_ClientId_Key",
                schema: "stakeholders",
                table: "StakeholderTypes",
                newName: "IX_StakeholderTypes_TenantId_Key");

            migrationBuilder.RenameIndex(
                name: "IX_StakeholderTypes_ClientId",
                schema: "stakeholders",
                table: "StakeholderTypes",
                newName: "IX_StakeholderTypes_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Stakeholders_ClientId",
                schema: "stakeholders",
                table: "Stakeholders",
                newName: "IX_Stakeholders_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_Clients_BrandKey",
                schema: "stakeholders",
                table: "Clients",
                newName: "IX_Tenants_BrandKey");

            migrationBuilder.RenameIndex(
                name: "IX_ClientEmailBaseTemplates_ClientId",
                schema: "notifications",
                table: "ClientEmailBaseTemplates",
                newName: "IX_TenantEmailBaseTemplates_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_LoginActivities_ClientId",
                schema: "authentication",
                table: "LoginActivities",
                newName: "IX_LoginActivities_TenantId");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                schema: "stakeholders",
                table: "StakeholderTypes",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                schema: "stakeholders",
                table: "Stakeholders",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                schema: "payments",
                table: "Wallets",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                schema: "payments",
                table: "SubscriptionActivations",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                schema: "notifications",
                table: "ClientEmailBaseTemplates",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                schema: "notifications",
                table: "EmailNotificationLogs",
                newName: "TenantId");

            migrationBuilder.RenameColumn(
                name: "ClientId",
                schema: "authentication",
                table: "LoginActivities",
                newName: "TenantId");

            migrationBuilder.RenameTable(
                name: "ClientEmailBaseTemplates",
                schema: "notifications",
                newName: "TenantEmailBaseTemplates",
                newSchema: "notifications");

            migrationBuilder.RenameTable(
                name: "Clients",
                schema: "stakeholders",
                newName: "Tenants",
                newSchema: "stakeholders");
        }
    }
}
