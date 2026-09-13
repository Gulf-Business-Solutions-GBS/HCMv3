using System.Collections.Generic;

namespace HCMv3.BusinessRules.Records;

/// <summary>
/// Applies the record changes an AfterSave rule asked for, to the current unit of work.
///
/// <para>
/// It STAGES changes; it does not save them. The caller saves, inside the same transaction as the record that
/// triggered the rule -- that is what makes "raise an error and do nothing" true of a failed follow-up: the
/// follow-up and the original save commit together or not at all.
/// </para>
/// </summary>
public interface IRuleRecordWriter
{
    void Apply(IReadOnlyList<RuleRecordChange> changes);
}

/// <summary>What kind of change a record action asked for.</summary>
public enum RuleRecordChangeKind
{
    Create = 1,
    Update = 2
}

/// <summary>
/// One create or update an AfterSave rule decided on, with its values already evaluated.
///
/// <para>
/// This is how the evaluator stays free of the database. It resolves every expression and hands back a plain
/// description of the change; a separate writer applies it. The evaluator's safety argument -- it can only touch
/// the objects it is given -- survives the engine being allowed to write other tables.
/// </para>
/// </summary>
public class RuleRecordChange
{
    public RuleRecordChange(
        string ruleCode,
        RuleRecordChangeKind kind,
        string table,
        long? recordId,
        IReadOnlyDictionary<string, object> values)
    {
        RuleCode = ruleCode;
        Kind = kind;
        Table = table;
        RecordId = recordId;
        Values = values;
    }

    public string RuleCode { get; }

    public RuleRecordChangeKind Kind { get; }

    public string Table { get; }

    /// <summary>The row to update. Null for a create.</summary>
    public long? RecordId { get; }

    public IReadOnlyDictionary<string, object> Values { get; }

    public override string ToString()
    {
        return Kind == RuleRecordChangeKind.Create
            ? "create " + Table
            : "update " + Table + " #" + RecordId;
    }
}
