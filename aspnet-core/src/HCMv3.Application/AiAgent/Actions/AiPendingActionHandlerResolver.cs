using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Dependency;

namespace HCMv3.AiAgent.Actions;

public class AiPendingActionHandlerResolver : ITransientDependency
{
    private readonly IReadOnlyDictionary<string, IAiPendingActionHandler> _handlers;

    public AiPendingActionHandlerResolver(IEnumerable<IAiPendingActionHandler> handlers)
    {
        var list = handlers.ToList();

        var duplicate = list.GroupBy(h => h.ActionType, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1);
        if (duplicate != null)
        {
            throw new InvalidOperationException($"Two AI action handlers claim action type '{duplicate.Key}'.");
        }

        _handlers = list.ToDictionary(h => h.ActionType, StringComparer.Ordinal);
    }

    public IReadOnlyCollection<IAiPendingActionHandler> All => _handlers.Values.ToList();

    public IAiPendingActionHandler Get(string actionType) =>
        _handlers.TryGetValue(actionType, out var handler)
            ? handler
            : throw new InvalidOperationException($"No AI action handler is registered for '{actionType}'.");
}
