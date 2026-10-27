using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_Workflow_On_Clearance_Roster_HealthReimburse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkflowRequestId",
                table: "EmployeeSubstituteDuties",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "EmployeeSubstituteDuties",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRequestId",
                table: "EmployeeHealthReimburses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "EmployeeHealthReimburses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeInWorkflow",
                table: "ClearanceRequestTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Cleared BEFORE the column is added, and this is a deliberate delete of rows.
            //
            // Until now ClearanceRequestFieldDetail held nothing but an id, a tenant id and a parent id. Its
            // one meaningful column -- the workflow role saying WHICH DEPARTMENT signs that checklist line --
            // was dropped when the table was first ported, on the grounds that the workflow module was out of
            // scope. So any existing row records no fact whatsoever: there is no value to migrate, because
            // there is no information there to migrate.
            //
            // Keeping them is not the safe option. WorkflowRoleId is required with a scaffolded default of 0,
            // 0 matches no workflow role, and the foreign key added below would then fail to create --
            // aborting this migration halfway on exactly the databases that have the most data.
            //
            // An administrator re-creates these lines against real departments on the clearance setup screen,
            // which is the only place the correct values have ever existed.
            migrationBuilder.Sql("DELETE FROM ClearanceRequestFieldDetails;");

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRoleId",
                table: "ClearanceRequestFieldDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EmployeeClearances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ClearanceRequestTypeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WorkflowRequestId = table.Column<int>(type: "int", nullable: true),
                    WorkflowStatus = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeClearances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeClearances_ClearanceRequestTypes_ClearanceRequestTypeId",
                        column: x => x.ClearanceRequestTypeId,
                        principalTable: "ClearanceRequestTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeClearances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeClearanceDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeClearanceId = table.Column<int>(type: "int", nullable: false),
                    ClearanceRequestFieldId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ApproverId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeClearanceDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeClearanceDetails_ClearanceRequestFields_ClearanceRequestFieldId",
                        column: x => x.ClearanceRequestFieldId,
                        principalTable: "ClearanceRequestFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeClearanceDetails_EmployeeClearances_EmployeeClearanceId",
                        column: x => x.EmployeeClearanceId,
                        principalTable: "EmployeeClearances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeClearanceDetails_Employees_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeHealthReimburses_TenantId_EmployeeId_PaymentDate_WorkflowStatus",
                table: "EmployeeHealthReimburses",
                columns: new[] { "TenantId", "EmployeeId", "PaymentDate", "WorkflowStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_ClearanceRequestFieldDetails_TenantId_WorkflowRoleId",
                table: "ClearanceRequestFieldDetails",
                columns: new[] { "TenantId", "WorkflowRoleId" });

            migrationBuilder.CreateIndex(
                name: "IX_ClearanceRequestFieldDetails_WorkflowRoleId",
                table: "ClearanceRequestFieldDetails",
                column: "WorkflowRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeClearanceDetails_ApproverId",
                table: "EmployeeClearanceDetails",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeClearanceDetails_ClearanceRequestFieldId",
                table: "EmployeeClearanceDetails",
                column: "ClearanceRequestFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeClearanceDetails_EmployeeClearanceId",
                table: "EmployeeClearanceDetails",
                column: "EmployeeClearanceId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeClearanceDetails_TenantId_EmployeeClearanceId_ClearanceRequestFieldId",
                table: "EmployeeClearanceDetails",
                columns: new[] { "TenantId", "EmployeeClearanceId", "ClearanceRequestFieldId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeClearances_ClearanceRequestTypeId",
                table: "EmployeeClearances",
                column: "ClearanceRequestTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeClearances_EmployeeId",
                table: "EmployeeClearances",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeClearances_TenantId_EmployeeId_WorkflowStatus",
                table: "EmployeeClearances",
                columns: new[] { "TenantId", "EmployeeId", "WorkflowStatus" });

            migrationBuilder.AddForeignKey(
                name: "FK_ClearanceRequestFieldDetails_WorkflowRoles_WorkflowRoleId",
                table: "ClearanceRequestFieldDetails",
                column: "WorkflowRoleId",
                principalTable: "WorkflowRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClearanceRequestFieldDetails_WorkflowRoles_WorkflowRoleId",
                table: "ClearanceRequestFieldDetails");

            migrationBuilder.DropTable(
                name: "EmployeeClearanceDetails");

            migrationBuilder.DropTable(
                name: "EmployeeClearances");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeHealthReimburses_TenantId_EmployeeId_PaymentDate_WorkflowStatus",
                table: "EmployeeHealthReimburses");

            migrationBuilder.DropIndex(
                name: "IX_ClearanceRequestFieldDetails_TenantId_WorkflowRoleId",
                table: "ClearanceRequestFieldDetails");

            migrationBuilder.DropIndex(
                name: "IX_ClearanceRequestFieldDetails_WorkflowRoleId",
                table: "ClearanceRequestFieldDetails");

            migrationBuilder.DropColumn(
                name: "WorkflowRequestId",
                table: "EmployeeSubstituteDuties");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "EmployeeSubstituteDuties");

            migrationBuilder.DropColumn(
                name: "WorkflowRequestId",
                table: "EmployeeHealthReimburses");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "EmployeeHealthReimburses");

            migrationBuilder.DropColumn(
                name: "IncludeInWorkflow",
                table: "ClearanceRequestTypes");

            migrationBuilder.DropColumn(
                name: "WorkflowRoleId",
                table: "ClearanceRequestFieldDetails");
        }
    }
}
