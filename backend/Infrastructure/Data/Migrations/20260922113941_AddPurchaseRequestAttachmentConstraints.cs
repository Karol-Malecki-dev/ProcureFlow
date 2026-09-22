using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseRequestAttachmentConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_PurchaseRequestAttachments_SizeBytes_Positive",
                table: "PurchaseRequestAttachments",
                sql: "\"SizeBytes\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PurchaseRequestAttachments_SizeBytes_Positive",
                table: "PurchaseRequestAttachments");
        }
    }
}
