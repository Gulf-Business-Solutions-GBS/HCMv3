using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_LoanType_WageTypeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WageTypeId",
                table: "LoanTypes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanTypes_WageTypeId",
                table: "LoanTypes",
                column: "WageTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_LoanTypes_WageTypes_WageTypeId",
                table: "LoanTypes",
                column: "WageTypeId",
                principalTable: "WageTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LoanTypes_WageTypes_WageTypeId",
                table: "LoanTypes");

            migrationBuilder.DropIndex(
                name: "IX_LoanTypes_WageTypeId",
                table: "LoanTypes");

            migrationBuilder.DropColumn(
                name: "WageTypeId",
                table: "LoanTypes");
        }
    }
}
