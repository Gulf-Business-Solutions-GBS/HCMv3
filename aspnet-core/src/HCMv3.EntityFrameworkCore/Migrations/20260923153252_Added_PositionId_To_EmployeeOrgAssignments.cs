using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_PositionId_To_EmployeeOrgAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PositionId",
                table: "EmployeeOrgAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrgAssignments_PositionId",
                table: "EmployeeOrgAssignments",
                column: "PositionId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeOrgAssignments_Positions_PositionId",
                table: "EmployeeOrgAssignments",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeOrgAssignments_Positions_PositionId",
                table: "EmployeeOrgAssignments");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeOrgAssignments_PositionId",
                table: "EmployeeOrgAssignments");

            migrationBuilder.DropColumn(
                name: "PositionId",
                table: "EmployeeOrgAssignments");
        }
    }
}
