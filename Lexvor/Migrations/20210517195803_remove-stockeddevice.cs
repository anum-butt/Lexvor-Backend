using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class removestockeddevice : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LossClaims_StockedDevice_StockedDeviceId",
                table: "LossClaims");

            migrationBuilder.DropIndex(
                name: "IX_LossClaims_StockedDeviceId",
                table: "LossClaims");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_LossClaims_StockedDeviceId",
                table: "LossClaims",
                column: "StockedDeviceId");

            migrationBuilder.AddForeignKey(
                name: "FK_LossClaims_StockedDevice_StockedDeviceId",
                table: "LossClaims",
                column: "StockedDeviceId",
                principalTable: "StockedDevice",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
