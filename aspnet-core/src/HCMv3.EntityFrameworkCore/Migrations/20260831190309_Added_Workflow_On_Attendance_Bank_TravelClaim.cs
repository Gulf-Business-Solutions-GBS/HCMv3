using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_Workflow_On_Attendance_Bank_TravelClaim : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkflowRequestId",
                table: "TravelFeedbacks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "TravelFeedbacks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRequestId",
                table: "EmployeeBanks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "EmployeeBanks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRequestId",
                table: "EmployeeAttendances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "EmployeeAttendances",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkflowRequestId",
                table: "TravelFeedbacks");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "TravelFeedbacks");

            migrationBuilder.DropColumn(
                name: "WorkflowRequestId",
                table: "EmployeeBanks");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "EmployeeBanks");

            migrationBuilder.DropColumn(
                name: "WorkflowRequestId",
                table: "EmployeeAttendances");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "EmployeeAttendances");
        }
    }
}
