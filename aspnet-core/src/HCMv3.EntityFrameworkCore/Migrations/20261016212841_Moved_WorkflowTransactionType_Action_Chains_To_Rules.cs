using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Moved_WorkflowTransactionType_Action_Chains_To_Rules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkflowTransactionTypeRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    WorkflowTransactionTypeId = table.Column<int>(type: "int", nullable: false),
                    RequestSubTypeId = table.Column<int>(type: "int", nullable: true),
                    RequestAction = table.Column<int>(type: "int", nullable: true),
                    RequestMasterId = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTransactionTypeRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTransactionTypeRules_RequestMasters_RequestMasterId",
                        column: x => x.RequestMasterId,
                        principalTable: "RequestMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowTransactionTypeRules_WorkflowTransactionTypes_WorkflowTransactionTypeId",
                        column: x => x.WorkflowTransactionTypeId,
                        principalTable: "WorkflowTransactionTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransactionTypeRules_RequestMasterId",
                table: "WorkflowTransactionTypeRules",
                column: "RequestMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransactionTypeRules_TenantId_WorkflowTransactionTypeId",
                table: "WorkflowTransactionTypeRules",
                columns: new[] { "TenantId", "WorkflowTransactionTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransactionTypeRules_WorkflowTransactionTypeId",
                table: "WorkflowTransactionTypeRules",
                column: "WorkflowTransactionTypeId");

            // Carry each type's per-action chain over as a rule (Amendment = 1, Cancellation = 2, Return = 3,
            // as WorkflowRequestAction numbers them) before the columns go. Only chains that still exist, so a
            // dangling id cannot fail the foreign key.
            foreach (var (column, action) in new[] { ("AmendmentRequestMasterId", 1), ("CancellationRequestMasterId", 2), ("ReturnRequestMasterId", 3) })
            {
                // Through EXEC so it is compiled only when it runs. In the idempotent deploy script this
                // block is still compiled on a later run, when the migration is skipped and the columns
                // below have already been dropped -- "Invalid column name" on every server update.
                migrationBuilder.Sql($@"
EXEC(N'
INSERT INTO WorkflowTransactionTypeRules
    (TenantId, WorkflowTransactionTypeId, RequestSubTypeId, RequestAction, RequestMasterId, CreationTime, IsDeleted)
SELECT t.TenantId, t.Id, NULL, {action}, t.{column}, GETDATE(), 0
FROM WorkflowTransactionTypes t
JOIN RequestMasters m ON m.Id = t.{column} AND m.IsDeleted = 0
WHERE t.{column} IS NOT NULL AND t.IsDeleted = 0;');");
            }

            migrationBuilder.DropColumn(
                name: "AmendmentRequestMasterId",
                table: "WorkflowTransactionTypes");

            migrationBuilder.DropColumn(
                name: "CancellationRequestMasterId",
                table: "WorkflowTransactionTypes");

            migrationBuilder.DropColumn(
                name: "ReturnRequestMasterId",
                table: "WorkflowTransactionTypes");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkflowTransactionTypeRules");

            migrationBuilder.AddColumn<int>(
                name: "AmendmentRequestMasterId",
                table: "WorkflowTransactionTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CancellationRequestMasterId",
                table: "WorkflowTransactionTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReturnRequestMasterId",
                table: "WorkflowTransactionTypes",
                type: "int",
                nullable: true);
        }
    }
}
