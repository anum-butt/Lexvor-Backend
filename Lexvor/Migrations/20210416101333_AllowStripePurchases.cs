using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class AllowStripePurchases : Migration
    {
		protected override void Up(MigrationBuilder migrationBuilder) {


			migrationBuilder.AddColumn<bool>(
				name: "AllowStripePurchases",
				table: "PlanTypes",
				nullable: false,
				defaultValue: false);

		}

        protected override void Down(MigrationBuilder migrationBuilder)
        {
         
            migrationBuilder.DropColumn(
                name: "AllowStripePurchases",
                table: "PlanTypes");
        }
    }
}
