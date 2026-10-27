using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.OrganizationManagement;

namespace HCMv3.TimeManagement;

/// <summary>
/// Date-effective assignment of a <see cref="DutyPeriod"/> to a <see cref="WorkLocation"/> (ported from
/// legacy XAF <c>HCM10.Module.BusinessObjects.TimeManagement.WorkLocationDutyPeriod</c>, Wave 3).
///
/// Same date-effective/EndDate-auto-chaining shape as <see cref="EmployeeDutyPeriod"/>, but keyed by
/// WorkLocation instead of Employee. The EndDate auto-chaining (legacy <c>SetEndDate</c>) and
/// no-duplicate-StartDate-per-WorkLocation validation are ported in
/// <c>WorkLocationDutyPeriodsAppService</c>. The legacy <c>IExcelImport</c> marker is an XAF UI concept
/// and is not ported.
/// </summary>
public class WorkLocationDutyPeriod : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int WorkLocationId { get; set; }

    public virtual WorkLocation WorkLocation { get; set; }

    public virtual int DutyPeriodId { get; set; }

    public virtual DutyPeriod DutyPeriod { get; set; }

    /// <summary>
    /// "Effective Date" in the legacy UI.
    /// </summary>
    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
