using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dukan.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeFirebaseProjectIdUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FirebaseConfigs_ProjectId",
                table: "FirebaseConfigs");

            migrationBuilder.CreateIndex(
                name: "IX_FirebaseConfigs_ProjectId",
                table: "FirebaseConfigs",
                column: "ProjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FirebaseConfigs_ProjectId",
                table: "FirebaseConfigs");

            migrationBuilder.CreateIndex(
                name: "IX_FirebaseConfigs_ProjectId",
                table: "FirebaseConfigs",
                column: "ProjectId");
        }
    }
}
