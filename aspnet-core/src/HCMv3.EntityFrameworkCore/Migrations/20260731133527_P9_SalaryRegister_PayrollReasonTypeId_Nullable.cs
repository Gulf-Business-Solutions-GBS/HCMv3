using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class P9_SalaryRegister_PayrollReasonTypeId_Nullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalaryRegisters_PayrollReasonTypes_PayrollReasonTypeId",
                table: "SalaryRegisters");

            migrationBuilder.AlterColumn<int>(
                name: "PayrollReasonTypeId",
                table: "SalaryRegisters",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryRegisters_PayrollReasonTypes_PayrollReasonTypeId",
                table: "SalaryRegisters",
                column: "PayrollReasonTypeId",
                principalTable: "PayrollReasonTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalaryRegisters_PayrollReasonTypes_PayrollReasonTypeId",
                table: "SalaryRegisters");

            migrationBuilder.AlterColumn<int>(
                name: "PayrollReasonTypeId",
                table: "SalaryRegisters",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryRegisters_PayrollReasonTypes_PayrollReasonTypeId",
                table: "SalaryRegisters",
                column: "PayrollReasonTypeId",
                principalTable: "PayrollReasonTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
