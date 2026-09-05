using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medzo.CatalogueInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseLongInventoryVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "inventory_items",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "int unsigned");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<uint>(
                name: "Version",
                table: "inventory_items",
                type: "int unsigned",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}
