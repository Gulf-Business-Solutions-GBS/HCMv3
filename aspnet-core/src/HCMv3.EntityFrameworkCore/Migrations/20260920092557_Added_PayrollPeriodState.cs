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
            // Added as guarded SQL rather than AddColumn, because a first attempt at this migration could add the
            // columns and still fail on the UPDATE below. That leaves the columns in place while the migration goes
            // unrecorded, so the next run would stop on a duplicate column and the server could not be brought up to
            // date without hand-editing the database.
            migrationBuilder.Sql(
                "IF COL_LENGTH('PayrollControlRecords', 'PeriodState') IS NULL " +
                "ALTER TABLE [PayrollControlRecords] ADD [PeriodState] int NOT NULL DEFAULT 0;");

            migrationBuilder.Sql(
                "IF COL_LENGTH('PayrollControlRecords', 'StateChangedTime') IS NULL " +
                "ALTER TABLE [PayrollControlRecords] ADD [StateChangedTime] datetime2 NULL;");

            migrationBuilder.Sql(
                "IF COL_LENGTH('PayrollControlRecords', 'StateChangedUserId') IS NULL " +
                "ALTER TABLE [PayrollControlRecords] ADD [StateChangedUserId] bigint NULL;");

            // Existing periods keep the state their flags already describe: closed wins over locked, as it does in
            // the engine, and anything else is simply not started.
            //
            // Wrapped in EXEC for the same reason as Added_WorkflowStep_To_WorkFlowApprovers: SQL Server parses a
            // whole batch before running any of it, so an UPDATE naming a column added earlier in that batch is
            // checked against the schema as it was BEFORE the column existed, and fails with "Invalid column name
            // 'PeriodState'". EXEC defers that check until the statement actually runs.
            migrationBuilder.Sql(
                "EXEC(N'UPDATE [PayrollControlRecords] SET [PeriodState] = CASE " +
                "WHEN [PeriodClosed] = 1 THEN 3 WHEN [PeriodLocked] = 1 THEN 1 ELSE 0 END;');");
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
