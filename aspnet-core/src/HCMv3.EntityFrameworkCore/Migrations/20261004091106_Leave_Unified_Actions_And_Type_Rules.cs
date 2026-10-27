using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Leave_Unified_Actions_And_Type_Rules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<int>(
                name: "RequestAction",
                table: "WorkflowRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RequestAction",
                table: "WorkflowGroupDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApproverDelegateId",
                table: "EmployeeAbsences",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PolicyAcceptedOn",
                table: "EmployeeAbsences",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrimaryDelegateId",
                table: "EmployeeAbsences",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecondaryDelegateId",
                table: "EmployeeAbsences",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LeaveDurationType",
                table: "AbsenceTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxFutureDays",
                table: "AbsenceTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequirePolicyAcceptance",
                table: "AbsenceTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmendmentRequestMasterId",
                table: "WorkflowTransactionTypes");

            migrationBuilder.DropColumn(
                name: "CancellationRequestMasterId",
                table: "WorkflowTransactionTypes");

            migrationBuilder.DropColumn(
                name: "ReturnRequestMasterId",
                table: "WorkflowTransactionTypes");

            migrationBuilder.DropColumn(
                name: "RequestAction",
                table: "WorkflowRequests");

            migrationBuilder.DropColumn(
                name: "RequestAction",
                table: "WorkflowGroupDetails");

            migrationBuilder.DropColumn(
                name: "ApproverDelegateId",
                table: "EmployeeAbsences");

            migrationBuilder.DropColumn(
                name: "PolicyAcceptedOn",
                table: "EmployeeAbsences");

            migrationBuilder.DropColumn(
                name: "PrimaryDelegateId",
                table: "EmployeeAbsences");

            migrationBuilder.DropColumn(
                name: "SecondaryDelegateId",
                table: "EmployeeAbsences");

            migrationBuilder.DropColumn(
                name: "LeaveDurationType",
                table: "AbsenceTypes");

            migrationBuilder.DropColumn(
                name: "MaxFutureDays",
                table: "AbsenceTypes");

            migrationBuilder.DropColumn(
                name: "RequirePolicyAcceptance",
                table: "AbsenceTypes");
        }
    }
}
