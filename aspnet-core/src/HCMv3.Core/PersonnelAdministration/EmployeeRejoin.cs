using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee rejoin record, ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeRejoin</c>.
///
/// <para>Saving a rejoin is no longer record-keeping only: <see cref="Services.IEmployeeRejoinService"/> runs
/// legacy's <c>PerformValidations</c> / <c>ValidatePreviousEntry</c>, <c>UpdatePreviousValues</c> and
/// <c>SaveEmployeeInformation</c> for it (design note <c>docs/design/legacy-gap-batch8-round-b.md</c>,
/// section 2). Legacy's <c>StartDate</c> is <see cref="RejoinDate"/> here, and <c>RejoinWithPrevXxx</c> is
/// <c>CarryForwardXxxBalance</c>.</para>
///
/// <list type="bullet">
/// <item><description><see cref="CreateNewEmployeeCode"/> off: the same employee is made Active again, and every
/// balance NOT carried forward is zeroed by a record dated the day before the rejoin.</description></item>
/// <item><description><see cref="CreateNewEmployeeCode"/> on: a new employee is created from the old one (legacy's
/// field list), linked by <see cref="Employee.PreviousEmpCode"/>, and every balance that IS carried forward is
/// opened on the new employee. The old employee is left as it was.</description></item>
/// </list>
///
/// <para>Rows saved before the service existed had no side effects, and they are not re-run.</para>
/// </summary>
public class EmployeeRejoin : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime RejoinDate { get; set; }

    [StringLength(2000)]
    public virtual string Comments { get; set; }

    public virtual bool CarryForwardLeaveBalance { get; set; }

    public virtual bool CarryForwardTicketBalance { get; set; }

    public virtual bool CarryForwardIndemnityBalance { get; set; }

    public virtual bool CarryForwardLoanBalance { get; set; }

    /// <summary>
    /// Legacy <c>CreateNewEmployeeCode</c>: the employee rejoins as a NEW employee with a new code instead of the
    /// old record being reactivated. It also decides whether the rejoin restarts service on the old record for
    /// Full &amp; Final: only rejoins that kept the code do.
    /// </summary>
    public virtual bool CreateNewEmployeeCode { get; set; }
}
