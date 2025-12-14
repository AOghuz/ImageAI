using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addnewmig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "wallet");

            migrationBuilder.CreateTable(
                name: "CoinPackages",
                schema: "wallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CoinAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoinPackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServicePrices",
                schema: "wallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelSystemName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WalletAccounts",
                schema: "wallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRecords",
                schema: "wallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WalletAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ProviderGroupKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false, defaultValue: "TRY"),
                    CoinAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PackageNameSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRecords_WalletAccounts_WalletAccountId",
                        column: x => x.WalletAccountId,
                        principalSchema: "wallet",
                        principalTable: "WalletAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservations",
                schema: "wallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WalletAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelSystemName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RelatedTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_WalletAccounts_WalletAccountId",
                        column: x => x.WalletAccountId,
                        principalSchema: "wallet",
                        principalTable: "WalletAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                schema: "wallet",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WalletAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReferenceId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_WalletAccounts_WalletAccountId",
                        column: x => x.WalletAccountId,
                        principalSchema: "wallet",
                        principalTable: "WalletAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoinPackages_IsActive_DisplayOrder",
                schema: "wallet",
                table: "CoinPackages",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_IdempotencyKey",
                schema: "wallet",
                table: "PaymentRecords",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_ProviderTransactionId",
                schema: "wallet",
                table: "PaymentRecords",
                column: "ProviderTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_WalletAccountId",
                schema: "wallet",
                table: "PaymentRecords",
                column: "WalletAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ExpiresAt",
                schema: "wallet",
                table: "Reservations",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Status",
                schema: "wallet",
                table: "Reservations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_WalletAccountId",
                schema: "wallet",
                table: "Reservations",
                column: "WalletAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePrices_ModelSystemName",
                schema: "wallet",
                table: "ServicePrices",
                column: "ModelSystemName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletAccounts_UserId",
                schema: "wallet",
                table: "WalletAccounts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_CreatedAt",
                schema: "wallet",
                table: "WalletTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_ReferenceId",
                schema: "wallet",
                table: "WalletTransactions",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_WalletAccountId",
                schema: "wallet",
                table: "WalletTransactions",
                column: "WalletAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CoinPackages",
                schema: "wallet");

            migrationBuilder.DropTable(
                name: "PaymentRecords",
                schema: "wallet");

            migrationBuilder.DropTable(
                name: "Reservations",
                schema: "wallet");

            migrationBuilder.DropTable(
                name: "ServicePrices",
                schema: "wallet");

            migrationBuilder.DropTable(
                name: "WalletTransactions",
                schema: "wallet");

            migrationBuilder.DropTable(
                name: "WalletAccounts",
                schema: "wallet");
        }
    }
}
