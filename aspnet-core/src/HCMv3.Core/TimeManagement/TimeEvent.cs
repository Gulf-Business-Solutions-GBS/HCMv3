using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;

namespace HCMv3.TimeManagement;

/// <summary>
/// Raw punch/clock event (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TimeManagement.TimeEvent</c>,
/// Wave 4).
///
/// DEFERRED / NOT PORTED:
///  - The legacy <c>Photo</c>/<c>SampleImage2</c> computed properties that read a selfie image off disk via
///    <c>HttpContext.Current.Server.MapPath</c> are classic ASP.NET (System.Web) file-system access that has
///    no direct translation in ASP.NET Core. Only the raw <see cref="GeoSelfiePath"/> string is ported;
///    resolving/serving the actual image is left for a follow-up (needs a redesigned file-storage approach,
///    e.g. an <c>IBinaryObjectManager</c>/blob-storage-backed endpoint).
///  - The <c>AllowDuplicateInTimeEvent</c>-setting-driven branch of the legacy duplicate check (which relaxes
///    the duplicate check to include TransSource when the setting is on) is NOT ported -- no global-settings
///    infrastructure exists in this port. Only the "no duplicate (Employee, TransType, TransDate, TransTime)"
///    check is ported (see <c>TimeEventsAppService</c>).
///  - The legacy <c>OnDeleting</c> cascade that deletes derived single-day <c>EmployeeAttendance</c> rows and
///    triggers <c>SetRetroDate</c> retro-calculation is NOT ported -- <c>EmployeeAttendance</c> and the
///    retro-calc infrastructure do not exist anywhere in this port. This is a deliberate, significant
///    simplification: deleting a <see cref="TimeEvent"/> here has no cascading side effects.
///  - The legacy <c>prevEmployee</c>/<c>prevTransDate</c> "previous value" tracking fields exist only to
///    support the dropped <c>OnDeleting</c> cascade above and are therefore also not ported.
///  - The legacy <c>Locked</c>/<c>SysCreator</c>/<c>SysCreatedOn</c>/<c>SysModifier</c>/<c>SysModifiedOn</c>/
///    <c>SysTenant</c> "Sys fields" are superseded by <see cref="FullAuditedEntity"/> + <see cref="TenantId"/>.
///    The legacy <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
///
/// <para>
/// <b>Workflow.</b> A punch is also the transaction behind a missing-punch request (legacy
/// <c>SkipPunchRequest</c>): the employee asks for a clock event that was never recorded, and if it is
/// granted the row here IS that punch -- there is no second request table shadowing this one. Hence
/// <see cref="IWorkflowEnabledEntity"/>. Until the request is granted the row's
/// <see cref="WorkflowStatus"/> keeps it behind the <c>ApprovedOnly</c> filter and therefore out of the
/// Time Engine's punch load, so a punch nobody has agreed to cannot quietly recompute an attendance day.
/// See <c>TimeEventWorkflowTransactionHandler</c>.
/// </para>
/// </summary>
public class TimeEvent : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    [StringLength(50)]
    public virtual string PIN { get; set; }

    public virtual int? ActualTransTypeId { get; set; }

    public virtual TransType ActualTransType { get; set; }

    public virtual int TransTypeId { get; set; }

    public virtual TransType TransType { get; set; }

    public virtual DateTime TransDate { get; set; }

    public virtual DateTime TransTime { get; set; }

    public virtual int? AccessUnitId { get; set; }

    public virtual AccessUnit AccessUnit { get; set; }

    [StringLength(50)]
    public virtual string IPAddress { get; set; }

    public virtual TransSource TransSource { get; set; }

    /// <summary>
    /// External device/import batch record identifier. Required + unique (among Machine-sourced rows) when
    /// <see cref="TransSource"/> == <see cref="TimeManagement.TransSource.Machine"/> -- enforced in
    /// <c>TimeEventsAppService</c>, not here.
    /// </summary>
    public virtual long RecordSource { get; set; }

    /// <summary>
    /// Geo-location fields (added 2020-02-30 / 2020-06-23 in the legacy source).
    /// </summary>
    [StringLength(1000)]
    public virtual string DeviceAgentString { get; set; }

    public virtual double Latitude { get; set; }

    public virtual double Longitude { get; set; }

    public virtual double RangeDifference { get; set; }

    public virtual bool PunchInRange { get; set; }

    /// <summary>
    /// Employee body temperature captured at punch time (added 2020-06-23 in the legacy source).
    /// </summary>
    public virtual double Temperature { get; set; }

    public virtual int? WorkLocationId { get; set; }

    public virtual WorkLocation WorkLocation { get; set; }

    /// <summary>
    /// Path to the geo-selfie image captured at punch time. See class remarks: resolving/serving the actual
    /// image (legacy <c>Photo</c>/<c>SampleImage2</c>) is deferred, not ported.
    /// </summary>
    [StringLength(1000)]
    public virtual string GeoSelfiePath { get; set; }

    /// <summary>The approval request that governs this punch, when it was asked for through self service.</summary>
    public virtual int? WorkflowRequestId { get; set; }

    /// <summary>Null for an ordinary punch -- one that came off a device, an import or an HR screen -- which
    /// is every row that exists today, and which therefore keeps behaving exactly as it does now. Read by the
    /// ApprovedOnly filter, which is what keeps a requested-but-not-yet-granted punch out of the Time
    /// Engine.</summary>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
