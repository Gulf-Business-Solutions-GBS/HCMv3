using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.ProjectManagement;

/// <summary>
/// Header row for a per-employee-per-period project register (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ProjectManagement.ProjectRegister</c>). Legacy <c>PerformValidation</c>
/// was an empty no-op, so no business validation is ported here or in <c>ProjectRegistersAppService</c>
/// beyond plain CRUD. Its aggregated child is <see cref="ProjectRegisterDetail"/>, linked back via
/// <see cref="ProjectRegisterDetail.ProjectRegisterId"/>.
/// </summary>
public class ProjectRegister : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime Period { get; set; }
}
