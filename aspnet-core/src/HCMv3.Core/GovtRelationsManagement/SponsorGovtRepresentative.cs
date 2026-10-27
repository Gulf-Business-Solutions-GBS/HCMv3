using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Effective-dated government representative (Employee) assignment for a <see cref="Sponsor"/> (ported from
/// legacy XAF <c>HCM10.Module.BusinessObjects.GovtRelationsManagement.SponsorGovtRepresentative</c>, one of
/// <see cref="Sponsor"/>'s aggregated child collections -- see <see cref="Sponsor"/> remarks). Legacy
/// <c>PerformValidation</c> parity (no overlapping date ranges among siblings for the same Sponsor) is
/// ported in <c>SponsorGovtRepresentativesAppService</c>.
/// </summary>
public class SponsorGovtRepresentative : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int SponsorId { get; set; }

    public virtual Sponsor Sponsor { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
