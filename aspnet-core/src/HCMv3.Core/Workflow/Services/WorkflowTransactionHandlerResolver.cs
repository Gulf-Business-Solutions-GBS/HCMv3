using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Dependency;
using Abp.Extensions;

namespace HCMv3.Workflow.Services;

/// <inheritdoc cref="IWorkflowTransactionHandlerResolver"/>
public class WorkflowTransactionHandlerResolver : HCMv3DomainServiceBase, IWorkflowTransactionHandlerResolver, ISingletonDependency
{
    private readonly Dictionary<string, IWorkflowTransactionHandler> _handlersByRequestType;

    public WorkflowTransactionHandlerResolver(IEnumerable<IWorkflowTransactionHandler> handlers)
    {
        // Two handlers claiming the same type is a deployment mistake with no safe resolution -- picking
        // either one silently would mean a request's side effects depend on assembly scan order. It fails at
        // startup, where it is obvious, rather than at submit time for one unlucky employee.
        var duplicates = handlers
            .Where(h => !h.RequestType.IsNullOrWhiteSpace())
            .GroupBy(h => h.RequestType, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException(
                "More than one IWorkflowTransactionHandler is registered for: " + string.Join(", ", duplicates) +
                ". Each transaction type must have exactly one handler.");
        }

        _handlersByRequestType = handlers
            .Where(h => !h.RequestType.IsNullOrWhiteSpace())
            .ToDictionary(h => h.RequestType, h => h, StringComparer.OrdinalIgnoreCase);
    }

    public IWorkflowTransactionHandler Resolve(string requestType)
    {
        if (requestType.IsNullOrWhiteSpace())
        {
            return null;
        }

        return _handlersByRequestType.TryGetValue(requestType, out var handler) ? handler : null;
    }
}
