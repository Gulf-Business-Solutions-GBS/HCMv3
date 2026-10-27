using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Made_SimulationSalaryRegister_PayrollReasonType_Optional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SimulationSalaryRegisters_PayrollReasonTypes_PayrollReasonTypeId",
                table: "SimulationSalaryRegisters");

            migrationBuilder.AlterColumn<int>(
                name: "PayrollReasonTypeId",
                table: "SimulationSalaryRegisters",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_SimulationSalaryRegisters_PayrollReasonTypes_PayrollReasonTypeId",
                table: "SimulationSalaryRegisters",
                column: "PayrollReasonTypeId",
                principalTable: "PayrollReasonTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SimulationSalaryRegisters_PayrollReasonTypes_PayrollReasonTypeId",
                table: "SimulationSalaryRegisters");

            migrationBuilder.AlterColumn<int>(
                name: "PayrollReasonTypeId",
                table: "SimulationSalaryRegisters",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SimulationSalaryRegisters_PayrollReasonTypes_PayrollReasonTypeId",
                table: "SimulationSalaryRegisters",
                column: "PayrollReasonTypeId",
                principalTable: "PayrollReasonTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
