using System;
using System.Collections.Generic;
using System.Linq;

namespace HCMv3.Common;

/// <summary>
/// Pure, framework-agnostic id-list compression utility (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.RLSService</c>'s nested <c>RLSRangeStore.Build</c>): dedupes
/// and sorts a list of ids, then compresses contiguous runs into inclusive ranges while leaving
/// non-contiguous ids as "singles". This is the compression algorithm only -- legacy's
/// <c>RLSRangeStore.GetCriteria</c>/<c>RLSService.BuildInOrRangeCriteria</c> turned the compressed shape
/// into a DevExpress <c>CriteriaOperator</c> object graph for XAF grid data-access-mode filtering, which
/// has no equivalent in this ABP/EF Core port (see <c>IRlsService</c> remarks -- <c>GetRlsEmployeeIdsAsync</c>
/// callers just use a plain <c>List&lt;int&gt;</c> with EF Core's <c>.Where(x => ids.Contains(x.Id))</c>
/// instead). <see cref="CompressToSqlFragment"/> is provided here purely as a framework-agnostic string
/// builder for the rare case a caller needs a literal SQL WHERE-fragment (e.g. for
/// <c>FromSqlRaw</c>/<c>FromSqlInterpolated</c>) rather than an in-memory <c>Contains</c> filter.
/// </summary>
public static class IdRangeCompressor
{
    /// <summary>
    /// Compresses <paramref name="ids"/> into deduped/sorted "singles" (ids with no contiguous neighbor)
    /// and "ranges" (inclusive start/end pairs for runs of 2 or more consecutive ids), mirroring legacy
    /// <c>RLSRangeStore.Build</c>'s in-place unique-compaction + contiguous-run-splitting algorithm.
    /// </summary>
    public static (List<int> Singles, List<(int Start, int End)> Ranges) Compress(IEnumerable<int> ids)
    {
        var singles = new List<int>();
        var ranges = new List<(int Start, int End)>();

        if (ids == null)
        {
            return (singles, ranges);
        }

        var sorted = ids.Distinct().OrderBy(x => x).ToList();
        if (sorted.Count == 0)
        {
            return (singles, ranges);
        }

        var start = sorted[0];
        var prev = sorted[0];

        for (var i = 1; i <= sorted.Count; i++)
        {
            var breakRun = i == sorted.Count || sorted[i] != prev + 1;
            if (breakRun)
            {
                if (start == prev)
                {
                    singles.Add(start);
                }
                else
                {
                    ranges.Add((start, prev));
                }

                if (i < sorted.Count)
                {
                    start = sorted[i];
                    prev = sorted[i];
                }
            }
            else
            {
                prev = sorted[i];
            }
        }

        return (singles, ranges);
    }

    /// <summary>
    /// Builds a SQL WHERE-fragment (e.g. <c>"EmployeeId IN (1,2,3) OR EmployeeId BETWEEN 100 AND 150"</c>)
    /// from the compressed shape of <paramref name="ids"/>, suitable for use inside
    /// <c>FromSqlRaw</c>/<c>FromSqlInterpolated</c> or a raw SQL query's WHERE clause.
    /// </summary>
    /// <remarks>
    /// <paramref name="columnName"/> is concatenated directly into the returned fragment and is NOT
    /// parameterized -- callers must only ever pass a trusted, hardcoded column name (never raw user
    /// input) to avoid SQL injection. The <paramref name="ids"/> values themselves are safe to embed
    /// literally since they are constrained to <see cref="int"/>.
    /// </remarks>
    public static string CompressToSqlFragment(string columnName, IEnumerable<int> ids)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            throw new ArgumentException("columnName is required.", nameof(columnName));
        }

        var (singles, ranges) = Compress(ids);

        if (singles.Count == 0 && ranges.Count == 0)
        {
            // Matches legacy RLSRangeStore.GetCriteria/AlwaysFalse: an empty id list should never match
            // any row.
            return "1 = 0";
        }

        var parts = new List<string>();

        if (singles.Count > 0)
        {
            parts.Add($"{columnName} IN ({string.Join(",", singles)})");
        }

        foreach (var range in ranges)
        {
            parts.Add($"{columnName} BETWEEN {range.Start} AND {range.End}");
        }

        return string.Join(" OR ", parts);
    }
}
