using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_Workflow_On_Wave3_Transaction_Types : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "ManpowerPlannings");

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRequestId",
                table: "PAEmployees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "PAEmployees",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRequestId",
                table: "ManpowerPlannings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "ManpowerPlannings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsWithdrawalReasonMandatory",
                table: "ManageSelfServiceSettingses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "EmployeeDocumentMvmts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GuarantorId",
                table: "EmployeeDocumentMvmts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WithdrawalReason",
                table: "EmployeeDocumentMvmts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRequestId",
                table: "EmployeeDocumentMvmts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "EmployeeDocumentMvmts",
                type: "int",
                nullable: true);

            // BACKFILL, and it must run here -- after the column exists and before the foreign key is
            // created. EmployeeDocumentMvmt gained a required EmployeeId, so every existing row was just
            // given the scaffolded default of 0. Zero matches no employee, so the FK below would fail to
            // create on any database that already holds movements, and the migration would abort halfway.
            //
            // The value is not guessed: a movement is always a movement OF a document, and that document
            // already knows whose it is.
            // Wrapped in EXEC() so the column reference is resolved at execution
            // time rather than batch-compile time. Without this, an idempotent
            // script (dotnet ef migrations script --idempotent) fails with
            // "Invalid column name 'EmployeeId'": SQL Server compiles the whole
            // batch up front, and EmployeeId is added by this same migration.
            migrationBuilder.Sql(@"
                EXEC(N'
                    UPDATE m
                    SET m.EmployeeId = d.EmployeeId
                    FROM EmployeeDocumentMvmts m
                    INNER JOIN EmployeeDocuments d ON d.Id = m.EmployeeDocumentId
                    WHERE m.EmployeeId = 0;
                ');");

            migrationBuilder.CreateTable(
                name: "EmployeeExitPermits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ExternalRequestNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TravelReason = table.Column<int>(type: "int", nullable: false),
                    ExitDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeExitPermits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeExitPermits_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeHRServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    HRServiceTypeId = table.Column<int>(type: "int", nullable: false),
                    ServiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeHRServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeHRServices_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeHRServices_HRServiceTypes_HRServiceTypeId",
                        column: x => x.HRServiceTypeId,
                        principalTable: "HRServiceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeITServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ITServiceTypeId = table.Column<int>(type: "int", nullable: false),
                    ServiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_EmployeeITServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeITServices_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeITServices_ITServiceTypes_ITServiceTypeId",
                        column: x => x.ITServiceTypeId,
                        principalTable: "ITServiceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocumentMvmts_EmployeeId",
                table: "EmployeeDocumentMvmts",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocumentMvmts_GuarantorId",
                table: "EmployeeDocumentMvmts",
                column: "GuarantorId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocumentMvmts_TenantId_EmployeeId_WorkflowStatus",
                table: "EmployeeDocumentMvmts",
                columns: new[] { "TenantId", "EmployeeId", "WorkflowStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeExitPermits_EmployeeId",
                table: "EmployeeExitPermits",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeHRServices_EmployeeId",
                table: "EmployeeHRServices",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeHRServices_HRServiceTypeId",
                table: "EmployeeHRServices",
                column: "HRServiceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeITServices_EmployeeId",
                table: "EmployeeITServices",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeITServices_ITServiceTypeId",
                table: "EmployeeITServices",
                column: "ITServiceTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeDocumentMvmts_Employees_EmployeeId",
                table: "EmployeeDocumentMvmts",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeDocumentMvmts_Employees_GuarantorId",
                table: "EmployeeDocumentMvmts",
                column: "GuarantorId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeDocumentMvmts_Employees_EmployeeId",
                table: "EmployeeDocumentMvmts");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeDocumentMvmts_Employees_GuarantorId",
                table: "EmployeeDocumentMvmts");

            migrationBuilder.DropTable(
                name: "EmployeeExitPermits");

            migrationBuilder.DropTable(
                name: "EmployeeHRServices");

            migrationBuilder.DropTable(
                name: "EmployeeITServices");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeDocumentMvmts_EmployeeId",
                table: "EmployeeDocumentMvmts");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeDocumentMvmts_GuarantorId",
                table: "EmployeeDocumentMvmts");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeDocumentMvmts_TenantId_EmployeeId_WorkflowStatus",
                table: "EmployeeDocumentMvmts");

            migrationBuilder.DropColumn(
                name: "WorkflowRequestId",
                table: "PAEmployees");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "PAEmployees");

            migrationBuilder.DropColumn(
                name: "WorkflowRequestId",
                table: "ManpowerPlannings");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "ManpowerPlannings");

            migrationBuilder.DropColumn(
                name: "IsWithdrawalReasonMandatory",
                table: "ManageSelfServiceSettingses");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "EmployeeDocumentMvmts");

            migrationBuilder.DropColumn(
                name: "GuarantorId",
                table: "EmployeeDocumentMvmts");

            migrationBuilder.DropColumn(
                name: "WithdrawalReason",
                table: "EmployeeDocumentMvmts");

            migrationBuilder.DropColumn(
                name: "WorkflowRequestId",
                table: "EmployeeDocumentMvmts");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "EmployeeDocumentMvmts");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ManpowerPlannings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
