using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class userplanoptionsfix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeviceOptions_UserDevices_UserDeviceId",
                table: "DeviceOptions");

            migrationBuilder.DropIndex(
                name: "IX_DeviceOptions_UserDeviceId",
                table: "DeviceOptions");

            migrationBuilder.DropColumn(
                name: "UserDeviceId",
                table: "DeviceOptions");

            migrationBuilder.AddColumn<string>(
                name: "ChosenOptions",
                table: "UserDevices",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChosenOptions",
                table: "UserDevices");

            migrationBuilder.AddColumn<Guid>(
                name: "UserDeviceId",
                table: "DeviceOptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceOptions_UserDeviceId",
                table: "DeviceOptions",
                column: "UserDeviceId");

            migrationBuilder.AddForeignKey(
                name: "FK_DeviceOptions_UserDevices_UserDeviceId",
                table: "DeviceOptions",
                column: "UserDeviceId",
                principalTable: "UserDevices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
