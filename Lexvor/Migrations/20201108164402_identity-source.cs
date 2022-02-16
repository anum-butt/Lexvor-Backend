using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class identitysource : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Identities_Profiles_ProfileId",
                table: "Identities");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOrders_Profiles_ProfileId",
                table: "UserOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOrders_Plans_UserPlanId",
                table: "UserOrders");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserPlanId",
                table: "UserOrders",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProfileId",
                table: "UserOrders",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProfileId",
                table: "Identities",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Identities",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "Identities",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Identities_Profiles_ProfileId",
                table: "Identities",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOrders_Profiles_ProfileId",
                table: "UserOrders",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOrders_Plans_UserPlanId",
                table: "UserOrders",
                column: "UserPlanId",
                principalTable: "Plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

			migrationBuilder.Sql("update Identities set Source = 1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Identities_Profiles_ProfileId",
                table: "Identities");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOrders_Profiles_ProfileId",
                table: "UserOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_UserOrders_Plans_UserPlanId",
                table: "UserOrders");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Identities");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Identities");

            migrationBuilder.AlterColumn<Guid>(
                name: "UserPlanId",
                table: "UserOrders",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AlterColumn<Guid>(
                name: "ProfileId",
                table: "UserOrders",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AlterColumn<Guid>(
                name: "ProfileId",
                table: "Identities",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid));

            migrationBuilder.AddForeignKey(
                name: "FK_Identities_Profiles_ProfileId",
                table: "Identities",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOrders_Profiles_ProfileId",
                table: "UserOrders",
                column: "ProfileId",
                principalTable: "Profiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserOrders_Plans_UserPlanId",
                table: "UserOrders",
                column: "UserPlanId",
                principalTable: "Plans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
