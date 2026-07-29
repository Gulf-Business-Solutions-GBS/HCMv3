using Microsoft.Data.SqlClient;

namespace HCMv3.LegacyMigrator.Migration;

/// <summary>
/// Counts how many rows a NULL -&gt; NOT NULL coercion would ACTUALLY affect, rather than merely reporting that
/// the coercion is theoretically possible.
///
/// <para>This matters a lot for signal-to-noise. The legacy XPO schema declares almost every column nullable
/// while the HCMv3 entities declare many of the same columns NOT NULL, so a purely schema-derived warning list
/// runs to hundreds of lines and buries the handful of cases where real data is being silently altered. Only
/// non-zero counts are worth a human's attention.</para>
/// </summary>
public static class NullCoercionProbe
{
    public static Dictionary<string, int> Count(
        string targetConnectionString,
        string sourceDatabase,
        string sourceTable,
        IReadOnlyCollection<string> sourceColumns,
        string whereClause)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (sourceColumns.Count == 0)
        {
            return counts;
        }

        var ordered = sourceColumns.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var selectList = string.Join(",\n       ",
            ordered.Select((c, i) => $"SUM(CASE WHEN s.[{c}] IS NULL THEN 1 ELSE 0 END) AS c{i}"));

        var sql = $"SELECT {selectList}\nFROM [{sourceDatabase}].[dbo].[{sourceTable}] s\n{whereClause};";

        using var conn = new SqlConnection(targetConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 600 };
        using var r = cmd.ExecuteReader();
        if (r.Read())
        {
            for (var i = 0; i < ordered.Count; i++)
            {
                if (!r.IsDBNull(i) && r.GetInt32(i) > 0)
                {
                    counts[ordered[i]] = r.GetInt32(i);
                }
            }
        }

        return counts;
    }
}
