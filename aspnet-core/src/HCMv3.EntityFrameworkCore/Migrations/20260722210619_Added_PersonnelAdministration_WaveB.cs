using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_PersonnelAdministration_WaveB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    PersonalActionId = table.Column<int>(type: "int", nullable: false),
                    ActionReasonId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecommendedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_EmployeeActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeActions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeActions_PersonalActionReasons_ActionReasonId",
                        column: x => x.ActionReasonId,
                        principalTable: "PersonalActionReasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeActions_PersonalActions_PersonalActionId",
                        column: x => x.PersonalActionId,
                        principalTable: "PersonalActions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeRejoins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    RejoinDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CarryForwardLeaveBalance = table.Column<bool>(type: "bit", nullable: false),
                    CarryForwardTicketBalance = table.Column<bool>(type: "bit", nullable: false),
                    CarryForwardIndemnityBalance = table.Column<bool>(type: "bit", nullable: false),
                    CarryForwardLoanBalance = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeRejoins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeRejoins_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ContractTypeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmployeeActionId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeContracts_ContractTypes_ContractTypeId",
                        column: x => x.ContractTypeId,
                        principalTable: "ContractTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeContracts_EmployeeActions_EmployeeActionId",
                        column: x => x.EmployeeActionId,
                        principalTable: "EmployeeActions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeContracts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeOrgAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JobTitleId = table.Column<int>(type: "int", nullable: true),
                    CostCenterId = table.Column<int>(type: "int", nullable: true),
                    WorkLocationId = table.Column<int>(type: "int", nullable: true),
                    PostingGroupId = table.Column<int>(type: "int", nullable: true),
                    BudgetingGroupId = table.Column<int>(type: "int", nullable: true),
                    OrganizationUnitId = table.Column<long>(type: "bigint", nullable: true),
                    EmployeeActionId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeOrgAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeOrgAssignments_AbpOrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "AbpOrganizationUnits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeOrgAssignments_BudgetingGroups_BudgetingGroupId",
                        column: x => x.BudgetingGroupId,
                        principalTable: "BudgetingGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeOrgAssignments_CostCenters_CostCenterId",
                        column: x => x.CostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeOrgAssignments_EmployeeActions_EmployeeActionId",
                        column: x => x.EmployeeActionId,
                        principalTable: "EmployeeActions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeOrgAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeOrgAssignments_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeOrgAssignments_PostingGroups_PostingGroupId",
                        column: x => x.PostingGroupId,
                        principalTable: "PostingGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeOrgAssignments_WorkLocations_WorkLocationId",
                        column: x => x.WorkLocationId,
                        principalTable: "WorkLocations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSeparations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastDayInService = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NoticePeriodDays = table.Column<int>(type: "int", nullable: false),
                    PlannedLastDayInService = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SeparationType = table.Column<int>(type: "int", nullable: false),
                    SeparationReasonId = table.Column<int>(type: "int", nullable: false),
                    RevokeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmployeeActionId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeSeparations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSeparations_EmployeeActions_EmployeeActionId",
                        column: x => x.EmployeeActionId,
                        principalTable: "EmployeeActions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeSeparations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeSeparations_SeparationReasons_SeparationReasonId",
                        column: x => x.SeparationReasonId,
                        principalTable: "SeparationReasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeActions_ActionReasonId",
                table: "EmployeeActions",
                column: "ActionReasonId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeActions_EmployeeId",
                table: "EmployeeActions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeActions_PersonalActionId",
                table: "EmployeeActions",
                column: "PersonalActionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeContracts_ContractTypeId",
                table: "EmployeeContracts",
                column: "ContractTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeContracts_EmployeeActionId",
                table: "EmployeeContracts",
                column: "EmployeeActionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeContracts_EmployeeId",
                table: "EmployeeContracts",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrgAssignments_BudgetingGroupId",
                table: "EmployeeOrgAssignments",
                column: "BudgetingGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrgAssignments_CostCenterId",
                table: "EmployeeOrgAssignments",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrgAssignments_EmployeeActionId",
                table: "EmployeeOrgAssignments",
                column: "EmployeeActionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrgAssignments_EmployeeId",
                table: "EmployeeOrgAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrgAssignments_JobTitleId",
                table: "EmployeeOrgAssignments",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrgAssignments_OrganizationUnitId",
                table: "EmployeeOrgAssignments",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrgAssignments_PostingGroupId",
                table: "EmployeeOrgAssignments",
                column: "PostingGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrgAssignments_WorkLocationId",
                table: "EmployeeOrgAssignments",
                column: "WorkLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRejoins_EmployeeId",
                table: "EmployeeRejoins",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSeparations_EmployeeActionId",
                table: "EmployeeSeparations",
                column: "EmployeeActionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSeparations_EmployeeId",
                table: "EmployeeSeparations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSeparations_SeparationReasonId",
                table: "EmployeeSeparations",
                column: "SeparationReasonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeContracts");

            migrationBuilder.DropTable(
                name: "EmployeeOrgAssignments");

            migrationBuilder.DropTable(
                name: "EmployeeRejoins");

            migrationBuilder.DropTable(
                name: "EmployeeSeparations");

            migrationBuilder.DropTable(
                name: "EmployeeActions");
        }
    }
}
