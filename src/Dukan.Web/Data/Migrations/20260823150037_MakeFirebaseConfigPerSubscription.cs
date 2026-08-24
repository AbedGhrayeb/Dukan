using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dukan.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeFirebaseConfigPerSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FirebaseConfigs_Customers_CustomerId",
                table: "FirebaseConfigs");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "FirebaseConfigs",
                newName: "SubscriptionId");

            migrationBuilder.RenameIndex(
                name: "IX_FirebaseConfigs_CustomerId",
                table: "FirebaseConfigs",
                newName: "IX_FirebaseConfigs_SubscriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_FirebaseConfigs_Subscriptions_SubscriptionId",
                table: "FirebaseConfigs",
                column: "SubscriptionId",
                principalTable: "Subscriptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FirebaseConfigs_Subscriptions_SubscriptionId",
                table: "FirebaseConfigs");

            migrationBuilder.RenameColumn(
                name: "SubscriptionId",
                table: "FirebaseConfigs",
                newName: "CustomerId");

            migrationBuilder.RenameIndex(
                name: "IX_FirebaseConfigs_SubscriptionId",
                table: "FirebaseConfigs",
                newName: "IX_FirebaseConfigs_CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_FirebaseConfigs_Customers_CustomerId",
                table: "FirebaseConfigs",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
