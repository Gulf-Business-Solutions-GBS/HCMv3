using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_PayrollPeriodState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PeriodState",
                table: "PayrollControlRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StateChangedTime",
                table: "PayrollControlRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "StateChangedUserId",
                table: "PayrollControlRecords",
                type: "bigint",
                nullable: true);

            // Existing periods keep the state their flags already describe: closed wins over locked, as it does in
            // the engine, and anything else is simply not started.
            migrationBuilder.Sql(
                "UPDATE [PayrollControlRecords] SET [PeriodState] = CASE " +
                "WHEN [PeriodClosed] = 1 THEN 3 WHEN [PeriodLocked] = 1 THEN 1 ELSE 0 END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PeriodState",
                table: "PayrollControlRecords");

            migrationBuilder.DropColumn(
                name: "StateChangedTime",
                table: "PayrollControlRecords");

            migrationBuilder.DropColumn(
                name: "StateChangedUserId",
                table: "PayrollControlRecords");
        }
    }
}
