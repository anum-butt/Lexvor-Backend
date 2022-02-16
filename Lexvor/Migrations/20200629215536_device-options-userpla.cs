using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class deviceoptionsuserpla : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "Identities",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "UserDeviceId",
                table: "DeviceOptions",
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeviceOptions_UserDevices_UserDeviceId",
                table: "DeviceOptions");

            migrationBuilder.DropIndex(
                name: "IX_DeviceOptions_UserDeviceId",
                table: "DeviceOptions");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Identities");

            migrationBuilder.DropColumn(
                name: "UserDeviceId",
                table: "DeviceOptions");
        }
    }
}
