using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class identityrework : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Addresses_Identities_IdentityId",
                table: "Addresses");

            migrationBuilder.DropIndex(
                name: "IX_Addresses_IdentityId",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "Emails",
                table: "Identities");

            migrationBuilder.DropColumn(
                name: "Names",
                table: "Identities");

            migrationBuilder.DropColumn(
                name: "IdentityId",
                table: "Addresses");

            migrationBuilder.AddColumn<string>(
                name: "AccountFirstName",
                table: "PayAccounts",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountLastName",
                table: "PayAccounts",
                nullable: true);

            migrationBuilder.Sql("update payaccounts set AccountFirstName = AccountName");

            migrationBuilder.DropColumn(
	            name: "AccountName",
	            table: "PayAccounts");

            migrationBuilder.AddColumn<Guid>(
                name: "AddressId",
                table: "Identities",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AuthenticityConfidence",
                table: "Identities",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDate",
                table: "Identities",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                table: "Identities",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "Identities",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Identities",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IdentityDocumentId",
                table: "Identities",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Identities",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Identities_AddressId",
                table: "Identities",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Identities_IdentityDocumentId",
                table: "Identities",
                column: "IdentityDocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Identities_Addresses_AddressId",
                table: "Identities",
                column: "AddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Identities_IdentityDocuments_IdentityDocumentId",
                table: "Identities",
                column: "IdentityDocumentId",
                principalTable: "IdentityDocuments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Identities_Addresses_AddressId",
                table: "Identities");

            migrationBuilder.DropForeignKey(
                name: "FK_Identities_IdentityDocuments_IdentityDocumentId",
                table: "Identities");

            migrationBuilder.DropIndex(
                name: "IX_Identities_AddressId",
                table: "Identities");

            migrationBuilder.DropIndex(
                name: "IX_Identities_IdentityDocumentId",
                table: "Identities");

            migrationBuilder.DropColumn(
                name: "AccountFirstName",
                table: "PayAccounts");

            migrationBuilder.DropColumn(
                name: "AccountLastName",
                table: "PayAccounts");

            migrationBuilder.DropColumn(
                name: "AddressId",
                table: "Identities");

            migrationBuilder.DropColumn(
                name: "AuthenticityConfidence",
                table: "Identities");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                table: "Identities");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "Identities");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "Identities");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Identities");

            migrationBuilder.DropColumn(
                name: "IdentityDocumentId",
                table: "Identities");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Identities");

            migrationBuilder.AddColumn<string>(
                name: "AccountName",
                table: "PayAccounts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Emails",
                table: "Identities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Names",
                table: "Identities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IdentityId",
                table: "Addresses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_IdentityId",
                table: "Addresses",
                column: "IdentityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Addresses_Identities_IdentityId",
                table: "Addresses",
                column: "IdentityId",
                principalTable: "Identities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
