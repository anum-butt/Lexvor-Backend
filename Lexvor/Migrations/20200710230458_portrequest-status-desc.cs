using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class portrequeststatusdesc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StatusDescription",
                table: "PortRequests",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StatusDescription",
                table: "PortRequests");
        }
    }
}
