using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_EmployeeOrgAssignment_Group_SubGroup_PayrollArea_FKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeGroupId",
                table: "EmployeeOrgAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeSubGroupId",
                table: "EmployeeOrgAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PayrollAreaId",
                table: "EmployeeOrgAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrgAssignments_EmployeeGroupId",
                table: "EmployeeOrgAssignments",
                column: "EmployeeGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrgAssignments_EmployeeSubGroupId",
                table: "EmployeeOrgAssignments",
                column: "EmployeeSubGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrgAssignments_PayrollAreaId",
                table: "EmployeeOrgAssignments",
                column: "PayrollAreaId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeOrgAssignments_EmployeeGroups_EmployeeGroupId",
                table: "EmployeeOrgAssignments",
                column: "EmployeeGroupId",
                principalTable: "EmployeeGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeOrgAssignments_EmployeeSubGroups_EmployeeSubGroupId",
                table: "EmployeeOrgAssignments",
                column: "EmployeeSubGroupId",
                principalTable: "EmployeeSubGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeOrgAssignments_PayrollAreas_PayrollAreaId",
                table: "EmployeeOrgAssignments",
                column: "PayrollAreaId",
                principalTable: "PayrollAreas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeOrgAssignments_EmployeeGroups_EmployeeGroupId",
                table: "EmployeeOrgAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeOrgAssignments_EmployeeSubGroups_EmployeeSubGroupId",
                table: "EmployeeOrgAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeOrgAssignments_PayrollAreas_PayrollAreaId",
                table: "EmployeeOrgAssignments");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeOrgAssignments_EmployeeGroupId",
                table: "EmployeeOrgAssignments");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeOrgAssignments_EmployeeSubGroupId",
                table: "EmployeeOrgAssignments");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeOrgAssignments_PayrollAreaId",
                table: "EmployeeOrgAssignments");

            migrationBuilder.DropColumn(
                name: "EmployeeGroupId",
                table: "EmployeeOrgAssignments");

            migrationBuilder.DropColumn(
                name: "EmployeeSubGroupId",
                table: "EmployeeOrgAssignments");

            migrationBuilder.DropColumn(
                name: "PayrollAreaId",
                table: "EmployeeOrgAssignments");
        }
    }
}
