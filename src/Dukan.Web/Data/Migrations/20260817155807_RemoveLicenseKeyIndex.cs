using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dukan.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLicenseKeyIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_LicenseKey",
                table: "Subscriptions");

            migrationBuilder.AlterColumn<string>(
                name: "LicenseKey",
                table: "Subscriptions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_LicenseKey",
                table: "Subscriptions",
                column: "LicenseKey",
                unique: true,
                filter: "[LicenseKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_LicenseKey",
                table: "Subscriptions");

            migrationBuilder.AlterColumn<string>(
                name: "LicenseKey",
                table: "Subscriptions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_LicenseKey",
                table: "Subscriptions",
                column: "LicenseKey",
                unique: true);
        }
    }
}
