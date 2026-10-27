using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_LoanTypeDetail_WageType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WageTypeId",
                table: "LoanTypeDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanTypeDetails_WageTypeId",
                table: "LoanTypeDetails",
                column: "WageTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_LoanTypeDetails_WageTypes_WageTypeId",
                table: "LoanTypeDetails",
                column: "WageTypeId",
                principalTable: "WageTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LoanTypeDetails_WageTypes_WageTypeId",
                table: "LoanTypeDetails");

            migrationBuilder.DropIndex(
                name: "IX_LoanTypeDetails_WageTypeId",
                table: "LoanTypeDetails");

            migrationBuilder.DropColumn(
                name: "WageTypeId",
                table: "LoanTypeDetails");
        }
    }
}
