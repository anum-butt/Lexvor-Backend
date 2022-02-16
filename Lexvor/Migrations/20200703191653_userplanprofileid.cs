using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class userplanprofileid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProfileId",
                table: "UserDevices",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_ProfileId",
                table: "UserDevices",
                column: "ProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserDevices_Profiles_ProfileId",
                table: "UserDevices",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
			migrationBuilder.Sql(@"update UserDevices
									set planid = p.id
									from plans p
									join UserDevices ud on ud.id = p.UserDeviceId");
            migrationBuilder.Sql(@"update UserDevices
						            set profileid = p.profileid
									from UserDevices ud
						            join plans p on p.id = ud.PlanId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserDevices_Profiles_ProfileId",
                table: "UserDevices");

            migrationBuilder.DropIndex(
                name: "IX_UserDevices_ProfileId",
                table: "UserDevices");

            migrationBuilder.DropColumn(
                name: "ProfileId",
                table: "UserDevices");
        }
    }
}
