using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class edit_package : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CoinAmount",
                schema: "wallet",
                table: "CoinPackages",
                newName: "DisplayOrder");

            migrationBuilder.RenameColumn(
                name: "AmountUSD",
                schema: "wallet",
                table: "CoinPackages",
                newName: "PriceUSD");

            migrationBuilder.RenameColumn(
                name: "AmountInKurus",
                schema: "wallet",
                table: "CoinPackages",
                newName: "CoinAmountInKurus");

            migrationBuilder.RenameIndex(
                name: "IX_CoinPackages_IsActive_AmountUSD",
                schema: "wallet",
                table: "CoinPackages",
                newName: "IX_CoinPackages_IsActive_PriceUSD");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PriceUSD",
                schema: "wallet",
                table: "CoinPackages",
                newName: "AmountUSD");

            migrationBuilder.RenameColumn(
                name: "DisplayOrder",
                schema: "wallet",
                table: "CoinPackages",
                newName: "CoinAmount");

            migrationBuilder.RenameColumn(
                name: "CoinAmountInKurus",
                schema: "wallet",
                table: "CoinPackages",
                newName: "AmountInKurus");

            migrationBuilder.RenameIndex(
                name: "IX_CoinPackages_IsActive_PriceUSD",
                schema: "wallet",
                table: "CoinPackages",
                newName: "IX_CoinPackages_IsActive_AmountUSD");
        }
    }
}
