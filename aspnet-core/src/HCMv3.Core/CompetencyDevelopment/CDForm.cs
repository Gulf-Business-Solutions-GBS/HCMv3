using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.CompetencyDevelopment;

/// <summary>
/// An employee's competency-development review form for a <see cref="CDPeriod"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.CompetencyDevelopment.CDForm</c>). Its 3 aggregated child collections
/// (<c>CDPlan</c>, <c>MidYearReview</c>, <c>AnnualYearReview</c>) are ported as flat satellite entities
/// (<see cref="CDPlan"/>, <see cref="CDMidYearReview"/>, <see cref="CDAnnualYearReview"/>) each carrying its
/// own <c>CDFormId</c> FK, same flat parent+child pattern used throughout this port.
/// </summary>
public class CDForm : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int CDPeriodId { get; set; }

    public virtual CDPeriod CDPeriod { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual double OverallScore { get; set; }

    public virtual double ModeratedScore { get; set; }

    public virtual bool EmployeeSignedOff { get; set; }

    public virtual bool ManagerSignedOff { get; set; }
}
