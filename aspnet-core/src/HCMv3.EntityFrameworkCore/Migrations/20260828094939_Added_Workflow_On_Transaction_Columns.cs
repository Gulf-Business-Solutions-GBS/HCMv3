using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_Workflow_On_Transaction_Columns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequestSubTypeId",
                table: "WorkflowRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SummaryAr",
                table: "WorkflowRequests",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SummaryEn",
                table: "WorkflowRequests",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TargetEntityId",
                table: "WorkflowRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRequestId",
                table: "EmployeeTickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "EmployeeTickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRequestId",
                table: "EmployeeOvertimes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "EmployeeOvertimes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRequestId",
                table: "EmployeeLoans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "EmployeeLoans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRequestId",
                table: "EmployeeDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "EmployeeDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRequestId",
                table: "EmployeeAssets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "EmployeeAssets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRequestId",
                table: "EmployeeAbsences",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "EmployeeAbsences",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRequests_TenantId_RequestType_TargetEntityId",
                table: "WorkflowRequests",
                columns: new[] { "TenantId", "RequestType", "TargetEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkFlowApprovers_TenantId_ApproverId_IsApproved",
                table: "WorkFlowApprovers",
                columns: new[] { "TenantId", "ApproverId", "IsApproved" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkflowRequests_TenantId_RequestType_TargetEntityId",
                table: "WorkflowRequests");

            migrationBuilder.DropIndex(
                name: "IX_WorkFlowApprovers_TenantId_ApproverId_IsApproved",
                table: "WorkFlowApprovers");

            migrationBuilder.DropColumn(
                name: "RequestSubTypeId",
                table: "WorkflowRequests");

            migrationBuilder.DropColumn(
                name: "SummaryAr",
                table: "WorkflowRequests");

            migrationBuilder.DropColumn(
                name: "SummaryEn",
                table: "WorkflowRequests");

            migrationBuilder.DropColumn(
                name: "TargetEntityId",
                table: "WorkflowRequests");

            migrationBuilder.DropColumn(
                name: "WorkflowRequestId",
                table: "EmployeeTickets");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "EmployeeTickets");

            migrationBuilder.DropColumn(
                name: "WorkflowRequestId",
                table: "EmployeeOvertimes");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "EmployeeOvertimes");

            migrationBuilder.DropColumn(
                name: "WorkflowRequestId",
                table: "EmployeeLoans");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "EmployeeLoans");

            migrationBuilder.DropColumn(
                name: "WorkflowRequestId",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "WorkflowRequestId",
                table: "EmployeeAssets");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "EmployeeAssets");

            migrationBuilder.DropColumn(
                name: "WorkflowRequestId",
                table: "EmployeeAbsences");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "EmployeeAbsences");
        }
    }
}
