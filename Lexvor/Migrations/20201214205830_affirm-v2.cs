using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class affirmv2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AffirmChargeId",
                table: "UserDevices",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PurchasedWithAffirm",
                table: "UserDevices",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AffirmChargeId",
                table: "UserDevices");

            migrationBuilder.DropColumn(
                name: "PurchasedWithAffirm",
                table: "UserDevices");
        }
    }
}
