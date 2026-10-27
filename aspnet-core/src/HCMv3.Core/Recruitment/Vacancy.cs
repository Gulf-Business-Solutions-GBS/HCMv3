using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.ManpowerPlanningManagement;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Recruitment;

/// <summary>
/// Job vacancy/requisition (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Recruitment.Vacancy</c>). Legacy <c>PerformValidation</c> parity (auto
/// Code, duplicate Code check, required fields) is enforced in <c>VacanciesAppService</c>, not here.
/// Legacy <c>DocAttachment</c> is replaced by the shared per-tenant attachment store (owner type
/// <c>"Vacancy"</c>, the <c>&lt;app-attachments&gt;</c> panel on the vacancy form). Legacy <c>Position</c> FK
/// is <see cref="PositionId"/> (ManpowerPlanningManagement <see cref="ManpowerPlanningManagement.Position"/>).
/// Legacy computed <c>ActualDaysToFill</c> and <c>CodeWithTitle</c> are not persisted -- computed in
/// <c>VacanciesAppService</c> for the list DTO instead. Legacy
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

    /// <summary>Legacy <c>Vacancy.Recruiter</c> (an <see cref="Employee"/>).</summary>
    public virtual int? RecruiterId { get; set; }
    public virtual Employee Recruiter { get; set; }

    /// <summary>Legacy <c>Vacancy.Position</c>.</summary>
    public virtual int? PositionId { get; set; }
    public virtual Position Position { get; set; }

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
