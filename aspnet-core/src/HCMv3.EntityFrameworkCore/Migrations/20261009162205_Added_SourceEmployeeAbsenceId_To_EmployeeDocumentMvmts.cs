using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_SourceEmployeeAbsenceId_To_EmployeeDocumentMvmts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceEmployeeAbsenceId",
                table: "EmployeeDocumentMvmts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocumentMvmts_SourceEmployeeAbsenceId",
                table: "EmployeeDocumentMvmts",
                column: "SourceEmployeeAbsenceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeeDocumentMvmts_SourceEmployeeAbsenceId",
                table: "EmployeeDocumentMvmts");

            migrationBuilder.DropColumn(
                name: "SourceEmployeeAbsenceId",
                table: "EmployeeDocumentMvmts");
        }
    }
}
