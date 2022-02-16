using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class portreqnewfields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressLine1",
                table: "PortRequests",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressLine2",
                table: "PortRequests",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "PortRequests",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "PortRequests",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "PortRequests",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MiddleInitial",
                table: "PortRequests",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OSPName",
                table: "PortRequests",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "PortRequests",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Zip",
                table: "PortRequests",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressLine1",
                table: "PortRequests");

            migrationBuilder.DropColumn(
                name: "AddressLine2",
                table: "PortRequests");

            migrationBuilder.DropColumn(
                name: "City",
                table: "PortRequests");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "PortRequests");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "PortRequests");

            migrationBuilder.DropColumn(
                name: "MiddleInitial",
                table: "PortRequests");

            migrationBuilder.DropColumn(
                name: "OSPName",
                table: "PortRequests");

            migrationBuilder.DropColumn(
                name: "State",
                table: "PortRequests");

            migrationBuilder.DropColumn(
                name: "Zip",
                table: "PortRequests");
        }
    }
}
