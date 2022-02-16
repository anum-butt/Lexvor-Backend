using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class upgradedeviceid : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UpgradeDeviceId",
                table: "Plans",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plans_UpgradeDeviceId",
                table: "Plans",
                column: "UpgradeDeviceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Plans_Devices_UpgradeDeviceId",
                table: "Plans",
                column: "UpgradeDeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Plans_Devices_UpgradeDeviceId",
                table: "Plans");

            migrationBuilder.DropIndex(
                name: "IX_Plans_UpgradeDeviceId",
                table: "Plans");

            migrationBuilder.DropColumn(
                name: "UpgradeDeviceId",
                table: "Plans");
        }
    }
}
