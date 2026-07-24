using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee basic pay header record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeBasicPay</c>, Wave 2 deferred bucket).
///
/// DELIBERATE SIMPLIFICATIONS / DROPPED FIELDS:
/// <list type="bullet">
/// <item><description>The legacy auto-EndDate cascade (each new/edited/deleted BasicPay record recomputes the
/// <c>EndDate</c> of sibling records for the same Employee so periods never overlap) is NOT ported -- only
/// the essential no-duplicate-StartDate-per-employee validation is (see
/// <c>EmployeeBasicPaysAppService</c>, same cross-record-cascade omission approach as Wave 1's
/// <c>EmployeeAbsAdjustment</c>).</description></item>
/// <item><description><c>Locked</c>/<c>SysXxx</c> audit fields are covered by <c>FullAuditedEntity</c> and
/// not re-ported.</description></item>
/// </list>
/// </summary>
public class EmployeeBasicPay : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    /// <summary>Legacy display name "Effective Date".</summary>
    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    /// <summary>Legacy display name "Total Amount". Sum of <see cref="EmployeeBasicPayDetail.Amount"/> rows.</summary>
    public virtual double Amount { get; set; }

    public virtual PayType PayType { get; set; }

    public virtual PayInterval PayInterval { get; set; }

    public virtual int? EmployeeActionId { get; set; }

    public virtual EmployeeAction EmployeeAction { get; set; }
}
