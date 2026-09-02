using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_TimeLogApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimeLogApprovalId",
                table: "TimeRegisters",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TimeLogApprovals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    OrganizationUnitId = table.Column<long>(type: "bigint", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_TimeLogApprovals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeLogApprovals_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TimeLogApprovalDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TimeLogApprovalId = table.Column<int>(type: "int", nullable: false),
                    TimeRegisterId = table.Column<int>(type: "int", nullable: false),
                    ProposedActualWorkingDay = table.Column<double>(type: "float", nullable: false),
                    ProposedAbsentDay = table.Column<double>(type: "float", nullable: false),
                    ProposedDedLateInEarlyOutMins = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_TimeLogApprovalDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeLogApprovalDetails_TimeLogApprovals_TimeLogApprovalId",
                        column: x => x.TimeLogApprovalId,
                        principalTable: "TimeLogApprovals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimeLogApprovalDetails_TimeRegisters_TimeRegisterId",
                        column: x => x.TimeRegisterId,
                        principalTable: "TimeRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TimeLogHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TimeLogApprovalId = table.Column<int>(type: "int", nullable: false),
                    TimeRegisterId = table.Column<int>(type: "int", nullable: false),
                    DateOfAction = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualWorkingDay = table.Column<double>(type: "float", nullable: false),
                    AbsentDay = table.Column<double>(type: "float", nullable: false),
                    DedLateInEarlyOutMins = table.Column<double>(type: "float", nullable: false),
                    ApprovedActualWorkingDay = table.Column<double>(type: "float", nullable: false),
                    ApprovedAbsentDay = table.Column<double>(type: "float", nullable: false),
                    ApprovedDedLateInEarlyOutMins = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_TimeLogHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeLogHistories_TimeLogApprovals_TimeLogApprovalId",
                        column: x => x.TimeLogApprovalId,
                        principalTable: "TimeLogApprovals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeLogHistories_TimeRegisters_TimeRegisterId",
                        column: x => x.TimeRegisterId,
                        principalTable: "TimeRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimeRegisters_TimeLogApprovalId",
                table: "TimeRegisters",
                column: "TimeLogApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeLogApprovalDetails_TenantId_TimeRegisterId",
                table: "TimeLogApprovalDetails",
                columns: new[] { "TenantId", "TimeRegisterId" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeLogApprovalDetails_TimeLogApprovalId",
                table: "TimeLogApprovalDetails",
                column: "TimeLogApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeLogApprovalDetails_TimeRegisterId",
                table: "TimeLogApprovalDetails",
                column: "TimeRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeLogApprovals_EmployeeId",
                table: "TimeLogApprovals",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeLogApprovals_TenantId_EmployeeId_WorkflowStatus",
                table: "TimeLogApprovals",
                columns: new[] { "TenantId", "EmployeeId", "WorkflowStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeLogHistories_TenantId_TimeRegisterId",
                table: "TimeLogHistories",
                columns: new[] { "TenantId", "TimeRegisterId" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeLogHistories_TimeLogApprovalId",
                table: "TimeLogHistories",
                column: "TimeLogApprovalId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeLogHistories_TimeRegisterId",
                table: "TimeLogHistories",
                column: "TimeRegisterId");

            migrationBuilder.AddForeignKey(
                name: "FK_TimeRegisters_TimeLogApprovals_TimeLogApprovalId",
                table: "TimeRegisters",
                column: "TimeLogApprovalId",
                principalTable: "TimeLogApprovals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TimeRegisters_TimeLogApprovals_TimeLogApprovalId",
                table: "TimeRegisters");

            migrationBuilder.DropTable(
                name: "TimeLogApprovalDetails");

            migrationBuilder.DropTable(
                name: "TimeLogHistories");

            migrationBuilder.DropTable(
                name: "TimeLogApprovals");

            migrationBuilder.DropIndex(
                name: "IX_TimeRegisters_TimeLogApprovalId",
                table: "TimeRegisters");

            migrationBuilder.DropColumn(
                name: "TimeLogApprovalId",
                table: "TimeRegisters");
        }
    }
}
