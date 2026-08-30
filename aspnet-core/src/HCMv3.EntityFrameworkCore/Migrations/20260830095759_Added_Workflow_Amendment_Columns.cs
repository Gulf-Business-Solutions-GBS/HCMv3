using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_Workflow_Amendment_Columns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AmendmentJson",
                table: "WorkflowRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAmendment",
                table: "WorkflowRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SnapshotJson",
                table: "WorkflowRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmendmentJson",
                table: "WorkflowRequests");

            migrationBuilder.DropColumn(
                name: "IsAmendment",
                table: "WorkflowRequests");

            migrationBuilder.DropColumn(
                name: "SnapshotJson",
                table: "WorkflowRequests");
        }
    }
}
