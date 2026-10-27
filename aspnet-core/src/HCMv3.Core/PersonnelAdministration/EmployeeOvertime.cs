using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee overtime request record (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeOvertime</c>, Wave 4 deferred bucket).
///
/// <see cref="OvertimeTypeId"/> FKs <c>HCMv3.GeneralMasterInfo.OvertimeType</c> (already ported).
///
/// The legacy non-persistent <c>ExpectedOvertimeAmount</c>/<c>GetExpectedOvertimeAmount</c> is not a
/// property here: it is calculated on read by
/// <see cref="HCMv3.PersonnelAdministration.Services.IExpectedOvertimeAmountService"/> and never stored.
/// </summary>
public class EmployeeOvertime : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
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

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowRequestId"/>
    public virtual int? WorkflowRequestId { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowStatus"/>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
