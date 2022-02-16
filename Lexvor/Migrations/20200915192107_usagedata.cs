using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class usagedata : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsageDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    MDN = table.Column<string>(nullable: true),
                    Date = table.Column<DateTime>(nullable: false),
                    SMS = table.Column<int>(nullable: false),
                    Minutes = table.Column<int>(nullable: false),
                    KBData = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageDays", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsageDays");
        }
    }
}
