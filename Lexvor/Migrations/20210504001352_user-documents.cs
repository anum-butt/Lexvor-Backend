using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class userdocuments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true),
                    ProfileId = table.Column<Guid>(nullable: false),
                    UserPlanId = table.Column<Guid>(nullable: false),
                    DocumentType = table.Column<int>(nullable: false),
                    URL = table.Column<string>(nullable: true),
                    GeneratedOn = table.Column<DateTime>(nullable: false),
                    ViewedOn = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDocuments_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_UserDocuments_Plans_UserPlanId",
                        column: x => x.UserPlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserDocuments_ProfileId",
                table: "UserDocuments",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDocuments_UserPlanId",
                table: "UserDocuments",
                column: "UserPlanId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDocuments");
        }
    }
}
