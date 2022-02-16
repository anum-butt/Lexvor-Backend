using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class externalwirelessid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExternalWirelessCustomerId",
                table: "Profiles",
                nullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "LastBalance",
                table: "PayAccounts",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExternalWirelessCustomerId",
                table: "Profiles");

            migrationBuilder.AlterColumn<float>(
                name: "LastBalance",
                table: "PayAccounts",
                type: "real",
                nullable: true,
                oldClrType: typeof(double),
                oldNullable: true);
        }
    }
}
