using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class userorderacc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ProfileId = table.Column<Guid>(nullable: true),
                    UserPlanId = table.Column<Guid>(nullable: true),
                    OrderId = table.Column<Guid>(nullable: false),
                    OrderDate = table.Column<DateTime>(nullable: false),
                    Total = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserOrders_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserOrders_Plans_UserPlanId",
                        column: x => x.UserPlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserAccessories",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    OrderId = table.Column<Guid>(nullable: false),
                    Accessory = table.Column<string>(nullable: true),
                    LifetimeWarranty = table.Column<bool>(nullable: false),
                    Price = table.Column<int>(nullable: false),
                    UserOrderId = table.Column<Guid>(nullable: true),
                    UserOrderId1 = table.Column<Guid>(nullable: true),
                    UserOrderId2 = table.Column<Guid>(nullable: true),
                    UserOrderId3 = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccessories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAccessories_UserOrders_UserOrderId",
                        column: x => x.UserOrderId,
                        principalTable: "UserOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAccessories_UserOrders_UserOrderId1",
                        column: x => x.UserOrderId1,
                        principalTable: "UserOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAccessories_UserOrders_UserOrderId2",
                        column: x => x.UserOrderId2,
                        principalTable: "UserOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserAccessories_UserOrders_UserOrderId3",
                        column: x => x.UserOrderId3,
                        principalTable: "UserOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_UserOrders_ProfileId",
                table: "UserOrders",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserOrders_UserPlanId",
                table: "UserOrders",
                column: "UserPlanId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserAccessories");

            migrationBuilder.DropTable(
                name: "UserOrders");
        }
    }
}
