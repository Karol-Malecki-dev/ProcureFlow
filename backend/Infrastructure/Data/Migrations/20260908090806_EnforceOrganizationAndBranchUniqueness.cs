using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceOrganizationAndBranchUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UX_Organizations_Active",
                table: "Organizations",
                column: "IsArchived",
                unique: true,
                filter: "\"IsArchived\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Branches_OrganizationId_Name",
                table: "Branches",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_Organizations_Active",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Branches_OrganizationId_Name",
                table: "Branches");
        }
    }
}
