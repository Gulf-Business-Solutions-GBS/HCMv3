using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee certification/course record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeCertification</c>). Legacy has an
/// overlap-validation block but it is commented out (dead code) -- only blank-field validation is
/// ported (see <c>EmployeeCertificationsAppService</c>).
/// </summary>
public class EmployeeCertification : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string Course { get; set; }

    public virtual int InstitutionId { get; set; }

    public virtual Institution Institution { get; set; }

    [StringLength(64)]
    public virtual string Duration { get; set; }

    [StringLength(256)]
    public virtual string Certificate { get; set; }

    [StringLength(64)]
    public virtual string Grade { get; set; }

    /// <summary>
    /// Legacy <c>CertificationType</c>. Existing rows read as <c>Professional</c>
    /// (0), the legacy default. Only <c>Training</c> rows can be linked
    /// from an <c>EmployeeTraining</c>.
    /// </summary>
    public virtual CertificationType CertificationType { get; set; }
}
