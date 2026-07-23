using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Date-bound Employee-Manager assignment (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeManager</c>). The legacy
/// <c>EmployeeAction</c> FK is omitted (Wave B, not ported yet). The EndDate auto-chaining
/// (<c>SetEndDate</c>) and no-duplicate-StartDate validation are ported in
/// <c>EmployeeManagersAppService</c>.
/// </summary>
public class EmployeeManager : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int ManagerId { get; set; }

    public virtual Employee Manager { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
