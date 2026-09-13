using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;
using HCMv3.BusinessRules.Records;

namespace HCMv3.BusinessRules.Services;

/// <summary>
/// Runs every rule armed on one base object and event.
///
/// <para>
/// The half of the engine that knows about tenants, caches, effective dates and failure policy. Everything
/// to do with interpreting a rule tree lives in <see cref="IRuleEvaluator"/>; everything to do with
/// deciding WHICH trees to walk lives here. Callers -- an entity event handler, a workflow resolver, the
/// test harness -- only ever talk to this.
/// </para>
///
/// <para><b>Failure policy, which is the part that matters in production.</b> A rule that raises a
/// deliberate business error aborts the operation with that message, localized: that is the rule working.
/// A rule that is BROKEN -- bad field, failed conversion, unparseable definition -- aborts the operation
/// too, unless its assignment says <c>ContinueOnError</c>. The default is deliberately the strict one. A
/// derivation that silently stops deriving is how a probation date quietly goes missing for six months, and
/// a loud failure at the moment configuration breaks is far cheaper than a quiet one discovered later.</para>
/// </summary>
public interface IBusinessRuleRunner : IDomainService
{
    /// <summary>
    /// Runs the armed rules against the context, applying writes in place.
    ///
    /// <para>
    /// Throws <c>UserFriendlyException</c> when a rule raises a business error or when a rule fails and its
    /// assignment does not tolerate errors. Returns normally -- with <see cref="BusinessRuleRunResult"/>
    /// describing what happened -- in every other case, including when no rule is armed at all, which is
    /// the overwhelmingly common case and costs one cache lookup.
    /// </para>
    /// </summary>
    Task<BusinessRuleRunResult> RunAsync(RuleExecutionContext context, int? targetEntityId = null);

    /// <summary>
    /// Whether any rule is armed on the event and in effect on <paramref name="asOf"/>. A cache read, no
    /// database work -- so a caller can skip building an expensive context (the leave hook loads the employee
    /// and the leave type to populate one) on the overwhelmingly common save where nothing is armed at all.
    /// </summary>
    Task<bool> HasArmedRulesAsync(int? tenantId, string baseObjectCode, RuleEventType eventType, DateTime asOf);

    /// <summary>
    /// Runs a single rule definition without persisting anything and without consulting assignments -- the
    /// authoring screen's "test this rule" button.
    ///
    /// <para>
    /// Errors are RETURNED rather than thrown here. A test run reporting every problem at once is the
    /// point of the feature; a test harness that stops at the first error would just be the save button.
    /// </para>
    /// </summary>
    Task<BusinessRuleRunResult> TestAsync(
        string definitionJson,
        string baseObjectCode,
        RulePurpose purpose,
        RuleExecutionContext context);
}

/// <summary>What a whole event's worth of rules did. Also the shape the test harness returns to the UI.</summary>
public class BusinessRuleRunResult
{
    /// <summary>Rules that were armed, effective and actually evaluated.</summary>
    public int RulesEvaluated { get; set; }

    /// <summary>Of those, how many had a condition that held.</summary>
    public int RulesMatched { get; set; }

    public List<RuleFieldWrite> Writes { get; } = new();

    /// <summary>
    /// Creates and updates to other tables, from every rule that ran. Only returned when no rule raised an
    /// error -- RunAsync throws first -- so a caller applying them never applies half a rule set.
    /// </summary>
    public List<RuleRecordChange> RecordChanges { get; } = new();

    /// <summary>Business errors raised by rules, already localized.</summary>
    public List<string> Errors { get; } = new();

    /// <summary>
    /// Rules that broke, as "code: reason". Populated even when execution continued, so a tolerated failure
    /// is still visible to whoever reads the result rather than being swallowed entirely.
    /// </summary>
    public List<string> Failures { get; } = new();

    /// <summary>Human-readable trace of the evaluation, for the test harness and the execution log.</summary>
    public string Trace { get; set; }

    public bool HasErrors => Errors.Count > 0;

    public bool HasFailures => Failures.Count > 0;
}
