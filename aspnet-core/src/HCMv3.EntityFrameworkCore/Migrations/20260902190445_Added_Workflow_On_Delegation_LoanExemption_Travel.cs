using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_Workflow_On_Delegation_LoanExemption_Travel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TravelPurpose",
                table: "Travels",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRequestId",
                table: "Travels",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "Travels",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AnyOtherExpense",
                table: "TravelDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "HotelComments",
                table: "TravelDetails",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHotelRequired",
                table: "TravelDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVisaRequired",
                table: "TravelDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OtherComments",
                table: "TravelDetails",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisaComments",
                table: "TravelDetails",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "TravelDetailCosts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeId",
                table: "EmployeeLoanExemptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRequestId",
                table: "EmployeeLoanExemptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "EmployeeLoanExemptions",
                type: "int",
                nullable: true);

            // BACKFILL, and it has to run here -- after the column exists, before the foreign key below.
            //
            // EmployeeLoanExemption gained a required EmployeeId so it can be employee-scoped, and every
            // existing row was just handed the scaffolded default of 0. Zero matches no employee, so the FK
            // would fail to create on any database that already holds exemptions -- and this one holds 390.
            // The migration would abort halfway, having added the columns but not the constraint.
            //
            // The value is not guessed: an exemption is always an exemption OF a loan, and the loan already
            // records whose it is.
            //
            // Wrapped in EXEC() so the column reference resolves at execution time rather than batch-compile
            // time. Without it an idempotent script fails with "Invalid column name 'EmployeeId'", because
            // SQL Server compiles the whole batch up front and that column is added by this same migration.
            migrationBuilder.Sql(@"
                EXEC(N'
                    UPDATE x
                    SET x.EmployeeId = l.EmployeeId
                    FROM EmployeeLoanExemptions x
                    INNER JOIN EmployeeLoans l ON l.Id = x.EmployeeLoanId
                    WHERE x.EmployeeId = 0;
                ');");

            migrationBuilder.CreateIndex(
                name: "IX_Travels_TenantId_EmployeeId_WorkflowStatus",
                table: "Travels",
                columns: new[] { "TenantId", "EmployeeId", "WorkflowStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_TravelDetailCosts_CurrencyId",
                table: "TravelDetailCosts",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLoanExemptions_EmployeeId",
                table: "EmployeeLoanExemptions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLoanExemptions_TenantId_EmployeeId_WorkflowStatus",
                table: "EmployeeLoanExemptions",
                columns: new[] { "TenantId", "EmployeeId", "WorkflowStatus" });

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeLoanExemptions_Employees_EmployeeId",
                table: "EmployeeLoanExemptions",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TravelDetailCosts_Currencies_CurrencyId",
                table: "TravelDetailCosts",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeLoanExemptions_Employees_EmployeeId",
                table: "EmployeeLoanExemptions");

            migrationBuilder.DropForeignKey(
                name: "FK_TravelDetailCosts_Currencies_CurrencyId",
                table: "TravelDetailCosts");

            migrationBuilder.DropIndex(
                name: "IX_Travels_TenantId_EmployeeId_WorkflowStatus",
                table: "Travels");

            migrationBuilder.DropIndex(
                name: "IX_TravelDetailCosts_CurrencyId",
                table: "TravelDetailCosts");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeLoanExemptions_EmployeeId",
                table: "EmployeeLoanExemptions");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeLoanExemptions_TenantId_EmployeeId_WorkflowStatus",
                table: "EmployeeLoanExemptions");

            migrationBuilder.DropColumn(
                name: "TravelPurpose",
                table: "Travels");

            migrationBuilder.DropColumn(
                name: "WorkflowRequestId",
                table: "Travels");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "Travels");

            migrationBuilder.DropColumn(
                name: "AnyOtherExpense",
                table: "TravelDetails");

            migrationBuilder.DropColumn(
                name: "HotelComments",
                table: "TravelDetails");

            migrationBuilder.DropColumn(
                name: "IsHotelRequired",
                table: "TravelDetails");

            migrationBuilder.DropColumn(
                name: "IsVisaRequired",
                table: "TravelDetails");

            migrationBuilder.DropColumn(
                name: "OtherComments",
                table: "TravelDetails");

            migrationBuilder.DropColumn(
                name: "VisaComments",
                table: "TravelDetails");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "TravelDetailCosts");

            migrationBuilder.DropColumn(
                name: "EmployeeId",
                table: "EmployeeLoanExemptions");

            migrationBuilder.DropColumn(
                name: "WorkflowRequestId",
                table: "EmployeeLoanExemptions");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "EmployeeLoanExemptions");
        }
    }
}
