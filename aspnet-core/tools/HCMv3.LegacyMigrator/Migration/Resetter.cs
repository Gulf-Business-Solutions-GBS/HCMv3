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

    public List<(string Table, int Deleted)> Reset(IReadOnlyList<TableSpec> plan)
    {
        var deleted = new List<(string, int)>();

        using var conn = new SqlConnection(_targetConnectionString);
        conn.Open();

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
}
