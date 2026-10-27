using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee-bound date entry (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeDate</c>). Plain CRUD, no
/// StartDate/EndDate chaining in the legacy source.
/// </summary>
public class EmployeeDate : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [Required]
    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int? DateTypeId { get; set; }

    public virtual DateType DateType { get; set; }

    public virtual DateTime DateData { get; set; }

    public virtual double DayData { get; set; }
}
