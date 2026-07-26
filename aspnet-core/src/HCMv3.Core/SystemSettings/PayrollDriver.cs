using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.SystemSettings;

/// <summary>
/// Per-country payroll driver configuration (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.PayrollDriver</c>). Legacy <c>Country</c> navigation property
/// is ported here as a plain FK id (<see cref="CountryId"/>), following this codebase's flat-FK convention
/// (see <c>PersonnelAdministration.Employee.NationalityId</c>). Legacy <c>PerformValidation</c> real business
/// rules -- Country is required, and at most one <see cref="PayrollDriver"/> row per Country -- are ported to
/// <c>PayrollDriverAppService</c> (not here).
/// </summary>
public class PayrollDriver : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int CountryId { get; set; }

    public virtual bool LeaveDeductionStartMonth { get; set; }
}
