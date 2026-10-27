using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_DocumentType_PrintReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrintReportId",
                table: "DocumentTypes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_PrintReportId",
                table: "DocumentTypes",
                column: "PrintReportId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentTypes_PrintReports_PrintReportId",
                table: "DocumentTypes",
                column: "PrintReportId",
                principalTable: "PrintReports",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentTypes_PrintReports_PrintReportId",
                table: "DocumentTypes");

            migrationBuilder.DropIndex(
                name: "IX_DocumentTypes_PrintReportId",
                table: "DocumentTypes");

            migrationBuilder.DropColumn(
                name: "PrintReportId",
                table: "DocumentTypes");
        }
    }
}
