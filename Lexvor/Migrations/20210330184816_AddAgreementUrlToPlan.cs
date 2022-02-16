using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class AddAgreementUrlToPlan : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgreementUrl",
                table: "Plans",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgreementUrl",
                table: "Plans");
        }
    }
}
