using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Calculated payroll cost-distribution header row (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.CostRegister</c>). System-generated payroll-calculation
/// output — no business validation in the legacy source (plain CRUD). Its child is
/// <see cref="CostRegisterDetail"/>, linked back via <see cref="CostRegisterDetail.CostRegisterId"/>.
/// </summary>
public class CostRegister : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime Period { get; set; }

    public virtual PayrollRunType PayrollRunType { get; set; }
}
