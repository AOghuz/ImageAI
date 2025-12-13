using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wallet.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class add_currencyatwallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                schema: "wallet",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "PackageId",
                schema: "wallet",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PackageNameSnapshot",
                schema: "wallet",
                table: "Payments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PaymentAmountInKurus",
                schema: "wallet",
                table: "Payments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceUSD",
                schema: "wallet",
                table: "Payments",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                schema: "wallet",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PackageId",
                schema: "wallet",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PackageNameSnapshot",
                schema: "wallet",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PaymentAmountInKurus",
                schema: "wallet",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PriceUSD",
                schema: "wallet",
                table: "Payments");
        }
    }
}
