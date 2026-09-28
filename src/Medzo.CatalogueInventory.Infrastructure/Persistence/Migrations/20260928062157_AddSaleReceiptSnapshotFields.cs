using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medzo.CatalogueInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleReceiptSnapshotFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PharmacistUsername",
                table: "stock_movements",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "stock_movements",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PharmacistUsername",
                table: "stock_movements");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "stock_movements");
        }
    }
}
