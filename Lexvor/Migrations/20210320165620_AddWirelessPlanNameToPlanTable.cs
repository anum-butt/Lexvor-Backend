using Microsoft.EntityFrameworkCore.Migrations;	

namespace Lexvor.Migrations {
	public partial class AddWirelessPlanNameToPlanTable : Migration {
		protected override void Up(MigrationBuilder migrationBuilder) {
			migrationBuilder.AddColumn<string>(
				name: "WirelessPlanName",
				table: "Plans",
				nullable: true);
		}

		protected override void Down(MigrationBuilder migrationBuilder) {
			migrationBuilder.DropColumn(
				name: "WirelessPlanName",
				table: "Plans");
		}
	}
}
