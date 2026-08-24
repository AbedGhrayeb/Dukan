using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dukan.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerStoreName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StoreName",
                table: "Customers",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                unicode: true,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoreName",
                table: "Customers");
        }
    }
}
