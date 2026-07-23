using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Date-bound Employee-DottedLineManager assignment (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeDottedLineManager</c>). Unlike
/// <see cref="EmployeeManager"/>, there is no EndDate auto-chaining in the legacy source -- only an
/// overlap/duplicate check (ported in <c>EmployeeDottedLineManagersAppService</c>).
/// </summary>
public class EmployeeDottedLineManager : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int ManagerId { get; set; }

    public virtual Employee Manager { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
