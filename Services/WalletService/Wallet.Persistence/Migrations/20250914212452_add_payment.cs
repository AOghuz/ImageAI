using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class add_payment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CoinPackages",
                schema: "wallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AmountUSD = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    AmountInKurus = table.Column<long>(type: "bigint", nullable: false),
                    CoinAmount = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoinPackages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Reference",
                schema: "wallet",
                table: "Transactions",
                column: "Reference");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_WalletAccountId_Type_CreatedAtUtc",
                schema: "wallet",
                table: "Transactions",
                columns: new[] { "WalletAccountId", "Type", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_JobId",
                schema: "wallet",
                table: "Reservations",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Status_ExpiresAtUtc",
                schema: "wallet",
                table: "Reservations",
                columns: new[] { "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_WalletAccountId_Status",
                schema: "wallet",
                table: "Reservations",
                columns: new[] { "WalletAccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Provider_Status_CreatedAtUtc",
                schema: "wallet",
                table: "Payments",
                columns: new[] { "Provider", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ProviderTxnId",
                schema: "wallet",
                table: "Payments",
                column: "ProviderTxnId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserId_IsActive",
                schema: "wallet",
                table: "Accounts",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CoinPackages_IsActive_AmountUSD",
                schema: "wallet",
                table: "CoinPackages",
                columns: new[] { "IsActive", "AmountUSD" });

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Accounts_WalletAccountId",
                schema: "wallet",
                table: "Payments",
                column: "WalletAccountId",
                principalSchema: "wallet",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Accounts_WalletAccountId",
                schema: "wallet",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "CoinPackages",
                schema: "wallet");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_Reference",
                schema: "wallet",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_WalletAccountId_Type_CreatedAtUtc",
                schema: "wallet",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_JobId",
                schema: "wallet",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_Status_ExpiresAtUtc",
                schema: "wallet",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_WalletAccountId_Status",
                schema: "wallet",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Provider_Status_CreatedAtUtc",
                schema: "wallet",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_ProviderTxnId",
                schema: "wallet",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_UserId_IsActive",
                schema: "wallet",
                table: "Accounts");
        }
    }
}
