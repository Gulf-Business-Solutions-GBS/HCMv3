namespace HCMv3.Workflow.Services;

/// <summary>
/// Finds the <see cref="IWorkflowTransactionHandler"/> for a transaction type. Kept behind an interface so
/// the approval engine never enumerates handlers itself.
/// </summary>
public interface IWorkflowTransactionHandlerResolver
{
    /// <summary>
    /// The handler for <paramref name="requestType"/>, or <c>null</c> when the type has none.
    ///
    /// <para>
    /// Null is a legitimate answer, not an error: a type can be catalogued and routed for approval while
    /// having no type-specific validation or side effects. The engine treats a missing handler as "generic
    /// approval only".
    /// </para>
    /// </summary>
    IWorkflowTransactionHandler Resolve(string requestType);
}
