using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class AccessoriesStockLevel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChargeType",
                table: "Charges",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStockLevel",
                table: "Accessories",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChargeType",
                table: "Charges");

            migrationBuilder.DropColumn(
                name: "CurrentStockLevel",
                table: "Accessories");
        }
    }
}
