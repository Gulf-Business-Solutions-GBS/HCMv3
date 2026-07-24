using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee overtime request record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeOvertime</c>, Wave 4 deferred bucket).
///
/// <see cref="OvertimeTypeId"/> FKs <c>HCMv3.GeneralMasterInfo.OvertimeType</c> (already ported).
///
/// DELIBERATE SIMPLIFICATION: the legacy <c>ExpectedOvertimeAmount</c>/<c>GetExpectedOvertimeAmount</c>
/// computed-amount logic depends on <c>EmployeeBasicPayDetail</c>/<c>TimeDataService</c>/payroll-settings
/// wage-rate calculation -- NOT ported on the entity itself (equivalent read-time computation belongs in
/// <c>EmployeeOvertimesAppService</c> if/when needed, not stored here).
/// </summary>
public class EmployeeOvertime : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime EffectiveDate { get; set; }

    public virtual int OvertimeTypeId { get; set; }

    public virtual OvertimeType OvertimeType { get; set; }

    /// <summary>Requested OT hours by employee.</summary>
    public virtual double OvertimeHours { get; set; }

    /// <summary>Populated from the (unported) Time Engine in legacy; kept as a plain stored field.</summary>
    public virtual double ActualOvertimeHours { get; set; }

    /// <summary>Approved OT hours by Manager.</summary>
    public virtual double ApprovedOvertimeHours { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
