using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseRequestFulfillmentMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FulfillmentNote",
                table: "PurchaseRequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FulfillmentOrderNumber",
                table: "PurchaseRequests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FulfillmentNote",
                table: "PurchaseRequests");

            migrationBuilder.DropColumn(
                name: "FulfillmentOrderNumber",
                table: "PurchaseRequests");
        }
    }
}
