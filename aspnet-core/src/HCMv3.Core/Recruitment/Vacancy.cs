using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;

namespace HCMv3.Recruitment;

/// <summary>
/// Job vacancy/requisition (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.Vacancy</c>). Legacy <c>PerformValidation</c> parity (auto
/// Code, duplicate Code check, required fields) is enforced in <c>VacanciesAppService</c>, not here.
/// Legacy <c>DocAttachment</c> is dropped (same simplification used throughout this port -- no HCMv3
/// equivalent). Legacy <c>Position</c> FK (<c>HCM10.Module.BusinessObjects.ManpowerPlanningManagement
/// .Position</c>) is dropped -- ManpowerPlanningManagement has not been ported to HCMv3 yet; a follow-up
/// dispatch can re-add it once that module exists. Legacy computed <c>ActualDaysToFill</c> and
/// <c>CodeWithTitle</c> are not persisted -- computed in the DTO mapping instead. Legacy
/// <c>OrganizationUnit</c> FK targets ABP's built-in <c>Abp.Organizations.OrganizationUnit</c> (a
/// <c>long</c> id), same pattern as <c>OrgUnitProfile.OrganizationUnitId</c>.
/// </summary>
public class Vacancy : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    public virtual DateTime RequestionDate { get; set; }

    public virtual int? JobTitleId { get; set; }
    public virtual JobTitle JobTitle { get; set; }

    public virtual DateTime PublishOn { get; set; }

    public virtual DateTime ClosingDate { get; set; }

    public virtual int? RecruiterId { get; set; }

    /// <summary>
    /// Mirrors legacy <c>Vacancy.Template</c> -- an apparently-redundant second FK to <see cref="OnbGroup"/>
    /// alongside <see cref="OnbGroupId"/> (both present and both compiled in the legacy source; kept for
    /// parity rather than silently dropped).
    /// </summary>
    public virtual int? TemplateOnbGroupId { get; set; }

    public virtual long? OrganizationUnitId { get; set; }

    public virtual bool IsBudgeted { get; set; }

    public virtual VacancyStatus VacancyStatus { get; set; }

    public virtual int SLADaysToFill { get; set; }

    public virtual string JobDescription { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string VacancyTitle { get; set; }

    public virtual int? WorkLocationId { get; set; }
    public virtual WorkLocation Location { get; set; }

    public virtual int TotalVacancy { get; set; }

    [StringLength(100)]
    public virtual string MonthlySalaryRange { get; set; }

    public virtual VacancyEmploymentType EmploymentType { get; set; }

    public virtual int YearsOfExperience { get; set; }

    public virtual int? QualificationId { get; set; }
    public virtual Qualification Qualification { get; set; }

    public virtual int? OnbGroupId { get; set; }
    public virtual OnbGroup OnbGroup { get; set; }
}
