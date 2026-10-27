using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee gratuity/indemnity payment record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeGratuity</c>, Wave 2 deferred bucket).
/// Legacy display name for the feature is "Indemnity".
///
/// DELIBERATE SIMPLIFICATIONS / DROPPED FIELDS:
/// <list type="bullet">
/// <item><description>The legacy <c>prevXxx</c> change-tracking fields (used only to cascade quota-balance
/// updates against the PREVIOUS employee/date/days when a record is edited) are <c>[NonPersistent]</c> in the
/// legacy source and not ported as entity columns. The quota-balance cascade against
/// <see cref="EmployeeGratuityQuota"/> itself is NOT ported either (same cross-entity-cascade omission
/// approach as Wave 1's <c>EmployeeAbsAdjustment</c>) -- see <c>EmployeeGratuitiesAppService</c>.</description></item>
/// </list>
/// </summary>
public class EmployeeGratuity : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    /// <summary>Legacy display name "Indemnity Paid Date".</summary>
    public virtual DateTime GratuityPaidDate { get; set; }

    /// <summary>Legacy display name "Indemnity Paid Days".</summary>
    public virtual double GratuityPaidDays { get; set; }

    /// <summary>Legacy display name "Reset Indemnity Calculation".</summary>
    public virtual bool ResetGratuityCalc { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
