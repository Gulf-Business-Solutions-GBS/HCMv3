using System.Text;

namespace HCMv3.LegacyMigrator.Migration;

public sealed class TableResult
{
    public string SourceTable { get; init; }
    public string TargetTable { get; init; }

    /// <summary>Live (GCRecord IS NULL) source rows matching the selection scope.</summary>
    public int RowsInScope { get; set; }

    /// <summary>In scope but already present in LegacyIdMap from an earlier run -- the idempotency skip.</summary>
    public int RowsAlreadyMapped { get; set; }

    /// <summary>Dropped because a NOT NULL foreign key could not be resolved through the map.</summary>
    public int RowsSkippedUnresolvedFk { get; set; }

    public int RowsWritten { get; set; }

    public TimeSpan Duration { get; set; }

    public bool Skipped { get; set; }
    public string SkipReason { get; set; }

    /// <summary>Legacy columns present in the source that nothing in the target consumes.</summary>
    public List<string> DroppedSourceColumns { get; } = new();

    /// <summary>Target columns that could not be sourced and were left NULL or type-defaulted.</summary>
    public List<string> DefaultedTargetColumns { get; } = new();

    /// <summary>Anything else worth shouting about (FK targets outside the plan, NULL-to-NOT-NULL coercions...).</summary>
    public List<string> Warnings { get; } = new();

    public string NotesText
    {
        get
        {
            var sb = new StringBuilder();
            if (Skipped) sb.AppendLine("SKIPPED: " + SkipReason);
            foreach (var w in Warnings) sb.AppendLine("WARN: " + w);
            if (DefaultedTargetColumns.Count > 0) sb.AppendLine("DEFAULTED TARGET COLS: " + string.Join(", ", DefaultedTargetColumns));
            if (DroppedSourceColumns.Count > 0) sb.AppendLine("DROPPED LEGACY COLS: " + string.Join(", ", DroppedSourceColumns));
            return sb.Length == 0 ? null : sb.ToString();
        }
    }
}
