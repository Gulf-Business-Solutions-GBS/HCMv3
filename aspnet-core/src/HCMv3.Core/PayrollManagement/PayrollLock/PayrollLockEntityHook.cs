using Abp.Domain.Entities;
using Abp.Events.Bus.Entities;
using Abp.Events.Bus.Handlers;
using Abp.Runtime.Caching;
using HCMv3.SystemSecurity;
using HCMv3.Workflow;

namespace HCMv3.PayrollManagement.PayrollLock;

/// <summary>
/// Puts every create, update and delete through <see cref="IPayrollLockGuard"/> -- the equivalent of legacy
/// running the check in <c>MyBaseObject</c>, the base class every business object shared.
///
/// <para><b>One handler for every entity.</b> ABP raises <c>EntityChangingEventData</c> for creating, updating and
/// deleting (soft deletes included), before anything is written, and raises it again for each base type of the
/// entity -- so a handler on <see cref="Entity{TPrimaryKey}"/> of <c>int</c> hears every <c>int</c>-keyed entity.
/// That is deliberate here, unlike the business rule hooks, which opt entities in one at a time: which types the
/// lock covers is company configuration, not code, so it cannot be decided per type at compile time. The
/// guard's first step is a cached set lookup, so an unrelated save pays almost nothing.</para>
///
/// <para>Registered by hand in <c>HCMv3CoreModule</c>, for the reason given on the business rule hooks there.</para>
/// </summary>
public class PayrollLockEntityHook : IEventHandler<EntityChangingEventData<Entity<int>>>
{
    private readonly IPayrollLockGuard _guard;
    private readonly IPayrollRetroTrigger _retroTrigger;

    public PayrollLockEntityHook(IPayrollLockGuard guard, IPayrollRetroTrigger retroTrigger)
    {
        _guard = guard;
        _retroTrigger = retroTrigger;
    }

    /// <summary>
    /// Refuse first, record second -- legacy does both in this order in <c>MyBaseObject.OnSaving</c>, and a change
    /// that is not allowed must not move the employee's recalculation date.
    /// </summary>
    public void HandleEvent(EntityChangingEventData<Entity<int>> eventData)
    {
        if (IsNotYetInEffect(eventData.Entity))
        {
            return;
        }

        _guard.EnsureChangeAllowed(eventData.Entity);
        _retroTrigger.RecordChange(eventData.Entity, deleted: eventData.Entity is ISoftDelete { IsDeleted: true });
    }

    /// <summary>
    /// A workflow-managed row that is still a REQUEST -- drafted, awaiting approval, or rejected -- is not a
    /// payroll change, so the lock does not apply to it and it does not move the recalculation date.
    ///
    /// <para>
    /// Legacy got this for free: a request lived in its own table, so an employee could always apply and only
    /// the approval, which wrote the transaction, met the lock. With one row for both, the same row passes
    /// through here while it is still invisible to payroll (the <c>ApprovedOnly</c> filter hides it), and
    /// refusing it meant nobody could even ask for leave while a payroll run was open.
    /// </para>
    ///
    /// <para>
    /// Judged on the status the row is being saved WITH. Becoming Approved is a payroll change and is checked,
    /// which keeps "approval is refused while the period is locked" exactly as it was. So is a row saved as
    /// Cancelled: that only ever happens to one that was approved. A null status is a row that never went
    /// through workflow and is effective at once.
    /// </para>
    /// </summary>
    private static bool IsNotYetInEffect(object entity)
    {
        return entity is IWorkflowEnabledEntity
        {
            WorkflowStatus: RequestStatus.Draft or RequestStatus.Open or RequestStatus.Rejected
        };
    }
}

/// <summary>
/// Forgets a company's cached payroll-sensitive list when its System Objects change, so ticking or unticking
/// "payroll sensitive" takes effect on the next save rather than when the cache happens to expire.
/// </summary>
public class PayrollSensitiveCacheInvalidator : IEventHandler<EntityChangedEventData<SysObject>>
{
    private readonly ICacheManager _cacheManager;

    public PayrollSensitiveCacheInvalidator(ICacheManager cacheManager)
    {
        _cacheManager = cacheManager;
    }

    public void HandleEvent(EntityChangedEventData<SysObject> eventData)
    {
        _cacheManager
            .GetCache<int, PayrollSensitiveTypes.SensitiveTypeNames>(PayrollSensitiveTypes.CacheName)
            .Remove(eventData.Entity.TenantId);
    }
}
