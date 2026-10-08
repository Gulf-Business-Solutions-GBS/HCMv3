using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Passport_Custody_Trail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ActualWithdrawDate",
                table: "EmployeeDocumentMvmts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExtendsMovementId",
                table: "EmployeeDocumentMvmts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "HandedOverByUserId",
                table: "EmployeeDocumentMvmts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OverdueReminderSentOn",
                table: "EmployeeDocumentMvmts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReceivedByUserId",
                table: "EmployeeDocumentMvmts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnRemarks",
                table: "EmployeeDocumentMvmts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocumentMvmts_ExtendsMovementId",
                table: "EmployeeDocumentMvmts",
                column: "ExtendsMovementId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeDocumentMvmts_EmployeeDocumentMvmts_ExtendsMovementId",
                table: "EmployeeDocumentMvmts",
                column: "ExtendsMovementId",
                principalTable: "EmployeeDocumentMvmts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeDocumentMvmts_EmployeeDocumentMvmts_ExtendsMovementId",
                table: "EmployeeDocumentMvmts");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeDocumentMvmts_ExtendsMovementId",
                table: "EmployeeDocumentMvmts");

            migrationBuilder.DropColumn(
                name: "ActualWithdrawDate",
                table: "EmployeeDocumentMvmts");

            migrationBuilder.DropColumn(
                name: "ExtendsMovementId",
                table: "EmployeeDocumentMvmts");

            migrationBuilder.DropColumn(
                name: "HandedOverByUserId",
                table: "EmployeeDocumentMvmts");

            migrationBuilder.DropColumn(
                name: "OverdueReminderSentOn",
                table: "EmployeeDocumentMvmts");

            migrationBuilder.DropColumn(
                name: "ReceivedByUserId",
                table: "EmployeeDocumentMvmts");

            migrationBuilder.DropColumn(
                name: "ReturnRemarks",
                table: "EmployeeDocumentMvmts");
        }
    }
}
