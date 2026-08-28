namespace HCMv3;

/// <summary>
/// Names of this application's own ABP data filters, alongside ABP's built-in
/// <c>Abp.Domain.Uow.AbpDataFilters</c> (SoftDelete, MustHaveTenant, MayHaveTenant).
/// </summary>
public static class HCMv3DataFilters
{
    /// <summary>
    /// Hides workflow-managed rows that have not been approved yet.
    ///
    /// This is the filter that makes "one table for the request and the transaction" safe. In the legacy
    /// system a pending leave lived in its own <c>LeaveRequest</c> table, so payroll, quota, attendance and
    /// every report were blind to it by construction -- and the price was permanent drift between that
    /// table and <c>EmployeeLeave</c>. Here the pending row sits in <c>EmployeeAbsences</c> itself, and this
    /// filter is what keeps it invisible until it is approved.
    ///
    /// Enabled by default, so a developer who knows nothing about workflow writes an ordinary query and
    /// still cannot see un-approved data. Screens that must see pending rows -- ESS, the approval inbox,
    /// the requester's own history -- opt out explicitly:
    /// <code>
    /// using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
    /// {
    ///     // pending, draft and rejected rows are visible here
    /// }
    /// </code>
    /// Applies to every entity implementing <c>HCMv3.Workflow.IWorkflowEnabledEntity</c>.
    ///
    /// <para>
    /// Read "approved" as "cleared for the rest of the system to use", not literally "someone pressed
    /// Approve". A row whose <c>WorkflowStatus</c> is null was never submitted for approval at all -- either
    /// its transaction type has workflow switched off, or HR entered it directly -- and it passes this
    /// filter. That is deliberate and is what makes the filter backward compatible: every row that exists
    /// today has a null status and keeps behaving exactly as it did before.
    /// </para>
    /// <para>
    /// Deliberately NOT named <c>EffectiveOnly</c>: <c>Effective</c> already means date-effectivity
    /// throughout this codebase (<c>EffectiveDate</c>, <c>EffectiveDateInRecPayment</c>, and ~109 files
    /// besides), so that name was read as a date filter. This one checks no dates at all.
    /// </para>
    /// </summary>
    public const string ApprovedOnly = "ApprovedOnly";
}
