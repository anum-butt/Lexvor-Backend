using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class linepricing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableDeviceOrdering",
                table: "PlanTypes",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LinePricing1Id",
                table: "PlanTypes",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LinePricing2Id",
                table: "PlanTypes",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LinePricing3Id",
                table: "PlanTypes",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LinePricing4Id",
                table: "PlanTypes",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "LastBalance",
                table: "PayAccounts",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastBalanceCheck",
                table: "PayAccounts",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Devices",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OptionValue",
                table: "DeviceOptions",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "OptionGroup",
                table: "DeviceOptions",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "LinePricing",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequiredNumOfLines = table.Column<int>(nullable: false),
                    InitiationFee = table.Column<int>(nullable: false),
                    MonthlyCost = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LinePricing", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhooksResponses",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ObjectType = table.Column<string>(nullable: true),
                    ObjectId = table.Column<string>(nullable: true),
                    Received = table.Column<DateTime>(nullable: false),
                    ReceivedAction = table.Column<string>(nullable: true),
                    Text = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhooksResponses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlanTypes_LinePricing1Id",
                table: "PlanTypes",
                column: "LinePricing1Id");

            migrationBuilder.CreateIndex(
                name: "IX_PlanTypes_LinePricing2Id",
                table: "PlanTypes",
                column: "LinePricing2Id");

            migrationBuilder.CreateIndex(
                name: "IX_PlanTypes_LinePricing3Id",
                table: "PlanTypes",
                column: "LinePricing3Id");

            migrationBuilder.CreateIndex(
                name: "IX_PlanTypes_LinePricing4Id",
                table: "PlanTypes",
                column: "LinePricing4Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlanTypes_LinePricing_LinePricing1Id",
                table: "PlanTypes",
                column: "LinePricing1Id",
                principalTable: "LinePricing",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlanTypes_LinePricing_LinePricing2Id",
                table: "PlanTypes",
                column: "LinePricing2Id",
                principalTable: "LinePricing",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlanTypes_LinePricing_LinePricing3Id",
                table: "PlanTypes",
                column: "LinePricing3Id",
                principalTable: "LinePricing",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlanTypes_LinePricing_LinePricing4Id",
                table: "PlanTypes",
                column: "LinePricing4Id",
                principalTable: "LinePricing",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlanTypes_LinePricing_LinePricing1Id",
                table: "PlanTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_PlanTypes_LinePricing_LinePricing2Id",
                table: "PlanTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_PlanTypes_LinePricing_LinePricing3Id",
                table: "PlanTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_PlanTypes_LinePricing_LinePricing4Id",
                table: "PlanTypes");

            migrationBuilder.DropTable(
                name: "LinePricing");

            migrationBuilder.DropTable(
                name: "WebhooksResponses");

            migrationBuilder.DropIndex(
                name: "IX_PlanTypes_LinePricing1Id",
                table: "PlanTypes");

            migrationBuilder.DropIndex(
                name: "IX_PlanTypes_LinePricing2Id",
                table: "PlanTypes");

            migrationBuilder.DropIndex(
                name: "IX_PlanTypes_LinePricing3Id",
                table: "PlanTypes");

            migrationBuilder.DropIndex(
                name: "IX_PlanTypes_LinePricing4Id",
                table: "PlanTypes");

            migrationBuilder.DropColumn(
                name: "EnableDeviceOrdering",
                table: "PlanTypes");

            migrationBuilder.DropColumn(
                name: "LinePricing1Id",
                table: "PlanTypes");

            migrationBuilder.DropColumn(
                name: "LinePricing2Id",
                table: "PlanTypes");

            migrationBuilder.DropColumn(
                name: "LinePricing3Id",
                table: "PlanTypes");

            migrationBuilder.DropColumn(
                name: "LinePricing4Id",
                table: "PlanTypes");

            migrationBuilder.DropColumn(
                name: "LastBalance",
                table: "PayAccounts");

            migrationBuilder.DropColumn(
                name: "LastBalanceCheck",
                table: "PayAccounts");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Devices",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "OptionValue",
                table: "DeviceOptions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string));

            migrationBuilder.AlterColumn<string>(
                name: "OptionGroup",
                table: "DeviceOptions",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string));
        }
    }
}
