using HCMv3.LegacyMigrator.Plan;
using Microsoft.Data.SqlClient;

namespace HCMv3.LegacyMigrator.Migration;

/// <summary>
/// Removes everything a previous run of this tool wrote, in reverse dependency order, using
/// <c>LegacyIdMap</c> as the record of exactly which target rows we own.
///
/// <para>This is what makes a customer cutover rehearsable: you can migrate, run the engines, find a mapping
/// defect, fix the plan and start again from a genuinely clean slate -- without truncating tables and thereby
/// destroying rows that were NOT created by the migration (the target may legitimately already contain
/// hand-entered configuration). Only ids present in the map are deleted.</para>
/// </summary>
public sealed class Resetter
{
    private readonly string _targetConnectionString;
    private readonly int _commandTimeoutSeconds;

    public Resetter(string targetConnectionString, int commandTimeoutSeconds)
    {
        _targetConnectionString = targetConnectionString;
        _commandTimeoutSeconds = commandTimeoutSeconds;
    }

    /// <summary>
    /// Calculated ENGINE OUTPUT tables, in child-before-parent order.
    ///
    /// <para>These are never migrated -- copying legacy's values into them would destroy the very
    /// legacy-vs-port comparison the migration exists to enable -- so they are absent from
    /// <c>LegacyIdMap</c> and the mapped-row delete below cannot see them. But they DO carry a foreign key to
    /// <c>Employees</c>, so once the engines have run even once, they block the reset:
    /// <c>The DELETE statement conflicted with the REFERENCE constraint "FK_TimeLogs_Employees_EmployeeId"</c>.
    /// That makes a cutover rehearsal a one-shot affair, which defeats the point of --reset.</para>
    ///
    /// <para>They are purged first, scoped to the migrated employees only. This is safe precisely because
    /// they are derived: re-running the Time and Payroll engines reproduces them exactly. The list is an
    /// explicit allowlist rather than "delete whatever blocks us" -- an unknown blocking table may hold
    /// hand-entered data, and this tool must never silently destroy rows it did not create.</para>
    /// </summary>
    private static readonly (string Table, string EmployeeIdExpression)[] EngineOutputTables =
    {
        // Child first: SalaryRegisterDetails reaches Employees only through its parent register.
        ("SalaryRegisterDetails",
         "[SalaryRegisterId] IN (SELECT [Id] FROM [dbo].[SalaryRegisters] WHERE [EmployeeId] IN ({0}))"),
        ("SalaryRegisters",      "[EmployeeId] IN ({0})"),
        ("TimeRegisters",        "[EmployeeId] IN ({0})"),
        ("MonthlyTimeRegisters", "[EmployeeId] IN ({0})"),
        ("YearlyTimeRegisters",  "[EmployeeId] IN ({0})"),
        ("TimeLogs",             "[EmployeeId] IN ({0})"),
        ("PayrollLogs",          "[EmployeeId] IN ({0})")
    };

    public List<(string Table, int Deleted)> Reset(IReadOnlyList<TableSpec> plan)
    {
        var deleted = new List<(string, int)>();

        using var conn = new SqlConnection(_targetConnectionString);
        conn.Open();

        deleted.AddRange(PurgeEngineOutputs(conn));

        foreach (var spec in plan.Reverse())
        {
            var sql = $@"
IF OBJECT_ID('dbo.{spec.Target}') IS NOT NULL
BEGIN
    DELETE t FROM [dbo].[{spec.Target}] t
    INNER JOIN {IdMapStore.MapTable} m ON m.EntityName = @Entity AND m.NewId = t.[Id];
    SELECT @@ROWCOUNT;
END
ELSE SELECT 0;";

            int rows;
            using (var cmd = new SqlCommand(sql, conn) { CommandTimeout = _commandTimeoutSeconds })
            {
                cmd.Parameters.AddWithValue("@Entity", spec.Target);
                rows = (int)cmd.ExecuteScalar();
            }

            using (var cmd = new SqlCommand($"DELETE FROM {IdMapStore.MapTable} WHERE EntityName = @Entity;", conn)
                   { CommandTimeout = _commandTimeoutSeconds })
            {
                cmd.Parameters.AddWithValue("@Entity", spec.Target);
                cmd.ExecuteNonQuery();
            }

            if (rows > 0)
            {
                deleted.Add((spec.Target, rows));
            }
        }

        return deleted;
    }

    /// <summary>
    /// Deletes calculated engine output for the migrated employees only. See
    /// <see cref="EngineOutputTables"/> for why this is both necessary and safe.
    /// </summary>
    private List<(string Table, int Deleted)> PurgeEngineOutputs(SqlConnection conn)
    {
        var deleted = new List<(string, int)>();

        // The employees this tool created, straight from the map -- never "all employees". A target database
        // may hold employees that predate the migration, and their registers are not ours to delete.
        var employeeIds = new List<int>();
        using (var cmd = new SqlCommand(
                   $"SELECT NewId FROM {IdMapStore.MapTable} WHERE EntityName = 'Employees';", conn)
               { CommandTimeout = _commandTimeoutSeconds })
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                employeeIds.Add(reader.GetInt32(0));
            }
        }

        if (employeeIds.Count == 0)
        {
            return deleted;
        }

        var idList = string.Join(",", employeeIds);

        foreach (var (table, predicateFormat) in EngineOutputTables)
        {
            var predicate = string.Format(predicateFormat, idList);
            var sql = $@"
IF OBJECT_ID('dbo.{table}') IS NOT NULL
BEGIN
    DELETE FROM [dbo].[{table}] WHERE {predicate};
    SELECT @@ROWCOUNT;
END
ELSE SELECT 0;";

            using var cmd = new SqlCommand(sql, conn) { CommandTimeout = _commandTimeoutSeconds };
            var rows = (int)cmd.ExecuteScalar();

            if (rows > 0)
            {
                deleted.Add((table + " (engine output)", rows));
            }
        }

        return deleted;
    }
}
