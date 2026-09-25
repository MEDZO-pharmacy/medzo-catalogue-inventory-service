using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Medzo.CatalogueInventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowConfirmedMedicineDuplicates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_medicines_NormalizedName",
                table: "medicines");

            migrationBuilder.CreateIndex(
                name: "IX_medicines_NormalizedName",
                table: "medicines",
                column: "NormalizedName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_medicines_NormalizedName",
                table: "medicines");

            migrationBuilder.CreateIndex(
                name: "IX_medicines_NormalizedName",
                table: "medicines",
                column: "NormalizedName",
                unique: true);
        }
    }
}
