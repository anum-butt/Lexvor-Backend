using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class portimeivalie : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockedDevice_Devices_DeviceId",
                table: "StockedDevice");

            migrationBuilder.AddColumn<bool>(
                name: "IMEIValid",
                table: "UserDevices",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "IMEI",
                table: "StockedDevice",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "DeviceId",
                table: "StockedDevice",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MDNPortable",
                table: "Plans",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddForeignKey(
                name: "FK_StockedDevice_Devices_DeviceId",
                table: "StockedDevice",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockedDevice_Devices_DeviceId",
                table: "StockedDevice");

            migrationBuilder.DropColumn(
                name: "IMEIValid",
                table: "UserDevices");

            migrationBuilder.DropColumn(
                name: "MDNPortable",
                table: "Plans");

            migrationBuilder.AlterColumn<string>(
                name: "IMEI",
                table: "StockedDevice",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<Guid>(
                name: "DeviceId",
                table: "StockedDevice",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AddForeignKey(
                name: "FK_StockedDevice_Devices_DeviceId",
                table: "StockedDevice",
                column: "DeviceId",
                principalTable: "Devices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
