using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee prior-employment work experience record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeWorkExperience</c>). Note:
/// <see cref="Position"/> here is free text (job title at the previous employer), NOT a FK to the
/// (unported, Wave B/ManpowerPlanning) Position entity.
/// </summary>
public class EmployeeWorkExperience : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    [Required]
    [StringLength(256)]
    public virtual string Company { get; set; }

    [StringLength(256)]
    public virtual string Position { get; set; }

    [StringLength(256)]
    public virtual string Location { get; set; }

    [StringLength(1000)]
    public virtual string ReasonForLeaving { get; set; }
}
