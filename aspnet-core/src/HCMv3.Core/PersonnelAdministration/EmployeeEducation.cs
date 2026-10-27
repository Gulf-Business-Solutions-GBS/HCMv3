using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee education record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeEducation</c>). The legacy file has
/// EndDate auto-chaining copy-pasted onto this entity even though education records are not really
/// "current period" data (multiple concurrent entries make sense) -- per Wave A scope, that chaining and
/// any overlap validation are NOT ported; this is plain CRUD with blank-field validation only. The
/// <c>DocAttachment</c> child is skipped entirely (see Wave A deferral notes).
/// </summary>
public class EmployeeEducation : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual int EducationId { get; set; }

    public virtual Education Education { get; set; }

    public virtual int EducationBranchId { get; set; }

    public virtual EducationBranch EducationBranch { get; set; }

    public virtual int? UniversityId { get; set; }

    public virtual University University { get; set; }

    public virtual int? InstitutionId { get; set; }

    public virtual Institution Institution { get; set; }

    public virtual int? CountryId { get; set; }

    public virtual Country Country { get; set; }

    [StringLength(256)]
    public virtual string Location { get; set; }

    public virtual double DurationOfCourse { get; set; }

    [StringLength(64)]
    public virtual string Grade { get; set; }

    [StringLength(16)]
    public virtual string YearOfPassing { get; set; }
}
