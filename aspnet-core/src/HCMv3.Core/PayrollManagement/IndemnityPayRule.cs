using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// IndemnityPayRule (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.IndemnityPayRule</c>). Keyed by
/// <see cref="PersonnelAdministration.SeparationType"/> (reused from PersonnelAdministration Wave B) rather
/// than a duplicate local enum. <see cref="MaxUnit"/> reuses the existing <see cref="Unit"/> enum
/// (Day/Month), already ported for <c>EmployeeGroup.IndemnityMaxUnit</c>. Its child rows
/// (<see cref="IndemnityPayRuleDetail"/>) are listed/edited separately, same pattern as the
/// EmployeeSubGroup* children in prior waves.
/// </summary>
public class IndemnityPayRule : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual PersonnelAdministration.SeparationType SeparationType { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual double MaxNumber { get; set; }

    public virtual Unit MaxUnit { get; set; }

    public virtual double MaxPayPercentage { get; set; }

    public virtual int? CountryId { get; set; }

    public virtual Country Country { get; set; }
}
