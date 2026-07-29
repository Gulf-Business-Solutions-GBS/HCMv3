using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class P8_EntityGapClosures_ProjectLog_EmployeePayment_RegisterLine_OvertimeType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectLogs_Employees_EmployeeId",
                table: "ProjectLogs");

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemGenerated",
                table: "SalaryRegisterDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "ProjectLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "WageTypeId",
                table: "OvertimeTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemGenerated",
                table: "EmployeePayments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OffCyclePaymentDate",
                table: "EmployeePayments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeTypes_WageTypeId",
                table: "OvertimeTypes",
                column: "WageTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_OvertimeTypes_WageTypes_WageTypeId",
                table: "OvertimeTypes",
                column: "WageTypeId",
                principalTable: "WageTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectLogs_Employees_EmployeeId",
                table: "ProjectLogs",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OvertimeTypes_WageTypes_WageTypeId",
                table: "OvertimeTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectLogs_Employees_EmployeeId",
                table: "ProjectLogs");

            migrationBuilder.DropIndex(
                name: "IX_OvertimeTypes_WageTypeId",
                table: "OvertimeTypes");

            migrationBuilder.DropColumn(
                name: "IsSystemGenerated",
                table: "SalaryRegisterDetails");

            migrationBuilder.DropColumn(
                name: "WageTypeId",
                table: "OvertimeTypes");

            migrationBuilder.DropColumn(
                name: "IsSystemGenerated",
                table: "EmployeePayments");

            migrationBuilder.DropColumn(
                name: "OffCyclePaymentDate",
                table: "EmployeePayments");

            migrationBuilder.AlterColumn<int>(
                name: "EmployeeId",
                table: "ProjectLogs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectLogs_Employees_EmployeeId",
                table: "ProjectLogs",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
