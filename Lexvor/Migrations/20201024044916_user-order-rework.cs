using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class userorderrework : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAccessories_UserOrders_UserOrderId",
                table: "UserAccessories");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAccessories_UserOrders_UserOrderId1",
                table: "UserAccessories");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAccessories_UserOrders_UserOrderId2",
                table: "UserAccessories");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAccessories_UserOrders_UserOrderId3",
                table: "UserAccessories");

            migrationBuilder.DropIndex(
                name: "IX_UserAccessories_UserOrderId",
                table: "UserAccessories");

            migrationBuilder.DropIndex(
                name: "IX_UserAccessories_UserOrderId1",
                table: "UserAccessories");

            migrationBuilder.DropIndex(
                name: "IX_UserAccessories_UserOrderId2",
                table: "UserAccessories");

            migrationBuilder.DropIndex(
                name: "IX_UserAccessories_UserOrderId3",
                table: "UserAccessories");

            migrationBuilder.DropColumn(
                name: "UserOrderId",
                table: "UserAccessories");

            migrationBuilder.DropColumn(
                name: "UserOrderId1",
                table: "UserAccessories");

            migrationBuilder.DropColumn(
                name: "UserOrderId2",
                table: "UserAccessories");

            migrationBuilder.DropColumn(
                name: "UserOrderId3",
                table: "UserAccessories");

            migrationBuilder.AddColumn<Guid>(
                name: "Accessory1Id",
                table: "UserOrders",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Accessory2Id",
                table: "UserOrders",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Accessory3Id",
                table: "UserOrders",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LifetimeWarrantyPrice",
                table: "UserAccessories",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserOrders_Accessory1Id",
                table: "UserOrders",
                column: "Accessory1Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrders_Accessory2Id",
                table: "UserOrders",
                column: "Accessory2Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrders_Accessory3Id",
                table: "UserOrders",
                column: "Accessory3Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserOrders_UserAccessories_Accessory1Id",
                table: "UserOrders",
                column: "Accessory1Id",
                principalTable: "UserAccessories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOrders_UserAccessories_Accessory2Id",
                table: "UserOrders",
                column: "Accessory2Id",
                principalTable: "UserAccessories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOrders_UserAccessories_Accessory3Id",
                table: "UserOrders",
                column: "Accessory3Id",
                principalTable: "UserAccessories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserOrders_UserAccessories_Accessory1Id",
                table: "UserOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOrders_UserAccessories_Accessory2Id",
                table: "UserOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOrders_UserAccessories_Accessory3Id",
                table: "UserOrders");

            migrationBuilder.DropIndex(
                name: "IX_UserOrders_Accessory1Id",
                table: "UserOrders");

            migrationBuilder.DropIndex(
                name: "IX_UserOrders_Accessory2Id",
                table: "UserOrders");

            migrationBuilder.DropIndex(
                name: "IX_UserOrders_Accessory3Id",
                table: "UserOrders");

            migrationBuilder.DropColumn(
                name: "Accessory1Id",
                table: "UserOrders");

            migrationBuilder.DropColumn(
                name: "Accessory2Id",
                table: "UserOrders");

            migrationBuilder.DropColumn(
                name: "Accessory3Id",
                table: "UserOrders");

            migrationBuilder.DropColumn(
                name: "LifetimeWarrantyPrice",
                table: "UserAccessories");

            migrationBuilder.AddColumn<Guid>(
                name: "UserOrderId",
                table: "UserAccessories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserOrderId1",
                table: "UserAccessories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserOrderId2",
                table: "UserAccessories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserOrderId3",
                table: "UserAccessories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessories_UserOrderId",
                table: "UserAccessories",
                column: "UserOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessories_UserOrderId1",
                table: "UserAccessories",
                column: "UserOrderId1");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessories_UserOrderId2",
                table: "UserAccessories",
                column: "UserOrderId2");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccessories_UserOrderId3",
                table: "UserAccessories",
                column: "UserOrderId3");

            migrationBuilder.AddForeignKey(
                name: "FK_UserAccessories_UserOrders_UserOrderId",
                table: "UserAccessories",
                column: "UserOrderId",
                principalTable: "UserOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAccessories_UserOrders_UserOrderId1",
                table: "UserAccessories",
                column: "UserOrderId1",
                principalTable: "UserOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAccessories_UserOrders_UserOrderId2",
                table: "UserAccessories",
                column: "UserOrderId2",
                principalTable: "UserOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAccessories_UserOrders_UserOrderId3",
                table: "UserAccessories",
                column: "UserOrderId3",
                principalTable: "UserOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
