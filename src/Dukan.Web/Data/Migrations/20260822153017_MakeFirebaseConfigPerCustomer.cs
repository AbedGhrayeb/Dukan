using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dukan.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeFirebaseConfigPerCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing global config (without CustomerId) is not valid for per-customer model — remove it.
            // Admin will re-add per customer via UI.
            migrationBuilder.Sql("DELETE FROM [FirebaseConfigs]");

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "FirebaseConfigs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_FirebaseConfigs_CustomerId",
                table: "FirebaseConfigs",
                column: "CustomerId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FirebaseConfigs_Customers_CustomerId",
                table: "FirebaseConfigs",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FirebaseConfigs_Customers_CustomerId",
                table: "FirebaseConfigs");

            migrationBuilder.DropIndex(
                name: "IX_FirebaseConfigs_CustomerId",
                table: "FirebaseConfigs");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "FirebaseConfigs");
        }
    }
}
