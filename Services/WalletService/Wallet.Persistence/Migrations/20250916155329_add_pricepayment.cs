using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class add_pricepayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentAmountInKurus",
                schema: "wallet",
                table: "Payments");

            migrationBuilder.RenameColumn(
                name: "PriceUSD",
                schema: "wallet",
                table: "Payments",
                newName: "Price");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Price",
                schema: "wallet",
                table: "Payments",
                newName: "PriceUSD");

            migrationBuilder.AddColumn<long>(
                name: "PaymentAmountInKurus",
                schema: "wallet",
                table: "Payments",
                type: "bigint",
                nullable: true);
        }
    }
}
