using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Gap_Batch8_RoundA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentPrintForms");

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "Travels",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ApplicantId",
                table: "Travels",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "TravelFeedbacks",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ApplicantId",
                table: "TravelFeedbacks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BankTransfer",
                table: "EmployeeSeparations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Comments",
                table: "EmployeeSeparations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OnHoldAirTicketPay",
                table: "EmployeeSeparations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OnHoldAll",
                table: "EmployeeSeparations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OnHoldAllDeductions",
                table: "EmployeeSeparations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OnHoldAllEarnings",
                table: "EmployeeSeparations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OnHoldBasicPay",
                table: "EmployeeSeparations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OnHoldFromDate",
                table: "EmployeeSeparations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "OnHoldIndemnityPay",
                table: "EmployeeSeparations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OnHoldLeavePay",
                table: "EmployeeSeparations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "OnHoldLoanDeduction",
                table: "EmployeeSeparations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OnHoldTillDate",
                table: "EmployeeSeparations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevokeComments",
                table: "EmployeeSeparations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LeavePlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlannedDays = table.Column<double>(type: "float", nullable: false),
                    ProjectedLeaveBalance = table.Column<double>(type: "float", nullable: true),
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
                    table.PrimaryKey("PK_LeavePlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeavePlans_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OvertimeBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    OrganizationUnitId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_OvertimeBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OvertimeBatches_AbpOrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "AbpOrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OvertimeBatchDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    OvertimeBatchId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TimeRegisterId = table.Column<int>(type: "int", nullable: true),
                    ShiftStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShiftEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignIn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PlannedHours = table.Column<double>(type: "float", nullable: false),
                    TotalHours = table.Column<double>(type: "float", nullable: false),
                    ApprovedHours = table.Column<double>(type: "float", nullable: false),
                    LateIn = table.Column<double>(type: "float", nullable: false),
                    EarlyOut = table.Column<double>(type: "float", nullable: false),
                    OvertimeTypeId = table.Column<int>(type: "int", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EmployeeOvertimeId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_OvertimeBatchDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OvertimeBatchDetails_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OvertimeBatchDetails_OvertimeBatches_OvertimeBatchId",
                        column: x => x.OvertimeBatchId,
                        principalTable: "OvertimeBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OvertimeBatchDetails_OvertimeTypes_OvertimeTypeId",
                        column: x => x.OvertimeTypeId,
                        principalTable: "OvertimeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Travels_ApplicantId",
                table: "Travels",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_Travels_TenantId_ApplicantId",
                table: "Travels",
                columns: new[] { "TenantId", "ApplicantId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Travels_EmployeeOrApplicant",
                table: "Travels",
                sql: "([EmployeeId] IS NOT NULL AND [ApplicantId] IS NULL) OR ([EmployeeId] IS NULL AND [ApplicantId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_TravelFeedbacks_ApplicantId",
                table: "TravelFeedbacks",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelFeedbacks_TenantId_ApplicantId",
                table: "TravelFeedbacks",
                columns: new[] { "TenantId", "ApplicantId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_TravelFeedbacks_EmployeeOrApplicant",
                table: "TravelFeedbacks",
                sql: "([EmployeeId] IS NOT NULL AND [ApplicantId] IS NULL) OR ([EmployeeId] IS NULL AND [ApplicantId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_LeavePlans_EmployeeId",
                table: "LeavePlans",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeavePlans_TenantId_EmployeeId_WorkflowStatus",
                table: "LeavePlans",
                columns: new[] { "TenantId", "EmployeeId", "WorkflowStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_LeavePlans_TenantId_StartDate_EndDate",
                table: "LeavePlans",
                columns: new[] { "TenantId", "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeBatchDetails_EmployeeId",
                table: "OvertimeBatchDetails",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeBatchDetails_OvertimeBatchId",
                table: "OvertimeBatchDetails",
                column: "OvertimeBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeBatchDetails_OvertimeTypeId",
                table: "OvertimeBatchDetails",
                column: "OvertimeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeBatchDetails_TenantId_EmployeeId_Date",
                table: "OvertimeBatchDetails",
                columns: new[] { "TenantId", "EmployeeId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeBatchDetails_TenantId_OvertimeBatchId",
                table: "OvertimeBatchDetails",
                columns: new[] { "TenantId", "OvertimeBatchId" });

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeBatches_OrganizationUnitId",
                table: "OvertimeBatches",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeBatches_TenantId_WorkflowStatus",
                table: "OvertimeBatches",
                columns: new[] { "TenantId", "WorkflowStatus" });

            migrationBuilder.AddForeignKey(
                name: "FK_TravelFeedbacks_Applicants_ApplicantId",
                table: "TravelFeedbacks",
                column: "ApplicantId",
                principalTable: "Applicants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Travels_Applicants_ApplicantId",
                table: "Travels",
                column: "ApplicantId",
                principalTable: "Applicants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TravelFeedbacks_Applicants_ApplicantId",
                table: "TravelFeedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_Travels_Applicants_ApplicantId",
                table: "Travels");

            migrationBuilder.DropTable(
                name: "LeavePlans");

            migrationBuilder.DropTable(
                name: "OvertimeBatchDetails");

            migrationBuilder.DropTable(
                name: "OvertimeBatches");

            migrationBuilder.DropIndex(
                name: "IX_Travels_ApplicantId",
                table: "Travels");

            migrationBuilder.DropIndex(
                name: "IX_Travels_TenantId_ApplicantId",
                table: "Travels");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Travels_EmployeeOrApplicant",
                table: "Travels");

            migrationBuilder.DropIndex(
                name: "IX_TravelFeedbacks_ApplicantId",
                table: "TravelFeedbacks");

            migrationBuilder.DropIndex(
                name: "IX_TravelFeedbacks_TenantId_ApplicantId",
                table: "TravelFeedbacks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_TravelFeedbacks_EmployeeOrApplicant",
                table: "TravelFeedbacks");

            migrationBuilder.DropColumn(
                name: "ApplicantId",
                table: "Travels");

            migrationBuilder.DropColumn(
                name: "ApplicantId",
                table: "TravelFeedbacks");

            migrationBuilder.DropColumn(
                name: "BankTransfer",
                table: "EmployeeSeparations");

            migrationBuilder.DropColumn(
                name: "Comments",
                table: "EmployeeSeparations");

            migrationBuilder.DropColumn(
                name: "OnHoldAirTicketPay",
                table: "EmployeeSeparations");

            migrationBuilder.DropColumn(
                name: "OnHoldAll",
                table: "EmployeeSeparations");

            migrationBuilder.DropColumn(
                name: "OnHoldAllDeductions",
                table: "EmployeeSeparations");

            migrationBuilder.DropColumn(
                name: "OnHoldAllEarnings",
                table: "EmployeeSeparations");

            migrationBuilder.DropColumn(
                name: "OnHoldBasicPay",
                table: "EmployeeSeparations");

            migrationBuilder.DropColumn(
                name: "OnHoldFromDate",
                table: "EmployeeSeparations");

            migrationBuilder.DropColumn(
                name: "OnHoldIndemnityPay",
                table: "EmployeeSeparations");

            migrationBuilder.DropColumn(
                name: "OnHoldLeavePay",
                table: "EmployeeSeparations");

            migrationBuilder.DropColumn(
                name: "OnHoldLoanDeduction",
                table: "EmployeeSeparations");

            migrationBuilder.DropColumn(
                name: "OnHoldTillDate",
                table: "EmployeeSeparations");

            migrationBuilder.DropColumn(
                name: "RevokeComments",
                table: "EmployeeSeparations");

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "Travels",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "TravelFeedbacks",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentPrintForms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentPrintForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentPrintForms_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentPrintForms_DocumentTypeId",
                table: "DocumentPrintForms",
                column: "DocumentTypeId");
        }
    }
}
