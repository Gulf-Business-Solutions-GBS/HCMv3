using System.Collections.Generic;
using System.Linq;
using HCMv3.BusinessRules.Definition;

namespace HCMv3.BusinessRules.Services;

/// <summary>
/// Which actions a rule may carry out on each event.
///
/// <para>
/// The event decides what is even meaningful. A rule on <see cref="RuleEventType.OnValidate"/> or
/// <see cref="RuleEventType.BeforeDelete"/> exists to refuse something, so changing a field there would be a
/// validation quietly editing the record it is judging -- or editing a record that is about to be deleted.
/// </para>
///
/// <para>
/// Enforced in TWO places, on purpose. At arm time, so an administrator is told immediately that the rule
/// they are attaching can never do its job on that event. And at run time, because a rule's definition can
/// be edited after it was armed. The failure this replaces was a silent one: a disallowed "set" merely traced
/// "would set" and did nothing, so the rule looked healthy and changed nothing, forever.
/// </para>
/// </summary>
public static class RuleEventActionPolicy
{
    private static readonly IReadOnlyDictionary<RuleEventType, HashSet<string>> AllowedByEvent =
        new Dictionary<RuleEventType, HashSet<string>>
        {
            // Defaults on a new form, and warnings about them.
            [RuleEventType.OnInit] = new() { RuleSetAction.KindName, RuleErrorAction.KindName },

            // Derive values before the record is written, or refuse the write.
            [RuleEventType.BeforeSave] = new() { RuleSetAction.KindName, RuleErrorAction.KindName },

            // Checks only.
            [RuleEventType.OnValidate] = new() { RuleErrorAction.KindName },
            [RuleEventType.BeforeDelete] = new() { RuleErrorAction.KindName },

            // The record is already written by now, so it cannot be re-derived here. What AfterSave is FOR is
            // follow-up changes to other tables, made inside the same transaction; refusing still rolls the
            // whole save back.
            [RuleEventType.AfterSave] = new()
            {
                RuleErrorAction.KindName,
                RuleCreateRecordAction.KindName,
                RuleUpdateRecordAction.KindName
            },

            // Chooses the approval chain by setting the routing parameter.
            [RuleEventType.OnRoute] = new() { RuleSetAction.KindName, RuleErrorAction.KindName },
        };

    /// <summary>The action kinds <paramref name="eventType"/> permits. Empty for an event nothing runs on.</summary>
    public static IReadOnlyCollection<string> AllowedKindsFor(RuleEventType eventType)
    {
        return AllowedByEvent.TryGetValue(eventType, out var kinds)
            ? kinds
            : new HashSet<string>();
    }

    public static bool Allows(RuleEventType eventType, string actionKind)
    {
        return AllowedByEvent.TryGetValue(eventType, out var kinds) && kinds.Contains(actionKind);
    }

    /// <summary>
    /// The distinct action kinds in <paramref name="definition"/> that <paramref name="eventType"/> does not
    /// permit, in the order they first appear. Both branches of every block count: an Else branch that
    /// assigns a field is just as unable to run on OnValidate as a Then branch.
    /// </summary>
    public static IReadOnlyList<string> FindDisallowedKinds(RuleDefinition definition, RuleEventType eventType)
    {
        if (definition == null)
        {
            return new List<string>();
        }

        return definition.Blocks
            .SelectMany(block => (block.Then ?? new List<RuleAction>()).Concat(block.Else ?? new List<RuleAction>()))
            .Select(action => action.Kind)
            .Where(kind => !Allows(eventType, kind))
            .Distinct()
            .ToList();
    }
}
