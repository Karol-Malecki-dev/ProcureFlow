using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseRequestBudgetsAndApprovalDecisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BranchMonthlyBudgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    LimitAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    UsedAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchMonthlyBudgets", x => x.Id);
                    table.CheckConstraint("CK_BranchMonthlyBudgets_LimitAmount_NonNegative", "\"LimitAmount\" >= 0");
                    table.CheckConstraint("CK_BranchMonthlyBudgets_Month_Range", "\"Month\" >= 1 AND \"Month\" <= 12");
                    table.CheckConstraint("CK_BranchMonthlyBudgets_UsedAmount_NonNegative", "\"UsedAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_BranchMonthlyBudgets_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseRequestApprovalDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorRole = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Decision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequestAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    AvailableBudget = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    OverBudgetAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BudgetYear = table.Column<int>(type: "integer", nullable: true),
                    BudgetMonth = table.Column<int>(type: "integer", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseRequestApprovalDecisions", x => x.Id);
                    table.CheckConstraint("CK_PurchaseRequestApprovalDecisions_OverBudgetAmount_NonNegati~", "\"OverBudgetAmount\" >= 0");
                    table.CheckConstraint("CK_PurchaseRequestApprovalDecisions_RequestAmount_NonNegative", "\"RequestAmount\" >= 0");
                    table.ForeignKey(
                        name: "FK_PurchaseRequestApprovalDecisions_PurchaseRequests_PurchaseR~",
                        column: x => x.PurchaseRequestId,
                        principalTable: "PurchaseRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseRequestApprovalDecisions_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_BranchMonthlyBudgets_Branch_Period",
                table: "BranchMonthlyBudgets",
                columns: new[] { "BranchId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequestApprovalDecisions_ActorUserId",
                table: "PurchaseRequestApprovalDecisions",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseRequestApprovalDecisions_PurchaseRequestId_DecidedA~",
                table: "PurchaseRequestApprovalDecisions",
                columns: new[] { "PurchaseRequestId", "DecidedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "UX_PurchaseRequestApprovalDecisions_Request_Role",
                table: "PurchaseRequestApprovalDecisions",
                columns: new[] { "PurchaseRequestId", "ActorRole" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BranchMonthlyBudgets");

            migrationBuilder.DropTable(
                name: "PurchaseRequestApprovalDecisions");
        }
    }
}
