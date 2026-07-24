using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee-Sponsor assignment record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeSponsor</c>, Wave 3 deferred bucket).
///
/// <see cref="SponsorId"/>/<see cref="GovtJobTitleId"/> FK <c>HCMv3.GovtRelationsManagement.Sponsor</c>/
/// <c>GovtJobTitle</c> (already ported). <see cref="WorkVisaTypeId"/> FKs
/// <c>HCMv3.GeneralMasterInfo.WorkVisaType</c> (already ported).
///
/// CROSS-MODULE QUOTA ENFORCEMENT (genuinely new for this wave, unlike Wave 1/2's simple ledger cascades):
/// the legacy entity's <c>UpdateQuotaBalance</c>/<c>OnDeleting</c> increment/decrement
/// <c>SponsorJobTitle.CitizenQuotaUsed</c>/<c>NonCitizenQuotaUsed</c> (based on whether the assigned
/// employee's Nationality's NationalityGroup is "Citizen" or not, and only when the assignment's
/// StartDate/EndDate window covers "today") when a Sponsor+GovtJobTitle assignment is created/deleted --
/// see <c>EmployeeSponsorsAppService.CreateAsync</c>/<c>Delete</c>, which apply the same +1/-1 to the
/// matching <c>SponsorJobTitle</c> row's Used counter, floored at 0 (the legacy has no floor, but this port
/// adds one defensively per instruction). This is NOT ported on the entity itself, consistent with every
/// other cross-entity cascade in Waves 1-3.
/// </summary>
public class EmployeeSponsor : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int SponsorId { get; set; }

    public virtual Sponsor Sponsor { get; set; }

    public virtual int GovtJobTitleId { get; set; }

    public virtual GovtJobTitle GovtJobTitle { get; set; }

    /// <summary>Legacy display name "Employee-Sponsor Relation".</summary>
    [StringLength(256)]
    public virtual string EmpSponsorRelation { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual double ContractSalary { get; set; }

    public virtual int SalaryCurrencyId { get; set; }

    public virtual Currency SalaryCurrency { get; set; }

    public virtual ResidencyType ResidencyType { get; set; }

    public virtual int WorkVisaTypeId { get; set; }

    public virtual WorkVisaType WorkVisaType { get; set; }
}
