using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee grievance record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeGrievance</c>, Wave 4 deferred bucket).
///
/// <see cref="GrievanceTypeId"/> FKs <c>HCMv3.GeneralMasterInfo.GrievanceType</c> (already ported).
/// </summary>
public class EmployeeGrievance : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int GrievanceTypeId { get; set; }

    public virtual GrievanceType GrievanceType { get; set; }

    public virtual DateTime GrievanceDate { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
